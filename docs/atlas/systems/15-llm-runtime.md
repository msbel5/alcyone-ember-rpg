# 15-llm-runtime

## HLD - Ne ve Neden

LLM calisma zamani, deterministik cekirdegin URETTIGI sonucun uzerine yalnizca *susleme* katan
yerel-oncelikli bir metin uretim hattidir: NPC selamlamalari, topic cevaplari, serbest-metin
sorulari ve DM kahinin (Oracle) kehanetleri, once HER ZAMAN deterministik bir satirla doldurulur;
LLM cevabi ancak temiz ve bos-olmayan cikarsa o satirin yerine gecer (DomainSimulationAdapter.Dialog.Topics.cs:62-67,
DomainSimulationAdapter.Fate.cs:94-96). Motor, LLamaSharp `StatelessExecutor` uzerinden yerel bir
GGUF Qwen2.5-1.5B modelidir (NativeLlmClient.cs:26,317); her istek kendi ChatML prompt'unu sifirdan
kurar — oturum durumu modelde degil, oyun dunyasinin `NpcMemory` kayitlarindadir (stateless felsefe;
"GemRB iskeleti + LLM icerigi", Docs/SYSTEMS_ATLAS.md §4). Oyuncuya gorunen etki: "X thinks…"
yer tutucusu, token'lar aktikca canli buyuyen cumleye doner (M3a streaming), final cevap ekrana
oturur ve cevabin icinden en fazla 3 yeni takip-sorusu balonu filizlenir (DomainSimulationAdapter.Dialog.Source.cs:33-38,
DialogStreamText.cs:20-39). Determinizm iki kilitle korunur: (1) tum dunya/diyalog yazimlari
worker thread'den degil, tick basinda drenajlanan `_mainThreadApply` kuyrugundan uygulanir (DET-02,
DomainSimulationAdapter.Clock.cs:8,20-30); (2) sampling seed'i aktor id + soru-tekrari sayacindan
turetilir, ayni durum ayni cikti demektir (DomainSimulationAdapter.Dialog.Topics.cs:40, NativeLlmClient.cs:175).
Zayif modelin celisme bicimleri (turn-marker sizintisi, talimat papaganligi, marka sizintisi) uc ayri
katmanda metin filtresiyle kesilir — bu sistem "vitrin" degil, W28/BUG-DIALOG-TURNLEAK gibi yasanmis
kazalarin uzerine insa edilmis bir savunma hattidir.

## HLD - Akis

Kadans: bu sistem tick-kadansli DEGIL, UI-olay gudumludur (topic tiklamasi, serbest metin, Oracle
"Ask"). Tek kadans bagi: sonuclarin dunyaya yazilmasi `AdvanceTick` basindaki `DrainMainThreadApply`
ile deterministik ana-thread'de olur (DomainSimulationAdapter.Clock.cs:8).

**A) NPC diyalog akisi (selamlama / topic / serbest metin):**

1. Oyuncu konusmayi acar veya topic secer → `SelectTopic` / `AskFreeText` deterministik satiri
   HEMEN yazar (`topic.Answer` ya da "X considers …", DomainSimulationAdapter.Dialog.Source.cs:319-323,375-379),
   `ActorTalked` olayini append eder (332-337) ve konusma NPC hafizasina yazilir (`RecordConversationMemory`, 103-112).
2. Aktorun `NpcSeedRecord`'u varsa id-tabanli, yoksa isim-tabanli LLM yolu secilir
   (DomainSimulationAdapter.Dialog.Source.cs:349-357). `GenerateNpcTopicAnswerAsync` calisir
   (DomainSimulationAdapter.Dialog.Topics.cs:21): `_isDialogThinking=true`, `_streamingPartialLine=""`,
   konusma-serisi (`gen`) ve istek-serisi (`req`) yakalanir (26-29).
3. `LlmRequest` kurulur: system prompt (rol + dunya stili + companion/tanidik/oyuncu-adi/tekrar
   ekleri + `FollowupsInstruction`), `RecentTurns` = `NpcMemoryLlmEnvelope.RecallLines(...,8)`
   (Topics.cs:34-42; hafiza kaynagi Source.cs:90-100).
4. `Task.Run(() => CompleteLlmOrEmpty(router, request, onPartial))` — bloklayan cagri worker
   thread'de (Topics.cs:53; DomainSimulationAdapter.Dialog.Text.cs:258-272).
5. Router: `LlmRoutingService.Complete(request, onPartial, out chosen)` once `LocalStreaming`
   delegesini dener; bos/hatali cikarsa duz zincire (local → cloud → mock-bos) duser
   (LlmRoutingService.cs:44-59,61-91). Uretimde `LocalStreaming = _nativeLlm.Complete(req, onPartial)`,
   cloud = null (ForgeBootstrap.cs:55-61).
6. `NativeLlmClient.CompleteAsync`: model lazy-load (`LLamaWeights` + `StatelessExecutor`,
   NativeLlmClient.cs:303-321), `BuildPrompt` ChatML kurar (system 2400 + son 4 tur x 900 char,
   toplam 6000 char clamp; 225-240,343-355), `InferAsync` token dongusu: her token'da BIRIKMIS
   metin `onPartial`'a itilir (worker thread; 189-197), 60sn timeout + `_inferenceLock` ile tekli
   sira (184-203), cikis `StripTrailingTurnMarkers` ile kaynakta temizlenir (210).
7. Streaming apply: her partial `_mainThreadApply`'a enqueue edilir; drain aninda `gen`/`req`
   seri kontrolu gecerse `TrimStreamPartial` (mid-stream "User:"/"Memory" kesigi, Source.cs:231-238)
   uygulanip `_streamingPartialLine`'a yazilir (Topics.cs:48-52). `GetCurrentLine` thinking modunda
   `partial + " …"` dondurur (Source.cs:33-38).
8. Final apply (yine kuyruk uzerinden): seri kontrolleri → `SanitizeNpcLine` (turn-marker +
   provider-hata metni filtresi, Text.cs:225-255) → `SplitFollowups` (govde/sorular ayrimi +
   papagan reddi, DialogStreamText.cs:20-51). Bos olmayan govde `_currentDialogLine` olur,
   `RecordNpcSaid` NPC'nin kendi sozunu hafizaya yazar (Source.cs:180-188), `AbsorbFollowups`
   yeni balonlari buyutur (cap 6; Source.cs:166-173), `_isDialogThinking=false` (Topics.cs:54-70).
   Secilen balon `ConsumeOption` ile tuketilmisti (Source.cs:305); "Any news?" balonu hic
   tuketilmez ve RumorMill'den deterministik-aninda cevaplanir, LLM'e gitmez (Source.cs:296-304).

**B) DM Oracle (ConsultFate) akisi:**

1. UI "consul" ekranini acar (`ConsulFateView`, InGameUiController.cs:412) ve `AskOracle` her
   soruda `oracle.ConsultFate(question)` cagirir → aninda "The oracle consults the fates..."
   yer tutucusu doner, `_oraclePending=true` (InGameUiController.cs:527-540,
   DomainSimulationAdapter.Fate.cs:30-39).
2. `ConsultFateAsync`: SONUC deterministiktir — tick-tuzlu d100 → `ConsultFateOutcomeBucket`
   (Fate.cs:56-58); LLM'e yalnizca "bu sonucu susle" gorevi verilir. `LlmRequest` "consult_fate"
   system-prompt-id'si, 150 token, seed=tick, `consult_fate` ToolDescriptor'u ile kurulur (60-81).
3. Cagri non-streaming `Task.Run(CompleteLlmOrEmpty)` (87); apply yine `_mainThreadApply`
   uzerinden: `SanitizeNpcLine` temiz cikti verirse kehanet o olur, yoksa
   "THE FATES DECREE: …" deterministik satiri (94-96).
4. DET-03: modelin `ProposedToolCalls`'u GERCEK kapidan gecer — `LlmProposalValidator` +
   tek-girisli `ToolRegistry` + `ToolCallRouter`; kabul edilenler invoke edilir, reddedilenler
   loglanir, tracer kayitlari `_world.ToolCallTrace`'e eklenir (Fate.cs:105-123).
5. `_fateReady=true`; `InGameUiController.Update` her kare `TryConsumeResolvedFate()`'i yoklar ve
   kehaneti acik Oracle ekranina akitir (Fate.cs:41-46,126; InGameUiController.cs:204-212).
   Not: gorev ipucundaki "OracleScreen" adinda bir sinif YOK — gercek sinif `ConsulFateView`'dur
   (ConsulFateView.cs:62-67).

**C) Saglayici zinciri ve bootstrap:**

1. `ForgeBootstrap.Awake`: `NativeLlmClient(modelRoot, fallback: null)` + `LlmRoutingService(local, cloud:null, cloudKind:Mock)`
   + `LocalStreaming` kancasi → `ForgeLocator.Register` (ForgeBootstrap.cs:48-63).
2. `ModelBootstrap` manifest/SHA dogrulamali indirme yapar ama LLM router'i ASLA yeniden
   baglamaz — yalniz asset-forge + embedding client rebind edilir (ModelBootstrap.cs:184-217;
   koruma gerekce yorumu ForgeLocator.cs:22-24).
3. Model dosyasi "gercek GGUF" kapisindan gecer: boyut >= 1MB + "GGUF" magic — Git-LFS pointer
   stub'i "model var" yalanina donusemez (LEFT-005, NativeLlmClient.cs:85-111).

## LLD - Veri Modeli

**`LlmRequest`** (Assets/Scripts/Domain/AiDm/LlmEnvelope.cs:11-59, immutable):
- `SystemPromptId: string` (bos olamaz), `ConversationId: string` (bos olamaz),
  `AvailableTools: IReadOnlyList<ToolDescriptor>`, `MaxTokens: int` (>0),
  `Seed: ulong`, `SystemPrompt: string`, `RecentTurns: IReadOnlyList<string>`.

**`LlmResponse`** (LlmEnvelope.cs:65-82, immutable):
- `Text: string` (null → ""), `ProposedToolCalls: IReadOnlyList<ToolCallRequest>`, `TokensUsed: int` (>=0).

**`LlmProviderKind`** (Assets/Scripts/Domain/AiDm/LlmProviderKind.cs:6-37, readonly struct):
- `_code: string`; sabitler: `LocalQwen("local_qwen")`, `CloudAnthropic`, `CloudOpenAi`, `Mock`
  (11-14); `Code`, `IsEmpty` (28-29).

**`NativeLlmClient`** (Assets/Scripts/Infrastructure/AiDm/NativeLlmClient.cs:24-356):
- Sabitler: `DefaultModelFileName = "qwen2.5-1.5b-instruct-q4_k_m.gguf"` (26), `DefaultDownloadUrl`
  HuggingFace (27), `NativeContextTokens=2048` (35), `NativeBatchTokens=512` (36),
  `MaxNativePromptChars=6000` (37), `MaxNativeGenerationTokens=192` (38), `MinUsableModelBytes=1_000_000` (90),
  `TurnMarkers = {"User:","Assistant:","System:","Memory:","<|im","\nUser","\nMemory"}` (118-119).
- Alanlar: `_modelPath`, `_fallback: LocalQwenClient`, `_downloadUrl`, `_inferenceLock: SemaphoreSlim(1,1)`,
  `_loadLock` (29-33); `#if USE_LLAMASHARP` altinda `_weights: LLamaWeights`, `_executor: StatelessExecutor`
  (40-43); `_isInitialised` (45).
- Cikarsama parametreleri: `MaxTokens=min(istek,192)`, `AntiPrompts={"User:","Memory"}`,
  `OverflowStrategy=TruncateAndReprefill`, `ContextTruncationPercentage=0.5`,
  `DefaultSamplingPipeline{Seed=(uint)request.Seed, Temperature=0.7}` (167-178).

**`LlmRoutingService`** (Assets/Scripts/Simulation/AiDm/LlmRoutingService.cs:15-122):
- `_local, _cloud: LlmDispatch`, `_cloudKind: LlmProviderKind` (17-19);
  `LocalStreaming: LlmStreamingDispatch` (yazilabilir kanca, 40).
- Delege tipleri: `LlmDispatch(LlmRequest): LlmResponse` (6), `LlmStreamingDispatch(LlmRequest, Action<string>): LlmResponse` (9).

**`DialogStreamText`** (Assets/Scripts/Simulation/AiDm/DialogStreamText.cs:11-67, saf/statik):
- `FollowupsInstruction: const string` — "End with one final line exactly like: FOLLOWUPS: …" (13-15).

**Adapter diyalog durumu** (Presentation, DomainSimulationAdapter partial'lari):
- `_currentDialogLine: string` (DomainSimulationAdapter.cs:38), `_pendingFate: string` (42),
  `_isFateThinking: bool` (43), `_isDialogThinking: bool` (44), `_streamingPartialLine: string` (50),
  `_fateReady: bool` (Fate.cs:26), `_conversationSerial: int` (Dialog.Binding.cs:172),
  `_dialogRequestSerial: int` (Dialog.Source.cs:85),
  `_liveOptions: Dictionary<string, List<string>>` — NPC-basi yasayan balon listesi, farewell'i
  atlatir (Source.cs:139-158), `_topicAskCounts` uzerinden tekrar sayaci (`NextAskCount`, 209-214),
  `_mainThreadApply: ConcurrentQueue<Action>` (Clock.cs:20-21).

**`FlavourBudget`** (Assets/Scripts/Simulation/AiDm/FlavourBudget.cs:10-47):
- `_spent, _capPerTick: int`; `TryReserve/ResetForTick/UpdateCap`. NOT: canli diyalog yolunda
  KULLANILMIYOR — yalniz `NpcFlavourService` (NarrationServices.cs:23-25) ve testler referans veriyor.

**Oracle tarafi:** `ToolDescriptor(ToolId("consult_fate"), ToolSurfaceKind.Dm, [query:string], ToolSideEffect.Read)`
(Fate.cs:60-69); `ConsultFateOutcomeBucket.D100/FromRoll` deterministik zar (Fate.cs:57-58; tanim
Domain/AiDm/ConsultFateOutcomeBucket.cs — ic alanlari bu taramada okunmadi, dogrulanmadi).

## LLD - Fonksiyon Haritasi

**Infrastructure (EmberCrpg.Infrastructure.AiDm):**
- `NativeLlmClient.CompleteAsync(LlmRequest, CancellationToken, Action<string> onPartial=null): Task<LlmResponse>`
  (NativeLlmClient.cs:145) — lazy-load + ChatML prompt + `InferAsync` token dongusu + 60sn timeout;
  hata/model-yok halinde fallback ya da bos cevap.
- `NativeLlmClient.Complete(LlmRequest[, Action<string>]): LlmResponse` (133,140) — `SyncTaskBridge`
  uzerinden bloklayan sarmalayici; streaming overload M3a icin.
- `NativeLlmClient.BuildPrompt(LlmRequest): string` (225) — `<|im_start|>` ChatML; system 2400 char,
  son 4 tur 900'er char, toplam 6000 char (`ClampSegment` 343, `ClampPrompt` 349 — kesigi tur
  sinirina hizalar).
- `NativeLlmClient.StripTrailingTurnMarkers(string): string` (static, 121) — AntiPrompt sonrasi sizan
  "User:"/"<|im"/"Memory:" kuyrugunu KAYNAKTA keser.
- `NativeLlmClient.IsUsableModelFile(string): bool` (static, 92) — boyut+magic GGUF kapisi (LEFT-005).
- `NativeLlmClient.EnsureModelReady(Action<float>): Task` (250) — yalniz `EMBER_ALLOW_MODEL_DOWNLOAD=1`
  ile indirir (E7-016, 244-248); aksi halde "hazir" der ve fallback'e birakir.
- `NativeLlmClient.LoadModelSync()` (303) — `ModelParams{ContextSize=2048, BatchSize=512, GpuLayerCount=-1}`
  → `LLamaWeights.LoadFromFile` → `new StatelessExecutor(weights, parameters)` (309-318).
- `SyncTaskBridge.Run<T>(Func<Task<T>>): T` (SyncTaskBridge.cs:8) — `Task.Run(...).GetAwaiter().GetResult()`.
- `LocalQwenClient.CompleteAsync / IsAvailableAsync` (LocalQwenClient.cs:40,57) — Ollama-uyumlu HTTP;
  sinif yorumu "experimental, uretimde bagli degil" der (13-18).

**Simulation (EmberCrpg.Simulation.AiDm):**
- `LlmRoutingService.Complete(LlmRequest, Action<string>, out LlmProviderKind): LlmResponse`
  (LlmRoutingService.cs:44) — streaming-yerel dene, faydasizsa duz zincire dus.
- `LlmRoutingService.Complete(LlmRequest, out LlmProviderKind): LlmResponse` (61) — local → cloud → bos-mock;
  yalniz-tool-cagrili cevap da "basari" sayilir (69-76).
- `LlmRoutingService.LooksLikeProviderFailure(string): bool` (112) — "native error:" vb. metinleri
  basarisizlik sayar ki zincir dususe devam etsin.
- `DialogStreamText.SplitFollowups(string): (Body, List<string>)` (DialogStreamText.cs:20) — "FOLLOWUPS:"
  isaretini NEREDE olursa olsun onurlandirir; talimat-papagani govdeyi bosaltir.
- `DialogStreamText.IsRealFollowup(string): bool` (41) — 8-110 char, "?" ile bitmeli, sablon-kelime reddi.
- `DialogStreamText.NaturalQuestion(string): string` (54) — menu etiketini soylenebilir cumleye cevirir.
- `NpcMemoryLlmEnvelope.RecallLines(world, ActorId, 8)` — NPC'nin prompt'a tasinan hafizasi
  (cagri: Dialog.Source.cs:90-91; ic yapisi bu taramada okunmadi).
- `DmNarrationService.Narrate / ConsultFateService.Resolve` (NarrationServices.cs:61,80) — proposal-log +
  governed tool-call cozumu; sinif yorumu acikca "bugun hicbir uretim host'u cagirmiyor, test-only" der (45-51).

**Presentation (adapter partial'lari + UI):**
- `DomainSimulationAdapter.SelectTopic(string)` (Dialog.Source.cs:281) — deterministik cevap + olay +
  hafiza + LLM tetigi; companion/quest/AnyNews kisa devreleri (291-304).
- `DomainSimulationAdapter.AskFreeText(string)` (360) — "free_text:" sentetik topic ile ayni hat.
- `GenerateNpcTopicAnswerAsync(NpcSeedRecord, string, AskAboutTopic)` (Dialog.Topics.cs:21) ve
  `GenerateAdHocTopicAnswerAsync(string, string, AskAboutTopic)` (76) — id'li/isimli LLM cevap uretimi.
- `GenerateNpcGreetingAsync(NpcSeedRecord)` (Dialog.Greetings.cs:21) ve `GenerateAdHocGreetingAsync(string)` (78)
  — selamlama esdegerleri (100 token, followups yok).
- `CompleteLlmOrEmpty(ILlmRouter, LlmRequest[, Action<string>]): LlmResponse` (static, Dialog.Text.cs:258,274)
  — router istisnasini yutar, deterministik satiri korur.
- `SanitizeNpcLine(string): string` (static, Text.cs:225) — turn-marker kesigi + provider-hata reddi.
- `TrimStreamPartial(string): string` (static, Source.cs:231) — ekrana yansiyacak partial'i
  anti-prompt yankisindan once keser.
- `GetCurrentLine(): string` (Source.cs:31) — thinking → partial+"…" → asla-bos final satir.
- `ConsultFate(string): string` (Fate.cs:30), `ConsultFateAsync` (48), `TryConsumeResolvedFate` (41).
- `InGameUiController.AskOracle(string)` (InGameUiController.cs:527) + Update icindeki yoklama (204-212).
- `ForgeBootstrap.Awake()` (ForgeBootstrap.cs:29) — tum zincirin kuruldugu yer;
  `ForgeLocator.Register/LlmRouter` (ForgeLocator.cs:12,15).
- `ModelBootstrap.ApplyLocator()` (ModelBootstrap.cs:184) — forge+embedding rebind, LLM'e DOKUNMAZ.

## LLD - Yazdigi/Okudugu Alanlar

FieldOwnershipRegistry diliyle: **bu sistemin HICBIR yazimi ledger'da deklare degil** —
`FieldOwnershipRegistry.cs` icinde "Llm", "dialog", "NpcMemory" veya "ToolCallTrace" gecen tek
satir yok (grep ile dogrulanmis yokluk). Sistem tick-kadansli olmadigi icin Reform #2 semasinin
disinda kalmis; yazimlar "UI-olay → main-thread-apply@tick-basi" orjinal kadansindadir.

**Yazdiklari (dunya durumu):**
- `World.NpcMemory[actorId].Events` ← "player_asked" satiri (`RecordConversationMemory`,
  Dialog.Source.cs:103-112) ve "npc_said" satiri, 90 char'a kirpilmis (`RecordNpcSaid`, 180-188).
- `World.Events` ← append `ActorTalked` "topic_selected id:{topicId}" (Source.cs:332-337);
  Oracle tool-call'lari `ToolCallRouter.Invoke(... _world.Events ...)` uzerinden (Fate.cs:118).
- `World.ToolCallTrace` ← Oracle tracer kayitlari (Fate.cs:121-122).

**Yazdiklari (adapter-yerel, dunya disi):** `_currentDialogLine`, `_streamingPartialLine`,
`_isDialogThinking`, `_isFateThinking`, `_pendingFate`, `_fateReady`, `_liveOptions`,
`_topicAskCounts`, `_dialogRequestSerial` — tumu Presentation katmaninda, save'e girmez
(dogrulanmadi: save kapsami 10-save-load.md'nin konusu).

**Okuduklari:**
- `World.NpcMemory` ← `NpcMemoryLlmEnvelope.RecallLines` (prompt hafizasi, Source.cs:90-100) ve
  `AcquaintanceSuffix` "met_player" taramasi (116-124).
- `World.Actors.Records` (aktor cozumu, Source.cs:325-329), `World.NpcSeeds` (349-353),
  `World.Topics` / `_conversation.Topics` (68-78), `World.WorldProfile.Style` (marka-guvenli
  `StyleDescriptor`, Text.cs:200-219), `World.Time` (hafiza damgalari), `World.CompanionIds`
  (Source.cs:260), `_tick` (Oracle seed'i, Fate.cs:56,76).
- Disk: GGUF model dosyasi (`_modelPath`), `EMBER_ALLOW_MODEL_DOWNLOAD` ortam degiskeni
  (NativeLlmClient.cs:246).

## LLD - Urettigi/Tukettigi Olaylar

**Urettigi:**
- `WorldEventKind.ActorTalked` (=2, WorldEventKind.cs:16) — topic seciminde "topic_selected id:…"
  yuku ile (Dialog.Source.cs:332-337).
- Oracle kabul edilmis tool-call'larinin `ToolCallRouter` ici olay/trace uretimi (Fate.cs:118;
  router'in event-append detayi bu taramada okunmadi, dogrulanmadi).

**Log tag'leri (Debug.Log/LogWarning):** `[NpcGreeting]` (Greetings.cs:60), `[NpcGreeting-adhoc]`
(113), `[NpcTopic-adhoc]` (Topics.cs:115), `[DialogLLM]` (Text.cs:269,279), `[fate] rejected LLM
tool call …` (Fate.cs:120), kehanetin kendisi `LogCombat` ile combat log'a (Fate.cs:127),
`[Forge]`/`ModelBootstrap:` bootstrap satirlari (ForgeBootstrap.cs:44, ModelBootstrap.cs:62-171).

**Tukettigi:** dogrudan bir WorldEvent TUKETMEZ; girdileri UI cagrilari ve `NpcMemory` satirlaridir.
(NpcMemory'yi dolduran diger ureticiler — witnessed attack vb. — 07-history-rumors ve ilgili
sistemlerin konusudur.)

## Testler

- `Assets/Tests/EditMode/AiDm/DialogStreamTextTests.cs` — W28 papagan kazasinin pini:
  talimat-yankisi bos govde + null followups; saglikli cevap 3 soru; `IsRealFollowup` red listesi.
- `Assets/Tests/EditMode/AiDm/NativeLlmModelReadinessTests.cs` — LEFT-005: Git-LFS pointer stub
  reddi, gercek GGUF magic kabulu (`IsUsableModelFile`).
- `Assets/Tests/EditMode/AiDm/LlmRoutingServiceTests.cs` — local-oncelik, bos-local→cloud dususu.
- `Assets/Tests/EditMode/AiDm/ConsultFateServiceTests.cs` — `seed % 100` off-by-one crash pini +
  35/35/30 bucket dagilimi (NOT: canli Oracle akisi `ConsultFateService`'i kullanmaz, inline gider —
  Fate.cs:48-129).
- `Assets/Tests/EditMode/AiDm/FlavourBudgetTests.cs` — butce sayaci (canli hatta bagli degil).
- `Assets/Tests/EditMode/Audit/AuditSixthPassCoverageTests.cs`,
  `Assets/Tests/EditMode/Audit/AuditFourthPassTailCoverageTests.cs`,
  `Assets/Tests/EditMode/Acceptance/FazSixToTwelveBackendAcceptanceTests.cs` — bu sistemin
  tiplerine dokunan audit/kabul suitleri (kapsam detayi bu taramada okunmadi).
- Adapter'in streaming-apply/seri-yaris davranisini (Topics.cs:48-70) pinleyen DOGRUDAN bir test
  bulunamadi — yaris duzeltmeleri yalniz kod yorumlariyla belgelenmis durumda.

## Bilinen Borclar + Kacak Kapilari

1. **`USE_LLAMASHARP` editorde TANIMSIZ — editor play-mode'da yerel LLM OLU.** ProjectSettings
   yalniz `SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY;USE_ONNX_RUNTIME` icerir
   (ProjectSettings/ProjectSettings.asset:591); define yalniz Win64 build menusunun
   `extraScriptingDefines`'inda (Windows64BuildMenu.cs:31) ve CI build'inde (unity-test.yml:313
   civari yorum) enjekte edilir. csc.rsp yok, asmdef `versionDefines` bos
   (EmberCrpg.Infrastructure.asmdef:15). Sonuc: editorde `CompleteAsync` `#else` dalina duser
   (NativeLlmClient.cs:218-222) ve `_fallback` da null oldugundan (madde 2) HER LLM cagrisi bos
   doner — editorde oyun tamamen deterministik satirlarla oynar. Plugins README'nin "define'i
   ekleyin" talimati (Assets/Plugins/x86_64/README.txt:26-29) projede uygulanmamis.
2. **Fallback zinciri kagit uzerinde, pratikte yok.** Uretim kurulumu `fallback: null`
   (ForgeBootstrap.cs:48) ve `cloud: null, cloudKind: Mock` (55-58) gecirir; `LocalQwenClient`
   sinif yorumu kendini "experimental, uretim baglamaz" ilan eder (LocalQwenClient.cs:13-18),
   `CloudLlmClient` hic referans almaz (icerigi bu taramada okunmadi). LEFT-005 yorumundaki
   "labelled fallback devreye girer" hikayesi (NativeLlmClient.cs:85-89) bugunku kablolamada
   "bos cevap doner" demektir.
3. **(f) ailesi: istek-serisi deyimi 4 kez kopyalanmis.** `gen`/`req` yakala + iki asamali seri
   kontrolu Greetings.cs:28-56/85-112 ve Topics.cs:28-57/83-114'te birebir tekrar eder —
   SYSTEMS_ATLAS.md:58-59 bu deyimi (f) "akislarda offset/durum-sifirlama" ailesinin ornegi olarak
   zaten isimlendirmis. Tek bir "guarded LLM request" yardimcisina cekilmedikce her yeni diyalog
   yolu ayni yarisi yeniden acabilir.
4. **Turn-marker temizligi uc kopyali ve LISTELERI FARKLI.** Kaynak `TurnMarkers` 7 marker
   (NativeLlmClient.cs:118-119), `SanitizeNpcLine` ayni 7'yi kendi dizisinde tekrarlar
   (Text.cs:230-233), `TrimStreamPartial` yalniz 2 marker bilir ("User:", "Memory"; Source.cs:233-235)
   — mid-stream ekranda "Assistant:" yankisi ILKE OLARAK gorunebilir (final'de silinir). Ayni
   sekilde `LooksLikeProviderFailure` hem router'da (LlmRoutingService.cs:112-121) hem adapterde
   (Text.cs:247-255) kopya. Drift riski: marker listesi tek yerde guncellenirse digerleri yalan soyler.
5. **Iptal yok: superseded istek sonuna kadar calisir.** Seri korumasi yalniz SONUCU dusurur;
   `Complete(request, onPartial)` `CancellationToken.None` ile gider (NativeLlmClient.cs:140-143),
   adapter hicbir token gecirmez — konusmayi terk eden oyuncunun eski istegi 60 saniyeye kadar
   CPU/GPU yakmaya devam eder (tek `_inferenceLock` yuzunden YENI istegi de kuyrukta bekletir,
   181). DET-04 timeout tek ust sinirdir (186-188).
6. **Seed 64→32 bit kirpilir:** `Seed = (uint)request.Seed` (NativeLlmClient.cs:175) — ozenle
   FNV/altin-oran ile turetilen 64-bit seed'lerin (Topics.cs:40,88-91) ust 32 biti atilir;
   (g) ailesi "unchecked sayaclar / % n onyargisi" akrabasi.
7. **Token-basina `resultText += enumerator.Current`** (NativeLlmClient.cs:195) — O(n^2) string
   birlestirme; 192 token tavaniyla pratikte kucuk ama StringBuilder'siz.
8. **Sessiz yutmalar:** `catch (Exception ex)` fallback'e duserken KAYNAKTA hicbir sey loglamaz
   (NativeLlmClient.cs:212-217, `ex` kullanilmiyor); `DispatchSafely`/`LocalStreaming` catch'leri
   de sessiz (LlmRoutingService.cs:50-51,106-110); apply kuyrugu istisnayi yutar
   (Clock.cs:27-28 "must never break the tick"). Teshis tek basina `[DialogLLM]` uyarisina ve
   diag loglarina kalir.
9. **Char-tabanli prompt butcesi token-tabanli baglama karsi:** 6000 char clamp (NativeLlmClient.cs:37)
   2048-token baglami asmamayi HEDEFLER ama garanti etmez; asim `TruncateAndReprefill` +
   %50 kirpma ile llama.cpp tarafina birakilmis (171-172) — bu yolun testi yok (dogrulanmadi).
10. **`FlavourBudget` canli hatta bagli degil** — diyalog yolunda tick-basi LLM cagri tavani
    fiilen yok; sinir, kullanicinin tiklama hizi ve `_inferenceLock` seridir. `DmNarrationService`/
    `ConsultFateService`/`NpcFlavourService` de "test-only" (NarrationServices.cs:45-51) — canli
    Oracle bunlarin inline kopyasini kullanir (Fate.cs:105-123): ayni dogrulama mantigi iki yerde.
11. **E7-005 legacy isim-fallback'i:** konusmada stabil id yoksa NPC isimle cozulur ve ayni gorunen
    isimli aktorlerde YANLIS kisiye baglanabilir — kod yorumu bunu acikca "E7-004 migrasyonuyla
    olecek dal" diye isaretler (Dialog.Source.cs:344-353).
12. **Kacak kapilari (bilincli):** `--ember-forge-off` komut satiri model kokunu bos klasore
    yonlendirir — yorum SDXL forge'u hedefler ama satir 48'deki `NativeLlmClient` da ayni
    `modelRoot`'u aldigi icin proof kosularinda YEREL LLM DE kapanir (ForgeBootstrap.cs:40-48);
    `EMBER_ALLOW_MODEL_DOWNLOAD=1` opt-in indirme kapisi (NativeLlmClient.cs:244-270);
    `NativeLlmClient.EnsureModelReady` indirmesi SHA dogrulamasiz (275-301), SHA'li yol yalniz
    `ModelBootstrap`'ta (ModelBootstrap.cs:154-168) — iki indirme yolu farkli guvenceyle yasiyor.
13. **Isimlendirme tuzagi:** Oracle ekrani `ConsulFateView` (dosya adinda "Consul", "Consult"
    degil; ConsulFateView.cs) — arama yapan ajanlarin "OracleScreen"/"ConsultFateView" tahminleri
    bosa duser.
14. **`Dispose` sirasi:** `_executor?.Context?.Dispose()` + `_weights?.Dispose()`
    (NativeLlmClient.cs:329-336) — `StatelessExecutor`'un Context sahipligi LLamaSharp 0.27'de
    kimde, cifte-dispose riski var mi: dogrulanmadi (LLamaSharp.xml incelenmedi).

# 15-llm-runtime

## HLD - Ne ve Neden (5-10 cumle)

LLM runtime, deterministik Simulation cekirdegini kirletmeden yerel-oncelikli bir metin uretim katmani kurar: gerçek `LLamaSharp.StatelessExecutor` motoru üzerinde bir GGUF Qwen (varsayilan `qwen2.5-1.5b-instruct-q4_k_m.gguf`), Presentation'da `ForgeBootstrap` tarafindan `ForgeLocator.NativeLlm` + `LlmRoutingService` altinda kablolanir. Katman uc birbirinden ayri katmana boluner - Domain sadece kayit tipleri (`LlmRequest`/`LlmResponse`/`ILlmRouter`) tutar; Simulation `LlmRoutingService` + `DialogStreamText` gibi saf metin kurallarina ev sahipligi yapar; Infrastructure `NativeLlmClient`/`LocalQwenClient`/`CloudLlmClient` HTTP+native saglayicilarini icerir (ARCH-05: `EmberCrpg.Infrastructure.AiDm` asmdef izolasyonu). Router her istegi once yerele gonderir, "yararli payload" testinden gecmezse cloud'a duser; `HasUsefulPayload` bir yanit metnini "native error:", "llama_decode failed", "InvalidInputBatch" gibi saglayici hatasi imzalari acisindan da eler. M3a stream yolu (`LocalStreaming` delegate + `Complete(request, onPartial, out chosen)`) her cozulen token'da birikmis metni asenkron push eder; DialogAdapter partiali worker thread'inden `_mainThreadApply` kuyruguyla ana thread'e tasir ve `_dialogRequestSerial` ile en son istegin kazanmasini garanti eder. Cevabin FOLLOWUPS kismi `DialogStreamText.SplitFollowups` ile ayristirilir - W28 "instruction parrot" olayina karsi 3 gercek soru + `IsRealFollowup` gate + `NaturalQuestion` etiket-cumleye cevirici saf birer fonksiyon olarak Simulation tarafinda pinlenir. DM oracle (`ConsultFate`) yolunda karar HER ZAMAN deterministiktir: `ConsultFateOutcomeBucket.D100(_tick FNV)` bir bucket seçer, LLM sadece o buketin ETRAFINDA süslü prophecy yazar; LLM'in oneriligi tool call'lari `LlmProposalValidator` + `ToolCallRouter` + `ToolCallTracer` uzerinden `_world.ToolCallTrace`'e islenir. W36-B13'te editor Qwen kablosu (DefaultNpcPortraitJsonProvider) offline modda kirilan Ollama HTTP yerine `ForgeLocator.NativeLlm.CompleteAsync`'e cevrilir, boylece portre uretimi de dialog/greeting/topics/fate ile ayni motor iyeliginden yararlanir. UYARI: `USE_LLAMASHARP` scripting-define su an `ProjectSettings.asset:591`'de tanimli DEGIL (yalnizca `SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY;USE_ONNX_RUNTIME`) - bu nedenle `NativeLlmClient.CompleteAsync`'in `#if USE_LLAMASHARP` govdesi derleme disi kalir ve gercek native inference kanalinin acilmasi manifest + define + build-time bagli bir "kacak kapi" olarak durur.

## HLD - Akış (numaralı adımlar)

1. `ModelBootstrap.Awake` `persistentDataPath/Models` klasorunu kurar, `StreamingAssets/Models/manifest.json`'u `UnityWebRequest` ile okur, `ModelManifest.VerifyAllPresent` (SHA256) ile isaretlenen eksik giris varsa `DownloadHandlerFile` ile indirir, yeniden SHA dogrular; eksik kalirsa "degraded" logu basar.
2. `ForgeBootstrap.Awake` `--ember-forge-off` gibi runtime bayraklarini kontrol edip `NativeLlmClient` (fallback = null) + `EmberForgeFactory` + `SerializedAssetForge` + `RuntimeSingleFigureForgeFactory.WrapNpcBillboards` insa eder, `LlmRoutingService(local => _nativeLlm.Complete, cloud = null, kind = Mock)` router'ini kurar, streaming icin `LocalStreaming = (req, onPartial) => _nativeLlm.Complete(req, onPartial)` atar, `ForgeLocator.Register(forge, nativeLlm, router)` cagirir.
3. `ModelBootstrap.ApplyLocator` (indirmeler bittikten sonra) forge ve embedding client'i `ForgeLocator.SetAssetForge` + `SetEmbeddingClient` ile REBIND eder - `NativeLlm` ve `LlmRouter` referanslarina DOKUNMAZ.
4. Dialog istekleri `DomainSimulationAdapter.GenerateNpcTopicAnswerAsync` (veya `GenerateNpcGreetingAsync`, `GenerateAdHocTopicAnswerAsync`) icinde: `_isDialogThinking = true`, `_streamingPartialLine = ""`, `int req = ++_dialogRequestSerial`, `SpeakPlayerQuestion(topicLabel)` cagirilir; `NpcMemory.RecallLines`'dan cekilen `RecentTurns` prompt icin hazirlanir; `LlmRequest` prompt gövdesi `CompanionPersonaSuffix + AcquaintanceSuffix + PlayerContextSuffix + RepeatAskSuffix + FollowupsInstruction` kuyruguyla insa edilir.
5. `CompleteLlmOrEmpty(router, request, onPartial)` `LlmRoutingService.Complete(request, onPartial, out chosen)`'i cagirir; `LocalStreaming` mevcut oldugu icin `_nativeLlm.Complete(req, onPartial)` yolu once denenir, partial her token'da worker thread'den `_mainThreadApply` kuyruguna dusurulur.
6. `NativeLlmClient.CompleteAsync` (compile edildiginde) `IsUsableModelFile` gate'ini gecirir - `_isInitialised` false ise `LoadModelSync` `Task.Run` ile arkada tetiklenir; `BuildPrompt` ChatML sablonu (`<|im_start|>system/user/assistant`) uretir, `ClampPrompt` 6000 karakter tavaninda kirpar, `InferenceParams` `MaxTokens = min(request.MaxTokens, 192)`, `AntiPrompts = {"User:", "Memory"}`, `Seed = request.Seed`, `Temperature = 0.7` ile hazirlanir, `_inferenceLock` SemaphoreSlim serilestirir, `InferAsync` `IAsyncEnumerable<string>` uzerinde 60 sn timeout ile cozulur, her token `resultText`'e eklenip `onPartial(resultText)` cagrilir.
7. Yanit metni `StripTrailingTurnMarkers` ile "User:/Assistant:/Memory:/<|im..." leaklerinden temizlenir, `LlmResponse(text, null, 0)` olarak router'a doner.
8. Router `HasUsefulPayload` gate'ini ile eler - `LooksLikeProviderFailure` gorulurse cloud fallback denenir (varsa) veya `LlmProviderKind.Mock` + bos LlmResponse doner.
9. Adapter apply-queue geri arasinda `req != _dialogRequestSerial` ise geciken cevap DUŞURÜLÜR (stale-reply race); serial gecerse `SanitizeNpcLine` (marker stripping) + `DialogStreamText.SplitFollowups` calisir - bos body durumunda deterministik line korunur; `AbsorbFollowups` FOLLOWUPS'lari `_liveOptions`'a soker ve `RecordNpcSaid` snippet'i `NpcMemory` `npc_said` olayi olarak kaydeder.
10. `ConsultFate` yolu: `ConsultFateAsync` dice bucket'i `ConsultFateOutcomeBucket.D100((uint)_tick * FNV32)` ile CEKER, prompt "The dice have rolled a X outcome (roll/100)" + FollowupsInstruction ile insa edilir, `Task.Run(() => CompleteLlmOrEmpty(router, request))` ile off-thread cozulur; apply-queue icinde `_pendingFate` deterministik "THE FATES DECREE: <bucket>" ile başlar, temiz body gelirse degistirilir, `_fateReady = true` bayragi ile UI polling icin isaretlenir, LLM'in `ProposedToolCalls` listesi `LlmProposalValidator` + `ToolCallRouter` (only `consult_fate` handler `AcceptedWith(bucket.Code)`) ile REDDEDILIR/onaylanir; kabul edilenler `ToolCallTracer`'a, oradan `_world.ToolCallTrace`'e yazilir.
11. `DefaultNpcPortraitJsonProvider.RequestAsync` (W36-B13): once `ForgeLocator.NativeLlm.IsAvailable` kontrolu; varsa `wired.CompleteAsync` cagrilir, `ExtractJsonObject` `{...}` blogunu izole eder; degilse env `EMBER_LOCAL_LLM_ENDPOINT` -> Ollama HTTP -> env `EMBER_NATIVE_LLM_MODEL` GGUF dev fallback yolu kullanilir; her yol `SystemPrompt` ("...matching the NpcPromptJson schema. No prose, no markdown fences.") ile guardlanir.

## LLD - Veri Modeli (file:line)

- `NativeLlmClient` — Assets/Scripts/Infrastructure/AiDm/NativeLlmClient.cs:23
  - `DefaultModelFileName = "qwen2.5-1.5b-instruct-q4_k_m.gguf"` — NativeLlmClient.cs:25
  - `DefaultDownloadUrl` (HuggingFace Qwen2.5-1.5B GGUF) — NativeLlmClient.cs:26
  - `_modelPath`, `_fallback: LocalQwenClient`, `_downloadUrl`, `_inferenceLock: SemaphoreSlim(1,1)`, `_loadLock: object` — NativeLlmClient.cs:28-31
  - `NativeContextTokens = 2048u`, `NativeBatchTokens = 512u`, `MaxNativePromptChars = 6000`, `MaxNativeGenerationTokens = 192` — NativeLlmClient.cs:33-36
  - `#if USE_LLAMASHARP _weights: LLamaWeights`, `_executor: StatelessExecutor` — NativeLlmClient.cs:38-41
  - `_isInitialised` (compiled-only under define) — NativeLlmClient.cs:43-45
  - `MinUsableModelBytes = 1_000_000` (LEFT-005 pointer/truncation floor) — NativeLlmClient.cs:88
  - `TurnMarkers[]` = `{ "User:", "Assistant:", "System:", "Memory:", "<|im", "\nUser", "\nMemory" }` — NativeLlmClient.cs:117-118
- `LlmClientConfig` — Assets/Scripts/Infrastructure/AiDm/LlmClientConfig.cs:17 (Provider, EndpointUrl, ApiKey, Enabled)
- `LocalQwenClient` — Assets/Scripts/Infrastructure/AiDm/LocalQwenClient.cs:21
  - `DefaultOllamaGenerateEndpoint = "http://localhost:11434/api/generate"` — LocalQwenClient.cs:22
- `CloudLlmClient` — Assets/Scripts/Infrastructure/AiDm/CloudLlmClient.cs:20 (`Kind = _config.Provider`)
- `LlmRoutingService` — Assets/Scripts/Simulation/AiDm/LlmRoutingService.cs:13
  - `_local: LlmDispatch`, `_cloud: LlmDispatch`, `_cloudKind: LlmProviderKind` — LlmRoutingService.cs:15-17
  - `LocalStreaming: LlmStreamingDispatch { get; set; }` — LlmRoutingService.cs:35
- `ILlmRouter` — Assets/Scripts/Simulation/AiDm/ILlmRouter.cs:5 (`Complete(req, out string chosen)`)
- `DialogStreamText.FollowupsInstruction` — Assets/Scripts/Simulation/AiDm/DialogStreamText.cs:13
- `ForgeLocator` fields: `AssetForge`, `NativeLlm: NativeLlmClient`, `LlmRouter: ILlmRouter`, `Embedding: EmbeddingClient` — Assets/Scripts/Presentation/Ember/Forge/ForgeLocator.cs:10-13
- `ForgeBootstrap._nativeLlm: NativeLlmClient` — Assets/Scripts/Presentation/Ember/Forge/ForgeBootstrap.cs:22
- `DefaultNpcPortraitJsonProvider.SystemPrompt` — Assets/Scripts/Presentation/Ember/CharacterCreation/DefaultNpcPortraitJsonProvider.cs:13
- Adapter state: `_isDialogThinking`, `_streamingPartialLine` — Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.cs:45, 51
- Adapter state: `_dialogRequestSerial: int` — DomainSimulationAdapter.Dialog.Source.cs:93
- Adapter state: fate — `_isFateThinking`, `_fateReady`, `_pendingFate`, `_pendingFateFollowups` — DomainSimulationAdapter.Fate.cs:25 ve civari

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

- `NativeLlmClient.FromModelFile(modelFilePath, fallback, downloadUrl): NativeLlmClient` — NativeLlmClient.cs:63 — Manifest-secilen `.gguf` yolunu dogrudan ver.
- `NativeLlmClient.IsUsableModelFile(path): bool` — NativeLlmClient.cs:91 — GGUF magic + 1MB tabani; LFS pointer stub'i "yok" sayar.
- `NativeLlmClient.IsAvailable: bool` — NativeLlmClient.cs:107 — `_isInitialised || IsUsableModelFile(_modelPath)`.
- `NativeLlmClient.StripTrailingTurnMarkers(raw): string` — NativeLlmClient.cs:120 — "User:/Assistant:/Memory:/<|im" ilk oyununda keser.
- `NativeLlmClient.Complete(request): LlmResponse` — NativeLlmClient.cs:132 — `SyncTaskBridge` uzerinden async'i bekletir.
- `NativeLlmClient.Complete(request, onPartial): LlmResponse` — NativeLlmClient.cs:138 — M3a streaming twin.
- `NativeLlmClient.CompleteAsync(request, ct, onPartial=null): Task<LlmResponse>` — NativeLlmClient.cs:143 — Native prompt akisi + `_inferenceLock` + 60sn timeout; USE_LLAMASHARP tanimli degilse fallback.
- `NativeLlmClient.BuildPrompt(request): string` — NativeLlmClient.cs:218 — ChatML `<|im_start|>system/user/assistant` insa eder.
- `NativeLlmClient.EnsureModelReady(progress): Task` — NativeLlmClient.cs:242 — Kullanilabilir dosya varsa yukler; yoksa yalnizca `EMBER_ALLOW_MODEL_DOWNLOAD=1` opt-in ile HTTP indirir.
- `NativeLlmClient.LoadModelSync(): void` — NativeLlmClient.cs:284 — `ModelParams` (2048 ctx, 512 batch, GPU -1), `LLamaWeights.LoadFromFile` + `new StatelessExecutor(...)`.
- `LlmRoutingService.Complete(req, out chosen): LlmResponse` — LlmRoutingService.cs:64 — Local-first, yararli payload testi, cloud fallback.
- `LlmRoutingService.Complete(req, onPartial, out chosen): LlmResponse` — LlmRoutingService.cs:42 — Streaming twin; `LocalStreaming` varsa oncelikli.
- `LlmRoutingService.HasUsefulPayload(response): bool` — LlmRoutingService.cs:99 — Metin bos veya "native error:" imzasi tasiyorsa yararli sayilmaz.
- `DialogStreamText.SplitFollowups(answer): (Body, Followups)` — DialogStreamText.cs:21 — "FOLLOWUPS:" markerini bulur; instruction-parrot durumunda body BOŞ doner.
- `DialogStreamText.IsRealFollowup(q): bool` — DialogStreamText.cs:42 — 8-110 kar `?`-sonlu; "first question/in-character/traveller might" parrot'lari reddeder.
- `DialogStreamText.NaturalQuestion(label): string` — DialogStreamText.cs:56 — "Ask about X" -> "What can you tell me about X?"; companion_join/companion_leave ozel-kase.
- `DomainSimulationAdapter.CompleteLlmOrEmpty(router, request, onPartial): LlmResponse` — DomainSimulationAdapter.Dialog.Text.cs:258 — Streaming router varsa oyle cagrir; provider exception'i tutup bos yanit doner.
- `DomainSimulationAdapter.SanitizeNpcLine(raw): string` — DomainSimulationAdapter.Dialog.Text.cs:225 — En erken chat-turn marker'i once keser, saglayici hata metni gorurse bos donerek "deterministik line"i korur.
- `DomainSimulationAdapter.ConsultFateAsync(question): Task` — DomainSimulationAdapter.Fate.cs:59 — Bucket->prompt->off-thread router->apply-queue; `LlmProposalValidator + ToolCallRouter + ToolCallTracer` ile arac cagrilarini gate'ler.
- `DomainSimulationAdapter.GenerateNpcTopicAnswerAsync(npc, topicId, topic): Task` — DomainSimulationAdapter.Dialog.Topics.cs:21 — Seed'li id, memory recall, streaming partial + serial gate + FOLLOWUPS split.
- `DomainSimulationAdapter.GenerateAdHocTopicAnswerAsync(actorName, topicId, topic): Task` — DomainSimulationAdapter.Dialog.Topics.cs:78 — Ayni sozde-random FNV seed'li seyahat, seed'siz authored NPC'ler icin.
- `DomainSimulationAdapter.RecallDialogMemory(npcId): List<string>` — DomainSimulationAdapter.Dialog.Source.cs:106 — `NpcMemoryLlmEnvelope.RecallLines(_world, ActorId(DialogMemoryKey(npcId)), 8)`.
- `DomainSimulationAdapter.SpeakPlayerQuestion(questionText): void` — DomainSimulationAdapter.Dialog.Source.cs:139 — `NaturalQuestion` + `SpeechDirector.FeedFinal(PlayerVoiceKey, text)`.
- `ForgeBootstrap.EnsureNativeLlmReady(progress): Task` — ForgeBootstrap.cs:88 — `_nativeLlm.EnsureModelReady(progress)` proxy.
- `ForgeLocator.Register(forge, llm, router): void` — ForgeLocator.cs:15 — Ilk full-set kayit (bootstrap).
- `ForgeLocator.SetAssetForge(forge): void` — ForgeLocator.cs:23 — Yalnizca forge'yi rebind eder, NativeLlm/LlmRouter'a dokunmaz.
- `ForgeLocator.Clear(): void` — ForgeLocator.cs:40 — OnDestroy'da hepsini bosaltir.
- `DefaultNpcPortraitJsonProvider.RequestAsync(seed, correctionReason, ct): Task<string>` — DefaultNpcPortraitJsonProvider.cs:23 — Once wired `ForgeLocator.NativeLlm`, degilse env-GGUF veya Ollama HTTP; `ExtractJsonObject` ile `{...}` blogunu izole.
- `DefaultNpcPortraitJsonProvider.Request(seed, reason): string` — DefaultNpcPortraitJsonProvider.cs:17 — 8 sn CancellationToken'li senkron sarmalayici.
- `ModelBootstrap.BootstrapRoutine(): IEnumerator` — ModelBootstrap.cs:57 — Manifest yukle -> SHA verify -> eksik indir -> re-verify -> `ApplyLocator`.
- `ModelBootstrap.ApplyLocator(): void` — ModelBootstrap.cs:181 — `ForgeLocator.SetAssetForge`/`SetEmbeddingClient`; forge rebind sirasinda NativeLlm KORUNUR.

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Yazar:
- `World.NpcMemory[actorId].Events` — `player_asked` (`RecordConversationMemory`), `npc_said` (`RecordNpcSaid`), `MarkDialogueSeen(topicId)` (topic secildikten sonra ve `ConsumeOption` icinde).
- `World.ToolCallTrace` — `ConsultFateAsync` icindeki `ToolCallTracer.Entries` (yalnizca ana-thread apply-queue icinde eklenir).
- Adapter-yerel (World'e degmeyen): `_currentDialogLine`, `_streamingPartialLine`, `_isDialogThinking`, `_isFateThinking`, `_pendingFate`, `_pendingFateFollowups`, `_fateReady`, `_liveOptions[memoKey]`, `_topicAskCounts[askKey]`, `_dialogRequestSerial`, `_conversationSerial`.
- OS/disk: `persistentDataPath/Models/**` (ModelBootstrap indirmeler ve `File.Delete` SHA mismatch'te), NativeLlmClient `EnsureModelReady` opt-in yolu.

Okur:
- `World.NpcMemory` — `NpcMemoryLlmEnvelope.RecallLines`, `AcquaintanceSuffix`, `RecallDialogMemoryByName`.
- `World.Actors.Records` — player id/ismi cikartma, ad-hoc topic yolu.
- `World.WorldProfile.Style` (dogrudan `StyleDescriptor()` uzerinden prompt suffix).
- `World.Topics` / `Conversation.Topics` — fallback topic listesi.
- `_tick` (adapter monotonik) — `ConsultFateAsync` seed'i.
- GGUF `_modelPath` dosyasi — `NativeLlmClient.IsUsableModelFile`, `LoadModelSync`.
- `manifest.json` — `ModelBootstrap.LoadManifestRoutine`.
- Env: `EMBER_ALLOW_MODEL_DOWNLOAD` (opt-in download), `EMBER_LOCAL_LLM_ENDPOINT`, `EMBER_NATIVE_LLM_MODEL` (portrait dev fallback).
- Runtime bayrak: `--ember-forge-off` (ForgeBootstrap forge redirect; LLM'i etkilemez).

FieldOwnershipRegistry uyarisi: bu sistem `World.*` alanlarina yazdiginda AYNI `WorldState`'i tek yazar-sistem (DomainSimulationAdapter/dialog partial'lari) mainThreadApply kuyrugu icinden yapar; kayit `World.NpcMemory`, `World.ToolCallTrace` icin `dialog-runtime` yazicisi kolonuna oturur. Adapter alanlari `Registry`'de tutulmaz (Presentation state).

## LLD - Ürettiği/Tükettiği Olaylar

Uretir (log/event):
- `WorldEventKind.ActorTalked` — dialog topic secildiginde `SelectTopic` yolunda dolayli (Dialog.Binding.cs).
- Debug/log tag'leri: `[NpcGreeting]`, `[NpcGreeting-adhoc]`, `[NpcTopic-adhoc]`, `[DialogLLM]`, `[fate]`, `[Forge]`, `[ModelBootstrap]`, `LogCombat(prophecy)`.
- `NpcMemory.InteractionEvent` — `player_asked` ve `npc_said` snippet'leri.
- `ToolCallTracer.Entries` -> `_world.ToolCallTrace.Add(rec)`.
- Ses cikisi: `SpeechDirector.FeedFinal(voiceKey, text)` — SpeakPlayerQuestion tarafi (system 16'ya girdi).

Tuketir:
- `ForgeLocator.LlmRouter` (yoksa sessizce return).
- `ForgeLocator.NativeLlm` (portrait yolu icin varlik/uygunluk gate).
- `NpcMemoryLlmEnvelope.RecallLines` (memory rowlari LLM turns'e).
- `ConsultFateOutcomeBucket.D100(...)` sonucu (fate prompt icerigi).
- `ToolRegistry` + `ToolCallValidator` (fate yolu icinde in-line kurulan mini registry).
- `SpeechPlayerQuestion` -> `SpeechDirector` (Presentation TTS).

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

- `Assets/Tests/EditMode/AiDm/DialogStreamTextTests.cs` — REFORM #3 pin: `Split_InstructionOnlyEcho_YieldsEmptyBody_AndNoParrotFollowups`, `Split_HealthyAnswer_YieldsBodyAndRealQuestions`, `Split_NoMarker_PassesThroughUntouched`, `IsRealFollowup_RejectsParrotsAndFragments_AcceptsQuestions`, `NaturalQuestion_TurnsLabelsIntoSpeech` — W28 parrot olayi kalici pin.
- `Assets/Tests/EditMode/AiDm/NativeLlmModelReadinessTests.cs` — LEFT-005 pin: `RealGgufHeaderAboveFloor_IsUsable`, `LfsPointerStub_IsRejected`, `TruncatedGgufUnderFloor_IsRejected`, `WrongMagicAboveFloor_IsRejected`, `MissingFile_IsRejected`, `NullOrEmptyPath_IsRejected` + `StripTrailingTurnMarkers_*` (2026-05-31 headless proof leaki icin).
- `Assets/Tests/EditMode/AiDm/LlmRoutingServiceTests.cs` — `LocalSucceeds_PicksLocal`, `LocalReturnsEmpty_FallsBackToCloud`, `LocalThrows_FallsBackToCloud`, `LocalNativeFailureText_IsNotUsefulPayload`, `BothNull_ReturnsEmptyAndMockKind` — yararli-payload gate'i pinler.
- `Assets/Tests/EditMode/AiDm/LlmHttpBoundaryTests.cs` — HTTP client sinir davranisi (LocalQwen/Cloud paylasilan LlmHttpClientCore).
- `Assets/Tests/EditMode/AiDm/NpcMemoryLlmEnvelopeTests.cs` — Prompt turns icin memory recall siniri.
- `Assets/Tests/EditMode/AiDm/ConsultFateServiceTests.cs`, `LlmToolAuthorityTests.cs`, `LlmProposalValidatorTests.cs`, `ToolCallValidatorRouterTests.cs`, `ToolRegistryTests.cs`, `ToolSurfacesTests.cs` — DM oracle + tool-authority zinciri.
- `Assets/Tests/EditMode/AiDm/PlayerVoiceServiceTests.cs`, `NpcVoiceSignatureServiceTests.cs` — SpeakPlayerQuestion / dialog voice key.
- `Assets/Tests/EditMode/CharacterCreation/DefaultNpcPortraitJsonProviderTests.cs` — `RequestAsync_CanceledToken_ReturnsEmptyWithoutThrowing` (W36-B13 kablosunun cancellation semantigini pinler).
- `Assets/Tests/EditMode/Audit/AuditFourthPassTailCoverageTests.cs`, `Assets/Tests/EditMode/Acceptance/FazSixToTwelveBackendAcceptanceTests.cs` — cross-cutting audit + acceptance rowlari LLM router/native readiness'e deger.

Bilinen bosluk: B13 icin dogrudan "ForgeLocator.NativeLlm.IsAvailable true iken RequestAsync gercek JSON dondurur" senaryosunun unit karsiligi yok (scorecard: SHIPPED-NO-TEST).

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- W32 (79b707d8) `fix(perf)+fix(input)` — Fast-travel donmasi kok analizinde `NativeLlmClient` inferansi da 60 sn timeout + `SemaphoreSlim` serilesmesi kapsaminda pinlendi; B15 storm kill ile dolayli olarak LLM iplerinin block ihtimalini dusurdu.
- W33-W35 (61e340f3, 3aa87cf6, 20a3b899) — Sim slice'lari (FARM, SLEEP, WORK, ScheduleSystem shrink) ana konu; LLM runtime katmani DIREK degismedi ancak `FieldOwnershipRegistry.cs:99-124` bu haftalar boyunca genisledi ve `World.NpcMemory` yazar olarak dialog runtime'i icin ownership'i berkitti (B04 shipped `20a3b899`).
- W35 (20a3b899) — RUH_TESHIS §8 = 10/10; ownership seam LLM runtime'un `World.NpcMemory`/`World.ToolCallTrace` yazicisi olarak resmi olarak tanindi.
- W36 tail (f6c9e2d0) `feat(tail)` — B13 KAPANDI: `DefaultNpcPortraitJsonProvider` artik once `ForgeLocator.NativeLlm.IsAvailable` uzerinden wired native Qwen'e gidiyor; Ollama HTTP + env-GGUF dev fallback yolu KORUNUYOR. `SystemPrompt` "no markdown fences" ekiyle guclendi. Not: ayni tail commit'in scorecard'i USE_LLAMASHARP scripting-define'in HALA `ProjectSettings.asset:591`'de tanimlanmadigini ve degistirmedigini not ediyor.
- M3a streaming (f1636b1b) — Bu 5 haftalik pencerede degil (once), ama W32+ pin'leri (`_dialogRequestSerial` serial guard, `_mainThreadApply` marshalling) tum W32-W36 doneminde canli kaldi ve `RecordNpcSaid`, `AbsorbFollowups`, `SanitizeNpcLine` guardlari sistemin karakteristik "en son istek kazanir + parrot reddedilir + saglayici hatasi deterministik line'i bozamaz" kontratini pinlemeye devam etti.

## Bilinen Borçlar + Kaçak Kapıları

- **USE_LLAMASHARP scripting-define YOK** — `ProjectSettings.asset:591` yalnizca `SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY;USE_ONNX_RUNTIME` tanimlar. Sonuc: `NativeLlmClient.CompleteAsync`'in `#if USE_LLAMASHARP` bloku derleme disi kalir ve fallback'e (null olarak wire edildigi icin `EmptyResponse`) duser. B13'un fiiliyati manifest + define + build zamani birlesimine baglidir; kesin "editor'de yerel LLM cevap veriyor" garantisi icin build sirasinda define eklenmeli veya scorecard'daki not otomasyona baglanmali.
- **Portrait wired-yolunda LlmRouter yerine dogrudan `NativeLlmClient` cagriliyor** — `DefaultNpcPortraitJsonProvider.RequestAsync` `wired.CompleteAsync`'i cagirir; `HasUsefulPayload` gate'i ve cloud fallback bypass edilir. Native yaniti bos/native-error metniyse `ExtractJsonObject` bosa dusup deterministik placeholder yaniti dograr.
- **Fate tool-registry inline kurulur** — `ConsultFateAsync` her cagride yeni `ToolRegistry`, `ToolCallRouter`, `ToolCallValidator`, `ToolCallTracer` yaratir; global bir `ForgeLocator` benzeri tool spine yok. Ilerideki toolCall genislemesi tekrarli inline setup'a bagimli kalir.
- **`_fallback` her zaman null wire ediliyor** — `ForgeBootstrap` `NativeLlmClient(modelRoot, fallback: null)` ile insa eder. Yani native cikmadiginda otomatik LocalQwen/Ollama fallback YOK; sadece portrait dev-fallback yolu kendi `HttpClient` ile `LocalQwenClient` kurar.
- **`--ember-forge-off` LLM'i etkilemez** — Yalnizca ONNX forge modelRoot'unu redirect eder. Proof harness kosumlarinda LLM inferansi hala butcede yer tutabilir; kucuk bir `--ember-llm-off` bayragi (native/router'i mock ile degistir) mantikli bir sonraki iyilestirme.
- **`_mainThreadApply` guveni tick calisir olmasina bagli** — `AdvanceTick` cagrilmadigi (paused/headless) durumlarda partial ve final apply'lar kuyrukta birikir. Bir marathon proof durakladigi anlarda dialog Spinner "…" da kalabilir; guvenli akis icin `Flush` API'si asikar degil.
- **Model download hala HttpClient (opt-in)** — `EnsureModelReady` `HttpClient` ile 8KB buffer streamli indirir ama SHA verify YOK (ModelBootstrap manifest yolu SHA yapar, `NativeLlmClient.EnsureModelReady` yapmaz); ikisinin uc noktalari birbirinden bagimsiz ve `DefaultDownloadUrl` dogrudan HuggingFace URL'sine hardcoded.
- **DialogStreamText'in `NaturalQuestion` catch-all'i konusma etiketleri "Ask about "  ile baslamiyorsa kelimeleri aynen soyluyor** — Menu label'inin kendisi zaten cumleyse iyi, ama farkli locale (TR "X hakkinda sor") gelmis olsa duzelme yok; L10n katmani baglaninca bu fonksiyona locale-aware overload gerekecek.
- **`StripTrailingTurnMarkers` ve `SanitizeNpcLine` iki yerde ayni marker listesini tekrarliyor** — NativeLlmClient (kaynak) + adapter (savunma). Marker seti degisirse iki yer birden guncellenmeli; ortak bir static'e ekilmedi.
- **`LlmRoutingService` streaming API'sinde partial exception yutulur** — `try { response = LocalStreaming(...) } catch { response = null; }` sessiz down-grade yapar; retry/backoff yok.
- **B12/B22/B24 hala acik** — W36 scorecard'i magic rewire (SpellResolver hot-path), predation fallback site, view multi-writer compositor'i deferred birakti; dialog/LLM yolu bunlardan direkt etkilenmez ama world event log'lari (RumorMillCursorTrim, B21) uzerinden dolayli birlesim noktalari var.

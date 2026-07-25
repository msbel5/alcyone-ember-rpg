# 14-dialog-state

## HLD - Ne ve Neden (5-10 cumle)

Diyalog durum makinesi, oyuncunun bir NPC ile açtığı konuşmayı tek bir kaynaktan yöneten sistemdir: kimin konuştuğu (`ActorId`/`NpcId`/isim), hangi portrenin çizildiği, hangi konuların sunulduğu, ekrandaki cümlenin (deterministik veya LLM'den) ne olduğu ve LLM'in hâlâ düşünüp düşünmediği. Presentation katmanında `DomainSimulationAdapter`'ın Dialog partial'ları (Binding / Source / Topics / Greetings / Text) altında yaşar; UI, `IDialogSource` üzerinden okur, `SelectTopic` / `AskFreeText` ile yazar. Neden gerekli: eski akış konuşma verisini `_activeDialogActor` / `_currentPortrait` gibi dağınık alanlarda tutuyordu ve üç yarım servis (AskAbout, AskDm, NpcDialogue) arasında sızıntı yapıyordu — Oracle konuşması bir NPC cevabı ile bulanabiliyor, name-based lookup ikiz isimlerde yanlış aktöre bağlanabiliyordu. W23 bu dağınıklığı **DialogStateMachine v1** olarak katılaştırdı: seçilen konu tüketilir, cevabın FOLLOWUPS satırından yeni "kabarcık" sorular filizlenir ve bu memo veda edildikten sonra bile hayatta kalır (geri gelince kaldığı yerden devam eder). W28 konuşmayı bir **anıya** çevirdi (NPC kendi söylediğini yazar; oyuncu ne sorduğu ve "met_player" olayı NpcMemory'ye düşer). W31 hem "sadece Any News çıkıyor" **donmuş liste** bug'ını kapattı (memo lazy-seed + tükenince UNSEEN öncelikli refill) hem de yazma/okuma tarafında **ID uyumsuzluğunu** düzeltti (`DialogMemoryKey`: `GeneratedNpcActorOffset + NpcId` altında yazılan satırları oradan okur). Deterministik cümle daima önce basılır, LLM cevabı ancak temiz ve boş olmayan bir metin döndürürse onu **değiştirir** (BUG-DIALOG-EMPTY + BUG-DIALOG-TURNLEAK muhafızları); geç gelen cevap `_conversationSerial` / `_dialogRequestSerial` çiftini ihlal ederse sessizce düşer.

## HLD - Akış (numaralı adımlar)

1. **Bağla:** UI, `IPlayerCommandSink.GetDialogSource(ActorId)` (veya legacy `GetDialogSource(string name)`) çağırır. Binding tarafı `WorldState.Actors.TryGet` ile aktörü kilitler; id boşsa/eşleşmezse **honest empty** dönüş verir (`_suppressGlobalTopicFallback = true`, "There is no one here to talk to.").
2. **Sınıflandır:** `id.Value >= GeneratedNpcActorOffset (10_000)` ise `NpcId = id - offset` ile `NpcSeedRecord` geri kazanılır; authored slice aktörleri isimle eşleştirilir. Aktör düşman (`NpcRole.Outlaw`) ise `TryBeginWorldEncounter` combat ekranını açar — konuşma başlamaz.
3. **BeginConversation:** `_conversationSerial++` (in-flight LLM cevaplarını geçersizleştirir), speaker/portrait/topic seti tek bir `ConversationState` içine konur; `EnsureLiveOptions` per-actor **live memo**'yu ilk kez seed eder (dönüşte lived state korunur).
4. **met_player:** İlk kez konuşuluyorsa `NpcMemoryStore.GetOrCreate(actorId).RecordEvent("met_player", subjectId=player.Name)` yazılır — sonraki karşılaşmalar tanıdık selamı alır.
5. **Deterministik cümle (sync):** `_currentDialogLine = DeterministicGreeting(...)` — social-group × time-of-day matrisinden hash-deterministic seçilir; NpcMemory'de `met_player` varsa **acquaintance** havuzuna geçer; ~%35 olasılıkla `ComposeRumor` gerçek bir WorldEvent narrasyonu veya en yakın dungeon fısıltısı ekler.
6. **LLM greeting (async):** `GenerateNpcGreetingAsync` (seeded NPC) veya `GenerateAdHocGreetingAsync` (authored aktör) — `Task.Run` ile off-thread; partial'lar `_mainThreadApply` kuyruğuna işlenir; final metin `SanitizeNpcLine` sonrası boş değilse `_currentDialogLine`'ı değiştirir ve `RecordNpcSaid` ile aynı satır NPC'nin hafızasına düşer.
7. **UI, `GetTopics()` çağırır:** live memo boşsa (`_conversation.Topics.Count > 0` iken) **lazy seed** yapılır — `NpcMemory.HasDialogueSeen(topicId)` false olan konular önce sunulur, hepsi görülmüşse tam katalog geri gelir. Başa **AnyNewsTopic ("Any news?")** eklenir (asla tüketilmez). Companion topic'i (join/leave) uygunluğa göre eklenir. `_suppressGlobalTopicFallback` true iken global world topic'lerine düşmez.
8. **SelectTopic:** Companion topic ise `TryHandleCompanionTopic`, quest interaction ise `TryHandleQuestInteractionTopic` handle eder ve döner. "Any news?" ise `RumorMillSystem.PickFor` deterministik yanıt üretir. Aksi halde `ConsumeOption(topicId)` memo'dan çıkarır **ve** `NpcMemory.MarkDialogueSeen(topicId)` işaretler; bir katalog konusu değil ama `?` içeriyorsa (grown followup) `AskFreeText`'e yönlendirilir.
9. **SpeakPlayerQuestion:** `NaturalQuestion(label)` menu etiketini gerçek bir soruya çevirir ("Ask about watch" → "What can you tell me about watch?"), `SpeechDirector.FeedFinal` oyuncunun kendi ses key'iyle onu **sesli** okur; `RecordConversationMemory` "player_asked" olayını yazar.
10. **Answer:** `GenerateNpcTopicAnswerAsync` (seeded) veya `GenerateAdHocTopicAnswerAsync` (name-based) — prompt suffix'leri: `CompanionPersonaSuffix + AcquaintanceSuffix + PlayerContextSuffix + RepeatAskSuffix + FollowupsInstruction`; recall satırları `NpcMemoryLlmEnvelope.RecallLines(world, DialogMemoryKey, 8)`.
11. **Split + Absorb:** Dönen metin `SplitFollowups` ile `(Body, Followups)`'a bölünür; body varsa `_currentDialogLine`'ı değiştirir + `RecordNpcSaid`; `AbsorbFollowups` en fazla 6 kabarcık kapasitesiyle memo'ya yeni sorular ekler — **W23 growth**.
12. **Guard:** `gen != _conversationSerial || req != _dialogRequestSerial` ise geç cevap düşer; kullanıcı zaten başka birine döndü.
13. **EndConversation:** Serial bump, tüm dialog alanları sıfır, `_conversation = None` — Oracle konuşmasına önceki NPC sızmaz.

## LLD - Veri Modeli (file:line)

- `DomainSimulationAdapter._activeDialogActor : string` — `Adapters/DomainSimulationAdapter.cs:35`
- `DomainSimulationAdapter._activeDialogActorId : ActorId` — `Adapters/DomainSimulationAdapter.cs:36`
- `DomainSimulationAdapter._activeDialogNpcId : NpcId` — `Adapters/DomainSimulationAdapter.cs:37`
- `DomainSimulationAdapter._currentDialogLine : string` — `Adapters/DomainSimulationAdapter.cs:38`
- `DomainSimulationAdapter._currentPortrait : string` — `Adapters/DomainSimulationAdapter.cs:39`
- `DomainSimulationAdapter._conversation : ConversationState` — `Adapters/DomainSimulationAdapter.cs:41`
- `DomainSimulationAdapter._isDialogThinking : bool` — `Adapters/DomainSimulationAdapter.cs:45`
- `DomainSimulationAdapter._topicAskCounts : Dictionary<string,int>` — `Adapters/DomainSimulationAdapter.cs:48`
- `DomainSimulationAdapter._streamingPartialLine : string` — `Adapters/DomainSimulationAdapter.cs:51`
- `DomainSimulationAdapter._suppressGlobalTopicFallback : bool` — `Adapters/DomainSimulationAdapter.cs:56`
- `DomainSimulationAdapter.GeneratedNpcActorOffset : const ulong = 10_000` — `Adapters/DomainSimulationAdapter.cs:59`
- `DomainSimulationAdapter._conversationSerial : int` — `Adapters/DomainSimulationAdapter.Dialog.Binding.cs` (private, near EndConversation)
- `DomainSimulationAdapter._dialogRequestSerial : int` — `Adapters/DomainSimulationAdapter.Dialog.Source.cs`
- `DomainSimulationAdapter._liveOptions : Dictionary<string, List<string>>` — `Adapters/DomainSimulationAdapter.Dialog.Source.cs` (W23 memo)
- `DomainSimulationAdapter.AnyNewsTopic : const = "Any news?"` — `Adapters/DomainSimulationAdapter.Dialog.Source.cs`
- `DomainSimulationAdapter.CompanionJoinTopic / CompanionLeaveTopic : const` — `Adapters/DomainSimulationAdapter.Dialog.Source.cs`
- `ConversationState { ActorId; NpcId; ActorName; Portrait; Topics }` — `Domain/Narrative/ConversationState.cs`
- `ActorMemory.DialogueSeen : IReadOnlyCollection<string>` — `Domain/Memory/ActorMemory.cs:28`
- `ActorMemory.MarkDialogueSeen(string) / HasDialogueSeen(string) / ReplaceDialogueSeen(...)` — `Domain/Memory/ActorMemory.cs:38,44,69`
- `NpcMemoryStore.GetOrCreate(ActorId).RecordEvent(InteractionEvent)` — `Domain/Memory/NpcMemoryStore.cs`
- `DialogStreamText.FollowupsInstruction / SplitFollowups / IsRealFollowup / NaturalQuestion` — `Simulation/AiDm/DialogStreamText.cs`

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

- `IDialogSource GetDialogSource(string actorName)` — `Dialog.Binding.cs` — Legacy: name → `Actors.Records` ile ActorId'ye yükseltir; yoksa NpcSeed name-eşiyle konuşmaya girer.
- `IDialogSource GetDialogSource(ActorId id)` — `Dialog.Binding.cs` — Stabil id ile aktör bağlar; mismatch'te fallback yerine "no one here" honest-empty durum kurar (DLG-01).
- `void BeginConversation(ActorId, NpcId, string actorName, NpcSeedRecord npc)` — `Dialog.Binding.cs` — Serial bump + speaker/portrait/topic seti + deterministik greeting + async LLM greeting başlatma; ilk temas `met_player` memory yazar.
- `void EndConversation()` — `Dialog.Binding.cs` — Serial bump + tüm dialog alanlarını sıfır; in-flight LLM cevapları düşer.
- `static string ResolveConversationPortraitKey(NpcSeedRecord, string)` — `Dialog.Binding.cs` — Role/authored path'e göre `DialogPortraitKey.Normalize` çağrısı.
- `string DeterministicGreeting(string actorName, NpcSeedRecord npc, IReadOnlyList<AskAboutTopic> topics)` — `Dialog.Text.cs` — Social group × time-of-day matrisinden hash-deterministik selam; met_player varsa acquaintance havuzuna geçer; ~%35 gerçek rumor ekler.
- `static int SocialGroupFor(NpcRole)` — `Dialog.Text.cs` — 5 sosyal grup (commoner/merchant/priest/noble/outlaw).
- `static string[] GreetingPool(int group, int slot)` — `Dialog.Text.cs` — Zaman slot'unu lead ve iki komşu ile 3-elemanlı havuz üretir.
- `string ComposeRumor(uint h)` — `Dialog.Text.cs` — Son 32 WorldEvent'ten story-worthy olanları `NarrateEvent` ile cümleye çevirir; en yakın dungeon fısıltısını her rumor roll'una biner (F9).
- `static string NarrateEvent(WorldEvent)` — `Dialog.Text.cs` — WorldEventKind → tek cümleye map (NeedChanged / ChronicleEvent / GuardResponded ...).
- `string StyleDescriptor()` — `Dialog.Text.cs` — WorldProfile.Style enum'unu brand-safe insan cümlesine indirger (BUG-DIALOG-BRAND).
- `static string SanitizeNpcLine(string raw)` — `Dialog.Text.cs` — LLM'in ekrana bastığı chat-turn scaffolding'i (User:/Assistant:/Memory:) ilk marker'da keser; provider-failure imzasında boş döner.
- `static bool LooksLikeLlmProviderFailure(string)` — `Dialog.Text.cs` — "native error:", "llama_decode failed", "invalidinputbatch" imzalarını tanır.
- `static LlmResponse CompleteLlmOrEmpty(ILlmRouter, LlmRequest, Action<string> onPartial)` / `(ILlmRouter, LlmRequest)` — `Dialog.Text.cs` — Streaming overload'a route eder; hata olursa boş yanıt (deterministik satır kalır).
- `Task GenerateNpcGreetingAsync(NpcSeedRecord npc)` — `Dialog.Greetings.cs` — Off-thread LLM selamı; final metin `SanitizeNpcLine` sonrası `_currentDialogLine` + `RecordNpcSaid` (W31: ilk cümle bile hafızaya).
- `Task GenerateAdHocGreetingAsync(string actorName)` — `Dialog.Greetings.cs` — NpcSeed'siz authored aktörler için ikiz; FNV name seed kullanır.
- `Task GenerateNpcTopicAnswerAsync(NpcSeedRecord, string topicId, AskAboutTopic)` — `Dialog.Topics.cs` — Off-thread topic cevabı; `SplitFollowups` sonucu memo'ya `AbsorbFollowups`, body `_currentDialogLine` + `RecordNpcSaid`.
- `Task GenerateAdHocTopicAnswerAsync(string actorName, string topicId, AskAboutTopic)` — `Dialog.Topics.cs` — Name-based ikiz; her `asked` sayacında seed'e twist ekler.
- `ulong VoiceKey { get; }` — `Dialog.Source.cs` — Actor id varsa onu, yoksa `NpcVoiceSignatureService.VoiceKeyFor(name)` döner.
- `string GetCurrentLine()` — `Dialog.Source.cs` — Thinking'te streaming partial > "Thinking…"; final satır null/boş asla dönmez (BUG-DIALOG-EMPTY).
- `bool IsThinking` / `string GetPortraitName()` — `Dialog.Source.cs` — UI okuma seams.
- `IReadOnlyList<string> GetTopics()` — `Dialog.Source.cs` — Live memo (lazy-seed + UNSEEN öncelikli refill) + AnyNewsTopic önek + companion topic append; suppress durumunda boş.
- `void SelectTopic(string topicId)` — `Dialog.Source.cs` — Companion/quest short-circuit; AnyNews rumor; `ConsumeOption` + `MarkDialogueSeen`; grown-followup soruysa `AskFreeText`; katalog cevabı fallback + `WorldEvent(ActorTalked)` + LLM answer fire.
- `void AskFreeText(string question)` — `Dialog.Source.cs` — Serbest metni "free_text:" prefix'iyle sentetik topic haline getirir; NpcSeed varsa seeded, yoksa ad-hoc LLM path.
- `void EnsureLiveOptions(IEnumerable<string> seedTopicIds)` — `Dialog.Source.cs` — Memo yoksa seed'ler; varsa lived state korunur (dönüş konuşmalarında W23 continuity).
- `void ConsumeOption(string picked)` — `Dialog.Source.cs` — Memo'dan çıkar + `MarkDialogueSeen` (W32-pre: consumed = seen).
- `void AbsorbFollowups(List<string> followups)` — `Dialog.Source.cs` — Cap 6; dup guard'lı büyüme.
- `ulong DialogMemoryKey(ulong npcId)` — `Dialog.Source.cs` — **W31 fix**: aktif ActorId varsa onu, yoksa `GeneratedNpcActorOffset + npcId` — okuma/yazma ID paritesi.
- `List<string> RecallDialogMemory(ulong npcId)` / `RecallDialogMemoryByName(string)` — `Dialog.Source.cs` — `NpcMemoryLlmEnvelope.RecallLines(...)` çağırır (son 8 satır).
- `void RecordConversationMemory(ActorRecord actor, string topicId)` — `Dialog.Source.cs` — `MarkDialogueSeen` + `player_asked` olayı yaz.
- `void RecordNpcSaid(string line)` — `Dialog.Source.cs` — **W28**: NPC'nin kendi cümlesi (max 90 karakter) `npc_said` olayı olarak memory'ye yazılır.
- `void SpeakPlayerQuestion(string questionText)` — `Dialog.Source.cs` — `NaturalQuestion` + `PlayerVoiceService` key'i + `SpeechDirector.FeedFinal`.
- `int NextAskCount(string askKey)` / `static string RepeatAskSuffix(int)` — `Dialog.Source.cs` — Aynı soruyu ikinci defa soranın seed'ini/prompt'unu çeşitlendirir.
- `string AcquaintanceSuffix(ulong npcId)` / `string CompanionPersonaSuffix(ulong npcId)` / `string PlayerContextSuffix()` — `Dialog.Source.cs` — Prompt suffix'leri; met_player / companion / player.Name'i modele söyler.
- `void AppendCompanionTopics(List<string>)` / `bool TryHandleCompanionTopic(string)` — `Dialog.Source.cs` — Recruit/dismiss topic'lerini konuşma içinde açar (V3 YOLDAŞ).
- `SiteId FallbackSiteForDialog()` — `Dialog.Source.cs` — "Any news?" için lokal bir Settlement site'ı.
- `static (string, List<string>) SplitFollowups(string)` / `static string NaturalQuestion(string)` — `Simulation/AiDm/DialogStreamText.cs` — REFORM #3 pure text seam; pinlenmiş.

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

**Yazdığı (writer):**
- `World.NpcMemory` — command-driven boundary write (dialog): `BeginConversation` (`met_player`), `RecordConversationMemory` (`player_asked` + `MarkDialogueSeen`), `RecordNpcSaid` (`npc_said`), `ConsumeOption` (`MarkDialogueSeen`). `FieldOwnershipRegistry.cs:105` altında `World.NpcMemory` sadece `living.witness@Hourly:45` tick writer'ı ile listelenir; dialog boundary yazımları lint'ten muaftır (yorumda açık).
- `World.Events` — `SelectTopic` içinde `WorldEvent(WorldEventKind.ActorTalked, actor.Id, "topic_selected id:...")` append eder.
- `World.CompanionIds` — sadece companion topic handler'ı üzerinden `CompanionService.TryRecruit / TryDismiss` çağrısıyla dolaylı yazılır (asıl owner `living.companion_follow@PerTick:21`).

**Okuduğu (reader):**
- `World.NpcMemory` — `DeterministicGreeting`, `AcquaintanceSuffix`, `RecallDialogMemory*`, `GetTopics` refill (`HasDialogueSeen`).
- `World.Actors` — id → ActorRecord (`GetDialogSource`, `SelectTopic`, `AskFreeText`), player'ı bulmak için `CompanionService.FindPlayer`.
- `World.NpcSeeds` — Role/PortraitAssetPath ve seeded-LLM path'i için `NpcId` eşleme.
- `World.Topics` — sadece authored/ad-hoc yolda ve suppress kapalıysa fallback katalog.
- `World.Time`, `World.WorldProfile`, `World.Events` — `DeterministicGreeting` / `ComposeRumor` içinde time-slot ve rumor için.
- `World.Sites` — `FallbackSiteForDialog` "Any news?" için.
- `_world.PlayerClassName` — `SpeakPlayerQuestion` voice key'i.

## LLD - Ürettiği/Tükettiği Olaylar

**Ürettiği (`WorldEvent` / `InteractionEvent`):**
- `WorldEvent(WorldEventKind.ActorTalked, actorId, reason="topic_selected id:{topicId}")` — `SelectTopic` içinde.
- `InteractionEvent("met_player", subjectId=playerName)` — `BeginConversation` (ilk temas).
- `InteractionEvent("player_asked", subjectId=topicId)` — `RecordConversationMemory` (topic seçimi ve free-text).
- `InteractionEvent("npc_said", subjectId=<≤90 char snippet>)` — `RecordNpcSaid` (greeting + topic answer final metinlerinde).

**Tükettiği:**
- `WorldEvent` stream (`WorldEventKind.*`) — `ComposeRumor` / `NarrateEvent` son 32 olayı okur (dungeon reveal + story-worthy narration).
- `NpcMemory.Events` — `AcquaintanceSuffix` ve `DeterministicGreeting` `met_player` sorgusu.
- `NpcMemory.DialogueSeen` — `GetTopics` refill'inin UNSEEN önceliği (W32-pre).
- LLM streaming partial'ları — `_mainThreadApply` kuyruğu üzerinden `_streamingPartialLine`'a bağlanır.

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

- `Assets/Tests/EditMode/AiDm/DialogStreamTextTests.cs` — 5 test; `FollowupsInstruction`, `SplitFollowups`, `IsRealFollowup`, `NaturalQuestion` pinleri (REFORM #3 pure seam adversarial testleri).
- `Assets/Tests/EditMode/Narrative/NpcConversationTests.cs` — 9 test; konuşma başlatma / topic seçim / bind / fallback davranışları.
- `Assets/Tests/EditMode/Narrative/PersistentNpcMemoryTests.cs` — 2 test; NpcMemoryStore round-trip / DialogueSeen persistence.
- `Assets/Tests/EditMode/Narrative/NpcMemoryQueryServiceTests.cs` — `NpcMemoryLlmEnvelope.RecallLines` kapak.
- `Assets/Tests/EditMode/Narrative/DialogueServiceTests.cs` — 2 test; dialog service kontratları.
- `Assets/Tests/EditMode/Ui/DialogPortraitKeyTests.cs` — `DialogPortraitKey.Normalize` (portre key parite).
- `Assets/Tests/EditMode/Ui/DialogCursorPolicyTests.cs` — cursor policy sırasında dialog state.
- `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` — CAN SUYU V2 Gate9: memory'nin LLM prompt'una gerçekten ulaştığı (RecallLines invariantı).
- `Assets/Tests/EditMode/AiDm/LlmEnvelopeTests.cs` — `NpcMemoryLlmEnvelope.Build` seed/system/turns invariantı.
- `Assets/Tests/EditMode/Presentation/PlayableLoopCraftQuestTests.cs` — konuşma sonrası quest interaction end-to-end.
- `Assets/Tests/EditMode/Save/JsonSliceSaveServiceTests.cs` — NpcMemory persistence (DialogueSeen + Events round-trip) save/load katmanında.
- `Assets/Tests/EditMode/Audit/AuditCoverageGapsTests.cs` / `AuditSeventhPassCoverageTests.cs` — Dialog partial'larının audit surface'ta olduğu.

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W23 (baseline, öncesi):** DialogStateMachine v1 — `_liveOptions` memo, `ConsumeOption` / `AbsorbFollowups` / `EnsureLiveOptions`; picked bubble tüketilir, cevabın FOLLOWUPS satırından yeni sorular filizlenir; memo veda edildikten sonra yaşar.
- **W26:** `AnyNewsTopic ("Any news?")` — asla tüketilmez, `RumorMillSystem.PickFor` üzerinden site-lokal deterministik cevap.
- **W28:** `RecordNpcSaid` — NPC'nin kendi satırı `npc_said` olayı olarak NpcMemory'ye düşer; sonraki konuşmada `RecallDialogMemory` her iki tarafı da yankılar ("tek satır hafızası" bug'ı).
- **W31 — Frozen-topic bug kapatıldı:**
  - **Lazy-seed:** `Begin` boş bir memo seed'liyordu (topic'ler henüz yoktu). Artık memo `GetTopics` içinde, topic'ler kesin var olduğunda seed'lenir.
  - **UNSEEN öncelikli refill (W32-pre):** Hepsi tüketildiğinde katalog geri gelmiyordu (`sadece Any News çıkıyor`). `NpcMemory.HasDialogueSeen` sorgusu ile önce görülmemişler sunulur, hepsi görülmüşse tam katalog.
  - **Live re-render:** `ConsumeOption` artık `MarkDialogueSeen` de yapar → tekrar bağlanılan konuşmada memo tutarlı.
- **W31 — Natural questions:** `DialogStreamText.NaturalQuestion` menu label'ı gerçek soruya çevirir; `SpeakPlayerQuestion` bunu `SpeechDirector.FeedFinal` ile oyuncunun sesiyle okur (M3b.3).
- **W31 — Memory ID mismatch fixed:** `DialogMemoryKey(npcId)` — writer'lar (BeginConversation, RecordConversationMemory, RecordNpcSaid) `_activeDialogActorId` (=`GeneratedNpcActorOffset + NpcId`) altına yazıyordu; reader'lar (RecallDialogMemory, AcquaintanceSuffix) çıplak `NpcId` altından okuyordu → generated NPC'ler her konuşmada boş hafıza görüyordu. Artık her iki taraf da aynı key altında.
- **W31 — First-line memory:** `Generate*GreetingAsync` de final metni `RecordNpcSaid` ile yazıyor; ilk cümle bile bir sonraki konuşmanın recall'ına giriyor.
- **W32:** Atlas + INDEX + systems.json + bug scorecard workflow — 20 HLD/LLD dokümanı yayımlandı (bu dosya dahil).
- **W33 (dolaylı):** FieldOwnershipRegistry — `World.NpcMemory` altındaki dialog boundary yazımları lint'ten muaf; `living.witness@Hourly:45` tek tick writer olarak kalır.
- **W36 (2026-07-26):** `ForgeLocator` service locator pattern + `NativeLlmClient` dual-backend architecture — dialog LLM path'i `ForgeLocator.LlmRouter` üzerinden çözünüyor (Dialog.Greetings/Topics ilk çağrı); `LlmRoutingService` streaming overload'ı `CompleteLlmOrEmpty(router, request, onPartial)` içinde koruyor.

## Bilinen Borçlar + Kaçak Kapıları

- **E7-005 legacy name fallback:** `SelectTopic` / `AskFreeText` / `GenerateNpcGreetingAsync` yolları hâlâ NpcSeed'i önce stable NpcId ile, olmazsa isimle çözüyor (`LEGACY name fallback` yorumu, `Dialog.Source.cs`). E7-004 authored-scene actor-ID migration bitince bu şube ölmeli — o zamana kadar duplicate isimlerde yanlış aktöre bağlanma riski açık.
- **Ad-hoc greeting/topic prompt zayıf:** `GenerateAdHocGreetingAsync` `AcquaintanceSuffix` / `CompanionPersonaSuffix` çağırmıyor; authored aktörler için ilk temas hariç acquaintance path yok. Ad-hoc topic path `AcquaintanceSuffix` de eklenmedi (sadece seeded path).
- **`_topicAskCounts` in-memory:** Save/load'a yansımıyor — oyun yeniden başladığında repeat-ask twist sayacı sıfırlanır (kabul edilebilir kirlilik, ama not).
- **Rumor coin-flip:** `ComposeRumor` içinde `(h & 1) == 0` hâlâ event-line'ı yarıya düşürüyor; yorumda "F9: dungeon reveal her rumor roll'una biner" diyor ama event-line coin-flip'i olduğu gibi kalmış — dungeon fısıltısı %100 pass, event narrasyonu ~%50.
- **`SanitizeNpcLine` marker seti sabit:** Marker listesi kod-içinde; farklı bir base model farklı bir chat-turn scaffolding kullanırsa (`Human:`, `<|user|>`) düşmez.
- **`RecordNpcSaid` 90 karakter cap:** Uzun cevaplar özet olmadan kesiliyor; recall'da yarım cümle görünebilir.
- **Companion topic label = topic id:** `CompanionJoinTopic = "companion_join: Travel with me"` panel'de raw string olarak çiziliyor (yorumda kabul: "topic ids doubles as labels"). Yerelleştirme yapılmıyor.
- **`_liveOptions` bellek büyümesi:** Oyuncu çok NPC'yle konuştukça sözlük büyür; run boyunca temizlik yok (kabul edilebilir; save'e yansımaz).
- **`ComposeRumor` deterministik ama last-32-event window'a bağımlı:** WorldEvents akışı yavaşsa aynı fısıltı bir haftaya kadar dolaşabilir; content-freshness watchdog yok.
- **`FieldOwnershipRegistry` boundary write muafiyeti belgesel:** `World.NpcMemory` altında dialog writer'ları lint'e görünmüyor — yeni bir dialog writer eklenirse test yerine yalnızca kod-review yakalar.

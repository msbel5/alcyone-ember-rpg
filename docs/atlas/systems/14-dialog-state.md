# 14-dialog-state

Diyalog durum makinesi + render katmanı: `DomainSimulationAdapter.Dialog.*` partial'ları (Binding /
Source / Topics / Greetings / Text), `DialogStreamText` (saf metin kuralları), `ConversationState` +
`NpcTopicCatalog` (domain modeli) ve iki UI yüzeyi: yeni `DialogView` (UI Toolkit, birincil) ile
legacy `DialogBoxPanel` (uGUI, fallback). Tüm satırlar koddan doğrulandı; doğrulanamayanlar
`dogrulanmadi` etiketli.

## HLD - Ne ve Neden

Amaç: NPC ile konuşmak "global menü gezmek" değil "bir insanla konuşmak" hissi versin (EMB-045,
`NpcTopicCatalog.cs:6-13`). Mimari iki katman: her zaman ÇALIŞAN deterministik iskelet (rol+saat
selamlaşma matrisi, topic'lerin yazılı cevapları — `Dialog.Text.cs:28-91`) + onu yalnızca boş-olmayan
gerçek metin dönerse DEĞİŞTİREN yerel LLM zenginleştirmesi (`Dialog.Greetings.cs:68-71`,
`Dialog.Topics.cs:62-67`). W23 "DIALOG STATE MACHINE v1 (GemRB DLG skeleton + LLM content)"
(`Dialog.Source.cs:139-141`): NPC başına CANLI seçenek listesi — seçilen balon TÜKENİR
(`ConsumeOption`, `:160-164`), cevabın ürettiği FOLLOWUPS yeni balon olarak BÜYÜR
(`AbsorbFollowups`, `:166-173`) ve memo vedalaşmadan SONRA da yaşar (konuşmaya dönünce kaldığın
yerden devam). "Any news?" hiç tükenmeyen sabit balondur, RumorMill'den anında deterministik cevap
verir (W26, `:64,294-304`). Konuşmalar DENEYİMDİR: `met_player` / `player_asked` / `npc_said`
satırları NpcMemory'ye yazılır ve `RecallDialogMemory` bir sonraki prompt'a son 8 satırı geri
taşır (CAN SUYU V2.1, `:87-100`; Gate9 bunu kanıtlar). Oyuncuya görünen yüz: "Ask about X"
balonları, akışta damlayan streaming cevap satırı, portre, kendi sesinle SÖYLENEN soru (M3b.3,
`:128-137`) ve serbest metin sorma kutusu. Felsefe: UI hiçbir zaman boş/bayat satır göstermez
(BUG-DIALOG-EMPTY zinciri), bayat asenkron cevap hiçbir zaman ekrana sızmaz (çift serial guard).

## HLD - Akis

Kadans: tick-sistemi DEĞİL — oyuncu tıklamasıyla tetiklenen olay-güdümlü akış; tek tick bağı,
off-thread LLM sonuçlarının `AdvanceTick` başında drain edilmesidir.

1. **Bağlanma:** E tuşu → `EmberPlayerInteractRaycaster.OpenDialog` (`:123-179`).
   `InGameUiController` sahnedeyse yeni yol: `commands.GetDialogSource(ActorId|name)` +
   `inGameUi.OpenNpcDialog(...)` (`:136-155`); yoksa legacy `DialogBoxPanel.Source = ...`
   (`:158-166`).
2. **Çözümleme (DLG-01):** `GetDialogSource(ActorId)` id-first çözer
   (`Dialog.Binding.cs:38-78`); aktör bulunamazsa SESSİZCE global menüye düşmek yerine
   `_suppressGlobalTopicFallback=true` + "There is no one here to talk to." + LogWarning
   (`:41-57`). Üretilmiş NPC'nin seed'i `GeneratedNpcActorOffset` çıkarılarak geri bulunur
   (`:59-69`). Düşman aktörde konuşma değil dünya-karşılaşması açılır
   (`TryBeginWorldEncounter`, `:71-74`). String overload önce adı stabil id'ye yükseltmeyi
   dener (`:21-35`).
3. **BeginConversation hunisi (tek funnel):** `Dialog.Binding.cs:88-165`.
   `_conversationSerial++` (uçuştaki eski cevabı geçersiz kılar, `:90,167-172`), portre çözümü
   (`:95,190-212`), `EnsureLiveOptions` (W23 memo tohumu, `:96-99`), İLK konuşmada oyuncu adıyla
   `met_player` memory satırı (`:101-118`), topic seti = quest-etkileşim topic'leri +
   `NpcTopicCatalog.For(role, faction)` + 2 paylaşılan dünya topic'i (`:120-131`,
   `Dialog.QuestInteraction.cs:12-26`, `NpcTopicCatalog.cs:19-49`), deterministik açılış satırı
   (`:139`), sonra fire-and-forget LLM selamlaması (`:141`; seed'siz aktörler için ad-tabanlı
   ikiz, `:143-164`).
4. **UI açılışı:** `InGameUiController.OpenNpcDialog` (`:488-520`) `GetTopics()` SNAPSHOT'ını
   alır, her id'yi `HumanizeToken` ile etikete çevirir (`:500,1043-1058`) ve `DialogView`
   kurar (`:504-513`). Butonlar "N. Ask about {Label}" (etiket "?" içeriyorsa "N. {Label}") —
   **ekrandaki "Ask about X" metninin kaynağı `DialogView.cs:308-310`** (legacy panelde
   `DialogBoxPanel.cs:182`, ham topic ID ile).
5. **Topic listesi okuması:** `GetTopics` (`Dialog.Source.cs:50-79`): memo boşsa TAM BURADA
   lazily yeniden tohumlanır ("sadece any news cikiyor" canlı bug yaması, `:53-60`),
   "Any news?" 0. sıraya eklenir (`:64`), yol arkadaşı topic'leri sona
   (`AppendCompanionTopics`, `:253-262`). DLG-01 miss'inde dürüst BOŞ liste (`:76-77`).
6. **SelectTopic zinciri:** `Dialog.Source.cs:281-358`. Sıra: companion join/leave
   (`:291,264-279`) → quest etkileşimi (`:292`, `Dialog.QuestInteraction.cs:28-40`) →
   "Any news?" → `RumorMillSystem.PickFor` anında cevap (`:296-304`,
   `RumorMillSystem.cs:77`) → `ConsumeOption` (balon patlar, `:305`) → katalogda olmayan ve
   "?" içeren seçenek bir FOLLOWUP'tır, serbest-metin yoluna yönlenir (`:309-315`) →
   deterministik cevap (`AskAboutTopic.Answer` ya da "considers...", `:317-323`) →
   `ActorTalked` WorldEvent + `player_asked` memory (`:330-339`) → id-first NPC çözümü ile
   asenkron LLM topic cevabı (`:349-357`; ad fallback'i E7-004 ile ölecek LEGACY, `:344-353`).
7. **Asenkron cevap yolu:** `GenerateNpcTopicAnswerAsync` (`Dialog.Topics.cs:21-71`):
   `gen=_conversationSerial` + `req=++_dialogRequestSerial` yakalanır (en-son-istek-kazanır,
   `:28-29`), tekrar sorularda ask-sayacı seed'e ve prompt'a katılır (`:32,40`;
   `NextAskCount`/`RepeatAskSuffix`, `Dialog.Source.cs:209-219`), soru oyuncunun SESİYLE
   okunur (`:33`), prompt = persona + hafıza kaydı + `PlayerContextSuffix` +
   `FollowupsInstruction` (`:41-42`). Streaming partial'lar WORKER thread'den gelir,
   `_mainThreadApply` kuyruğuna marshal edilir ve serial'lar yeniden kontrol edilir (`:48-52`).
   Sonuç: `SanitizeNpcLine` (turn-leak kırpma) + `SplitFollowups`; boş-olmayan gövde satırı
   değiştirir + `npc_said` olarak NPC'nin KENDİ hafızasına yazılır (W28, `:62-67`),
   followup'lar memoya büyür (`:68`). Ad-tabanlı ikiz: `:76-128` (FNV ad+topic seed'i,
   `:88-91`).
8. **Tick drain:** `AdvanceTick` her main-thread tick başında `DrainMainThreadApply` çağırır
   (`Dialog...Clock.cs:6-30`) — DET-02: off-thread yazımlar deterministik ana thread'de,
   sırayla uygulanır; exception tick'i asla kırmaz (`:27-28`).
9. **Render döngüsü (birincil):** `InGameUiController` her frame (`:147-179`)
   `GetCurrentLine()`'ı `DialogView.SetCurrentLine`'a basar; thinking iken satır büyüyen
   streaming metnidir (`Dialog.Source.cs:31-45`) ve son loading balonuna da akıtılır
   (`UpdateLatestLoading`, `DialogView.cs:192-203`); `SpeechDirector.FeedPartial/FeedFinal`
   NPC sesini besler (`:170-173`); thinking bitince bekleyen balon `ResolveLatestResponse`
   ile çözülür (`:174-175`, `DialogView.cs:229-243`). Serbest metin / topic tıklaması
   `BeginQuestion` ile sağa yaslı soru balonu açar (`DialogView.cs:216-227,302-305,399-411`).
10. **Render döngüsü (legacy):** `DialogBoxPanel.Update` (`:110-202`): typewriter efekti,
    "Thinking..." nokta animasyonu, HER FRAME `GetTopics()` (canlı liste burada gerçekten
    canlı render edilir, `:177-188`), 1-9 sayı tuşları (`:190-196`), portre lookup'ı
    (`:162-175,204-216`).
11. **Kapanış:** her kapanma yolu `CloseScreen` → `EndConversation`
    (`InGameUiController.cs:469-477`; `Dialog.Binding.cs:178-188`): serial++, konuşma
    durumu sıfırlanır — Oracle'a sızma (bleed) kapanır. `_liveOptions` memosu BİLEREK
    silinmez (W23 resume).
12. **Host proxy:** `EmberWorldHost` da bir `IDialogSource`'tur; adapter varsa her çağrıyı
    ona forward eder, yoksa {work/trade/fate} kalıp cevaplarına düşer
    (`EmberWorldHost.DialogProxy.cs:9-103`).

## LLD - Veri Modeli

| Tip / Alan | İçerik | Kaynak |
|---|---|---|
| `ConversationState` | `ActorId`, `NpcId`, `ActorName`, `Portrait`, `Topics:IReadOnlyList<AskAboutTopic>`; immutable; `None` sentinel; `FindTopic(id)` yalnız BU aktörün sunduğu topic'i döner | `ConversationState.cs:16-58` |
| `AskAboutTopic` | `Id`, `Label`, `Answer` — üçü de zorunlu (ctor throw) | `AskAboutTopic.cs:11-30` |
| `DialogTopicOption` | UI tarafı (id, label) çifti — readonly struct | `DialogView.cs:11-21` |
| `_liveOptions` | `Dictionary<string, List<string>>` — W23 memosu; anahtar `"id:{actorId}"` ya da `"name:{actor}"` (`ActiveMemoKey`); değer canlı topic-id listesi (tüketilenler çıkar, followup'lar girer, tavan 6) | `Dialog.Source.cs:139-158,166-173` |
| `_topicAskCounts` | `Dictionary<string,int>` — `"id:npc\|topic"` → kaç kez soruldu; seed + prompt varyasyonu | `DomainSimulationAdapter.cs:45-48`, `Dialog.Source.cs:209-214` |
| Adapter diyalog alanları | `_activeDialogActor/_activeDialogActorId/_activeDialogNpcId`, `_currentDialogLine`, `_currentPortrait`, `_conversation`, `_isDialogThinking`, `_streamingPartialLine`, `_suppressGlobalTopicFallback` | `DomainSimulationAdapter.cs:35-55` |
| Yarış bekçileri | `_conversationSerial` (konuşma başına, `Dialog.Binding.cs:167-172`), `_dialogRequestSerial` (İSTEK başına — aynı konuşmada yavaş inference'ın yeniyi ezmesini kapatır) | `Dialog.Source.cs:81-85` |
| `_mainThreadApply` | `ConcurrentQueue<Action>` — off-thread LLM devamlılıklarının tek geçidi | `Dialog...Clock.cs:20-21` |
| Memory satırları | `InteractionEvent(EventType=...)`: `met_player` (SubjectId=oyuncu adı, `Dialog.Binding.cs:113-116`), `player_asked` (SubjectId=topicId, `Dialog.Source.cs:110-111`), `npc_said` (SubjectId=90 karaktere kırpılmış replik, `:180-188`); recall formatı `"day N: {EventType} — {SubjectId}"` | `MemoryRecallService.cs:62-82` |
| Sabitler | `AnyNewsTopic="Any news?"` (`Dialog.Source.cs:175`), `CompanionJoinTopic="companion_join: Travel with me"` / `CompanionLeaveTopic` (`:250-251`), `FollowupsInstruction` (`DialogStreamText.cs:13-15`), followup tavanı 6 (`Dialog.Source.cs:171`), recall derinliği 8 (`:91,99`), token bütçeleri: greeting 100 / topic 180 (`Dialog.Greetings.cs:34`, `Dialog.Topics.cs:38`) |  |
| `IDialogSource` | `VoiceKey`, `GetCurrentLine`, `GetTopics`, `SelectTopic`, `AskFreeText`, `EndConversation`, `IsThinking` (son üçü default'lu); `IDialogSourcePortrait += GetPortraitName` | `IDialogSource.cs:12-46` |
| `DialogDefDto` ailesi | GemRB-tarzı YAZARLI durum makinesi DTO'ları (state/transition/condition/action) — content'ten yüklenir ama runtime tüketicisi YOK (borçlara bak) | `DialogHistoryDtos.cs:5-44`, `ContentDatabase.Loader.cs:37` |

## LLD - Fonksiyon Haritasi

| İmza | Konum | Ne yapar |
|---|---|---|
| `GetDialogSource(ActorId) : IDialogSource` | `Dialog.Binding.cs:38` | Id-first bağlama; miss'te DLG-01 sesli boş durum; düşmanda encounter'a sapar. |
| `GetDialogSource(string) : IDialogSource` | `:21` | Adı stabil id'ye yükseltmeyi dener; olmazsa ad-tabanlı bağlar. |
| `BeginConversation(actorId, npcId, name, npc)` | `:88` | Tek funnel: serial++, portre, memo tohumu, met_player, topic seti, deterministik satır, async selamlama. |
| `EndConversation()` | `:178` | Serial++ + tüm konuşma durumunu sıfırlar; memo kalır. |
| `ResolveConversationPortraitKey(npc, name)` | `:190` | PortraitAssetPath → rol eşlemesi → ad; hep portre-özel anahtar. |
| `GetCurrentLine() : string` | `Dialog.Source.cs:31` | Thinking'de streaming partial + " …" ya da "X thinks…"; değilse asla boş olmayan `_currentDialogLine`. |
| `GetTopics() : IReadOnlyList<string>` | `:50` | Canlı memo (lazy reseed) + AnyNews@0 + companion topic'leri; DLG-01'de boş liste. |
| `SelectTopic(string topicId)` | `:281` | Akış madde 6'daki tam zincir. |
| `AskFreeText(string question)` | `:360` | `"free_text:{q}"` id'siyle sentetik topic; memory yazar; LLM'e yönlenir. |
| `EnsureLiveOptions(seed)` / `ConsumeOption` / `AbsorbFollowups` | `:153/:160/:166` | W23 memo yaşam döngüsü (var-olanı korur / siler / ≤6 büyütür). |
| `RecallDialogMemory(npcId)` / `RecallDialogMemoryByName(name)` | `:90/:93` | `NpcMemoryLlmEnvelope.RecallLines(world, actorId, 8)` — Gate9'un pinlediği TEK kanonik kaynak (`NpcMemoryLlmEnvelope.cs:36-43`). |
| `RecordConversationMemory(actor, topicId)` | `:103` | `MarkDialogueSeen` + `player_asked` satırı. |
| `RecordNpcSaid(line)` | `:180` | NPC'nin kendi repliği (≤90 kr) `npc_said` olarak kendi hafızasına (W28). |
| `SpeakPlayerQuestion(text)` | `:128` | `NaturalQuestion` + `PlayerVoiceService.PlayerVoiceKey` ile `SpeechDirector.FeedFinal`. |
| `AcquaintanceSuffix` / `CompanionPersonaSuffix` / `PlayerContextSuffix` / `RepeatAskSuffix` | `:116/:242/:223/:216` | Prompt ekleri: tanışıklık (adıyla), yoldaş sesi, oyuncu adı, tekrar-soru varyasyonu. |
| `TryHandleCompanionTopic(topicId)` | `:264` | `CompanionService.TryRecruit/TryDismiss` + deterministik cevap satırı. |
| `GenerateNpcGreetingAsync(npc)` / `GenerateAdHocGreetingAsync(name)` | `Dialog.Greetings.cs:21/:78` | Seed'li / ad-tabanlı selamlama; çift-serial guard; sanitize; boşsa deterministik satır kalır. |
| `GenerateNpcTopicAnswerAsync(npc, topicId, topic)` / `GenerateAdHocTopicAnswerAsync(name, ...)` | `Dialog.Topics.cs:21/:76` | Topic cevabı + followup emilimi + npc_said; akış madde 7. |
| `DeterministicGreeting(name, npc, topics)` | `Dialog.Text.cs:28` | Sosyal-grup × günün-saati matrisi (`:108-122`), tanışıklık havuzu (`:69-83`), %35 dedikodu bindirmesi (`:85-89,127-155`). |
| `SanitizeNpcLine(raw)` / `LooksLikeLlmProviderFailure` | `:225/:247` | Turn-leak marker'larında kes; provider hata metnini boşa indir. |
| `StyleDescriptor()` | `:200` | Marka/kod-adı temizliği (BUG-DIALOG-BRAND) → insan-okur tarz betimi. |
| `CompleteLlmOrEmpty(router, req[, onPartial])` | `:258` | Streaming overload varsa onu; exception'da boş cevap + `[DialogLLM]` warn. |
| `DialogStreamText.SplitFollowups` / `IsRealFollowup` / `NaturalQuestion` | `DialogStreamText.cs:20/:41/:54` | REFORM #3 saf kurallar; W28 papağan-cevap reddi; etiket→konuşma cümlesi. |
| `InGameUiController.OpenNpcDialog(src, name, portrait)` | `InGameUiController.cs:488` | Snapshot topic'lerle DialogView kurulumu; per-frame poll `:147-179`; kapanış `:469-484`. |
| `DialogView.BuildTopicButton(index, topic, onTopic)` | `DialogView.cs:300` | **"Ask about X" buton metni** (`:308-310`) + tıklamada soru balonu (`:304`). |
| `DialogBoxPanel.Update()` | `DialogBoxPanel.cs:110` | Legacy: typewriter, Thinking-noktaları, per-frame topics ("Ask about {id}", `:182`), sayı tuşları. |
| `EmberWorldHost.GetCurrentLine/GetTopics/SelectTopic/AskFreeText` | `EmberWorldHost.DialogProxy.cs:9/:36/:70/:93` | Adapter'a şeffaf forward; adaptersiz kalıp cevaplar. |
| `AddQuestInteractionTopics` / `TryHandleQuestInteractionTopic` | `Dialog.QuestInteraction.cs:12/:28` | Quest topic'leri listenin başına; seçimde LLM'siz deterministik quest cevabı. |

## LLD - Yazdigi/Okudugu Alanlar

FieldOwnershipRegistry diliyle: bu sistemin HİÇBİR yazısı ledger'da DEKLARE DEĞİL
(`FieldOwnershipRegistry.cs:15-53` — diyalog/NpcMemory/Events satırı yok). Yazılar tick-sistemi
kimliği taşımaz; hepsi olay-güdümlü (oyuncu tıklaması ana thread'de, LLM devamlılıkları
`_mainThreadApply` drain'inde). Guard-pursuit sınıfı "deklare edilmemiş yazar" ailesiyle aynı
kategori; çok-yazarlı çatışma bugün pratikte yok çünkü hepsi ana thread'e serileştirilmiş.
((a)-(g) aile harfi eşlemesi repo'da bulunamadı — dogrulanmadi.)

**Yazdıkları:**
- `World.NpcMemory` — `GetOrCreate` + `RecordEvent(met_player)` (`Dialog.Binding.cs:106-117`),
  `MarkDialogueSeen` + `RecordEvent(player_asked)` (`Dialog.Source.cs:106-111`),
  `RecordEvent(npc_said)` (`:184-187`). `NpcMemoryStore` null'sa burada MİNT edilir (`:106`).
- `World.Events` — append: `ActorTalked` `"topic_selected id:{topicId}"` (`Dialog.Source.cs:332-337`).
- `World.CompanionIds` — dolaylı: `CompanionService.TryRecruit/TryDismiss`
  (`Dialog.Source.cs:271-277`).
- Adapter-yerel diyalog alanları (sim-dışı): `_currentDialogLine`, `_isDialogThinking`,
  `_streamingPartialLine`, `_conversation`, `_liveOptions`, `_topicAskCounts`, iki serial.
- `SpeechDirector` statik beslemeleri (presentation, `Dialog.Source.cs:134-136`,
  `InGameUiController.cs:170-173`).

**Okudukları:** `World.Actors` (çözümleme + oyuncu bulma), `World.NpcSeeds` (persona/rol/faction),
`World.Topics` (paylaşılan dünya topic'leri), `World.NpcMemory` (recall + acquaintance),
`World.Time` (selamlaşma slotu, memory damgası), `World.Events.Events` (selamlaşma dedikodusu,
`Dialog.Text.cs:135-146`), `World.Rumors` (AnyNews → `RumorMillSystem.PickFor`),
`World.CompanionIds` (`Dialog.Source.cs:260`), `World.Sites.Records` (`FallbackSiteForDialog`,
`:190-198`), `World.WorldProfile.Style` (`Dialog.Text.cs:202`), `World.PlayerClassName`
(`Dialog.Source.cs:135`), quest durumu (`QuestInteractionService` üzerinden).

## LLD - Urettigi/Tukettigi Olaylar

**Üretilen:**
- `WorldEventKind.ActorTalked` — reason `"topic_selected id:{topicId}"`
  (`Dialog.Source.cs:332-337`). Aşağı akış tüketicisi: `PublishEventEchoes` chat piktogramı
  (`Dialog...Clock.cs:66-76` civarı, bkz. 07-history-rumors).
- NpcMemory `InteractionEvent` tipleri: `met_player`, `player_asked`, `npc_said` (yukarıda).
- Log tag'leri: `[NpcGreeting]` (`Dialog.Greetings.cs:60`), `[NpcGreeting-adhoc]` (`:113`),
  `[NpcTopic-adhoc]` (`Dialog.Topics.cs:115`), `[DialogLLM]` provider-hata warn'ı
  (`Dialog.Text.cs:269,279`), `"DLG-01: ..."` çözümleme-miss warn'ı (`Dialog.Binding.cs:53-55`).

**Tüketilen:**
- `World.Rumors` — `RumorMillSystem.PickFor` (AnyNews cevabı, `Dialog.Source.cs:298-301`).
- `World.Events` — `ComposeRumor`/`NarrateEvent` selamlaşma bindirmesi
  (`Dialog.Text.cs:127-193`).
- NpcMemory satırları — `NpcMemoryLlmEnvelope.RecallLines(world, actorId, 8)` prompt turn'leri
  olarak (`Dialog.Source.cs:90-100`).
- LLM cevabındaki `FOLLOWUPS:` protokol satırı — `SplitFollowups` ile balonlara
  (`DialogStreamText.cs:20-39`).

## Testler

- `Assets/Tests/EditMode/AiDm/DialogStreamTextTests.cs` — W28 pin'i: instruction-parrot cevap
  ekrana/balona ASLA (`:11-18`), sağlıklı split (`:21-29`), marker'sız passthrough (`:32-37`),
  `IsRealFollowup` red/kabul (`:40-46`), `NaturalQuestion` (`:49-55`).
- `Assets/Tests/EditMode/Narrative/NpcConversationTests.cs` — EMB-020/045: rol başına farklı
  topic'ler (`:16-25`), faction topic'i (`:36-42`), paylaşılan topic sıralaması (`:45-60`),
  determinizm (`:63-68`), `FindTopic` yalnız sunulanı cevaplar (`:71-80`), stabil id taşıma
  (`:83-104`).
- `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` — **Gate9**: tanık olunan saldırı,
  canlı diyalog yolunun çağırdığı AYNI `RecallLines` ile prompt'a ulaşmak ZORUNDA (`:253-272`).
- `Assets/Tests/EditMode/AiDm/NpcMemoryLlmEnvelopeTests.cs` — envelope/recall sözleşmesi.
- `Assets/Tests/EditMode/Presentation/PlayableLoopCraftQuestTests.cs` — quest topic'leri
  `GetDialogSource` + `SelectTopic` üzerinden uçtan uca (`:35-88,438-467`).
- `Assets/Tests/EditMode/Ui/DialogPortraitKeyTests.cs` — portre anahtarı normalizasyonu.
- `Assets/Tests/EditMode/Narrative/DialogueServiceTests.cs` — yalnız template substitution
  (`:15,23`); durum makinesiyle ilgisi yok.
- **Pinsiz çekirdek:** `_liveOptions` yaşam döngüsü (consume/absorb/resume), AnyNews'in
  tükenmezliği, `_dialogRequestSerial` yarışı, ask-count varyasyonu, `RecordNpcSaid`,
  `SanitizeNpcLine`, `DeterministicGreeting` matrisi ve İKİ render katmanının tamamı için test
  YOK (Assets/Tests grep'i: `SplitFollowups|AnyNews|_liveOptions|EnsureLiveOptions|RecordNpcSaid|
  npc_said|RecallDialogMemory` yalnız `DialogStreamTextTests.cs`'te eşleşti).

## Bilinen Borclar + Kacak Kapilari

1. **W23 durum makinesi BİRİNCİL UI'da canlı render edilmiyor.** `DialogView`'in topic paneli
   ctor'da BİR KEZ kurulur (`DialogView.cs:129-133`); `InGameUiController` per-frame poll'u
   yalnız satır/balon/portre günceller (`:162-179`) — `GetTopics`'i bir daha OKUMAZ. Yani
   tüketilen balon ekranda patlamaz, cevabın büyüttüğü FOLLOWUPS balonları konuşma
   KAPANIP YENİDEN AÇILANA kadar görünmez. Makinenin tam davranışı yalnız fallback
   `DialogBoxPanel`'de görünür (her frame `GetTopics`, `:177-188`). "The answer grows the next
   bubbles" vaadinin görsel yarısı birincil yolda askıda.
2. **`EnsureLiveOptions` bayat `_conversation`'dan tohumlanıyor.** `BeginConversation` memoyu
   YENİ aktörün anahtarıyla ama ESKİ `_conversation.Topics`'ten tohumlar (`Dialog.Binding.cs:96-99`
   — `_conversation` ancak `:126/:148`'de değişir). Yeni-UI yolunda zararsız (OpenNpcDialog →
   CloseScreen → EndConversation `_conversation=None` yapar, `InGameUiController.cs:491,475`);
   ama `EndConversation` ÇAĞIRMAYAN legacy raycaster yolunda (`EmberPlayerInteractRaycaster.cs:158-166`)
   A'dan B'ye geçişte B'nin ilk memosu A'nın topic id'leriyle doğabilir ve `ContainsKey` bekçisi
   (`Dialog.Source.cs:155`) bunu kalıcılaştırır. Runtime'da tetiklendiği gözlemlenmedi —
   dogrulanmadi; sınıf olarak "sadece any news cikiyor" bug'ının kardeşi.
3. **Memo ve ask-sayacı save'e yazılmıyor.** `_liveOptions` / `_topicAskCounts` adapter-instance
   alanları (`Dialog.Source.cs:142-143`, `DomainSimulationAdapter.cs:47-48`); load sonrası
   tüketilmiş balonlar geri gelir, tekrar-soru varyasyonu sıfırlanır. "Memo survives farewell"
   yalnız oturum içi doğru.
4. **İki render katmanı, üç etiket gerçeği.** Yeni UI etiketi `HumanizeToken(id)`'dir
   (`InGameUiController.cs:500`) — yazarlı `AskAboutTopic.Label` ("the watch") HİÇ kullanılmaz,
   ekranda "Ask about Watch" görünür; legacy panel ham ID basar ("Ask about watch",
   `DialogBoxPanel.cs:182`); LLM'e söylenen soruysa `topic.Label`'dır
   (`Dialog.Topics.cs:30,85`). Companion topic id'si etiket olarak da kullanılır ve
   HumanizeToken onu "Companion Join: travel with me" diye bozar (`:1046-1057` split kuralı).
   Tek kaynak yok; üç yüzey ayrı ayrı sürükleniyor.
5. **Yazarlı DLG içerik makinesi ölü veri.** GemRB-tarzı `DialogDefDto` state/transition ağacı
   content'ten yüklenir (`ContentDatabase.Loader.cs:37`) ve yalnız katalog testi pinler
   (`ContentDatabaseSystemsCatalogTests.cs:15,39`); runtime makinesi tamamen `_liveOptions` +
   LLM. `dialog_defs.json` emeği şu an hiçbir konuşmaya akmıyor.
6. **Selamlaşma yolunda `SplitFollowups` yok.** Topic cevabı FOLLOWUPS protokolünden geçer ama
   greeting yalnız `SanitizeNpcLine`'dan geçer (`Dialog.Greetings.cs:68`); prompt'u istemese de
   zayıf model W28-tarzı parrot yaparsa "FOLLOWUPS: ..." satırı selamlaşma olarak EKRANA basılır.
   Adversarial pin yalnız `SplitFollowups` fonksiyonunda var, greeting yolunda yok.
7. **Legacy panel streaming'i gizliyor.** `DialogBoxPanel.Update` `IsThinking` iken
   `GetCurrentLine`'ı hiç okumaz, "Thinking..." noktalarını basar (`DialogBoxPanel.cs:117-121`) —
   M3a streaming partial'ı (`Dialog.Source.cs:33-38`) fallback yolda hiç görünmez.
8. **Ad-tabanlı (aktörsüz) konuşmalarda hafıza tek yönlü.** `RecordNpcSaid` boş
   `_activeDialogActorId`'de sessizce çıkar (`Dialog.Source.cs:182`), `RecallDialogMemoryByName`
   aktör kaydı yoksa boş liste döner (`:95-99`) — gerçek ad-hoc konuşucu ne söylediğini hatırlar
   ne hatırlanır. E7-004 aktör-id göçü bitince sınıf kapanacak (aynı dosyada LEGACY notu,
   `:344-353`).
9. **Followup yönlendirme sezgisel.** Katalogda olmayan + "?" içeren her seçenek serbest-metin
   sayılır (`Dialog.Source.cs:309-315`); "?" içeren bir katalog id'si (bugün yalnız AnyNews, o da
   önce yakalanıyor `:296`) ya da "?"siz kalmış bir followup yanlış kola düşer. `IsRealFollowup`
   "?" şartı (`DialogStreamText.cs:44`) bugünkü üretimde bunu kapatıyor — sözleşme değil,
   tesadüfi hizalama.
10. **Tanılama asimetrisi.** Ad-hoc selamlama/cevap yolları `llm-len` diag logu basar
    (`Dialog.Greetings.cs:113`, `Dialog.Topics.cs:115-116`), seed'li `GenerateNpcTopicAnswerAsync`
    basmaz — sahada "LLM ateşlemiyor" avında seed'li yol kör. Greeting'deki log da "Remove once
    confirmed" notuyla kalıcılaşmış (`Dialog.Greetings.cs:57-59`).
11. **`_mainThreadApply` drain'i sadece `AdvanceTick` çağrılırsa akar.** Tick'i süren host
    durursa (pause/başka sahne) kuyruğa yazılmış cevaplar uygulanmaz, `_isDialogThinking`
    true'da asılı kalabilir (`Dialog...Clock.cs:6-30`; serial bekçileri sızıntıyı değil yalnız
    bayatlığı kapatır). Pause sırasında tick davranışı: dogrulanmadi.
12. **Ledger sessizliği.** `World.NpcMemory`/`World.Events`/`World.CompanionIds` diyalog
    yazarları `FieldOwnershipRegistry`'de deklare değil (yukarıdaki bölüm) — bugün tek-thread
    fiilen güvenli, ama "deklare edilmemiş yazar CI olayı olur" vaadinin (`:8-10`) kapsamı
    dışında yaşıyorlar.

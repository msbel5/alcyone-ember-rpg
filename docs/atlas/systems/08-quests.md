# 08-quests

> Kapsam: quest tick (QuestSystem), quest kaynagi/uretimi (QuestCatalog + WorldQuestGenerator), tamamlama yollari, HUD quest satiri (guidance/compass/delve), delve hedefleri (MainQuest uc-perde omurgasi).
> Kanit disiplini: her iddia file:line ile. Emin olunamayan yerler "dogrulanmadi" olarak isaretli.

## HLD - Ne ve Neden

Oyunda tek bir "quest sistemi" yok; **dort ayri quest ailesi** yan yana yasiyor ve her birinin kendi durum deposu, kendi tamamlanma yolu var:

1. **Katalog questleri (kernel)** — DFU-tarzi deterministik state machine: `QuestDefinition` (sablon) + `QuestState` (calisma durumu) + kosul/aksiyon nesneleri. Su an katalogda **tek quest** var: "Forge an Iron Ingot" (`Assets/Scripts/Data/Quest/QuestCatalog.cs:12-38`). Saatlik dunya tick'inde `QuestSystem.Tick` ile ilerler.
2. **Sabit dunya questleri (F2)** — Outlaw Bounty (kill, id 9001) ve Shrine Pilgrimage (visit, id 9002). Katalog DISINDA, `WorldState.WorldQuestStates` sozlugunde ham `ulong` anahtariyla yasarlar; tick'leri yok, tamamlama olay-tetiklidir (`DomainSimulationAdapter.WorldQuests.cs:9-28`).
3. **Uretilen kontratlar (F21 "gorev makinesi")** — `WorldQuestGenerator` NPC/yerlesim verisinden deterministik tek quest uretir (Fetch/Kill/Deliver/Visit); `WorldState.WorldContracts` listesinde yasar, jurnalde "Contracts" bolumu olarak gorunur.
4. **Ana quest (F31)** — `MainQuestState` uc-perde omurgasi: delve'lerden yazit parcalari → baskentin bilgesi → final delve'in Warden'i. Delve hedefleri bu ailenin isi.

Felsefe: kernel `QuestStore` bilerek katalog-only tutulmus ("the F2 lesson", `WorldState.cs:147-149`) cunku `QuestSystem.Tick` her aktif quest icin `QuestCatalog.Resolve` cagirir ve katalogda olmayan id **exception firlatir** (`QuestCatalog.cs:50`). Oyuncuya gorunen yuzey: HUD'daki quest/pusula/delve satirlari, jurnal ekrani, NPC diyalog konulari ve final perdesi (finale overlay).

## HLD - Akis

### A. Katalog questi (Forge an Iron Ingot) — tam dongu
1. **Baslatma (oyuncu tetikler):** Demirciyle konusma → `QuestInteractionService.BuildTopics` "forge_work" konusunu ekler (`QuestInteractionService.cs:23-36`); konu secilince `TryStartForgeQuest` cevheri+yakiti envantere koyar, `world.Quests.Add(...)` ile QuestState acar, `QuestStarted` olayi yazar (`QuestInteractionService.cs:96-114`). `world.Quests.Add` in tek cagri yeri burasi (grep ile dogrulandi).
2. **Tick (saatlik):** `QuestStep` id `"quest.tick"`, `TickCadence.Hourly`, order 15 (`DefaultTickSystems.cs:236-250`, kayit satir 46). `QuestSystem.Tick` aktif questleri gezer, kosullar saglaninca task'lar tetiklenir (`QuestSystem.cs:14-53`).
3. **Aninda yeniden degerlendirme:** Craft basarili olunca `ReevaluateQuestProgress()` ayni `QuestSystem`'i hemen calistirir — oyuncu bir saat beklemez (`DomainSimulationAdapter.Crafting.cs:48`, `DomainSimulationAdapter.QuestProgress.cs:7-15`).
4. **Tamamlama (oyuncu tetikler):** Tick DEGIL — demirciye donup konuyu tekrar secmek; ingot envanterden alinir, `state.SetCompleted(true)` + `QuestCompleted` olayi (`QuestInteractionService.cs:68-77`). Onemli: forge questinin task aksiyonlari arasinda `CompleteQuestAction` YOK (`QuestCatalog.cs:32-35`), yani tick bu questi asla kapatamaz — kapanis yalnizca NPC teslimiyle.

### B. Sabit dunya questleri (kill + visit)
1. **Seed:** worldgen hidrasyonunda `SeedWorldQuests()` — idempotent, restore edilen dunya kayitli durumunu korur (`DomainSimulationAdapter.Worldgen.Hydration.cs:31`, `WorldQuests.cs:20-28`).
2. **Tamamlama (olay-tetikli, tick yok):**
   - Herhangi bir outlaw dunyada dusunce `SettleWorldEncounterIfOver` → `CompleteWorldQuest(OutlawBountyQuestId, 50, ...)` (`DomainSimulationAdapter.WorldEncounter.cs:980`).
   - Shrine tipi yerlesime seyahat varisi → `CompleteWorldQuest(ShrinePilgrimageQuestId, 40, ...)` (`DomainSimulationAdapter.Travel.cs:63-64`).
3. `CompleteWorldQuest`: task isaretle + kapat + altin + 60 XP + reputation +1 (`WorldQuests.cs:143-155`).

### C. Uretilen kontratlar (F21)
1. **Uretim:** `AcceptGeneratedQuest(seed, force?)` → `WorldQuestGenerator.Generate(NpcSeeds, Overland.Settlements, buradaki yerlesim, gun, seed)`; kontrat `WorldContracts`'a eklenir, serial 9100+ (`WorldQuests.cs:35-62`).
2. **Okuma:** Jurnal yenilenirken `ReadGeneratedQuests()` — deadline'i gecen acik kontrat **lazy** olarak Failed'a cekilir (`WorldQuests.cs:65-78`); jurnale "Contracts" bolumu olarak islenir (`InGameUiController.cs:752-781`).
3. **Teslim:** `TryTurnInGeneratedQuest(id)` sablon-basina durustluk kontrolu (dogru yerlesim / kargo envanterde / hedef olu), sonra altin + 60 XP + rep +1 (`WorldQuests.cs:81-121`).
4. **DIKKAT — oyuncu yolu yok:** `AcceptGeneratedQuest` ve `TryTurnInGeneratedQuest`'in Scripts altindaki TEK cagricisi proof metodu `ProofRunGeneratedQuestLeg` (`WorldQuests.cs:125-135`) ve proof driver (`EmberProofScreenshotDriver.cs:585`). Diyalog konularinda ve JournalView'da kabul/teslim kontrolu grep'te bulunamadi. Yani F21 makinesi canli ama oyuncunun eline verilmemis (asagida "borclar").

### D. Ana quest (delve hedefleri)
1. **Seed:** `ConfigureMainQuest()` dunya uretiminde (`DomainSimulationAdapter.Worldgen.cs:86`): son Dungeon-tipi yerlesim final delve, gereken parca sayisi delve sayisina uyarlanir; intro `LastNarrative`'e yazilir (`DomainSimulationAdapter.MainQuest.cs:15-39`).
2. **Perde 1:** Delve sandigini yagmalamak → `MainQuest.TryFindInscription(delveId)` — delve basina bir parca, kilic dedup'undan ONCE calisir (yorumda anlatilan proof hatasi duzeltmesi) (`DomainSimulationAdapter.WorldEncounter.cs:420-438`, `MainQuestState.cs:41-66`).
3. **Perde 2:** Bilgeye danisma — su an yalniz proof driver cagiriyor: `ProofConsultSage` (`MainQuest.cs:67-77`; cagri `EmberProofScreenshotDriver.cs:1092`). E-tusu diyalogunda dogal bir "sage" konusu grep'te bulunamadi — dogrulanamadi, buyuk olasilikla yok.
4. **Perde 3:** "Warden of ..." adli dusman final delve'de dusunce `TryFellFinalWarden` → `RuntimeMainQuestMirror.FinaleRequested = true` → `RuntimeFinaleView.Update` finale overlay + krediler (`WorldEncounter.cs:982-991`, `RuntimeMainQuestFinale.cs:8-29`).

### E. HUD quest satiri (her HUD refresh'inde)
1. `InGameUiController` HUD verisini doldururken `ReadQuestGuidance()` cagirir; `ShowQuestGuidance` → `d.EventLine`, `ShowQuestCompass` → `d.CompassLine` ("QUEST X · 12tiles · north"), ve KOSULSUZ `ReadDelveGuidance()` → `d.DelveLine` ("DELVE X · ... · M to travel") (`InGameUiController.cs:307-323`, `BuildCompassLine` 328-333, `BuildDelveLine` 356-361; render `WorldHudView.cs:15,235-236`).
2. `ReadQuestGuidance` yalnizca forge questini bilir: forge tamam/verici yoksa en yakin zindana yonlendirir (`DomainSimulationAdapter.QuestGuidance.cs:23-62`, `NearestDungeonRow` 69-93). Uretilen kontratlar ve ana quest HUD pusulasina HIC yansimiyor.
3. Mesafe/yon: verici baska yerlesimdeyse overland tile (Chebyshev), ayni yerlesimdeyse yerel metre; oyuncu konumu `QuestGuidancePlayerTracker` MonoBehaviour'undan beslenir (`QuestGuidance.cs:40-57`, `QuestGuidancePlayerTracker.cs:12-41`).

## LLD - Veri Modeli

| Tip | Alanlar | Yer |
|---|---|---|
| `QuestId` | `ulong _value`; `IsEmpty` (0 = sentinel) | `Domain/Quest/QuestId.cs:9-66` |
| `QuestDefinition` | `Id, DisplayName, OneTime, ResourceBindings, Tasks[], CompletionTaskIndex`; `CreateTaskInstances()` klonlar | `Domain/Quest/QuestDefinition.cs:10-77` |
| `QuestTask` | `Condition, Actions[], Triggered`; `TryTrigger` tek-atis | `Domain/Quest/QuestTask.cs:10-68` |
| `QuestState` | `bool[] TriggeredTasks, IsComplete, IsSuccess, GameTime StartTick` | `Domain/Quest/QuestState.cs:10-52` |
| `QuestStore` | `Dictionary<QuestId,QuestState> + List<QuestId> _order` (deterministik siralama); `Active` enumerasyonu | `Domain/World/QuestStore.cs:8-58` |
| `QuestWorldView` | read-facade: `Time`, `CountInventoryTag`, `HasEvent`, `IsActorDead` | `Domain/Quest/QuestWorldView.cs:11-77` |
| `QuestMutationContext` | write-facade: `CompleteQuest`, `AppendEvent`, `GrantItem` (deterministik item id tohumu) | `Domain/Quest/QuestMutationContext.cs:12-85` |
| `QuestResourceBinding` / `QuestResourceValue` / `QuestResourceKind` | sembol → Person/Place/Item tagged union | `Domain/Quest/QuestResourceBinding.cs:12-177` |
| `WorldQuestRecord` | `Id, Template(Fetch/Kill/Deliver/Visit), GiverNpcId/Name, TargetSettlementId/Name, TargetNpcId/Name, ItemTemplateId, RewardGold, DeadlineDay, Completed, Failed, Title` | `Domain/Quest/WorldQuestRecord.cs:11-36` |
| `MainQuestState` | `Act(1-4), RequiredInscriptions, FinalDelveId, ClaimedDelveIds`; `EnsureInvariants` null/clamp onarimi | `Domain/Quest/MainQuestState.cs:13-96` |
| `QuestGuidanceRow` | `HasTarget, Title, Line, TargetName, DistanceTiles, Direction, Unit("m"/"tiles")` | `Presentation/Ember/UI/QuestGuidanceSource.cs:6-32` |
| WorldState kokleri | `Quests` (satir 41), `WorldContracts` (150), `WorldQuestStates` (151), `MainQuest` (154); `EnsureInvariants` null-onarim (74-77); `CopyFrom` (239-241) | `Domain/World/WorldState.cs` |
| Save DTO'lari | `QuestStateSaveData` (questId, startTickMinutes, isComplete, isSuccess, triggeredTasks[]), `WorldContractSaveData` (duz alan esleme) | `Data/Save/WorldSaveData.Quest.cs:7-41` |

Kosullar: `AllQuestCondition` (`AllQuestCondition.cs:7-36`), `WorldEventOccurredCondition` (kind+reason+atOrAfterQuestStart; `WorldEventOccurredCondition.cs:7-32`), `InventoryHasItemTagCondition` (`InventoryHasItemTagCondition.cs:9-34`), `TicksElapsedCondition` (`TicksElapsedCondition.cs:10-33`), `ActorDeadCondition` (`ActorDeadCondition.cs:10-29`).
Aksiyonlar: `AppendQuestEventAction` (`AppendQuestEventAction.cs:10-33`), `CompleteQuestAction` (`CompleteQuestAction.cs:7-22`), `GrantItemAction` (`GrantItemAction.cs:9-32`).

## LLD - Fonksiyon Haritasi

| Fonksiyon | Yer | Ne yapar |
|---|---|---|
| `QuestSystem.Tick(WorldState)` | `Simulation/Quest/QuestSystem.cs:14-24` | Aktif katalog questlerini gezer; null Quests/Events'te sessiz cikar. |
| `QuestSystem.TickQuest(...)` | `QuestSystem.cs:26-53` | `QuestCatalog.Resolve` + task instancelari + kosul degerlendirme; yeni tamamlanmada `QuestCompleted` olayi. |
| `QuestSystem.AppendStartedIfNeeded` | `QuestSystem.cs:55-66` | Olay lounde ayni reason'li `QuestStarted` yoksa ekler (dedup = tam string esleme). |
| `QuestCatalog.ForgeIronIngot()` / `Resolve(id)` | `Data/Quest/QuestCatalog.cs:14-51` | Tek yazarli quest; `Resolve` bilinmeyen id'de `KeyNotFoundException` FIRLATIR. |
| `QuestInteractionService.BuildTopics / TrySelectTopic` | `Simulation/Quest/QuestInteractionService.cs:23-78` | Diyalog konusu uretimi; baslatma/ilerleme/teslim replikleri ve durum gecisleri. |
| `QuestInteractionService.TryStartForgeQuest` | `QuestInteractionService.cs:96-114` | Envanter kapasite kontrolu, cevher+yakit grant, `Quests.Add`, `QuestStarted` olayi. |
| `WorldQuestGenerator.Generate(npcs, settlements, here, day, seed, force?)` | `Simulation/Quest/WorldQuestGenerator.cs:40-148` | splitmix64+xorshift64* ile deterministik tek kontrat; sablon rotasyonu, ham madde yoksa null. |
| `DomainSimulationAdapter.SeedWorldQuests()` | `Adapters/DomainSimulationAdapter.WorldQuests.cs:20-28` | 9001/9002 sabit ciftini idempotent seed'ler. |
| `DomainSimulationAdapter.AcceptGeneratedQuest` | `WorldQuests.cs:48-62` | Uret + kabul + `WorldContracts`'a ekle (serial 9100+). |
| `DomainSimulationAdapter.ReadGeneratedQuests` | `WorldQuests.cs:65-78` | Jurnal satirlari; sure asimini LAZY fail'ler. |
| `DomainSimulationAdapter.TryTurnInGeneratedQuest` | `WorldQuests.cs:81-121` | Sablon-basina teslim kontrolu; odul: altin + 60 XP + rep +1. |
| `DomainSimulationAdapter.CompleteWorldQuest(id, gold, label)` | `WorldQuests.cs:143-155` | Sabit cift icin tek-atis kapanis + odul. |
| `DomainSimulationAdapter.ReevaluateQuestProgress()` | `Adapters/DomainSimulationAdapter.QuestProgress.cs:7-15` | Craft sonrasi ani `QuestSystem.Tick` (cagri: `Crafting.cs:48`). |
| `DomainSimulationAdapter.ConfigureMainQuest()` | `Adapters/DomainSimulationAdapter.MainQuest.cs:15-39` | Uc perdeyi seed'te kurar (cagri: `Worldgen.cs:86`). |
| `MainQuestState.TryFindInscription / TryConsultSage / TryFellFinalWarden` | `Domain/Quest/MainQuestState.cs:41-95` | Perde gecisleri; sira-disi cagrilar REDDEDER (sessiz ilerleme yok). |
| `DomainSimulationAdapter.ReadQuestGuidance / ReadDelveGuidance` | `Adapters/DomainSimulationAdapter.QuestGuidance.cs:23-93` | HUD satiri kaynagi; forge-yoksa-en-yakin-zindan fallback'i. |
| `InGameUiController` HUD besleme | `UI/InGame/InGameUiController.cs:307-323` | EventLine/CompassLine/DelveLine doldurma; secenek kapilari `EmberRuntimeOptions.cs:28-29`. |
| `DomainSimulationAdapter.GetChapters()` (IJournalSource) | `Adapters/DomainSimulationAdapter.Journal.cs:11-47` | Kernel quest → jurnal projeksiyonu (statu + el yazisi govde metinleri). |
| `WorldSaveMapper.ToQuestStoreData / ToQuestStore / ToWorldQuestStatesData / ToWorldContracts*` | `Data/Save/SliceJson/WorldSaveMapper.Quest.cs:11-147` | Uc deponun cift yonlu save eslemesi (baglanti: `WorldSaveMapper.cs:79-81,112-115,185-187,250-256`). |
| `WorldStateDigest.AppendWorldQuests` | `Simulation/Composition/WorldStateDigest.cs:382-430` | Kontrat+durum digest bolumu; ikisi de bosken atlanir (pre-F22 golden digest korumasi). |

## LLD - Yazdigi/Okudugu Alanlar

**QuestSystem (tick):**
- Okur: `WorldState.Quests.Active`, `WorldState.Events` (dedup taramasi + kosul sorgulari), `WorldState.Time`, `WorldState.Actors` (player fallback), `WorldState.Sites` (ilk site fallback), `WorldState.PlayerInventory` (QuestWorldView.CountInventoryTag uzerinden).
- Yazar: `QuestState.TriggeredTasks` / `IsComplete` / `IsSuccess` (aksiyonlar araciligiyla), `WorldState.Events` (append), `WorldState.PlayerInventory` (GrantItemAction varsa — su an katalogda kullanilmiyor).

**QuestInteractionService:** Yazar: `WorldState.Quests` (Add), `WorldState.PlayerInventory` (grant/remove), `QuestState.SetCompleted`, `WorldState.Events`. Okur: `WorldState.Actors` (JobPreferences ile verici tespiti, `QuestInteractionService.cs:80-94`).

**Adapter (WorldQuests/MainQuest):** Yazar: `WorldState.WorldQuestStates`, `WorldState.WorldContracts`, `WorldState.MainQuest`, `WorldState.PlayerGold`, `WorldState.PlayerXp` (GrantXp), `WorldState.PlayerReputation`, `WorldState.LastNarrative`, `_lastCombatLine` (adapter-yerel). Okur: `WorldState.NpcSeeds`, `WorldState.Overland.Settlements`, `WorldState.Time` (gun turetimi `WorldQuests.cs:137-140`), `WorldState.Actors` (kill dogrulamasi).

**Guidance:** Okur: `Quests`, `Actors`, `NpcSeeds`, `Overland`; yazar: yalnizca `_questGuidancePlayerPosition` (adapter-yerel, sim-disi).

FieldOwnershipRegistry notu: quest alanlarinin resmi registry kaydini bu taramada dogrulayamadim — registry dosyasina bakilmadi (dogrulanmadi).

## LLD - Urettigi/Tukettigi Olaylar

| Olay | Kim uretir | Kim tuketir |
|---|---|---|
| `WorldEventKind.QuestStarted` (=27) | `QuestSystem.cs:65`, `QuestInteractionService.cs:110` | `QuestSystem.AppendStartedIfNeeded` dedup taramasi (`QuestSystem.cs:58-63`); proof raporu sayaci (`EmberProofScreenshotDriver.cs:2299,2354`) |
| `WorldEventKind.QuestTaskTriggered` (=28) | forge task aksiyonu `"quest_task_triggered:forge_iron_ingot"` (`QuestCatalog.cs:34`) | dogrudan tuketen bulunamadi (jurnal QuestState'ten okur, olaydan degil) |
| `WorldEventKind.QuestCompleted` (=29) | `QuestSystem.cs:46-51`, `QuestInteractionService.cs:75` | proof raporu sayaci (`EmberProofScreenshotDriver.cs:2300,2355`) |
| `WorldEventKind.RecipeCompleted` (`"recipe_completed:<id>"`) | uretim sistemi (bu dosyanin kapsami disi) | forge questinin kosulu (`QuestCatalog.cs:27-30`) |
| `WorldEventKind.DmConsultFate` | AI-DM `propose_quest` araci — **stub**: quest yaratmaz, sadece ozet loglar (`Simulation/AiDm/ToolUseService.cs:22,69-73`) | AI-DM tarafi (kapsam disi) |
| Log taglari | `[Quest]`, `[QuestGen]`, `[MainQuest]`, `[Rep]` (`WorldQuests.cs:27,59,74,117-119,152-154`; `MainQuest.cs:36,75`; `WorldEncounter.cs:435,990`) | insan/proof-log okuyuculari |

## Testler

- `Assets/Tests/EditMode/Quest/QuestSystemTests.cs` — tick tamamlama sinirlari: ingot yokken incomplete (24), onceden var olan ingot task'i isaretlemez (37), craft olayi sonrasi task isaretlenir ama quest kapanmaz (55), tam smelt dongusu deterministik (77).
- `Assets/Tests/EditMode/Quest/QuestModelTests.cs` — kosul esigi (16), task tek-atis + aksiyon sirasi (31), CompleteQuestAction/AppendQuestEventAction (57), ayni girdi → ayni gecisler (74).
- `Assets/Tests/EditMode/Quest/WorldQuestGeneratorTests.cs` — 20 seed 20 gecerli quest (47), ayni seed ayni quest (80), seed taramasi 4 sablona ulasir (91), force onore edilir (100), verici yoksa null (108).
- `Assets/Tests/EditMode/Quest/MainQuestStateTests.cs` — uc perde sirali (14), sira-disi/yanlis hedef reddi (36), kucuk dunya uyarlamasi (57), invariant onarimi (72).
- `Assets/Tests/EditMode/Presentation/PlayableLoopCraftQuestTests.cs:23` — NPC'den baslat → craft → teslim → jurnal statuleri; tam oynanabilir dongu pini.
- `Assets/Tests/EditMode/Presentation/JournalSourceTests.cs` — adapter yokken jurnal uydurmaz (15), seed'li quest jurnale yansir (25), yerlesim kancasi (41).
- `Assets/Tests/EditMode/Save/SaveLoadDigestRoundtripTests.cs:29` — `WorldQuests_SurviveSaveLoadRoundtrip`: kontrat + 9001/9002 durumlari byte-identik digest ile korunur.

## Bilinen Borclar + Kacak Kapilari

- **(en buyuk) F21 kontrat makinesinin oyuncu arayuzu yok.** `AcceptGeneratedQuest`/`TryTurnInGeneratedQuest` yalnizca `ProofRunGeneratedQuestLeg` (`WorldQuests.cs:125-135`) ve proof driver'dan (`EmberProofScreenshotDriver.cs:585`) cagriliyor; diyalog konularinda ve `JournalView.cs`'de kabul/teslim kontrolu yok (grep: 0 eslesme). Jurnal kontratlari sadece GOSTERIR (`InGameUiController.cs:752-781`). Sistem canli ama vitrin arkasi.
- **Katalog-disi id = tick crash'i.** `QuestCatalog.Resolve` bilinmeyen id'de firlatir (`QuestCatalog.cs:50`); `world.Quests`'e katalog-disi id ekleyen olursa saatlik tick patlar. Koruma sadece konvansiyon ("F2 lesson" yorumu, `WorldState.cs:147-149`, `WorldQuests.cs:12-16`) — runtime kontrolu yok. (a)-ailesi (kirilgan varsayim) adayi.
- **`CompletionTaskIndex` olu alan.** `QuestDefinition.cs:67`'de tanimli, dogrulama disinda hicbir yerde okunmuyor (grep: yalniz tanim). QuestSystem tamamlanmayi yalnizca `state.IsComplete` degisiminden anlar; forge questinde bu bayragi tick degil NPC teslimi kaldirir. Sablon-modeli ile calisma-modeli arasindaki bu kopukluk yeni quest yazarken tuzak.
- **`AppendStartedIfNeeded` O(events) tarama, her saatlik tick'te, quest basina** (`QuestSystem.cs:58-63`). Maraton kosularinda olay logu buyudukce maliyet lineer buyur; dedup tam string eslesmesine dayali ("quest_started:" + DisplayName) — DisplayName degisirse dedup sessizce kirilir, olay cift yazilir (`QuestSystem.cs:98-101` ve `QuestInteractionService.cs:110` ayni stringi iki ayri dosyada elle kurar).
- **Kill kontrati bosluklu dogrulama (potansiyel).** `TryTurnInGeneratedQuest` Kill kolu: hedef aktor `Actors`'ta YOKSA "hayatta" kontrolu vacuously gecer ve odeme yapilir (`WorldQuests.cs:100-105`). Outlaw NPC'lerinin her zaman actor olarak hidrate edildigi dogrulanmadi — hidrate edilmeyen bir hedefte quest bedava tamamlanir. (c/d)-ailesi dogrulama-boslugu adayi.
- **Deadline lazy-fail.** Sure asimi yalniz `ReadGeneratedQuests` (jurnal acilisi) veya teslim denemesinde islenir (`WorldQuests.cs:65-78,88`); sim tick'i kontratlara hic dokunmaz. Save/digest, jurnal hic acilmamis bir oturumda "acik ama aslinda olmus" kontrat tasiyabilir (durum gecisi gozlemcinin yan etkisi — (e)-ailesi kokusu).
- **HUD pusulasi yalnizca forge questini bilir.** Kontratlar, sabit cift ve ana quest `ReadQuestGuidance`'a baglanmamis (`QuestGuidance.cs:23-62`); ana quest ilerlemesi yalniz `LastNarrative` + loglarda. "Quest Lead" satiri tamamlanmis dunyada sonsuza dek en-yakin-zindan gosterir.
- **Perde 2 (bilge) oyuncu yolu dogrulanamadi.** `TryConsultSage`'in tek Scripts cagricisi `ProofConsultSage`; dogal diyalog kancasi bulunamadi. Oyuncu ana questi kendi basina Perde 3'e tasiyamayabilir — dogrulanmadi (diyalog katmaninin tamami taranmadi).
- **Sandik-acildi durumu save edilmiyor** — kilic dedup'i template-guard ile ayakta; yorum acikca "F22 alanı" diyor (`WorldEncounter.cs:414` civari dokuman yorumu).
- **`CurrentWorldDay` cift tanim riski:** gun formulu `WorldQuests.cs:137-140`'ta yerel; ayni turetim baska partial'larda da varsa (dogrulanmadi) gun-kaymasi tutarsizligi olasi.
- **AI-DM `propose_quest` stub:** quest onerir gibi gorunur ama yalnizca `DmConsultFate` olayi loglar (`ToolUseService.cs:69-73`) — gercek quest uretimine baglanmamis.

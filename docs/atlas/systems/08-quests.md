# 08-quests

## HLD - Ne ve Neden (5-10 cumle)

Ember'in "gorev" katmani tek bir sey degil, uc yasli paralel yol: (1) **catalog quest** — `QuestCatalog` uzerinde authored, deterministik state machine ile ilerleyen tekil hikaye gorevi ("Forge an Iron Ingot"), her tick `QuestSystem.Tick` tarafindan degerlendirilir. (2) **world quest / contract** — `WorldQuestGenerator` tarafindan runtime'da uretilen dort DFU-tarzi sablon (Fetch, Kill, Deliver, Visit), NPC ile diyalogda kabul edilir ve deadline'i gecince FAIL olur. (3) **main quest spine** — `MainQuestState` ile tanimli 3-perde ana hikaye (Inscriptions -> Sage -> Warden).

Neden bu ayrim: authored quest'ler dilbilgisel kompozisyon (bir sart + bir aksiyon; degerlendirme puredir), generated quest'ler oyunun tekrar oynanabilir "gorev makinesi"dir (W28'de smith'in tek hediyesi olmaktan cikip her `GivesWork` NPC'sinin verdigi bir contract akisi haline geldi). Iki depo (`world.Quests` = catalog-store, `world.WorldContracts` + `world.WorldQuestStates` = generated) bilincli ayri: catalog quest'ler ID uzayina baglidir, contract'lar ID'siz mint edilip runtime serial alir. W31'de `QuestInteractionService` `ForgeIronIngot` dialog topic'ini alarak forge errand'ini command-driven yapti (per-tick degil), W33'te `smith_commission` topic'i eklenerek ingot'un tuketicisi de forge NPC'sinde ayni akista yasadi. Cift depo saklama W32'de save mapper'a girdi: hem `Quests` (kernel store) hem `WorldQuestStates` + `WorldContracts` iki yonlu.

## HLD - Akış (numaralı adımlar)

1. **Bootstrap.** `SeedWorldQuests()` (adapter) `WorldQuestStates` icinde `OutlawBountyQuestId` (9001) + `ShrinePilgrimageQuestId` (9002) kayitlarini yaratir; kernel `world.Quests` bos baslar — `ForgeIronIngot` **only** dialog topic secildiginde `world.Quests.Add` ile eklenir.
2. **Diyalog aciliyor.** `AddQuestInteractionTopics` her NPC secildiginde iki kanal koyar: `QuestInteractionService.BuildTopics` (forge & smith-commission topic'leri; sadece Blacksmith/Artisan/JobKind.Smith icin) ve — `GivesWork(npc.Role)` true ise — `contract_work` topic'i.
3. **Forge topic secildi.** `TrySelectTopic` -> `TryStartForgeQuest` calisir: PlayerInventory'ye iron_ore x2 + fuel x1 seed'lenir, `QuestCatalog.ForgeIronIngot()` sartlariyla `world.Quests` icine kayit dusuulur, `QuestStarted` event'i basilir.
4. **QuestSystem.Tick (hourly).** `QuestStep` her saatlik cadance'de `world.Quests.Active`'i dolasir; `AllQuestCondition(InventoryHasItemTagCondition + WorldEventOccurredCondition)` sagsa `MarkTaskTriggered(0)` — yalnizca "iron_ingot" envanterde **ve** `recipe_completed:smelt_iron_ingot` event'i quest baslangicindan sonra atilmis olursa (provenance).
5. **Forge topic ikinci kez.** Task triggered ise `TrySelectTopic` player'dan 1 ingot alir, 10 gold odesir, `SetCompleted(true)`, `QuestCompleted` event'i basilir. Aksi halde honest refuse mesaji.
6. **Smith commission topic (W33).** Forge quest complete olduktan sonra smith artik sessiz degil: 2 ingot + 15 gold karsiligi `WorldItemCatalog.CreateForgedIronSword()` uretilir, `RecipeCompleted` event'iyle isaretlenir; envanter dolusa gold iade edilip ingot'lar geri konur (atomik refund).
7. **Contract topic (W28 wiring).** `HandleContractWork`: (a) mevcut acik contract'lardan HERE'de tamamlanabilecek varsa turn-in eder; (b) yoksa "you are not there / still draws breath" refuse mesajini gosterir; (c) hicbiri yoksa `AcceptGeneratedQuest(seed)` ile yeni contract mint eder — seed pure world state'tan turer (Time, npc id, contract sayisi).
8. **World-quest tick disi.** Contract'lar QuestSystem.Tick'e girmez; `ReadGeneratedQuests()` her okumada `today > DeadlineDay` ise `q.Failed = true` set eder (lazy fail). Turn-in `TryTurnInGeneratedQuest` template'e gore honest check yapar: Fetch/Deliver -> `PlayerInventory.TryRemove(item)` @ target settlement; Kill -> target actor `IsAlive == false`; Visit -> arriving at target settlement.
9. **Save/Load.** `WorldSaveMapper.Quest` `Quests` -> `QuestStateSaveData[]`, `WorldQuestStates` -> ayni sekilde ama raw ulong-key dict, `WorldContracts` -> `WorldContractSaveData[]` (13 alan) — ceyk yonu simetrik.

## LLD - Veri Modeli (file:line)

- `Assets/Scripts/Domain/Quest/QuestId.cs:9` — `readonly struct QuestId(ulong Value)`; `IsEmpty`.
- `Assets/Scripts/Domain/Quest/QuestDefinition.cs:10` — `sealed class QuestDefinition(Id, DisplayName, OneTime, ResourceBindings, IEnumerable<QuestTask>, CompletionTaskIndex)`; immutable, task listesi clone'lanir.
- `Assets/Scripts/Domain/Quest/QuestTask.cs:10` — `sealed class QuestTask(IQuestCondition, IEnumerable<IQuestAction>, bool triggered)`.
- `Assets/Scripts/Domain/Quest/QuestState.cs:10` — `sealed class QuestState(int taskCount, GameTime startTick)` with `bool[] TriggeredTasks`, `IsComplete`, `IsSuccess`.
- `Assets/Scripts/Domain/Quest/QuestResourceBinding.cs:12` — DFU-tarzi bindings dictionary (`QuestResourceValue`); Person/Place/Item.
- `Assets/Scripts/Domain/Quest/QuestWorldView.cs:11` — `readonly struct` — sartlarin okuma yuzeyi (world snapshot).
- `Assets/Scripts/Domain/Quest/QuestMutationContext.cs:12` — aksiyonlarin write yuzeyi (world + state + actorId + siteId).
- `Assets/Scripts/Domain/Quest/AllQuestCondition.cs:7`, `InventoryHasItemTagCondition.cs:9`, `WorldEventOccurredCondition.cs:7`, `ActorDeadCondition.cs:10`, `TicksElapsedCondition.cs:10` — sart primitifleri.
- `Assets/Scripts/Domain/Quest/AppendQuestEventAction.cs:10`, `CompleteQuestAction.cs:7`, `GrantItemAction.cs:9` — aksiyon primitifleri.
- `Assets/Scripts/Domain/Quest/MainQuestState.cs:13` — `Act` (1..4), `RequiredInscriptions`, `FinalDelveId`, `ClaimedDelveIds`.
- `Assets/Scripts/Domain/Quest/WorldQuestRecord.cs:20` — F21 contract data: `Template, GiverNpcId, GiverName, TargetSettlementId, TargetNpcId, ItemTemplateId, RewardGold, DeadlineDay, Completed, Failed, Title`.
- `Assets/Scripts/Domain/Quest/WorldQuestRecord.cs:11` — `enum WorldQuestTemplate { Fetch=0, Kill=1, Deliver=2, Visit=3 }`.
- `Assets/Scripts/Domain/World/QuestStore.cs:8` — `sealed class QuestStore` dict + insertion order list; ekleme idempotent DEGIL (`InvalidOperationException` on duplicate).
- `Assets/Scripts/Domain/World/WorldState.cs:41` — `public QuestStore Quests`.
- `Assets/Scripts/Domain/World/WorldState.cs:211` — `public List<WorldQuestRecord> WorldContracts` (F22).
- `Assets/Scripts/Domain/World/WorldState.cs:212` — `public Dictionary<ulong, QuestState> WorldQuestStates`.
- `Assets/Scripts/Data/Quest/QuestCatalog.cs:12` — `QuestId ForgeIronIngotId = new QuestId(2001UL)`.
- `Assets/Scripts/Data/Save/WorldSaveData.Quest.cs:7-11` — `quests[]`, `worldQuestStates[]`, `worldContracts[]`.
- `Assets/Scripts/Data/Save/WorldSaveData.Quest.cs:15-31` — `WorldContractSaveData` 13-field POCO.

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

- `QuestSystem.Tick(WorldState world)` — `Assets/Scripts/Simulation/Quest/QuestSystem.cs:14` — Her authored quest icin `TickQuest` cagirir; `world.Quests` bos ise no-op.
- `QuestSystem.TickQuest(WorldState, in QuestWorldView, QuestId, QuestState)` — `QuestSystem.cs:26` — `AppendStartedIfNeeded` + `task.TryTrigger` dongusu + tamamlaninca `QuestCompleted` event'i yayimlar.
- `QuestSystem.AppendStartedIfNeeded(...)` — `QuestSystem.cs:55` — Aynen bir kere `QuestStarted` event'i basilmasini garantiler (idempotent).
- `QuestSystem.ResolveEventActorId/SiteId(...)` — `QuestSystem.cs:68,81` — Resource binding'ten Person/Place cikarir, yoksa player/first-site fallback.
- `QuestInteractionService.BuildTopics(WorldState, ActorId, NpcSeedRecord)` — `Assets/Scripts/Simulation/Quest/QuestInteractionService.cs:28` — Sadece forge givers icin; quest tamamsa `smith_commission` topic, degilse `forge_work` topic.
- `QuestInteractionService.TrySelectTopic(...)` — `QuestInteractionService.cs:49` — Topic dispatch: smith_commission | forge_work; dis ise false doner (adapter default dialog akisina duser).
- `QuestInteractionService.IsForgeQuestGiver(WorldState, ActorId, NpcSeedRecord)` — `QuestInteractionService.cs:94` — Role Blacksmith/Artisan **veya** actor `JobPreferences` icinde `JobKind.Smith`.
- `QuestInteractionService.TryHandleSmithCommission(...)` — `QuestInteractionService.cs:113` — W31: 2 ingot + 15g -> `WorldItemCatalog.CreateForgedIronSword`; envanter dolusa full refund (gold geri + ingot geri).
- `QuestInteractionService.TryStartForgeQuest(...)` — `QuestInteractionService.cs:168` — Envanter kontrolu -> ore/fuel seed -> `Quests.Add(ForgeIronIngotId, new QuestState(1, world.Time))` -> `QuestStarted` event.
- `QuestCatalog.ForgeIronIngot()` — `Assets/Scripts/Data/Quest/QuestCatalog.cs:14` — Tek task, `AllQuestCondition` = `iron_ingot in inv` AND `RecipeCompleted:smelt_iron_ingot` post-quest.
- `QuestCatalog.Resolve(QuestId)` — `QuestCatalog.cs:45` — Bilinmeyen ID `KeyNotFoundException` firlatir (catalog-only, contract ID'leri buraya girmez).
- `WorldQuestGenerator.Generate(npcs, settlements, here, currentDay, seed, force?)` — `Assets/Scripts/Simulation/Quest/WorldQuestGenerator.cs:40` — Splitmix64 + xorshift64* ile giver sec, 4 template arasi rotate ederek raw material bulunca return; bulamayinca null.
- `WorldQuestGenerator.GivesWork(NpcRole)` — `WorldQuestGenerator.cs:23` — Merchant/Noble/Priest/Scholar/Innkeeper/Blacksmith/Healer TRUE — bu liste W28'de contract_work topic filtresi olarak da kullanildi.
- `DomainSimulationAdapter.AcceptGeneratedQuest(seed, force?)` — `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldQuests.cs:48` — Generate -> ID atama (`NextGeneratedQuestSerial` baslangic 9099) -> WorldContracts.Add.
- `DomainSimulationAdapter.ReadGeneratedQuests()` — `DomainSimulationAdapter.WorldQuests.cs:65` — Journal beslemesi; lazy failure (`today > DeadlineDay -> Failed = true`).
- `DomainSimulationAdapter.TryTurnInGeneratedQuest(QuestId)` — `DomainSimulationAdapter.WorldQuests.cs:81` — Template'e gore honest turn-in; +gold + XP(60) + Reputation +1 (F23).
- `DomainSimulationAdapter.ProofRunGeneratedQuestLeg()` — `DomainSimulationAdapter.WorldQuests.cs:125` — Proof harness: fetch mint -> buy -> turn-in tek satirda.
- `DomainSimulationAdapter.CompleteWorldQuest(QuestId, goldReward, label)` — `DomainSimulationAdapter.WorldQuests.cs:143` — Fixed pair (bounty/pilgrimage) icin task+complete+gold+XP+Rep helper.
- `DomainSimulationAdapter.SeedWorldQuests()` — `DomainSimulationAdapter.WorldQuests.cs:20` — Bounty (9001) + Pilgrimage (9002) idempotent seed; restore-safe (`ContainsKey` gate).
- `DomainSimulationAdapter.AddQuestInteractionTopics(actorId, npc, baseTopics)` — `DomainSimulationAdapter.QuestInteraction.cs:12` — Forge topic'leri + `contract_work` topic'ini merge eder (W31: contract_work'un dialog'a girdigi yer).
- `DomainSimulationAdapter.TryHandleQuestInteractionTopic(topicId)` — `DomainSimulationAdapter.QuestInteraction.cs:34` — Topic dispatch: contract_work -> `HandleContractWork`, digerleri -> `_questInteractions.TrySelectTopic`.
- `DomainSimulationAdapter.HandleContractWork(NpcSeedRecord)` — `DomainSimulationAdapter.QuestInteraction.cs:59` — Turn-in-then-mint policy; deterministic seed = Time.TotalMinutes*1000003 + npc.Id*8191 + count.
- `DomainSimulationAdapter.ReevaluateQuestProgress()` — `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.QuestProgress.cs:9` — Immediate mode QuestSystem.Tick — command-driven recomputation (event'ler sonrasi).
- `DomainSimulationAdapter.ReadQuestGuidance()` — `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.QuestGuidance.cs:23` — HUD compass: forge quest yoksa/tamsa nearest Dungeon; varsa cross-settlement Chebyshev vs local metre.
- `DefaultTickSystems.QuestStep.Run(in TickContext)` — `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs:314-327` — `TickCadence.Hourly` phase 15 — sim loop'ta QuestSystem.Tick'in tek register point'i.
- `MainQuestState.TryFindInscription/TryConsultSage/TryFellFinalWarden(...)` — `MainQuestState.cs:41,69,84` — 3-perde spine; out-of-order refuse (silent-advance yasak).

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Not: `FieldOwnershipRegistry` bugun quest-domain alanlarina explicit satir tutmuyor (W34-C tabaninda yok); asagidaki matrix runtime davranisin gozlemidir.

- **QuestSystem.Tick (owner: quest.tick step, hourly phase 15)**
  - READ: `world.Quests.Active`, `world.PlayerInventory.Items`, `world.Events.Events` (WorldEventOccurredCondition — full scan), `world.Time`, `world.Actors` (player fallback), `world.Sites` (site fallback).
  - WRITE: `QuestState.TriggeredTasks` (via `MarkTaskTriggered`), `QuestState.IsComplete/IsSuccess`, `world.Events.Append` (`QuestStarted`, `QuestCompleted`, `QuestTaskTriggered`).
- **QuestInteractionService (owner: dialog command path)**
  - READ: `world.PlayerInventory.Items/Capacity`, `world.PlayerGold`, `world.Actors[actorId].JobPreferences`, `npc.Role`, `world.Quests.TryGet(ForgeIronIngotId)`.
  - WRITE: `world.Quests.Add`, `world.PlayerInventory.TryAdd/TryRemoveStackable`, `world.PlayerGold`, `world.Events.Append`, `QuestState.SetCompleted`.
- **DomainSimulationAdapter world-quest surface**
  - READ: `world.NpcSeeds`, `world.Overland.Settlements`, `world.Time`, `world.Actors`, `world.PlayerInventory`, `world.PlayerEquipment`.
  - WRITE: `world.WorldContracts` (Add + q.Completed/Failed), `world.WorldQuestStates` (per-id `QuestState` set), `world.PlayerGold`, `world.PlayerReputation`, `world.PlayerXp` (via `GrantXp`).

## LLD - Ürettiği/Tükettiği Olaylar

- **Uretilen** (`world.Events.Append`):
  - `WorldEventKind.QuestStarted` — reason `"quest_started:<DisplayName>"` — `QuestSystem.cs:65`, `QuestInteractionService.cs:182`.
  - `WorldEventKind.QuestCompleted` — reason `"quest_completed:<DisplayName>:success|failure"` — `QuestSystem.cs:47`, `QuestInteractionService.cs:89`.
  - `WorldEventKind.QuestTaskTriggered` — reason `"quest_task_triggered:forge_iron_ingot"` — `QuestCatalog.cs:34` (via `AppendQuestEventAction`).
  - `WorldEventKind.RecipeCompleted` — reason `"smith_commission:forged_iron_sword"` — `QuestInteractionService.cs:146` (smith commission signals downstream).
- **Tuketilen** (condition scan):
  - `WorldEventKind.RecipeCompleted` prefix `"recipe_completed:smelt_iron_ingot"` — `WorldEventOccurredCondition` inside `QuestCatalog.ForgeIronIngot()` — quest tamamlanabilmesi icin oyuncunun **ate**s **basinda** ore+fuel'i eritmesi gerekir (provenance kilidi, W28'de eklendi).
  - `PlayerInventory.Contains("iron_ingot", 1)` — `InventoryHasItemTagCondition` — envanterde bulunmasi.

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

- `Assets/Tests/EditMode/Quest/QuestSystemTests.cs` — Tick invariants: eksik ingot ile no-op, provenance olmadan mark yok, post-quest craft ile mark var quest completes ancak sonucu.
- `Assets/Tests/EditMode/Quest/QuestModelTests.cs` — Definition/State/Task primitifleri (constructor guards, clone davranisi).
- `Assets/Tests/EditMode/Quest/MainQuestStateTests.cs` — F31 3-perde spine, out-of-order refuse.
- `Assets/Tests/EditMode/Quest/WorldQuestGeneratorTests.cs` — F21 DoD: 20 seed = 20 valid quest, determinism, 4 template ulasilabilir.
- `Assets/Tests/EditMode/Presentation/PlayableLoopCraftQuestTests.cs` — E2E: smith'ten forge quest baslat -> craft -> deliver -> reward + smith commission topic.
- `Assets/Tests/EditMode/Presentation/ForgeRuntimeHelpersTests.cs` — Forge portrait bootstrap + recipe path.
- `Assets/Tests/EditMode/Save/SaveLoadDigestRoundtripTests.cs` — `Quests`, `WorldQuestStates`, `WorldContracts` round-trip (F22).
- `Assets/Tests/EditMode/Content/ContentDatabaseWorldCatalogTests.cs` — `WorldQuestTemplatesDocumentDto` + `QuestConfigDto` yukleme sanity.
- `Assets/Tests/EditMode/Presentation/JournalSourceTests.cs` — `ReadGeneratedQuests` -> journal projeksiyonu (lazy failure dahil).

Not: **W32-W36 hikaye testleri** (Actions/Farm/Sleep/Work slice'lari) quest sistemine dogrudan yeni test eklemedi — o hafta blocklari sim-loop'un eylem/urun/uyku eksenlerine odaklandi. Save round-trip testleri quest depolarindaki F22 sozlesmeyi WORLD_SAVE golden'i uzerinden pinliyor.

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W28 (contract_work dialog wiring)** — `AddQuestInteractionTopics` `WorldQuestGenerator.GivesWork(npc.Role)` true olan her NPC'ye `contract_work` topic'i asti; `HandleContractWork` turn-in-first-then-mint policy'sini kurdu — F21 makinesi ilk kez proof harness disindan cagrilir hale geldi (`QuestInteraction.cs:20-28`).
- **W31 wound-suite (git commit 1e9b474b — "eight live wounds")**
  - `ForgeIronIngot` command-driven olarak QuestInteractionService uzerinde temizlendi; forge quest tamamlaninca 10 gold odendi (`QuestInteractionService.cs:88`).
  - `smith_commission` topic doguldu (2 ingot + 15g -> forged sword) — forge NPC'si artik "silent after errand" degil (`QuestInteractionService.cs:113-149`).
  - Full-refund invariant: sword add basarisiz olursa gold + ingots atomik iade edilir (`QuestInteractionService.cs:141-143`).
  - `HandleContractWork` seed hesabi deterministik: sadece world state'ten turer.
- **W32 spot-fix wave (c477c217, B02/B05/B07/B08)** — Bug scorecard dogrudan quest kodu icin bir satir tasimadi ama `WorldEventLog` full-scan reader tarafi olarak `QuestSystem`/`AppendStartedIfNeeded` B21 cursor migration listesine girdi (Bilinen Borclar).
- **W33 (FARM slice, 61e340f3)** — Quest kodu degismedi; ancak `Recipe`/`Inventory` yolu (smelt event provenance'i besleyen) row-Tick'e cikinca `WorldEventOccurredCondition`'un okudugu `recipe_completed:*` event'lerinin uretim yolu W34-C'de yeniden yazildi.
- **W34-C (recipe/work refactor)** — `RecipeSystem` row overloads (TryFund + row-Tick with real stamp) `recipe_completed:smelt_iron_ingot` event'inin **uretim** tarafini degistirdi; sart cephesi (`InventoryHasItemTagCondition + WorldEventOccurredCondition`) aynen kaldi — quest tamamlanma sartinin _uretici_si sim-side'da atomiklesti.
- **W35 (schedule/ownership shrink, 20a3b899)** — `ScheduleSystem` daraldi ama `QuestStep` (Hourly/15) siralamasi ayni; sadece decision/schedule step'lerinin kendisi radikal degisti — quest okumasi tick fazlarindan bagimsiz kaldi.
- **W36 (RUH_TESHIS post-arch tail, f6c9e2d0)** — 10 external bug'in 5'ini kapatti; quest surface'ta yeni davranis eklemedi, ama `HandleContractWork`'un dis triage'da yakalanan "contract yok, ask again when the roads change" fallback line'i son revizyondan gecti.

## Bilinen Borçlar + Kaçak Kapıları

1. **Cift depo asimetrisi.** Catalog `Quests` (kernel `QuestStore`, tick'i yiyen) ve generated `WorldContracts` + `WorldQuestStates` (raw dict + list, tick-siz) iki ayri hikaye. `QuestCatalog.Resolve` contract ID'lerine `KeyNotFoundException` firlatir — herhangi biri yanlislikla contract ID'yi `Quests`'e eklerse tick anda patlar. Adapter'in `_generatedQuests` isim dizisini gorece gec (F22) `WorldContracts`'a proxy yapmasi eski F21 kodunda ID/save bagi hala kirilgan.
2. **QuestStore.Add duplicate = throw.** Idempotent degil. Save round-trip nedeniyle mapper `ToQuestStore` her satir icin `store.Add` cagirir — save dosyasi bir gorevi iki kez tasirsa (bug savekit) load anda `InvalidOperationException` firlatir. `EnsureInvariants` (`WorldState.cs:83`) bunu tolere edecek `Clear -> Rebuild` yapmiyor.
3. **AppendStartedIfNeeded O(n).** `QuestSystem.cs:58` her tick `world.Events` **tamami**ni tarar; W35 sonrasi event log buyudukce hourly step'te lineer cost. B21 kayit (WorldEventLog Direct-Reader Inventory, Jul 25) bunu 9 reader'dan biri olarak isaretledi — cursor migration adayi.
4. **WorldEventOccurredCondition.atOrAfterQuestStart:false.** ForgeIronIngot'ta ozellikle false — quest'ten **once** yapilmis bir smelt tamlanmayi tetikleyebilir mi? Test `Tick_WithPreexistingIronIngot_DoesNotMarkForgeObjective` bu senaryoyu pinliyor: envanter var, event yok -> mark yok. Ama envantersiz baslayan oyuncuya seed edilen ore/fuel + kendi yaptigi smelt eventi normal path; provenance kilidinin false'lu cagrisi tarihsel bir kaza — flag'in true olmasi daha guvenli.
5. **`_immediateQuestSystem` cifte instance.** `QuestStep` sim-loop'ta bir `QuestSystem`, `ReevaluateQuestProgress` adapter'da ayri bir `QuestSystem` — stateless olduklari icin sorun cikmiyor ama iki calisma yolu tests'te ayni Tick kodunu iki kez calistirabilir; `QuestSystem` state sizdirmadigi surece bu bir latent risk.
6. **`_generatedQuests` proxy.** `DomainSimulationAdapter.WorldQuests.cs:32` `_world?.WorldContracts` proxy'si; `_world` null iken `_generatedQuests` null olur ama `AcceptGeneratedQuest` `if (_generatedQuests == null) return null` guardi Sirket'te var, `ReadGeneratedQuests`'in `_generatedQuests.Count` cagrisi guardsiz — headless bir preview'da NRE riski (test suite'te bootstrapped world oldugu icin gorunmuyor).
7. **Contract deadline lazy.** `Failed` bayragi yalnizca `ReadGeneratedQuests()` cagrilinca set edilir; UI journal'i acilmadan Time ilerlerse `HandleContractWork` icindeki `TryTurnInGeneratedQuest` icin de bir on-check var — ancak headless proof'lar `ReadGeneratedQuests`'i cagirmayabilir; failed rozeti gecikir.
8. **`GivesWork` role listesi hardcoded.** `WorldQuestGenerator.cs:23-38` — Merchant/Noble/Priest/Scholar/Innkeeper/Blacksmith/Healer. Content-driven degil; yeni bir role eklerken hem `GivesWork` hem `AddQuestInteractionTopics` (contract_work) hem dialog UX koordineli guncellenmeli.
9. **Contract turn-in string matching.** `HandleContractWork:70` `outcome.Contains("Paid") || outcome.Contains("concluded") || ...` — turn-in'in donen dogal-dil cikti string'ini success sinyali olarak kullanir. Localization gunu geldiginde bu satir kirilir; typed outcome enum'una yukseltilmeli.
10. **Main quest spine ile catalog/contract sistemleri temassiz.** `MainQuestState` ayri bir `WorldState` alani (bkz. `RuntimeMainQuestMirror`, `WorldSaveMapper` main-quest satiri); QuestSystem tick'i tarafindan izlenmez, kendi command yolu var. Bu bilincli bir ayrim ama "gorev sayisi" istatistigini cikartmak isteyen bir HUD tek surface degil uc surface toplamalidir.

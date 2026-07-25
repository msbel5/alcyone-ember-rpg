# 19-adapter-contract

## HLD - Ne ve Neden (5-10 cumle)

`IDomainSimulationAdapter` Presentation.Ember katmaninin Domain simulasyonuna baktigi TEK yuzeydir - Presentation asmi hicbir zaman `EmberCrpg.Domain.*` tipini dogrudan cagirmaz, adapter uzerinden gecer. Codex 6. audit / C-P2 asamasinda tek sisman arayuz alti rol arayuzune ayrilmistir: `IEmberSimulationClock` (sink+source), `IEmberHudReadModel`, `IWorldViewReadModel`, `IPlayerCommandSink`, `IConsultFateOracle`, `IEmberSaveBridge`. Aggregate arayuz bunlari devralir, boylece dar consumer'lar (HUD paneli, telemetry, spawner) sadece ihtiyac duyduklari rolu enjekte edebilir. `EmberDomainAdapterLocator` scene-scoped tekil register/resolve saglar; test tear-down'lari `Register(null)` cagirir. Tek somut implementasyon `DomainSimulationAdapter`'dir - `sealed partial class` olarak 40 partial dosyaya bolunmustur ve Codex ARCH-02 tarafindan surdurulur; `UnavailableSimulationAdapter` ise adapter boot edemedigi zamanki "duz cevaplayan" fallback'idir. W31'de eklenen `CurrentSettlementKey` (Travel partial'inde `SettlementId.Value` olarak yayilir) spawner'in fast-travel sonrasi eski sehrin billboard'larini toplama sinyalidir. W32-W34 boyunca `ActionVerbTable` on-screen fiil dictionary'si haline geldi: `Verb(ActorActionType)` bir DOMAIN alan kadi (`Actor.ActionState.CurrentAction`) alir ve VERBATIM string cevirir - projeksiyon sozluk disi tahmin (saat/pozisyon) yapmaz (`WorldProjection.DescribeActivity`). Bu nedenle tek "ne yapiyor?" yalanini tabladan uretmis olur - RUH_TESHIS §2.9 guess branch'i olu.

## HLD - Akış (numaralı adımlar)

1. Boot: `EmberWorldHost` tek `DomainSimulationAdapter` yaratir (ctor `WorldState` alir), `EmberDomainAdapterLocator.Register(adapter)` cagirir.
2. Her frame host `IEmberClockSink.AdvanceTick(tickIndex)` cagirir → `Clock` partial `DrainMainThreadApply` (DET-02 kuyrugu) → `_tickComposer.Advance(_world, tickIndex)` → `PublishEventEchoes` + `PublishFieldMirror`.
3. UI panelleri `EmberDomainAdapterLocator.WorldViewReadModel` (veya HUD/Fate/Save role handle'lari) uzerinden readonly DTO'lar okur: `JobQueueRows`, `ColonyNeedsRows`, `InventorySlots`, `Overland`, `PlayerOverlandTile`, `StartingSettlementName`, `CurrentSettlementKey`, `TryReadActor`, `GetSpawnableActors`, `RecentWorldEvents`.
4. `EmberGeneratedActorSpawner.SpawnMissingNearbyActors()` her frame `readModel.CurrentSettlementKey` degisti mi diye kontrol eder; degistiginde `_spawnedRoots` icinden yeni candidate listesinde OLMAYANLARI `Destroy` eder ve `_spawnedForSettlement`'i yeni key ile guncelter (Travel'in `_billboardOriginResolved=false` sinyali ile beraber).
5. Player girdi: `IPlayerCommandSink.TryMeleeStrike / TryCastSpell / TryInteract(ActorId) / GetDialogSource(ActorId) / SeedWorld` - hepsi Domain yazimini adapter icinde yapar; Presentation Domain tipine dokunmaz.
6. LLM/AI-DM async yollari `TryConsumeResolvedFate` polluyla adapter'a geri konur - post-await mutasyonlar `_mainThreadApply` ConcurrentQueue'ya girer ve bir sonraki `AdvanceTick`'in tepesinde main-thread'de drain edilir (EMB-007 race'i kapatti).
7. Verb projeksiyonu: `ProjectActor(ActorRecord)` → `DescribeActivity(actor)` → `action != None` ise `ActionVerbTable.Verb(action)` (VERBATIM); yalnizca `None` durumunda dar `DescribeScheduleWord` kalir (Guard: "on watch", Enemy: "hunting"; digerleri null - tahmin yok).
8. Save round-trip: `IEmberSaveBridge.ExportStateJson` / `RestoreStateJson`, `JsonSliceSaveService` deleged; adapter Domain snapshot'i (Actors + Worksites + Jobs + Soils + Plants + WorkOrderLedger + WorldEvents) opak JSON'a sarar.

## LLD - Veri Modeli (file:line)

- `IDomainSimulationAdapter` aggregate → `Assets/Scripts/Presentation/Ember/Adapters/IDomainSimulationAdapter.cs:284-292` (`IEmberSimulationClock, IEmberHudReadModel, IWorldViewReadModel, IPlayerCommandSink, IConsultFateOracle, IEmberSaveBridge`).
- Rol arayuzleri: `IEmberClockSink:23`, `IEmberClockSource:29`, `IEmberSimulationClock:34`, `IEmberHudReadModel:40`, `IWorldViewReadModel:48-97`, `IPlayerCommandSink:130-197`, `IConsultFateOracle:207-227`, `IEmberSaveBridge:238-259`.
- `SpawnableActor` readonly struct → `IDomainSimulationAdapter.cs:106-122` (Id ulong + Name + SpriteRole + WorldX/Z + Seed; hicbir Domain tipi tasimaz).
- `CurrentSettlementKey` → `IDomainSimulationAdapter.cs:96` (kontrat), impl `DomainSimulationAdapter.Travel.cs:29` (`=> CurrentSettlementOrStart.Value`), Unavailable `UnavailableSimulationAdapter.cs:50` (`=> 0UL`).
- `EmberDomainAdapterLocator` → `IDomainSimulationAdapter.cs:270-303` (`Current`, `ClockSource`, `HudReadModel`, `WorldViewReadModel`, `PlayerCommandSink`, `ConsultFateOracle`, `SaveBridge`, `Register`, `Clear`).
- Root partial state: `DomainSimulationAdapter.cs:28-100` - `_world`, `_saveService`, `_tickComposer`, `_tick`, `_lastCombatLine`, `_activeDialogActor/Id/NpcId`, `_currentDialogLine`, `_currentPortrait`, `_conversation`, `_pendingFate`, `_pendingFateFollowups`, `_isFateThinking`, `_isDialogThinking`, `_topicAskCounts`, `_streamingPartialLine`, `_suppressGlobalTopicFallback` + offset sabitleri `RegionSiteOffset=100_000UL`, `SettlementSiteOffset=200_000UL`, `GeneratedNpcActorOffset=10_000UL`.
- Travel state: `DomainSimulationAdapter.Travel.cs:17` `_currentSettlement`; `CurrentSettlementOrStart` (nearest-fallback → StartingSettlement).
- Billboard origin: `DomainSimulationAdapter.WorldProjection.cs:55-57` `_billboardOrigin`, `_billboardOriginResolved`.
- Wizard-derived state: `DomainSimulationAdapter.Worldgen.State.cs:29-33` - `GeneratedWorld`, `StartingRegion`, `StartingSettlement`, `StartingFaction`.
- `ActionVerbTable` (public static) → `Assets/Scripts/Presentation/Ember/Adapters/ActionVerbTable.cs:16-66` - 11 satirlik enum→string switch + `KindName` + `Unknown` sentinel (ilk bilinmeyende bir kez warn, ekrana `(Kind)` yazar).

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

- `void AdvanceTick(int tickIndex)` — `DomainSimulationAdapter.Clock.cs:6` — main-thread apply kuyrugunu bosaltir, tick composer'i advance eder, event echo + field mirror yayinlar.
- `int TickIndex` — `Clock.cs:31` — son advance edilen tick index.
- `IReadOnlyList<SpawnableActor> GetSpawnableActors()` — `WorldProjection.cs:140-163` — Player disi & yasayan actor'lari CURRENT settlement'a filtreleyerek Domain-free DTO listesi verir.
- `bool TryReadActor(ActorId id, out ActorViewState state)` — `WorldProjection.cs:39-46` — id-keyed billboard sync (SOUL-04).
- `bool TryReadActor(string actorName, out ActorViewState state)` — `WorldProjection.cs:23-35` — legacy isim yolu.
- `ActorViewState ProjectActor(ActorRecord actor)` — `WorldProjection.cs:87-99` — grid→world XZ projeksiyonu + `DescribeActivity` verb + `sleeping = Sleep action` + `actionKind = KindName`.
- `string DescribeActivity(ActorRecord actor)` — `WorldProjection.cs:105-110` — CurrentAction varsa `ActionVerbTable.Verb`, yoksa `DescribeScheduleWord`.
- `string DescribeScheduleWord(ActorRecord actor)` — `WorldProjection.cs:117-132` — Guard→"on watch", Enemy→"hunting", diger her sey null (W32-W34 tahmin branch'lari olu).
- `ulong CurrentSettlementKey` — `Travel.cs:29` — `CurrentSettlementOrStart.Value` (spawner despawn sinyalidir).
- `bool TryBeginTravelToSettlement(...)` — `Travel.cs:31-79` — player actor'u destination site merkezine tasir, `_currentSettlement` yazilir, `_billboardOriginResolved=false`.
- `bool TryTravelToSettlement(string, out string)` — `Travel.cs:103-111` — legacy sync yol (14-day capped) proof driver'lar icin.
- `void SeedWorld(string mood, string calling, string startLocation, uint? worldSeed=null)` — `Worldgen.cs:26-83` — wizard tuple → deterministic seed → `PlanetWorldService.GetOrGenerate` → `HydrateGeneratedWorld` → `Overland` projeksiyonu → main-quest arm.
- `void ApplyCharacterCreation(string playerName, string classId, string birthsignId)` — `Worldgen.cs:85-125` — Class+Birthsign stat/vital'lerini player actor'e uygular.
- `bool TryCastSpell(int spellSlotIndex)` — `Combat.Spells.cs` — mana/cooldown/target kontrol + refuse loglama.
- `bool TryMeleeStrike(string targetActorName, int rawDamage)` — `Combat.Melee.cs` — hedef domain actor'a hasar.
- `bool TryInteract(ActorId actorId)` / `bool TryInteract(string targetTag)` — `Combat.Interaction.cs` — E-key ile world objesi/actor etkilesimi.
- `IDialogSource GetDialogSource(ActorId id)` / `IDialogSource GetDialogSource(string actorName)` — `Dialog.Source.cs` — id-keyed dialog binding (name overload legacy fallback).
- `string ConsultFate()` / `ConsultFate(string question)` / `TryConsumeResolvedFate()` — `Fate.cs` — sync placeholder + async LLM polling.
- `string ExportStateJson()` / `void RestoreStateJson(string json)` — `Save.cs` — `JsonSliceSaveService` proxy.
- `void LogCombat(string message)` — root partial — `_lastCombatLine` set + Debug.Log.
- `void TakePlayerDamage(int amount)` — `Combat.cs` — player vitals decrement.
- `static string Verb(ActorActionType kind)` — `ActionVerbTable.cs:18-38` — 11 satirlik enum→string map (unknown → `Unknown` sentinel).
- `static string KindName(ActorActionType kind)` — `ActionVerbTable.cs:41-55` — `ActorViewState.ActionKind` icin stable string; None → null.
- `static void Register(IDomainSimulationAdapter)` / `Clear()` — `IDomainSimulationAdapter.cs:294-302` — scene-scoped singleton wire/reset.

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Adapter Presentation katmanindadir; `FieldOwnershipRegistry` tick-composer sistemlerinin (`living.*`, `econ.*`, `world.*`) domain field'lara yazma iznini deklare eder. Adapter kendi basina bu tabloda yer almaz - o "Player'in Domain'e komut yolu" ve "Domain'in Presentation'a okunma yolu" olarak durur.

- **Adapter uzerinden Presentation'a OKUNAN Domain alanlar** (read-only projection): `WorldState.Actors` (WorldProjection, GetSpawnableActors, TryReadActor), `Actor.Position` / `Actor.ActionState.CurrentAction` / `Actor.IsAlive` / `Actor.Vitals` / `Actor.Needs` / `Actor.Name` / `Actor.Role` (`ActorRecord` uzerinden), `WorldState.Sites` (`BillboardOrigin`, `CenterOfSite`), `WorldState.Overland` (`Overland` property + `PlayerOverlandTile`), `WorldState.Events` (`RecentWorldEvents`), `WorldState.Time`, `WorldState.WorldProfile`.
- **Player komut yolu ile Domain'e YAZILAN alanlar** (adapter Domain API'sini cagirdigi noktada owner o Domain sistemidir, degil adapter):
  - `WorldState.RoomSeed` — `Worldgen.SeedWorld:35` (bootstrap-only, world tabula rasa iken).
  - `WorldState.WorldProfile` — `Worldgen.SeedWorld:52-63`.
  - `WorldState.Overland` — `Worldgen.SeedWorld:73-75`.
  - `Actor.Position` (player) — `Travel.TryBeginTravelToSettlement:60` (`player.MoveTo`); FieldOwnershipRegistry `Actor.Position`'un tick-composer writer'larini tanir - adapter'in player-move'u Domain `ActorRecord.MoveTo` uzerinden Domain'e yazar (adapter tick disi fiat write yapmaz).
  - `WorldState.Actors` (player replace) — `Worldgen.ApplyCharacterCreation:120` (`_world.ReplaceActorView(ActorRole.Player, replacement)`).
  - `Actor.Vitals` (player rest / player damage) — `Travel.ApplyRest:99` + `Combat` partial `TakePlayerDamage`.
- **Adapter icinde YASAYAN presentation-only state** (Domain'de degil): `_tick`, `_billboardOrigin/_billboardOriginResolved`, `_currentSettlement`, `_lastCombatLine`, `_activeDialogActor/Id/NpcId`, `_conversation`, `_pendingFate/_pendingFateFollowups`, `_isFateThinking/_isDialogThinking`, `_topicAskCounts`, `_streamingPartialLine`, `_suppressGlobalTopicFallback`, `_mainThreadApply` (DET-02 kuyrugu). Bunlarin hicbiri `FieldOwnershipRegistry` icinde tanimli degildir cunku Domain snapshot'ina girmezler.

## LLD - Ürettiği/Tükettiği Olaylar

- **Tuketilen** (`AdvanceTick` icinde): `_world.Events` yeni event'lerini `PublishEventEchoes` uzerinden actor-adli floating echo'lara cevirir (`_echoCursor` current-end'de baslar - save yuklerken 10k eski event replay olmaz). `_world.Plants` uzerinden dominant stage `PublishFieldMirror` ile field visual'a yayilir (`_lastPlantsHash` degistiginde).
- **Uretilen**:
  - `_lastCombatLine` — `LogCombat`, `ApplyCharacterCreation` sonu ("X begins as Class under Birthsign.").
  - Combat pipeline event/log satirlari — `Combat.Melee.cs`, `Combat.Spells.cs`, `TakePlayerDamage`.
  - `Domain Seeded: seed=... style=... ...` — `Worldgen.SeedWorld:76-80` UnityEngine.Debug.Log tek satir bootstrap ozeti.
  - Dialog akisi: `Dialog.*` partial'lari asenkron LLM cikislarini `_streamingPartialLine`'a besler ve `TryConsumeResolvedFate`/`GetCurrentLine` uzerinden UI'a saglar.
- **Kanal-disi yol**: `ActionLogDebugSink.Enabled = HasActionLogFlag()` — root partial ctor'da `--ember-proof-screenshots` veya `--ember-action-log` bayragi varsa `[Action]` phase mirror greppable log akisini acar (normal oynanis payini omez).

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

- `Assets/Tests/EditMode/Presentation/VisualLayer/ActivityLabelTruthTests.cs` — `ActionVerbTable.Verb` 11 satirlik verbatim assert (`Verb(MoveToFood)="seeking food"`, ... `Verb(PerformWork)="working"`) + `KindName(None)==null` + kod-icinde `ActionVerbTable.Verb` cagrisinin varligi lint'i.
- `Assets/Tests/EditMode/Actions/WorkStoryChainTests.cs` — W34-C is-hikayesi; `smith.ActionState.CurrentAction == PerformWork` iken `ActionVerbTable`'in "working" verdigini pinler (satir 46-52).
- `Assets/Tests/EditMode/Actions/SleepInterruptionTests.cs` — W34-B uyku-hikayesi; `MoveToBed`/`Sleep` action akisi + label verbatim.
- `Assets/Tests/EditMode/Audit/AuditSixthPassCoverageTests.cs` — Codex 6. audit C-P2 rol-interface ayrimini pinler (`IEmberClockSink`, `IWorldViewReadModel` vs. aggregate).
- `Assets/Tests/EditMode/Audit/AuditSeventhPassCoverageTests.cs` — 7. audit C-P3 #12 default-method shim retirement + `EmberDomainAdapterLocator.ClockSource/HudReadModel/...` narrow accessor pin.
- `Assets/Tests/EditMode/Audit/AuditFourthPassTailCoverageTests.cs` — `RecentWorldEvents` tail snapshot kontrat.
- `Assets/Tests/EditMode/Audit/SelectSpellTargetTests.cs` — `TryCastSpell` slot->target kontrat.
- `Assets/Tests/EditMode/Audit/EmberWorldGenIntentHandoffTests.cs` — `SeedWorld` intent (mood/calling/start) → `WorldProfile` handoff pin (W30-W31).
- `Assets/Tests/EditMode/AiDm/LlmToolAuthorityTests.cs` — `IConsultFateOracle` async round-trip + `TryConsumeResolvedFate` bir-kere-drain kontrati.
- `Assets/Tests/EditMode/Presentation/JournalSourceTests.cs` — `IJournalSource` (adapter Journal partial) kontrat.
- `Assets/Tests/EditMode/Presentation/PlayableLoopCraftQuestTests.cs` — `ITradeSource/ICraftingSource/ILevelUpSource/ICombatScreenSource` full player-loop pin.
- `Assets/Tests/EditMode/Diagnostics/MarathonPassGateCensusTests.cs`, `Assets/Tests/EditMode/Diagnostics/ProofLivingCensusPeaksTests.cs` — `EmberDomainAdapterLocator` uzerinden marathon proof harness'in adapter census'unu pinler.

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W32 (SOUL-04 spawn-from-worldgen + label-truth)**: `SpawnableActor` DTO'su ve `IWorldViewReadModel.GetSpawnableActors()` eklendi (`IDomainSimulationAdapter.cs:81-90, 106-122`); `ProjectActor` ile spawner ayni grid→world formulunu paylasti (`WorldProjection.cs:140-163`). `ActionVerbTable` dogdu (`ActionVerbTable.cs`) ve `DescribeActivity` bir "action varsa VERBATIM Verb, yoksa dar schedule word" kontratina indi - `WorldProjection.DescribeScheduleWord` icindeki EAT tahmin branch'i (12-14 plaza "eating", aclik→"to the tavern") DELETED. `CurrentSettlementKey` (`IDomainSimulationAdapter.cs:91-96` + `Travel.cs:29`) `IWorldViewReadModel`'e eklendi; `EmberGeneratedActorSpawner:89-105` bunu bir onceki settlement key'i ile kiyaslayip stale billboard'lari `Destroy` etti - "npc ler koyden uzaklasmaya basliyorlar" live-bug'ini kapatti. `_billboardOriginResolved` bayragi `TryBeginTravelToSettlement`'ta `false`'a cekildi (`Travel.cs:62`), `BillboardOrigin` yeniden resolve etti.
- **W33 (FARM verb suit)**: `ActionVerbTable`'a `MoveToPlot="to the field"`, `PlantSeed="planting"`, `HarvestCrop="harvesting"`, `HaulCrop="hauling"` satirlari eklendi (`ActionVerbTable.cs:22-28`). `WorldProjection.DescribeScheduleWord` icindeki FARM guess branch'i (crop-belt proximity → "harvesting"/"tending the field") DELETED (`WorldProjection.cs:126-128` yorum). `IPlayerCommandSink` default-method shim'leri (7. audit C-P3 #12) retire edildi - `TryCastSpell/TryMeleeStrike/TryInteract` her implementation'da acikca override zorunlulugu geldi (`IDomainSimulationAdapter.cs:132-149`).
- **W34 (SLEEP + WORK verb suit)**: `ActionVerbTable`'a `MoveToBed="heading home"`, `Sleep="sleeping"`, `MoveToWorksite="to work"`, `PerformWork="working"` satirlari eklendi (`ActionVerbTable.cs:31-36`). `WorldProjection`'in gece guess branch'i (20-22 "winding down", hour+Chebyshev "sleeping") DELETED (`WorldProjection.cs:122-126`); WORK guess branch'i (schedule-derived "working") DELETED (`WorldProjection.cs:129-132`). `ProjectActor.sleeping` bayragi `Actor.ActionState.CurrentAction == Sleep`'e bagli olarak Domain'den okunuyor (`WorldProjection.cs:96`). RUH_TESHIS §2.9 tahmin katmani tamamen olu.
- **W35 (partial split leveling)**: `DomainSimulationAdapter.cs` "shared state only" kontratina indirildi (root partial 112 satir, `WorldState` + `JsonSliceSaveService` + `WorldTickComposer` + 15 kadar `_field`). Yeni partial'lar: `Combat.Helpers`, `Combat.Interaction`, `Combat.Melee`, `Combat.Spells` (Combat mono-partial'i 4 sorumlulukla ayrildi); `Dialog.Binding`, `Dialog.Greetings`, `Dialog.Source`, `Dialog.Text`, `Dialog.Topics` (Dialog'un 5-way'i); `Worldgen.Hydration`, `Worldgen.Npcs`, `Worldgen.NpcStats`, `Worldgen.Player`, `Worldgen.Production`, `Worldgen.Selection`, `Worldgen.State` (Worldgen'in 7-way'i); `WorldProjection`, `WorldQuests`, `WorldRows`, `WorldEncounter`, `QuestGuidance`, `QuestInteraction`, `QuestProgress`, `MainQuest`, `Overland`, `Haunters`, `Journal`, `LevelUp`, `Trade`, `Crafting`, `CombatScreen`, `Travel`, `Save`, `Fate`, `Hud`, `Clock`. Toplam **40 partial** (task hint'inde "24" idi - bu W32 draft'inin stale count'u; her partial `sealed partial class DomainSimulationAdapter` ile tek dosyada tek sorumluluk).
- **W36 (id-keyed dialog + save fidelity)**: `DLG-01` id-keyed `GetDialogSource(ActorId)` overload + `_suppressGlobalTopicFallback` bayragi (`IDomainSimulationAdapter.cs:151-167`, `DomainSimulationAdapter.cs:52-56`) - id resolve olmayinca "no one here" yerine global topics'e sessizce dusme retire edildi. `TryInteract(ActorId)` (`IDomainSimulationAdapter.cs:141-147`) actor id ile etkilesim kanonu. `IEmberSaveBridge` round-trip contract EditMode round-trip test suite'i altinda pinlendi. `SeedWorld` imzasi `uint? worldSeed = null` ile geniletildi (nullable seed - Jun 12 observation'i).

## Bilinen Borçlar + Kaçak Kapıları

- **Aggregate interface hala varliginin borcu**: `IDomainSimulationAdapter` hala 6 rolu tek yerde aggregate ediyor - Codex C-P2 not'u der ki "New callers should depend on the narrower role interfaces above". Uygulamada `WorldSceneDirector`, `EmberProofScreenshotDriver`, `EmberInteractable` gibi eski cagri yerleri hala `EmberDomainAdapterLocator.Current` (aggregate) alir. Yeni site'lar dar arayuz kullanmali; yenileme henuz bitmedi.
- **`UnavailableSimulationAdapter` sessiz false**: `TryMeleeStrike`, `TryCastSpell`, `TryInteract` hep `false` doner (`UnavailableSimulationAdapter.cs:54-57`); `ConsultFate` "unavailable" mesajini surekli tekrar eder. Bu bilincli bir dusustur (adapter boot edememis) ama test/proof kosumu Unavailable'a dustugunde hicbir suret ekranda uyarilmaz.
- **Ctor'da eager site hydration**: root partial ctor (`DomainSimulationAdapter.cs:74-92`) `_world.Sites`'in tum record'larini _saveService.Worksites'e boot-time'da mirror'lar (`Add` if not exists). Buyuk worldgen (~200 settlement, ~2000 site) durumunda O(sites) - kabul edilebilir ama planlanan streaming save-service'in geldiginde bu kopya zombie kalir.
- **`BillboardOrigin` lazy resolve tuzagi**: `_billboardOrigin` ilk `BillboardOrigin()` cagrisina kadar `(0,0)` doner. Travel `_billboardOriginResolved=false` ile origini re-resolve tetikler; site henuz hydrate edilmemisse origin `(0,0)`'da kalir ve `GetSpawnableActors` bir sonraki frame'de gercek konumu kaydeder - ilk frame'de billboard'lar yanlis noktaya spawn edip ikinci frame'de sync ile yerine gelir (koruma: BillboardOrigin cache ancak site.TryGet true olunca set edilir).
- **`_topicAskCounts` reset kanali yok**: Player ayni topic'i tekrar tekrar sorunca sayaci artar ve LLM prompt varyasyonuna eklenir - fakat conversation kapandiginda temizlenmiyor. Uzun sessions'ta dictionary buyuyebilir (pratikte "actor#topic" key'i bounded oldugundan risk kucuk).
- **`_mainThreadApply` istisna yeme**: `DrainMainThreadApply` (`Clock.cs:23-28`) apply exception'ini sessizce swallow eder ("a queued apply must never break the tick"). Bir LLM continuation Domain'e bozuk snapshot yazmaya calisirsa tick akmaya devam eder ama write kayiptir - proof harness log grep'i ile catch edilmeli.
- **`ActionVerbTable.Unknown` bir kez warn**: Yeni `ActorActionType` eklenip `Verb` satiri eklenmezse ekran `(NewKind)` gosterir ve sadece ilk gecte `Log.Warn` atar (`ActionVerbTable.cs:57-64`). "Sessiz gecmis olabilir" durumu warn'in loglarda kaybolmasi ile mumkun; W32 DOC5 §4 lint bunu compile-time'a cekemedi (enum ekleme run-time).
- **Locator scene-scoped ama non-thread-safe**: `_current` static field, `Register`/`Clear` naive assign. Additive scene loads ust uste `Register` cagirir - Codex C-P3 #C5 uyarisi der ki "overwrite without warning" - test tear-down `Register(null)` cagirmayi UNUTURSA bir sonraki test-run stale adapter tutar.

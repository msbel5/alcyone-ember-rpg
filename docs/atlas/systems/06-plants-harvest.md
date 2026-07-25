# 06-plants-harvest

## HLD - Ne ve Neden (5-10 cumle)

Bitki-hasat sistemi, dünyanın **canlı ekin** halkasıdır: soil hücrelerine iliştirilen `PlantComponent` satırları veri-tanımlı büyüme kurallarıyla `seed → sprout → ripe` yolunu **gün başına deterministik** ilerler, ripe olan bitkiyi bir **bodied hasatçı** eline alır, hasatçı yükü stockpile'a **yürüyerek** taşır. W33 öncesinde bu zincirin merkezi bir `world.harvest@Daily:25` fiat'ıydı: yakında bir el gördüğü an tabaka +2 yaz, bitkiyi seed'e sar - RUH_TESHIS §2.8'in "ışınlanan madde" kayıtlı hastalığı. W33 slice'ı o fiat'ı öldürdü, `HarvestCropAdvancer` (2 tick, plant→hands atomik commit) ve `HaulCropAdvancer` (yürü + ilk reach-contact'ta hands→pile) çiftini gerçek eyleme bağladı. W35 (B04) `FieldOwnershipRegistry`'ye `World.Plants` ve `World.Soils` yazar deklarasyonlarını ekledi - artık lint her iki mağarayı biliyor. W36 (B27) `PlantGrowthStep`'in hardcoded `isSnowing: false` telini kapadı: kaba `isSnowing = (season == Winter)` gate gerçek kar-blokajlı türleri kışın dondurur. Sunum tarafında 2026-07-24 REFORM #1 (P0-4) polar `RuntimeFieldBuilder` belt'ini emekliye çıkardı; `SimFieldView` sim'in `PlantComponent`'lerini `RuntimeFieldMirror.Plants` üzerinden **hücre başına stalk** olarak projekte ediyor - görsel alan artık sim alanının kendisi. Kısacası: **büyüme sistemi + hasat/haul action'ları + sim-görsel union** üç ayaklı bir tabure; hiçbirinin yerine öbürünün konuşmasına izin yok.

## HLD - Akış (numaralı adımlar)

1. **Boot**: `DomainSimulationAdapter.Worldgen.Production` başlangıç yerleşiminin merkezine 1 `WorksiteKind.Field` + 1 soil + 1 seed-tabakasında `PlantComponent` yerleştirir (SOUL-01 sabiti; deterministic anchor).
2. **Daily (@20)**: `PlantGrowthStep` her `PlantSpeciesDef` için `PlantGrowthSystem.AdvanceOneDay(species, world.Plants, world.Events, now, season, isSnowing: season == Winter)` çağırır. Species önce `CanGrow(season, isSnowing)` gate'inden geçer (W36); geçenler için her satırda `DaysInStage++`; `DaysToNextStage`'a ulaşan satır `TryGetNextStage` ile bir sonraki stage'e sarar ve `WorldEventKind.PlantStageAdvanced` event'i yazar.
3. **Shortage (@27)**: Stok < 4 iken `FarmingJobRequestFactory` bir `Plant` job'u board'a asar (bkz. `07-farming-jobs`); job atanınca aktör `ActorIntent.Plant → MoveToPlot → PlantSeed` zincirine girer. Bu zincir plant'ı **doğurur** (`FarmPlantAuthorshipTests` §F1); yeni plant `Plants` store'una PerTick:22 writer'ı üzerinden eklenir - Growth step onu yarın sabahtan itibaren ilerletir.
4. **Harvest kararı**: `ActorIntent.Harvest + MoveToPlot` completion'ında lifecycle `NextLinkFor` → `HarvestCrop` seçer. Hasatçı reservasyon `plotKey`li satırı elinde, `HarvestCropAdvancer` çalışır.
5. **Harvest tick 1-2**: 2 tick ilerler (`HarvestDurationTicks`), her tick öncesi (a) reservation kaybı, (b) plant kaybı / `IsHarvestable` düşmesi, (c) `Chebyshev(actor, plant) > HarvestReachCells (2)` şart-hatası bakılır - üçünden biri `Fail` (reservasyon serbest, matter konserve).
6. **Harvest commit (tick 2)**: **Atomik**: `Plants.Remove(plant.Id)` + `Soils.Replace(soil.WithoutPlant())` + `WorldEventKind.PlantHarvested` event + plot-row release + carry-row `TryReserve(CarryKey(species), untıl = now + haulWalk + 60)` + `state.WithCarriedUnits(HarvestYieldUnits: 2).Succeeded()`. Hiçbir chunk sınırı "yield basıldı ama bitki hâlâ ayakta" veya tersini görmez (W33-01 §6 CONSTRAINT).
7. **Haul walk**: `HaulCropAdvancer` her tick: carry-row'un tag'ini `TryParseCarryKey`'le okur; `FoodOperations.WithinEatReach`'te değilse `MovementService.StepToward(actor.Position, siteCentre, world.NavView)`.
8. **Haul deposit**: Reach-contact anında `FarmOperations.FindOrCreatePile(world, siteId).Add(cropTag, CarriedUnits)` + carry-row release + `state.WithCarriedUnits(0).Succeeded()`. Add-only (kapasite/rezervasyon yok) - varış sırası önemsiz.
9. **PerTick clock**: `DomainSimulationAdapter.Clock` her tick `PlantComponent.Rows`'u iterate eder, home-site plant'larını **hash**'ler, sadece hash değişirse `RuntimeFieldMirror.PublishPlants(cellArray)` yapar (redundant publish yok). Ayrıca dominant stage'i `RuntimeFieldMirror.Publish(count, stage)` ile ilan eder.
10. **Presentation poll**: `SimFieldView.Update` (1.5s periyot) `PlantsStamp` değiştiyse cell dictionary'sini diff'ler; new id → yeni plot GameObject + `CropStalkView` (`ExternalStage = cell.Stage`); missing id → parent plot destroy. `CropStalkView` (2s periyot) stage değişince target height ve palette rengini set eder, sonra frame-başı `MoveTowards` ile büyür/söner (harvest'ta "birden yok olma" playtest fix).

## LLD - Veri Modeli (file:line)

- **PlantComponent** (immutable, id-only) - `Assets/Scripts/Domain/Process/PlantComponent.cs:9-58`
  - `WorldComponentId Id` / `SiteId SiteId` / `GridPosition Position` / `string SpeciesId` / `PlantStageId StageId` / `int DaysInStage`
  - `WithStage(newStageId)` → `DaysInStage=0` sıfırlar; `WithDaysInStage(n)` sadece sayaç.
- **PlantSpeciesDef** (species catalog satırı) - `Assets/Scripts/Domain/Process/PlantSpeciesDef.cs:11-101`
  - `SpeciesId`, `SeedItemTag`, `HarvestItemTag`, `IReadOnlyList<PlantGrowthStageDef> Stages`, `IReadOnlyList<PlantGrowthRule> GrowthRules`
  - `TryGetStage(id, out stage)` / `TryGetNextStage(id, out next)` / `CanGrow(season, isSnowing)` / `FirstStage`.
- **PlantGrowthStageDef** - `Assets/Scripts/Domain/Process/PlantGrowthStageDef.cs:6-27`
  - `PlantStageId Id`, `string DisplayName`, `int DaysToNextStage` (0 = terminal), `bool IsHarvestable`.
- **PlantGrowthRule** - `Assets/Scripts/Domain/Process/PlantGrowthRule.cs:5-28`
  - `Season Season` (`Season.None` = wildcard), `bool AllowsGrowth`, `bool BlockedBySnow`.
  - `Matches(season)` + `CanGrow(isSnowing) = AllowsGrowth && (!isSnowing || !BlockedBySnow)`.
- **WorldState mağaraları** - `Assets/Scripts/Domain/World/WorldState.cs:54-58` yorumu
  - `world.Plants : ComponentStore<PlantComponent>` (SOUL-01 nedeniyle world root'unda)
  - `world.Soils : ComponentStore<SoilComponent>` (plant reference'ını `PlantId` üzerinden taşır; `WithPlant`/`WithoutPlant`).
- **Presentation mirror** - `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeFieldBuilder.cs:11-45`
  - `RuntimeFieldMirror.PlantCount / StageIndex` (agregat) + `PlantCell[] Plants + int PlantsStamp` (REFORM #1, cell array + monotonic version).
  - `PlantCell { ulong Id; int LocalX; int LocalZ; int Stage; }`.

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

- `PlantGrowthSystem.AdvanceOneDay(PlantSpeciesDef species, ComponentStore<PlantComponent> plants, WorldEventLog eventLog, GameTime now, Season season, bool isSnowing) : int` - `Assets/Scripts/Simulation/Process/PlantGrowthSystem.cs:17-79` - Species'in `CanGrow` gate'inden sonra o species'in tüm satırlarında `DaysInStage++` yapar, eşik dolarsa `WithStage(next)` + `PlantStageAdvanced` event yazar, ilerleyen bitki sayısını döner.
- `PlantSpeciesDef.CanGrow(Season, bool isSnowing) : bool` - `Assets/Scripts/Domain/Process/PlantSpeciesDef.cs:91-99` - İlk `Matches(season)` kuralının `CanGrow(isSnowing)` sonucunu döner; kural yoksa false (winter/summer default freeze).
- `PlantGrowthRule.CanGrow(bool isSnowing) : bool` - `Assets/Scripts/Domain/Process/PlantGrowthRule.cs:24-27` - `AllowsGrowth && (!isSnowing || !BlockedBySnow)` snow-gate kontratı.
- `HarvestCropAdvancer.Step(WorldState, ActorRecord, GameTime) : void` - `Assets/Scripts/Simulation/Living/Actions/HarvestCropAdvancer.cs:32-89` - Rezervasyon/plant/reach fail-fast; 2 tick ilerledikten sonra plant remove + soil `WithoutPlant` + `PlantHarvested` event + carry-row rezerve + `CarriedUnits = 2` atomik commit.
- `HaulCropAdvancer.Step(WorldState, ActorRecord, GameTime) : void` - `Assets/Scripts/Simulation/Living/Actions/HaulCropAdvancer.cs:16-64` - Boş el / row kayıp fail; site centre'a `StepToward` (siteless world'de permissive); `WithinEatReach`'te `FindOrCreatePile.Add(cropTag, units)` + release + `WithCarriedUnits(0).Succeeded()`.
- `FarmOperations.IsHarvestable(IReadOnlyList<PlantSpeciesDef>, PlantComponent) : bool` - `Assets/Scripts/Simulation/Living/Actions/FarmOperations.cs:98-108` - Species catalog'da lookup + stage'in `IsHarvestable` bayrağı.
- `FarmOperations.FindOrCreatePile(WorldState, SiteId) : StockpileComponent` - `Assets/Scripts/Simulation/Living/Actions/FarmOperations.cs:112-` - Site'ın stockpile'ını bulur veya oluşturur (haul deposit hedefi).
- `FarmOperations.Chebyshev(GridPosition, GridPosition) : long` - `Assets/Scripts/Simulation/Living/Actions/FarmOperations.cs:124-` - Yürüyüş ve reach ölçümleri için 8-yönlü mesafe.
- `FarmOperations.HarvestReachCells : const int = 2` - `Assets/Scripts/Simulation/Living/Actions/FarmOperations.cs:20` - Retired `HarvestHandsService.ReachCells`'ten VERBATIM.
- `FarmOperations.CarryKey(string cropTag) : string` / `TryParseCarryKey` / `TryParsePlotKey` - `Assets/Scripts/Simulation/Living/Actions/FarmOperations.cs:32-58` - Tek-yazar rezervasyon tag formatı ("plot:soilId" / "carry:cropTag").
- `HarvestCropAdvancer.HarvestDurationTicks : const int = 2` / `HarvestYieldUnits : const int = 2` - `Assets/Scripts/Simulation/Living/Actions/HarvestCropAdvancer.cs:19-22` - W33-01 §5 tek konut + retired HarvestStep "+2" ekonomik kalibrasyonu.
- `DefaultTickSystems.PlantGrowthStep.Run` - `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs:467-505` - Daily @20 step; `SeasonCalendar.SeasonAt`'i `Time.TotalMinutes`'tan alır, her species için `AdvanceOneDay(..., isSnowing: season == Season.Winter)` (W36 wound-close).
- `RuntimeFieldMirror.PublishPlants(PlantCell[]) : void` - `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeFieldBuilder.cs:40-44` - Cell array'i set + `PlantsStamp++` (SimFieldView'un poll tetikleyicisi).
- `RuntimeFieldMirror.Publish(int plantCount, int stageIndex) : void` - `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeFieldBuilder.cs:29-33` - Dominant stage'i clamp'leyerek agregat kanala yayınlar (legacy belt fallback için).
- `SimFieldView.Update()` - `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeFieldBuilder.cs:157-181` - Stamp diff varsa alive-set kur, missing id → destroy, new id → `BuildPlot`, mevcut → `ExternalStage = cell.Stage`.
- `SimFieldView.BuildPlot(PlantCell) : CropStalkView` - `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeFieldBuilder.cs:183-206` - LocalX/LocalZ'de bir soil + bir stalk cube; collider yok (walk-through crops).
- `CropStalkView.Update()` - `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeFieldBuilder.cs:70-95` - `ExternalStage ?? RuntimeFieldMirror.StageIndex` seçer; `_targetHeight = StageHeights[stage]`; her frame `MoveTowards(0.18/s)` ile büyür/söner (playtest fix "ekinler birden yok oluyor").
- `RuntimeFieldBuilder.BuildBelt / Build` - `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeFieldBuilder.cs:105-149` - Polar arc üstünde 15 stalk'lı `FarmBelt` (REFORM #1 sonrası retired; sadece `SimFieldView`'un boş olduğu senaryolar için fallback dekor).
- `DomainSimulationAdapter.Clock.PublishPlants pass` - `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Clock.cs:90-132` - Home-site plant satırlarını gezip cell listesi + FNV-1a hash kurar; hash değişirse `PublishPlants`; her tick agregat `Publish`.

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Kaynak: `Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs:78-99`.

- **World.Plants** yazarları:
  - `econ.plantgrowth@Daily:20` - `PlantGrowthStep` (stage advancement, `WithStage`/`WithDaysInStage`).
  - `living.action_advance@PerTick:22` - `PlantSeedAdvancer` (birth) + `HarvestCropAdvancer` (removal).
- **World.Soils** yazarları:
  - `living.action_advance@PerTick:22` - `PlantSeedAdvancer` (`WithPlant` on plant), `HarvestCropAdvancer` (`WithoutPlant` on harvest).
- **World.Reservations** (bu sistemin *kiracısı*): `HarvestCropAdvancer` plot-row release + carry-row `TryReserve`; `HaulCropAdvancer` carry-row release. Formal writer sahibi `living.action_advance@PerTick:22`.
- **World.Events** ekleyicileri: `PlantGrowthStep` (`PlantStageAdvanced`), `HarvestCropAdvancer` (`PlantHarvested`).
- **World.Stockpiles / SiteInventory** yazarı: `HaulCropAdvancer.Add(cropTag, units)` (bkz. `08-inventory-stockpile`).
- **RuntimeFieldMirror.Plants / PlantsStamp / StageIndex / PlantCount** yazarı: `DomainSimulationAdapter.Clock` PerTick pass (presentation-kutup, ownership registry kapsamı dışında).

## LLD - Ürettiği/Tükettiği Olaylar

- **Ürettiği** (`WorldEventKind`):
  - `PlantStageAdvanced` - `PlantGrowthSystem.AdvanceOneDay`; `ReasonTrace = [plant_growth, site:{siteId}, plant:{plantId}, species:{id}, from:{prevStage}, to:{nextStage}]`; key `plant_stage_advanced:{siteId}:{plantId}:{newStageId}`.
  - `PlantHarvested` - `HarvestCropAdvancer.Step` atomik commit satırı; description `harvested species:{speciesId} qty:{HarvestYieldUnits} by:{actorId}`; author `actor.Id`. **Retired writer**: `world.harvest@Daily:25` (fiat teleport) - grammar VERBATIM taşındı, sadece author artık gerçek.
  - `ActorActionCompleted` (`HarvestCrop` ve `HaulCrop` succeeded transition'ları) - `ActionLogManager` yolundan.
- **Tükettiği** (typed input, event kanalından değil):
  - `Season season` ← `SeasonCalendar.SeasonAt(world.Time)` (W36 wound-close bunu snow-gate'e çeviriyor).
  - `WorkOrderLedger.PlantJob` completion → PlantSeed → yeni `PlantComponent` (bkz. `07-farming-jobs`).
  - Shortage cascade `econ.shortage_response@Daily:27` → planting job (Plants store'una dolaylı input).

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

- `Assets/Tests/EditMode/Process/PlantGrowthSystemTests.cs` - `AdvanceOneDay` pure unit'i (age++/stage-flip/species-mismatch skip/eventlog satırı).
- `Assets/Tests/EditMode/Process/PlantDefinitionTests.cs` - `PlantSpeciesDef` ctor validation + `TryGetStage/TryGetNextStage/CanGrow`.
- `Assets/Tests/EditMode/Composition/PlantGrowthSnowGateWireTests.cs` - **W36 B27 story test**: `PlantGrowthStep` gerçekten `isSnowing = (season == Winter)` geçiriyor mu (BlockedBySnow tür kışın donuyor, winter-tolerant devam ediyor, ilkbaharda ikisi de ilerliyor).
- `Assets/Tests/EditMode/Process/HarvestSystemTests.cs` - Legacy hasat harness satırları (retired sisteme referanslar temizlendi; kalan invariant satırları action-yolunu doğruluyor).
- `Assets/Tests/EditMode/Process/PlantingSystemTests.cs` - Retired PlantingSystem'in kontratı; new PlantSeedAdvancer'a köprü.
- `Assets/Tests/EditMode/Actions/FarmPlantAuthorshipTests.cs` - **W33 F1**: plant sadece completed PlantSeed'ten doğar; remote/no-actor birth refuse.
- `Assets/Tests/EditMode/Actions/FarmHarvestTeleportDeathTests.cs` - **W33 F2**: hasatçı yoksa 3 daily boundary boyunca hiçbir şey değişmez (fiat teleport ölü kaldığının kanıtı).
- `Assets/Tests/EditMode/Actions/FarmPlotReservationConflictTests.cs` - **W33 F3**: iki farmer bir cell → deterministic winner, loser replan, interrupt = release.
- `Assets/Tests/EditMode/Actions/FarmHaulConservationTests.cs` - **W33 F4**: `TotalCrop = pile + hands + ripe-plot potential` her interrupt noktasında düz - dup/kayıp yok.
- `Assets/Tests/EditMode/Actions/FarmStoryChainTests.cs` - **W33 F5** (capstone): shortage → plant → grow → harvest → haul → eat tam zinciri.
- `Assets/Tests/EditMode/Process/FarmingJobIntegrationTests.cs` - Job → PlantSeed → Plant birth halkası (bkz. `07-farming-jobs`).
- `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs` - Daily:20 stage-advance ve PerTick:22 harvest/haul dijeste yansıyor (golden re-baseline W33 sonrası).
- `Assets/Tests/EditMode/Composition/ActionPhaseChunkingInvarianceTests.cs` - Doc 04 §F6: HarvestCrop commit satırı chunk sınırında split olmuyor.
- `Assets/Tests/EditMode/Composition/WorldTickRegistryTests.cs` - `World.Plants`/`World.Soils` yazar listesi (W35 B04 lint gate).
- `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` - "Bitki yıllarca ayakta kalırsa" living-world alarm.

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W32 (2026-07-25, 5049d445 "EAT slice")** - Bu sistemin doğrudan mutasyonu yok; ama W32 rezervasyon patterni (`ItemTag`, TTL, `TryGetByActor`) W33 hasat/haul akışının **iskeleti** oldu. `Reservations` mağarasına tek-yazar-per-actor kuralı bu sistemin plot→carry row swap'ının önkoşulu.
- **W33 (2026-07-25, 61e340f3 "the FARM slice: crops travel the world, teleports are dead")** - EN BÜYÜK HAREKET. Retired: `HarvestStep` (world.harvest@Daily:25 fiat teleport). Doğdu: `HarvestCropAdvancer` (2 tick, atomik plant→hands commit), `HaulCropAdvancer` (yürü + reach-contact deposit), `FarmOperations` (HarvestReachCells=2, Chebyshev, CarryKey/plotKey parse, IsHarvestable, FindOrCreatePile), `PlantSeedAdvancer` (plant'ı bodied doğuran tek yolu). Event grammar VERBATIM (PlantHarvested + description) - sadece author artık gerçek aktör. Lifecycle wiring: `(Harvest, MoveToPlot) → HarvestCrop → (Harvest, HarvestCrop) → HaulCrop`. Story tests F1-F5 + FarmSliceWorld fixture.
- **W34 (2026-07-25, 3aa87cf6 "SLEEP + WORK slices")** - Plant sistemine doğrudan dokunmadı ama tüm bodied-action patternini tekrar-eden şablonu (`SleepOperations`, `WorkOperations`, TryDecideX + NextLink + retirement) FarmOperations ile aynı diyeti pinledi. Bunun anlamı: bu sistemdeki `FarmOperations` API'si artık üç kardeşin ortak dili.
- **W35 (2026-07-25, 20a3b899 "ScheduleSystem shrinks, ownership widens")** - **B04**: `FieldOwnershipRegistry` içine `World.Plants` ve `World.Soils` yazar deklarasyonları resmen eklendi (`econ.plantgrowth@Daily:20` + `living.action_advance@PerTick:22`). Reverse lint artık her iki mağarayı da öğrendi; kimsenin kaçak yazması mümkün değil.
- **W36 (2026-07-26, f6c9e2d0 "the RUH_TESHIS post-arch tail")** - **B27 wound-close**: `DefaultTickSystems.PlantGrowthStep.Run` içindeki `isSnowing: false` hardcode ölü. Yerine kaba `isSnowing: season == Season.Winter` gate. `BlockedBySnow=true` türler kışın gerçekten donuyor; `winter-tolerant` türler ticaret gibi devam ediyor. Slice 2 spec'e (per-day weather roll) kadar tutulan geçici sözleşme.
- **REFORM #1 (2026-07-24, 0e44f00a "the visual field IS the sim field")** - W32 öncesi ama bu sistemin **sunum yüzü**: polar `RuntimeFieldBuilder.BuildBelt` retired (fallback dekora düştü), `RuntimeFieldMirror.PlantCell[]` + `PlantsStamp` publish yolu doğdu, `SimFieldView` her plant için ayrı stalk yaratıyor, `CropStalkView.ExternalStage` ile o cell'in kendi stage'ini giyiyor. Sim-görsel union'ı bu sistem için burada kapandı.

## Bilinen Borçlar + Kaçak Kapıları

- **B27 tamamen kapanmadı (partial)** - W36 sadece `isSnowing = (Winter)` kaba gate koydu; RUH_TESHIS "Slice 2 spec"in vaadettiği gerçek per-day deterministik hava rulesu hâlâ borç. Bahar/sonbaharda kar yağarsa `PlantGrowthSystem` haber alamıyor. Kaçak kapı: `RuntimeFieldMirror.WorldDay`/`MinutesOfDay` presentation'da yayınlanıyor - sim tarafında bir `IWeatherOracle.IsSnowingAt(GameTime)` doğması ve `PlantGrowthStep`'in onu tüketmesi gerek.
- **`RuntimeFieldBuilder.BuildBelt` polar dekor hâlâ derleniyor** - REFORM #1 sonrası kullanılmayan kod; `SimFieldView` boş bir sitede fallback görevi görüyor iddiası test'lenmemiş. Silinip silinmeyeceği açık borç (ARCHITECTURE_GAPS #4 kalıntısı).
- **`DomainSimulationAdapter.Clock` PublishPlants pass 64-satırla clamp'li** (`plantCells.Count < 64`). 100+ plant'lı büyük çiftliklerde presentation eksik cell görür - sim tarafı doğru ama SimFieldView "silinmiş" gibi gösterir. Ölçek büyüdükçe patlayacak sessiz kap.
- **`PlantHarvested` event ReasonTrace yok** - `PlantStageAdvanced` `ReasonTrace` doldururken hasat event'i sadece string description tutuyor. Auditor bir hasadın "kimin işi, hangi plot, hangi tick'te" cevabını action-log ile birleştirerek çıkarmak zorunda; standalone log incelemesi zayıf.
- **Species catalog boot-time-only** - `WorldTickComposer` ctor'da `plantSpecies` sabit iletiliyor. Runtime'da yeni species eklemek mümkün değil (mod hook boşluğu, W40+ borç).
- **`CanGrow` fallback kuralı = false** - `PlantGrowthRule` içinde `Season` eşleşmezse Growth susuyor. Yeni species eklerken `Season.None` catchall unutulursa bitki hiç büyümüyor - hata sessiz (`AdvanceOneDay` sadece `return 0` yapar, exception atmaz). `PlantDefinitionTests` şu an bu unutmayı yakalayacak lint satırı taşımıyor.
- **Retired `HarvestStep` bridging yok** - Save v3 dosyalarında Daily:25 registered writer'ı yoksa `WorldSaveMapperGoldenRoundtripTests` restore path'inde ghost writer sanılmıyor mu? W33 sonrası bu risk kapatıldı ama regresyon golden testleri sadece "no writer" varsayımıyla; explicit "old save has this writer → drop silently" migration branchi yok.
- **SimFieldView poll periyodu (1.5s) + CropStalkView poll periyodu (2s) sabit** - Frame budgeting'e reactive değiller; 500+ plant'lı sahnede update'ler dalgalanır. `PlantsStamp` monotonic olduğu için polling doğru ama incremental update değil, tam diff (kısa liste için tolere edilir).

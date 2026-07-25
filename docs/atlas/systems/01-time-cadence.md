# 01-time-cadence

## HLD - Ne ve Neden (5-10 cümle)

Ember'in zamanı ve kadans katmanı: dünyanın hangi hızda ilerlediğini ve hangi sistemin hangi ritmde çalıştığını belirleyen tek noktadır. `GameTime` deterministik bir "epoch'tan bu yana toplam dakika" tamsayısıdır — Unity saatinden bağımsız, bit-eş replay için tasarlanmış. `WorldTickComposer` `Advance(world, tickIndex)` çağrılarında iki tick arası farkı görür ve W29 REFORM #2'den beri **birer birer replay eder** (tek atlama yerine `delta` kez adım) — bu, saatli/günlük geçişlerin tam düştükleri yerde düşmesini garanti eder. `WorldTickRegistry` (cadence, order, id) üçlüsüne göre sıralanmış immutable bir sistem listesi tutar; `DefaultTickSystems.Create` fabrikası bu listeyi kurar ve `WorldTickComposer` üç bantta (PerTick / Hourly / Daily) çağırır. `FieldOwnershipRegistry` her yazılabilir alanın kim tarafından ve hangi cadence:order slotunda yazıldığını **çalıştırılabilir dokümantasyon** olarak tutar — W33'te reverse-lint testi eklendi (bir slotta gerçek sistem yoksa build kırılır). W32-W36 arası sekiz farklı vertical slice bu bantlara oturdu; hepsi aynı `WorldTickComposer.Advance` sözleşmesinden geçtiği için "kim ne zaman yazar" sorusunun tek cevabı vardır. Bu sistem tek başına oyun mantığı yapmaz; sadece **hangi sistemi hangi ritimde çağıracağını** ve **saati nasıl ileriye taşıyacağını** bilir.

## HLD - Akış (numaralı adımlar)

1. **Boot**: `WorldTickComposer` parametresiz kurucusu default `SeasonCalendar` (4 sezon × 90 gün) ve default `PlantSpeciesDef` (wheat) inşa eder; sekiz sistem instance'ını `DefaultTickSystems.Create(...)`'a verir ve dönen `WorldTickRegistry`'yi saklar.
2. **Registry inşası**: `DefaultTickSystems.Create` her sistemin `StepBase` sarmalayıcısını yaratır (id + cadence + order). `WorldTickRegistry` constructor'ı id'leri validate eder (null/whitespace/duplicate yasak), `(Cadence, Order, IdOrdinal)` üçlüsüne göre sıralar, `PerTick/Hourly/Daily` filtreli array'lerini kurar.
3. **Advance çağrısı**: `EmberTickDriver` (Presentation, 10 Hz) `DomainSimulationAdapter.AdvanceTick(tickIndex)` üzerinden `WorldTickComposer.Advance(world, tickIndex)` çağırır. Composer `delta = tickIndex - _lastTickIndex` hesaplar; `delta <= 0` no-op, negatif = yeni anchor.
4. **Per-tick replay loop** (REFORM #2): `for step in 0..delta`:
   a. `_tickRegistry.PerTick` üzerinde her sistemi `TickContext(world, world.Time, 1)` ile çağır (`core.time@10` en başta — saati bu tick ilerletir).
   b. `_ticksSinceHourly++`; eşik `TicksPerGameHour`'a ulaşırsa aksümülatörü düş ve `Hourly` bandını çağır.
   c. `_ticksSinceDaily++`; eşik `TicksPerGameDay`'e ulaşırsa aksümülatörü düş ve `Daily` bandını çağır.
5. **Tick profiler**: her sistem çağrısı `Stopwatch` ile ölçülür; toplam tick > 12 ms ise `TickPerf` logger'ı hangi sistemin ne kadar sürdüğünü tek satırda bastırır.
6. **Save/Load**: `DomainSimulationAdapter.Save` restore edildikten sonra `RebuildAccumulatorsFrom(world.Time)` çağrılır — `TotalMinutes mod TicksPerGameHour/Day` ile aksümülatörler deterministik olarak yeniden türetilir ve `_lastTickIndex = -1` re-anchor sağlar.

## LLD - Veri Modeli (file:line)

- **`GameTime` (readonly struct)** — `Assets/Scripts/Domain/Core/GameTime.cs:12-89`
  - Alan: `_totalMinutes` (long)
  - Sabitler: `MinutesPerHour=60`, `MinutesPerDay=1440`, `MinutesPerMonth=43200`, `MinutesPerYear=518400`, `DaysPerMonth=30`, `MonthsPerYear=12`, `DaysPerYear=360`
  - Türetilen: `Minute`, `Hour`, `DayOfMonth`, `Month`, `Year`, `DayOfYear`, `TotalMinutes`
  - Immutable — her `AddMinutes/Hours/Days/Months/Years` yeni struct döner
- **`TickCadence` (enum)** — `Assets/Scripts/Simulation/Composition/TickCadence.cs:3-8` — `PerTick=0, Hourly=1, Daily=2`
- **`TickContext` (readonly struct)** — `Assets/Scripts/Simulation/Composition/TickContext.cs:6-18` — `World`, `Stamp`, `Delta` (delta her zaman 1 çünkü replay tek adımdır)
- **`IWorldTickSystem` (interface)** — `Assets/Scripts/Simulation/Composition/IWorldTickSystem.cs:3-9` — `string Id`, `TickCadence Cadence`, `int Order`, `void Run(in TickContext)`
- **`WorldTickRegistry` (sealed class)** — `Assets/Scripts/Simulation/Composition/WorldTickRegistry.cs:6-63`
  - `IWorldTickSystem[] _ordered / _perTick / _hourly / _daily`
  - Sıralama anahtarı: `Cadence` → `Order` → `Id` ordinal (`WorldTickRegistry.cs:42-51`)
- **`WorldTickComposer` (sealed class)** — `Assets/Scripts/Simulation/Composition/WorldTickComposer.cs:47-317`
  - `_tickRegistry` (`WorldTickRegistry`)
  - `_lastTickIndex`, `_ticksSinceHourly`, `_ticksSinceDaily`
  - Statik `MinutesPerTick / TicksPerGameDay / TicksPerGameHour` — `EmberRuntimeOptionsProvider.Current.Tick` üzerinden okunur; **runtime tunable**, sabit değil (`WorldTickComposer.cs:49-66`)
  - Statik profiler: `SystemWatch`, `TickCosts`, `PerfLog`, `SlowTickMs=12d`
- **`FieldOwnershipRegistry` (static)** — `Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs:14-114`
  - Tek alan: `IReadOnlyDictionary<string, string[]> Writers` — key = alan adı, value = `"systemId@Cadence:Order"` yazar listesi
  - W35 sonrası 20 satır grubu (aşağıda döküldü)

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

- `WorldTickComposer.Advance(WorldState world, int tickIndex)` — `WorldTickComposer.cs:215-267` — delta hesaplar ve her tick için üç bandı sırayla replay eder; slow-tick profil raporlar
- `WorldTickComposer.ResetAnchor()` — `WorldTickComposer.cs:283-287` — sadece `_lastTickIndex = -1`; save-load sonrası re-anchor için, aksümülatörleri **KORUR** (8th pass A-P2 düzeltmesi)
- `WorldTickComposer.RebuildAccumulatorsFrom(GameTime worldTime)` — `WorldTickComposer.cs:296-301` — restore sonrası aksümülatörleri `TotalMinutes mod X` ile yeniden türetir
- `WorldTickComposer.BuildDefaultCalendar()` (static, private) — `WorldTickComposer.cs:89-98` — 4×90 gün sezon takvimi
- `WorldTickComposer.BuildDefaultPlantSpecies()` (static, private) — `WorldTickComposer.cs:103-122` — deterministik "wheat" (seed→sprout→ripe) katalog
- `WorldTickComposer.Accumulate(string name, double ms)` (static, private) — `WorldTickComposer.cs:212-213` — per-sistem stopwatch birikimi
- `WorldTickRegistry.ctor(IEnumerable<IWorldTickSystem> systems)` — `WorldTickRegistry.cs:13-33` — validate + sort + bant filtreleri kurar; boş id/duplicate id/null sistem atar
- `WorldTickRegistry.Compare(left, right)` (private static) — `WorldTickRegistry.cs:42-51` — sıralama anahtarı (Cadence, Order, IdOrdinal)
- `WorldTickRegistry.Filter(TickCadence cadence)` (private) — `WorldTickRegistry.cs:53-62` — bir bandı `_ordered` içinden filtreleyip array döner
- `DefaultTickSystems.Create(...)` (static) — `DefaultTickSystems.cs:27-79` — 20 sistemin tek fabrikası; `WorldTickRegistry` döner
- `DefaultTickSystems.ResolveProductionRecipe(RecipeId id)` (static) — `DefaultTickSystems.cs:88-98` — `ProductionRecipeRegistry.Resolve`'un null-on-unknown sarmalayıcısı (W34 WORK)
- `GameTimeAdvanceSystem.Advance(GameTime current, long minutes)` — `Time/GameTimeAdvanceSystem.cs:22-28` — safe AddMinutes (negatif dakika atar)
- `GameTimeAdvanceSystem.Advance(current, minutes, eventLog, siteId)` — `Time/GameTimeAdvanceSystem.cs:30-42` — advance + gün/sezon geçiş event'i emit eder
- `GameTimeAdvanceSystem.AppendTransitionEvents(...)` (private) — `Time/GameTimeAdvanceSystem.cs:44-88` — `DayAdvanced` + `SeasonChanged` WorldEvent'lerini basar

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Time-cadence sisteminin **doğrudan** dokunduğu tek FieldOwnershipRegistry satırı:

- `World.Time` → `core.time@PerTick:10` — `WorldTickComposer.cs` her tick `world.Time = _timeAdvance.Advance(...)` çağırır (`DefaultTickSystems.cs:114-118`, `TimeStep`)

Composer, registry ve field-ownership katmanı **hiçbir Actor/World alanını doğrudan yazmaz** — sadece diğer sistemleri organize eder. Ancak `FieldOwnershipRegistry` **tüm** kayıtlı yazarların ledger'ıdır; W35 sonrası (`FieldOwnershipRegistry.cs:14-114`) tam liste:

| Alan | Yazarlar |
|---|---|
| `Actor.Position` | `living.schedule@PerTick:20`, `living.action_advance@PerTick:22`, `living.companion_follow@PerTick:21`, `living.predation@Hourly:40`, `living.witness@Hourly:45`, `living.ambient@Hourly:50` |
| `Actor.Needs` | `living.needs@Hourly:30`, `living.action_advance@PerTick:22` (W32 ConsumeFood + W34 Sleep) |
| `Actor.ActionState` | `living.decision@PerTick:18`, `living.action_advance@PerTick:22` (W32 EAT) |
| `Actor.Vitals` | `living.predation@Hourly:40`, `living.witness@Hourly:45`, `living.companion_guard@Hourly:42` |
| `Actor.Mood` | `living.action_advance@PerTick:22`, `living.needs@Hourly:30` (W35) |
| `World.Reservations` | `living.decision@PerTick:18`, `living.action_advance@PerTick:22` (W32) |
| `World.GuardPursuits` | `living.witness@Hourly:45`, `living.schedule@PerTick:20` |
| `World.Stockpiles` | `living.action_advance@PerTick:22` (W32 TakeFood + W33 HaulCrop/PlantSeed + W34 PerformWork), `living.decision@PerTick:18` (W34 refund), `living.ambient@Hourly:50`, `world.caravans@Daily:10` |
| `World.WorkOrders` | `living.decision@PerTick:18`, `living.action_advance@PerTick:22` (W34) |
| `World.Jobs` | `econ.jobs@Hourly:10`, `living.action_advance@PerTick:22`, `econ.shortage_response@Daily:27` (W34) |
| `World.Plants` | `econ.plantgrowth@Daily:20`, `living.action_advance@PerTick:22` (W33) |
| `World.Soils` | `living.action_advance@PerTick:22` (W33) |
| `World.Rumors` | `living.rumors@Hourly:55` |
| `World.SiteUnrest` | `living.witness@Hourly:45` |
| `World.Time` | `core.time@PerTick:10` (W35) |
| `World.NpcMemory` | `living.witness@Hourly:45` (komut-güdümlü yazarlar bilerek DEKLARE EDİLMEDİ) (W35) |
| `World.CompanionIds` | `living.companion_follow@PerTick:21` (W35) |
| `World.Factions` | `politics.faction_decay@Daily:40` (W35) |

## LLD - Ürettiği/Tükettiği Olaylar

**Üretilen** (composer → `world.Events`):

- `WorldEventKind.DayAdvanced` — `GameTimeAdvanceSystem.AppendTransitionEvents` her gün dönüşünde (`Time/GameTimeAdvanceSystem.cs:53-65`)
- `WorldEventKind.SeasonChanged` — sezon geçişlerinde (`Time/GameTimeAdvanceSystem.cs:70-88`)
- `TickPerf` warn log satırı — `>12ms` tick'te "slow tick N: X.Xms — sysA=1.2ms sysB=3.4ms" (WorldTickComposer.cs:257-266) — `EmberLog` sink'i, WorldEvent değil

**Tüketilen**: hiçbiri. Composer olay okumaz; sadece registry'deki sistemleri çağırır.

**Diğer sistemler**: Registry içindeki her sistem kendi olay setini üretir (ör. `econ.shortage_response` → `PlantingJobPosted`, `living.action_advance` → `ActionCompleted/ActionFailed`, vb.). Bunlar 02-actor-actions ve 03-plant-growth doclarında sayılır.

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

**Composer / Registry / Cadence pinleri**:

- `Assets/Tests/EditMode/Composition/WorldTickRegistryTests.cs` — sıralama, duplicate reddi, **canonical order golden'ı** (W34 sonrası: 6 PerTick + 8 Hourly + 6 Daily satır)
- `Assets/Tests/EditMode/Composition/DefaultRegistryFixture.cs` — B03 fix: composer kurulumunun tek test-tarafı kaynağı
- `Assets/Tests/EditMode/Composition/CadenceChunkingInvarianceTests.cs` — 2 tam gün: tick-by-tick vs 1/7/13/40/61/127... chunked çağrılar birebir aynı event log üretmeli (REFORM #2 sözleşmesi)
- `Assets/Tests/EditMode/Composition/ActionPhaseChunkingInvarianceTests.cs` — W32/W33/W34 hikâye-testi: EAT + FARM (4 gün) + WORK (narrow cast) faz makinelerinin chunking invariance'ı; capture sink kullanıyor
- `Assets/Tests/EditMode/Composition/WorldTickComposerReplayTests.cs` — DET-01 save/load: `ResetAnchor` yetmez, `RebuildAccumulatorsFrom(world.Time)` gereklidir (kesintisiz run ile birebir eşdeğerlik)
- `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs` — same-seed double advance = byte-identical digest; W32 stage-A + W33 F7 + W34 sleep/work satırları için re-baseline'lı
- `Assets/Tests/EditMode/Composition/WorldTickFactionDecayTests.cs` — daily band 15 günlük itibar düşüş integrasyonu
- `Assets/Tests/EditMode/Composition/WorldLivesOverNTicksTests.cs` — N tick soak; yaşayan-dünya invariantları
- `Assets/Tests/EditMode/Composition/WorldNpcDailyRhythmTests.cs` — 8-aktörlü rutin (W33: iki tarım işi day 0'da tamamlanır, iki demirci işi ele geçirilmiş kalır)
- `Assets/Tests/EditMode/Composition/CatchupPerfPinTests.cs` — 14 günlük catch-up < 5 s (W29 O(delta) replay + W33 farm decide bantı içeride)
- `Assets/Tests/EditMode/Composition/LiveScaleCatchupPerfPinTests.cs` — ~40 site / ~800 civilian ölçekte bir günlük catchup < 3 s
- `Assets/Tests/EditMode/Composition/PlantGrowthSnowGateWireTests.cs` — B27 wound-close: `PlantGrowthStep.Run` `isSnowing: season == Season.Winter` (W36 tail)

**Field ownership lint**:

- `Assets/Tests/EditMode/Composition/FieldOwnershipRegistryTests.cs` — her deklare yazar gerçek kayıtlı sistem olmalı (reverse-lint); core alanların ownership satırı olmalı (W32'de `Actor.ActionState` + `World.Reservations` pinlendi)

**GameTime birim testleri**:

- `Assets/Tests/EditMode/Core/GameTimeTests.cs` — struct semantics
- `Assets/Tests/EditMode/Time/GameTimeAdvanceSystemTests.cs` — day/season transition event doğruluğu

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

**W32 (5049d445, 2026-07-25) — EAT slice**:
- `DefaultTickSystems.cs`: `DecisionStep` (`living.decision@PerTick:18`) + `ActionAdvancementStep` (`living.action_advance@PerTick:22`) eklendi; retired `EatOnArrivalStep` slotuna oturdu
- `FieldOwnershipRegistry.cs`: `Actor.ActionState` + `World.Reservations` satırları eklendi; `Actor.Position` altına `living.schedule` "NARROWED (W32): actionless actors only" notu; `Actor.Needs` altına W32 ConsumeFood notu
- `WorldTickRegistryTests.cs` canonical order golden'ı 6 PerTick satırıyla genişledi

**W33 (61e340f3, 2026-07-25) — FARM slice**:
- `DefaultTickSystems.cs`: `HarvestStep@Daily:25` **retired** (fiat +2/self-replant teleport öldü); `JobAssignmentStep` shrink — `StartRecipeForClaim` çıkarıldı, sadece claim + ghost-cancel + dead-claimant sweep kaldı
- `FieldOwnershipRegistry.cs`: `World.Plants`, `World.Soils` yeni satırlar; `World.Jobs` altına `living.action_advance` yazarı deklare edildi; `World.Stockpiles`'a HaulCrop/PlantSeed writer notları
- Test-tarafı: `DefaultRegistryFixture` doğdu (B03 fix — hand-typed known-id listesi rotting sorunu çözüldü), `FieldOwnershipRegistryTests` reverse-lint kazandı

**W34 (3aa87cf6, 2026-07-25) — SLEEP + WORK slices**:
- `DefaultTickSystems.cs`: `NeedConsumptionSystem` + `ConsumptionStep@Hourly:35` **retired** (positionless night fiat öldü — sleep artık `MoveToBed→Sleep` deed); `JobAssignmentStep.TickAssignedJobs` **retired** (free-running counter öldü — order progress SADECE `living.action_advance@PerTick:22`)
- `FieldOwnershipRegistry.cs`: `World.WorkOrders` yeni satır; `World.Jobs` multi-writer resmen deklare edildi (`econ.jobs@Hourly:10` + `living.action_advance@PerTick:22` + `econ.shortage_response@Daily:27`); `Actor.Needs`'e W34 Sleep notu; `World.Stockpiles`'a W34 PerformWork fund/mint notu

**W35 (20a3b899, 2026-07-25) — Ownership widens**:
- `FieldOwnershipRegistry.cs`: **6 yeni satır grubu** — `World.Time`, `World.Plants` (yeniden deklare — aşağı bak), `Actor.Mood`, `World.NpcMemory`, `World.CompanionIds`, `World.Factions`. Her deklare yazar reverse-lint ile gerçek kayıtlı adıma çözüldü; boot-only + komut-güdümlü yazarlar bilerek deklare edilmedi
- `ScheduleSystem` küçüldü (guard-only) — bu doc'un konusu değil, ancak `Actor.Position` yazarı listesinde `living.schedule@PerTick:20`'nin "actionless only" hakikati W35'te resmileşti

**W36 (f6c9e2d0, 2026-07-25) — RUH_TESHIS post-arch tail**:
- `DefaultTickSystems.cs`: B27 close — `PlantGrowthStep.Run`'da hardcoded `isSnowing: false` öldü; şimdi `season == Season.Winter` (Slice 2 gerçek weather'e kadar coarse gate)
- Composer/Registry contract'ı değişmedi; sadece step içi wire düzeltmesi

## Bilinen Borçlar + Kaçak Kapıları

- **`FieldOwnershipRegistry.cs:66` vs `:100`** — `World.Plants` anahtarı sözlükte **iki kez** ilan edildi (W33 satırı + W35 satırı). C# 6 indexer syntax (`["key"] = value`) duplicate atmaz, sessizce üzerine yazar — bu durumda ikinci (`W35`) tanım kazanır. İki tanım semantik olarak aynı yazarları saydığı için görünür bir arıza yok, ama ledger'ın kendisi single-source-of-truth iddiasını kırıyor. Küçük bir cleanup adayı.
- **`WorldTickComposer.MinutesPerTick` / `TicksPerGameDay/Hour` runtime derived** — `EmberRuntimeOptionsProvider.Current.Tick`'ten okunuyor, `EmberRuntimeOptions.Normalize` `TicksPerHour/Day`'i `MinutesPerTick`'ten türetiyor (`EmberRuntimeOptions.cs:254-255`). Memory ID **13178**: `ProofAdvanceHours` 8 saatlik advance için exact olmayan bir sonuç veriyor — `MinutesPerTick > 1` konfigürasyonlarda saat başı hizalama sapabilir. Bir uçtan pinlenmiş test yok.
- **`ResetAnchor` vs `RebuildAccumulatorsFrom` sıralama tuzağı** — 8th pass düzeltmesinden sonra `ResetAnchor` **aksümülatörleri korur**, ama restore path'i saati adjust etmeden `ResetAnchor` çağırırsa aksümülatörler eski durumda kalır. Tek doğru sıra: `world.Time` restore → `RebuildAccumulatorsFrom(world.Time)`. `DomainSimulationAdapter.Save.cs` doğru sırayı çağırıyor (grep pinli); ama yeni bir load path yazan bu tuzağa düşebilir.
- **Duplicate-add validation `WorldTickRegistry`'de var, `FieldOwnershipRegistry`'de yok** — registry aynı id iki kez eklerse `InvalidOperationException`; ownership dictionary aynı key iki kez alırsa (yukarıdaki `World.Plants` örneği) sessiz overwrite. Reverse-lint bunu yakalamaz — sadece ghost yazar arar, duplicate key aramaz.
- **`TickContext.Delta` her zaman 1** — REFORM #2 replay tek adım yapıyor; ancak arayüz `int Delta` sunuyor. Yeni bir sistem `context.Delta` üzerinden batch mantığı kurmak isterse (örn. "N tick worth of decay"), invariance testleri bunu yakalamaz — çünkü delta zaten 1'dir; ama chunking invariance sözleşmesini kırar. `TickContext` yorumu bunu söylemiyor, kaçak kapı.
- **Slow-tick eşiği (12 ms) hardcoded** — `WorldTickComposer.cs:196` `SlowTickMs = 12d`. Editor'da tolere edilebilir, headless CI'da farklı olması gerekebilir; runtime option değil.
- **`FieldOwnershipRegistry` "komut-güdümlü yazarlar bilerek deklare edilmedi" (dialog / trade completion / ToolUse / boot)** — W35 yorumunda açıkça belirtiliyor. Reverse-lint bu yazarları "ghost" saymaz çünkü ledger'da yoklar; ama tick-loop-dışı yazımların ownership'i hiçbir yerde denetlenmiyor. Yeni bir command handler bir alanı istediği zaman yazabilir; single-writer-per-field disiplini sadece tick loop içinde asayiş sağlar.

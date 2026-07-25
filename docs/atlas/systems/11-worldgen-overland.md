# 11-worldgen-overland

> Kapsam: tohum -> gezegen -> `GeneratedWorld` -> `OverlandMap` boru hatti (`PlanetWorldService`, `OverlandWorldgen` + partials, `WorldgenService`), `WorldProfile` kaydi ve W32-B02 "cold Continue" rebuild yolu (`DomainSimulationAdapter.Save.cs`).
> Kanit disiplini: her iddia `file:line` ile. Emin olunmayan yerler "dogrulanmadi" olarak isaretli.

## HLD - Ne ve Neden

Dunya uretimi **iki katman**: (1) FOUNDATION — `WorldgenService.Generate(seed, parameters)` (`Assets/Scripts/Simulation/Worldgen/WorldgenService.cs:55-86`) deterministic bir `GeneratedWorld` (bolgeler, yerlesimler, klanlar, iliskiler, NPC'ler, cok-yuzyilli tarih, jeografi) dokumusu uretir; (2) OVERLAND — `OverlandWorldgen.Generate(GeneratedWorld, OverlandParameters)` (`Assets/Scripts/Simulation/Overland/OverlandWorldgen.cs:29-49`) ayni `GeneratedWorld`'un jeografisini bir `OverlandMap` grid'ine (biyom + tile-seed + settlement id'leri) projekte eder. Ikisinin arasinda `PlanetWorldService.GetOrGenerate` (`Assets/Scripts/Presentation/Ember/Worldgen/PlanetWorldService.cs:20-33`) icosphere-planet boru hattini calistirir ve sonucu `PlanetWorldContext` singleton'una (`Assets/Scripts/Presentation/Ember/Worldgen/PlanetWorldContext.cs:26-38`) tohum basi tek-atim onbellekler.

Neden singleton onbellek: gezegen boru hatti (subdivision level 5, ~10,242 tile + plate simulasyonu) birkac saniye surer; karakter yaratim reveal'i onu **streamed** (observer'li) uretir, `SeedWorld` sonra ayni tohumla `Has(seed)` true bularak dogrudan onbellekten okur — cift-uretim yok, ayni tohum ayni GeneratedWorld (`PlanetWorldService.cs:25-27`).

Neden `Generate(GeneratedWorld, ...)` overload'u zorunlu: `OverlandWorldgen.Generate(uint, ...)` (`OverlandWorldgen.cs:17-27`) `WorldgenService.Generate(FallbackSeed_if_zero, Default)` cagirir — **planet path'i degil, flat worldgen path'ini** vurur; ayni tohum icin ayri bir harita cikar. B28 interlock (`Docs/ruh/w32/00-bug-triage.md:257`): SeedWorld ve B02 cold-load rebuild yalniz `Generate(generated, parameters)` overload'unu kullanir.

Overland tarafinda **maketleme kontrati**: `OverlandParameters.Width/Height` `world.Geography.Width/Height` ile birebir ayni olmali (`OverlandWorldgen.cs:52-59`); planet mapper 128x64 bir grid urettigi icin `SeedWorld` ve B02 rebuild yollari `overlandGeo = generated.Geography` uzerinden parametreleri turetir (`DomainSimulationAdapter.Worldgen.cs:78-86`, `DomainSimulationAdapter.Save.cs:65-70`).

**Discoverability invarianti** (v0.3 F9 "zindani bulamadim" oldurusu): her dunya en az `MinimumDungeons = 3` delve garanti eder (`OverlandWorldgen.Settlements.cs:41`); dogal biyom+size cikarimi delve uretmezse listede geriden dogru **kucuk, sehir-olmayan** yerlesimleri Dungeon'a dondurur (`OverlandWorldgen.Settlements.cs:44-61`) — City/Town ise asla demote edilmez.

**Cold-load rebuild (B02 spot-fix)**: kayitlar `OverlandMap`'i **hic** persist etmez (bir tohum turevidir); `WorldProfile` ise persist edilir (`WorldSaveMapper.cs:91,210`). `RestoreStateJson` (`Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Save.cs:30-83`) once ayni-oturum canli haritasini korur; hala null ve `WorldProfile != null` ise EXACT `SeedWorld` yolunu tekrar oynatir: `WorldgenParameters.For(profile.Style, profile.Genre)` -> `PlanetWorldService.GetOrGenerate(profile.Seed, parameters)` -> `SelectStartingRegion/Settlement/Faction` -> `OverlandWorldgen.Generate(generated, new OverlandParameters(geo.Width, geo.Height))`.

## HLD - Akis

### A. New Game — SeedWorld (`DomainSimulationAdapter.SeedWorld`, `.Worldgen.cs:29-91`)

1. **Tohum turet:** `worldSeed ?? FoldSeed(mood, calling, startLocation)` — uc dizeden FNV-1a folded uint. `_world.RoomSeed = (int)seed` (RuntimeHistorySystem icin) (`.Worldgen.cs:29-38`).
2. **Parametreler:** `style/genre/preferredSize` wizard dizelerinden parse edilir; `WorldgenParameters.For(style, genre)` (`WorldgenParameters.cs:105-166`) 200 yerlesim / 20 faction / 750 NPC / 1200 yil history varsayilan setinin style-genre varyantini uretir (`.Worldgen.cs:41-43`).
3. **Gezegen:** `PlanetWorldService.GetOrGenerate(seed, parameters)` — `Has(seed)` true ise onbellekten oku; yoksa `PlanetParameters(subdivision=5, plateCount∈[16..32], seaLevel=0.62)` (`PlanetWorldService.cs:37-43`) uzerinden `PlanetGenerator.Generate` + `PlanetToWorldMapper.Map` calistir, `PlanetWorldContext.Set(seed, field, world)`e yaz (`.Worldgen.cs:47-50`).
4. **Baslangic secimleri:** `SelectStartingRegion / SelectStartingSettlement(preferredSize) / SelectStartingFaction` (`.Worldgen.cs:51-54`).
5. **Profil kaydi:** `_world.WorldProfile = new WorldProfile(style, genre, seed, targetPopulation, regionCount, factionCount, historyYears, mood, calling, startLocation)` (`.Worldgen.cs:59-70`).
6. **Hydration:** `HydrateGeneratedWorld(generated, preferredSize)` (`.Worldgen.Hydration.cs:27-38`) sirasiyla: `HydrateSites` -> `HydrateFactions` -> `HydrateNpcs` -> `HydrateHistory` -> `SeedWorldQuests` -> `MovePlayerToStartingSettlement` -> `HydrateBlockedCells` (LAST — site sinirlarina ihtiyac duyar).
7. **Overland projeksiyonu:** `overlandGeo = generated.Geography`; `_world.Overland = OverlandWorldgen.Generate(generated, new OverlandParameters(w,h))` (`.Worldgen.cs:77-86`).
8. **Log + main-quest arm:** debug log satiri + `ConfigureMainQuest()` (`.Worldgen.cs:88-92`).

### B. Cold Continue — B02 Rebuild (`DomainSimulationAdapter.Save.cs:30-83`)

1. **Envelope decode:** `_saveService.LoadFromJson(json)` -> `restored` (WorldState) (`.Save.cs:38`).
2. **Live-overland koruma (playtest fix):** `var liveOverland = _world.Overland; _world.CopyFrom(restored);` -> `if (_world.Overland == null && liveOverland != null) _world.Overland = liveOverland` (`.Save.cs:45-49`).
3. **B02 fresh-process rebuild:** hala `Overland == null && WorldProfile != null` ise:
   - `parameters = WorldgenParameters.For(profile.Style, profile.Genre)` (`.Save.cs:58`).
   - `generated = PlanetWorldService.GetOrGenerate(profile.Seed, parameters)` — onbellek ilk cagrida bosdur, sentez calisir; sonraki cagrilar `Has(seed)` ile ucar (`.Save.cs:60`).
   - `GeneratedWorld = generated` + `SelectStartingRegion/Settlement/Faction` yeniden setlenir; `_billboardOriginResolved = false` (`.Save.cs:61-66`).
   - `_world.Overland = OverlandWorldgen.Generate(generated, new OverlandParameters(geo.Width, geo.Height))` (`.Save.cs:67-70`).
   - Log: `"[Load] B02 overland rebuilt from profile seed=... settlements=..."` (`.Save.cs:71-72`).
4. **Invariants + composer resync:** `_world.EnsureInvariants()` + `_tickComposer.RebuildAccumulatorsFrom(_world.Time)` (`.Save.cs:77-82`).

**Kalan sinir (`Docs/ruh/w32/00-bug-triage.md:56-63`):** `WorldSceneDirector.Realize` `EmberWorldHost.Awake`'te, `EmberSaveService.Start`'in `RestoreStateJson`'u tetiklemesinden ONCE calisir; rebuild sonrasi sahnenin re-realize'i (veya bekleyen-yuk tuketiminin realize once cekilmesi) hala acik borc (dogrulanmadi — kod tarafinda pin yok).

### C. FOUNDATION — `WorldgenService.Generate` faz sirasi (`WorldgenService.cs:59-86`)

`XorShiftRng(seed)` tek instance, SIKI cagri sirasi (deterministic replay bunun uzerine yapili): (0) `WorldGeographyProvider.Build(seed, parameters)` -> `geographyBuild`; (1) `GenerateRegions(rng, parameters, geographyBuild)` -> `regions`, `geographyBuild.Materialize(regions)` -> `geography`; (2) `GenerateSettlements(rng, parameters, regions)`; (3) `GenerateFactions(rng, parameters)`; (4) `GenerateFactionRelations(rng, factions)`; (5) `GenerateHistory(seed, parameters, geography, regions, factions, settlements)` -> `historyResult` + `ProjectHistoryState` -> `projected`; (6) `GenerateNpcs(rng, parameters, projected.Settlements, factions)`. Cikti: `new GeneratedWorld(seed, regions, projected.Settlements, factions, relations, npcs, historyResult.Events, projected.NotableFigures, geography)`.

### D. Overland projection (`OverlandWorldgen.cs:29-49`)

`Generate(GeneratedWorld world, OverlandParameters parameters)` sirasi:
1. `EnsureMatchingParameters(geography, parameters)` (dim mismatch throw).
2. `regionIds = geography.CopyRegionIds()`, `biomes = geography.CopyOverlandBiomes()`.
3. `settlements = ProjectSettlements(world.Settlements, geography)` (`.Settlements.cs:14-33`).
4. `tileSeeds = RollTileSeeds(seed, tileCount)` — `XorShiftRng(seed ^ 0xA511E9B3u)`.
5. `tiles = BuildTiles(w, h, regionIds, biomes, tileSeeds, settlements)` — her tile'a settlement-id listesi + `DetermineClimate(biome)` (`.cs:83-113`).
6. `OverlandMap` + `OverlandMapGeographyStore.Register` + `OverlandMapPlanetStore.Register(map, world.PlanetData)` (rich sidecar).

## LLD - Veri Modeli (file:line)

- `WorldProfile` (immutable) — `Assets/Scripts/Domain/Worldgen/WorldProfile.cs:8-56`
  - `Style : WorldStyle`, `Genre : WorldGenre`, `Seed : uint` (0 -> `2463534242u` default, `:29`)
  - `TargetPopulation, RegionCount, FactionCount, HistoryYears : int` (hepsi > 0, `:20-27`)
  - `MoodKeyword, PlayerCallingKeyword, StartLocationKeyword : string` (null -> empty, `:35-37`)
  - Equals/GetHashCode alan-bazli, ordinal string karsilastirmasi (`:52-83`)
- `WorldgenParameters` — `Assets/Scripts/Simulation/Worldgen/WorldgenParameters.cs:12-88`
  - `RegionCount(50), CapitalCount(1), CityCount(8), TownCount(40), VillageCount(151)` default;
  - `FactionCount(20), NpcCount(750), HistoryYears(1200), WorldStartYear(1)`;
  - `TargetPopulation(1_000_000)`, `Style, Genre` + `For(style, genre)` factory (`:105-166`).
- `GeneratedWorld` (immutable, ReadOnlyCollection sarmalayici) — `Assets/Scripts/Simulation/Worldgen/GeneratedWorld.cs:90-183`
  - `Seed:uint`, `Regions/Settlements/Factions/FactionRelations/Npcs/History/NotableFigures : IReadOnlyList<...>`, `Geography : WorldGeography`, `PlanetData : PlanetField` (sidecar, save'e girmez, `:160-165`).
  - `TotalPopulation` = sum of `Settlements[i].Population` (`:167-176`).
- `FactionRelationSeed` struct — `GeneratedWorld.cs:22-83` (kanonik siralama `FactionA.Value <= FactionB.Value`).
- `NpcSeedRecord` — `Assets/Scripts/Domain/Worldgen/NpcSeedRecord.cs:19-64` (id/home/faction non-empty, name non-blank, role != None guard).
- `PlanetWorldContext` singleton — `Assets/Scripts/Presentation/Ember/Worldgen/PlanetWorldContext.cs:15-42` (`Seed:uint?`, `Field:PlanetField`, `World:GeneratedWorld`, `Has(seed)`, `Set(seed,field,world)`, `Clear()`).
- `OverlandMap` (Domain) — `Assets/Scripts/Domain/Overland/OverlandMap.cs` (konum grep'lenmedi ama `OverlandWorldgen.cs:46` `new OverlandMap(w,h,tiles,settlements)`); `RegionTile(x,y,regionId,biome,settlementIds,tileSeed,climate)` yapisi `OverlandWorldgen.cs:85-95`'da insa ediliyor.

## LLD - Fonksiyon Haritasi (imza + file:line + 1 cumle)

- `PlanetWorldService.GetOrGenerate(uint seed, WorldgenParameters parameters, IPlanetGenerationObserver observer=null) : GeneratedWorld` — `PlanetWorldService.cs:20-33` — onbellek hit veya planet-generate+map+cache.
- `PlanetWorldService.ToPlanetParameters(WorldgenParameters) : PlanetParameters` — `PlanetWorldService.cs:36-43` — sabit fizik, plateCount = clamp(regionCount+8, 16..32).
- `PlanetWorldService.Generate(uint seed, PlanetParameters, IPlanetGenerationObserver) : PlanetField` — `PlanetWorldService.cs:45-52` — observer null ise `PlanetGenerator.Generate`, degilse `PlanetGenerationManager` ile streamed.
- `WorldgenService.Generate(uint seed, WorldgenParameters parameters) : GeneratedWorld` — `WorldgenService.cs:55-86` — FOUNDATION 7-faz deterministic boru hatti.
- `OverlandWorldgen.Generate(uint seed, OverlandParameters parameters) : OverlandMap` — `OverlandWorldgen.cs:17-27` — flat path (WorldgenService.Default), B02 icin **kullanma**.
- `OverlandWorldgen.Generate(GeneratedWorld world, OverlandParameters parameters) : OverlandMap` — `OverlandWorldgen.cs:29-49` — planet-aware path; SeedWorld + B02 rebuild yalniz bunu cagirir.
- `OverlandWorldgen.ProjectSettlements(IReadOnlyList<SettlementRecord>, WorldGeography) : IReadOnlyList<OverlandSettlement>` — `.Settlements.cs:14-33` — record'lardan overland settlement projekte eder, `EnsureMinimumDungeons` cagirir.
- `OverlandWorldgen.EnsureMinimumDungeons(List<OverlandSettlement>)` — `.Settlements.cs:44-61` — geriden dogru City/Town olmayan kucukleri Dungeon'a promote eder (>=3 garanti).
- `OverlandWorldgen.ClassifySettlementKind(SettlementSize, BiomeKind, int roll) : SettlementKind` — `.Settlements.cs:75-105` — Capital/City -> City; Town -> 70% Town / 30% Village; kucuk boyutlar biyom-bagimli dagilim (Mountain/Ash: Hamlet/Shrine/Dungeon; vs).
- `DomainSimulationAdapter.SeedWorld(string mood, string calling, string startLocation, uint? worldSeed=null)` — `.Worldgen.cs:29-92` — new-game tam yolu (yukarida akis A).
- `DomainSimulationAdapter.HydrateGeneratedWorld(GeneratedWorld, SettlementSize preferredSize)` — `.Worldgen.Hydration.cs:27-38` — Sites/Factions/Npcs/History/Quests/Player-move/BlockedCells sirasi.
- `DomainSimulationAdapter.HydrateSites(GeneratedWorld)` — `.Worldgen.Hydration.cs:105-142` — bolge site (grid) + settlement site (tile-koord veya legacy compact); her settlement icin stockpile larder (150 wheat).
- `DomainSimulationAdapter.HydrateFactions(GeneratedWorld)` — `.Worldgen.Hydration.cs:151-178` — ilk 3 faction'a Craft/Trade/Law tag'i garanti eder, iliskiler + StartingFaction rep bonusu.
- `DomainSimulationAdapter.HydrateNpcs(GeneratedWorld)` — `.Worldgen.Npcs.cs:23-56` — actor id offset + role stats + `HomeCellFor/DayAnchorFor` (deterministic per-npc spread) + job preference.
- `DomainSimulationAdapter.RestoreStateJson(string json)` — `.Save.cs:30-83` — B02 cold-load rebuild yolu (yukarida akis B).

## LLD - Yazdigi/Okudugu Alanlar (FieldOwnershipRegistry dilinde)

**Yaz — boot/adapter (undeclared, tick disi):**
- `World.WorldProfile` <- `DomainSimulationAdapter.SeedWorld` (`.Worldgen.cs:59`).
- `World.Overland` <- `DomainSimulationAdapter.SeedWorld` (`.Worldgen.cs:83`), `DomainSimulationAdapter.RestoreStateJson` (`.Save.cs:48,67`).
- `World.RoomSeed` <- `SeedWorld` (`.Worldgen.cs:32`).
- `World.Sites / World.Stockpiles` <- `HydrateSites` (`.Worldgen.Hydration.cs:118,140`).
- `World.Factions` <- `HydrateFactions` (`.Worldgen.Hydration.cs:153,172,176`).
- `World.Actors / World.NpcSeeds` <- `HydrateNpcs` (`.Worldgen.Npcs.cs:26-27,54`).
- `World.Blocked` <- `HydrateBlockedCells` (`.Worldgen.Hydration.cs:53-96`) — DERIVED (kaydedilmez, her load'da yeniden hydrate).
- `Adapter.GeneratedWorld / StartingRegion / StartingSettlement / StartingFaction` <- `SeedWorld` + `RestoreStateJson`.

**Oku:**
- `PlanetWorldContext` (Seed, Field, World) — `PlanetWorldService.GetOrGenerate`.
- `WorldProfile` — `RestoreStateJson` (`.Save.cs:56-58`).
- `GeneratedWorld.Geography` — `SeedWorld` (`.Worldgen.cs:82`), `RestoreStateJson` (`.Save.cs:65`), `OverlandWorldgen.Generate` (`.cs:37-38`).

**Not:** `FieldOwnershipRegistry` (`Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs:16-115`) worldgen alanlarini (`World.WorldProfile / World.Overland / World.NpcSeeds / World.Sites`) **declare etmez** — ledger yalniz **tick loop icinde yazilan** alanlari denetler; worldgen yazimlari boot-only + command-driven (`.cs:88-98` yorumu). Bu bilincli bir kapi, ihlal degil.

## LLD - Urettigi/Tukettigi Olaylar

**Uretilen (event log):**
- `WorldgenService.GenerateHistory` — `WorldHistoryEvent` akisi (`GeneratedWorld.History`); dogrudan bir domain event bus'i degil, hydrate icin uretilen sabit dokum (dogrulanmadi — event kind listesi grep'lenmedi).

**Tuketen:**
- `WorldgenViewController` / `WorldgenEventProjector` — reveal ekraninda `IPlanetGenerationObserver` uzerinden `PlanetGenerator` stage'lerini yayinlar (dogrulanmadi — sadece dosya adlarindan; `PlanetWorldService.Generate:45-52` observer parametresini kabul eder).
- `RuntimeHistorySystem` — `_world.RoomSeed`'i seed olarak kullanir (`.Worldgen.cs:32-34` yorumu).

Not: `PlanetField` (`GeneratedWorld.PlanetData`) **save payload'da yer almaz** (`GeneratedWorld.cs:161-164` yorumu); rich sidecar, sahne rendering'i tarafindan tuketilir.

## Testler (bu sistemi pinleyen test dosyalari)

- `Assets/Tests/EditMode/Overland/OverlandWorldgenTests.cs` — `Generate_SameSeed_ProducesIdenticalMap/BiomeGrid`, `DifferentSeed`, `Seed42FieldHash_MatchesGoldenMaster`, `ContinentalFields_HaveSaneLandFraction`, `RepresentativeSeeds_CoverEveryBiome`, `PlateBoundariesProduceMountainRanges`, `AssignsValidBiomes_AndSmoothsSingleTileIslands`, `ProjectsEveryWorldSettlementOntoItsHistoryTile`, `FromGeneratedWorld_ProjectsPassedWorldSettlementIds`, `MapHelpers_ReturnSaneDistancesAndNearestSettlement` (~13 test).
- `Assets/Tests/EditMode/Overland/OverlandMapImageSamplerTests.cs` — `Sample_SameSeed_ProducesIdenticalBytes`, `DifferentSeed_ChangesBytes`, `CoastBiome_SeparatesOceanWaterFromCoastLand`, `UpscaledImage_AddsGeographyReliefWithinTileProjection`, `Projection_TileCenterAndSamplerShareOneGrid`.
- `Assets/Tests/EditMode/Overland/OverlandScaleTests.cs` — `DefaultParameters_AreA16x16RegionGrid`, `DefaultRegionTile_Is40kmEdge_So1600Km2Each`, `DefaultTotalArea_IsAtLeastTwiceDaggerfall`, `TotalArea_ScalesWithGridAndRegionEdge`, `RegionEdgeKm_MustBePositive`.
- `Assets/Tests/EditMode/Overland/PlanetPinAlignmentTests.cs` — `EverySettlementPin_LandsOnNonOceanPixels_OfTheRenderedAtlas` (planet-mapper vs overland pin tutarliligi).
- `Assets/Tests/EditMode/Worldgen/WorldgenServiceTests.cs` — `SameSeedSameWorld`, `DifferentSeedDifferentWorld`, `TotalPopulationReflectsHistoryState`, `HistoryProjectionDropsFinalAbandonedSettlementsAndSurfacesFigures`, `NpcRosterUsesSurvivingSettlementsAndVariedRoles`, `DistinctSettlementNames`, `DistinctNpcNames_within_settlement`, `HistoryDeterministic`, `SettlementsAttachToRealRegions`, `SettlementsHaveAuthoritativeLandTiles`, `SampleSeed42_PrintsInspectionDump`, `WorldgenEventProjector_ProjectsGeneratedWorldToVisibleEvents`, `WorldgenEventProjector_CanEmitFailureAndStillComplete`.
- `Assets/Tests/EditMode/Worldgen/WorldProfileSaveRoundTripTests.cs` — `SliceSaveMapper_RoundTripsWorldProfile` (B02 fix'in prerequisite'i).
- `Assets/Tests/EditMode/Worldgen/NpcSeedSaveRoundTripTests.cs` — `SliceSaveMapper_RoundTripsNpcSeedPortraitAssetPath`.
- `Assets/Tests/EditMode/Worldgen/WorldStyleMatrixTests.cs`, `WorldHistorySimulatorTests.cs`, `WorldGenesisMapperTests.cs` — style/history/genesis pin'leri.
- `Assets/Tests/EditMode/WorldDirector/WorldGeoSamplerTests.cs`, `WorldGeoSamplerShoreTests.cs`, `WorldSpaceProjectionDirectionTests.cs`, `SettlementLayoutDeterminismTests.cs`, `SettlementLayoutStrategyFactoryTests.cs`, `StreetLayoutStrategyTests.cs` — jeografi-sampler + settlement layout deterministlik.
- **B02 cold-load pin'i: DOGRULANMADI** — `Assets/Tests` altinda `B02 / cold-load / RestoreStateJson-overland-rebuild` string'i grep'lenmedi; W32 fix `Docs/ruh/w32/00-bug-triage.md:42-63` **regresyon testi olmadan** yasiyor.

## W32-W36 Degisiklikleri

- **W32-B02 (spot-fix, `DomainSimulationAdapter.Save.cs:51-73`):** cold Continue oncesi kayipli overland; `RestoreStateJson` `WorldProfile.Seed`'ten `PlanetWorldService.GetOrGenerate` + `OverlandWorldgen.Generate(generated, ...)` cagirisi eklendi. B28 interlock nedeniyle `Generate(GeneratedWorld, ...)` overload'u zorunlu. `Docs/ruh/w32/00-bug-triage.md:42-63` — regresyon test PIN'i **DOGRULANMADI**.
- **W32-B28 (documented, `.Save.cs:53-56` yorumu + `Docs/ruh/w32/00-bug-triage.md:257`):** iki `OverlandWorldgen.Generate` overload'unun ayni tohum icin ayri harita uretmesi problemi; fix salt yorum + kullanim disiplini (planet path'i zorla) — kod ayrimi hala mevcut (`OverlandWorldgen.cs:17-27` vs `:29-49`).
- **W34-C (borc):** `FieldOwnershipRegistry` (`Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs`) B04 kapsaminda 7 alan ekledi (W35 icin isaretli); worldgen tarafi (`World.WorldProfile / World.Overland / World.NpcSeeds`) hala **undeclared** — bilincli boot-only karari (`.cs:88-98` yorumu).
- **W36 (post-fix):** `Docs/atlas/systems/11-worldgen-overland.md` (bu dosya) yeniden yazildi; `Jul 26, 2026 19842` observation'i `Atlas Full Regeneration + B01-B30 Bug Scorecard Workflow Launched` metadata'sini not eder. Kod tarafinda W35/W36 icin worldgen dosyalarinda **yeni yazim yok** (dogrulanmadi — `git log`ile tekrar dogrulanmali).
- **W32-B10 §A6 (indirekt, `.Worldgen.Hydration.cs:40-96`):** `HydrateBlockedCells` `SettlementLayoutStrategyFactory` uzerinden deterministic layouts'u sim-blocker grid'ine projekte eder; DERIVED, save'e girmez, her load'da yeniden calisir.

## Bilinen Borclar + Kacak Kapilari

1. **B02 test PIN eksik.** `RestoreStateJson` B02 rebuild yolu hala regresyon testi ile pinlenmedi (`Docs/ruh/w32/00-bug-triage.md:62` "Pin with a fresh-process save/load test"); "kayittan sonra dunya M haritasi calisiyor mu" invarianti sadece playtest yaklasimiyla korunuyor.
2. **Realize sirasi acik borc.** `WorldSceneDirector.Realize` `EmberWorldHost.Awake`'te calisir; B02 rebuild `EmberSaveService.Start` icinde gerceklesir — Realize ONCE calisirsa bos-Perlin failsafe'e duser (`WorldSceneDirector.cs:30-43`), sonra rebuild olsa bile sahne tazelenmez. Fix onerisi (`00-bug-triage.md:56-61`): re-realize veya pending-load'u realize once tuketmek. Kod yok.
3. **Dual `Generate` overload trap.** `OverlandWorldgen.Generate(uint seed, ...)` (`.cs:17-27`) hala public — yanlislikla cagrilirsa ayni tohum icin ayri harita uretir; B28 kod-level nakavtu yok, sadece yorum + disiplin.
4. **`FieldOwnershipRegistry` bosluklari.** `World.WorldProfile / World.Overland / World.NpcSeeds / World.Sites / World.Stockpiles (boot mint) / World.Actors (boot mint) / World.Factions (boot mint)` undeclared; W35 B04 pass'i boot-only siniri "yaz-ledger disi" olarak kabul etti — worldgen tarafi bilincli olarak lint disi.
5. **NPC-actor id collision riski.** `HydrateNpcs` `actorId = GeneratedNpcActorOffset + npc.Id.Value` (`.Worldgen.Npcs.cs:27`) — offset'in oyuncu/legacy id'lerle carpismadigi test dosyasinda pin'li **degil** (dogrulanmadi).
6. **`WorldGeography` 839 satirlik monolit.** `Assets/Scripts/Simulation/Worldgen/WorldGeography.cs` (elevation/climate/moisture/river + biome smoothing) tek dosyada; refactor bekleyen boyut, atlas'ta ayri bir "geography" sistemi ile ayirmayi hak eder.
7. **`PlanetWorldContext` global singleton.** Test paralelizminde ve New Game -> Load donusumlerinde `Clear()` cagrisi sorumlulugu adapter'da degil, tick composer'da veya bootstrap'ta net bir yerde durmuyor (dogrulanmadi — `Clear()` call-site'i grep'lenmedi). Ayni tohumla yeni oyun deterministik ama farkli tohumla New Game onceki cache'i overwrite eder — kacak: eski `PlanetField`'e referans tutan bir sistem eski dunyayla konusabilir.
8. **Overland persistence eksik — bilincli sadelik, kirilgan.** `OverlandMap` save'e girmez (tohum turevidir); B02 fix bu tercihe bagli. `WorldProfile.Seed` bir gun degistirilirse (mesela wizard yeniden parse) rebuild ayni haritayi uretmeyi durdurur — schema versiyonlama `WorldProfile` icin yok (`WorldSaveData.schemaVersion` var ama profil-yerel bir versiyon yok).

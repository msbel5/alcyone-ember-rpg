# 12-world-realize

Dünya gerçekleştirme hattı: overland grid + `RegionTile` + `SettlementKind` verisinden, aynı seed'in
her zaman aynı kasabayı ürettiği çalıştırma-zamanı (runtime) primitive geometrisine (`New Game`
sahnesi açılırken tek atım). `WorldSceneDirector.Realize` yönetir; deterministik plan
`SettlementLayoutStrategyFactory` üstünden gelir; her `BuildingPlacement`,
`RuntimeBuildingBuilder.Build` ile duvar/tavan/döşeme/pencere/kapı ve doorstep basamakları olan
bir kabuğa dönüşür; plaza (masa+banklar+kuyu), bölge bayrağı, madenler, tarlalar, kervan arabası
ve (Dungeon ise) mağara ağzı + çok-odalı delve aynı çağrının içinde eklenir. Tüm satırlar koddan
doğrulandı.

## HLD - Ne ve Neden (5-10 cümle)

Amaç: worldgen katmanının ürettiği dünyayı bake edilmiş sahnelere gömmek yerine, oyuncunun
üstünde durduğu tek tile'ı NEW GAME anında canlı olarak inşa etmek — "same seed → same town"
ilkesini gerçek geometriye kadar taşımak. Bu, Daggerfall-benzeri "yeryüzünün her karesi
yürünebilir" hedefinin girilebilir yüzü: yerleşim kimliği (kind + biome + region + seed) katı bir
plana dönüşür (Sim katmanı, `SettlementLayoutStrategy`), plan `RuntimeBuildingBuilder` +
`RuntimeDungeonBuilder` + `RuntimeMineBuilder` + `RuntimeCaravanBuilder` üstünden Unity
primitive'lerine (Cube/Cylinder) çevrilir — hiçbir AssetDatabase, hiçbir prefab yok
(`RuntimeBuildingBuilder.cs:7-9`). Populasyon kimliği aynı çağrıda pin'lenir
(`RuntimeNpcDensity.Cap`, `WorldSceneDirector.cs:58-59`) çünkü NPC spawner'ı Realize'dan SONRA
koşar ve kapasitesini bu statik kanaldan devşirir. Mimari felsefe: builder'lar tek yönlü yazarlar
(GameObject üretirler, RuntimeXxxInfo statiklerine anchor kaydederler), okuyucular
(EmberProofScreenshotDriver, EmberGeneratedActorSpawner, DomainSimulationAdapter) SONRA gelir —
çift yazar yok. BROKEN-state failsafe: `view.Overland == null` durumunda düz Perlin pad + rig
kurulup log basılır, oyuncu asla siyah boşluğa düşmez (`WorldSceneDirector.cs:29-43`).

## HLD - Akış (numaralı adımlar)

1. **Gate:** `WorldSceneDirector.Realize(view)` — `view == null` erken uyarıyla döner
   (`WorldSceneDirector.cs:21-27`). `view.Overland == null` failsafe pad + `TerrainStreamer` +
   `RuntimeLightingRig` + `RuntimePlayerRig` inşa eder (`:29-43`).
2. **Kimlik çözümü:** `ResolveHomeTile` (map'ten `PlayerOverlandTile`; yoksa merkez tile) +
   `ResolveKind` (yerleşim listesinden `Kind`; yoksa `Village`) + `homeTile.PropVariationSeed`
   (0 ise 1) — bunlar `SettlementContext(name, kind, biome, seed)` olur (`:45-48, 61, 305-321`).
3. **Log seam:** `EmberLog.Sink ??= Debug.Log` (simulation katmanına Unity log kanalını enjekte
   eder, `:51-52`).
4. **NPC kapasite pin'i:** `RuntimeNpcDensity.Cap = NpcCapFor(kind)` — City=24, Town=16,
   Village=10, Hamlet=6, Inn=5, Shrine=4, Dungeon=14 (`:58-59, 290-303`).
5. **Plan:** `SettlementLayoutStrategyFactory.For(kind).Plan(context)` — City/Town →
   `StreetLayoutStrategy`, Village → `VillageLayoutStrategy`, Hamlet/Inn/Shrine/Dungeon →
   `CompactLayoutStrategy` (kompakt VillageLayoutStrategy min=1, max=3, radius=5),
   `SettlementLayoutStrategyFactory.cs:16-33`.
6. **Root + Terrain:** `GameObject("GeneratedLocation")` altına
   `TerrainStreamer.Initialize(seed, biome, geoSampler)` — `WorldGeoSampler.TryCreate` ile map'e
   bağlı gerçek jeografi varsa deniz seviyesi rölatiftir, yoksa legacy Perlin
   (`WorldSceneDirector.cs:64-78`).
7. **Kabuklar:** `layout.Buildings` üzerinde döngü — her placement için
   `RuntimeBuildingBuilder.Build(root, placement)` (bir GameObject "Building" döndürür); ilk üç
   bina F26 fonksiyonel rol alır (`AttachFunctionalRole`): 0=Tavern (amber sign +
   `RuntimeTavernView`), 1=Temple (beyaz sign + `RuntimeTempleView`), 2=Shop (yeşil sign +
   `RuntimeShopCounterView`); dünya konumları `RuntimeInteriorInfo.Record` ile kaydedilir + host
   adapter tavana pin'lenir (`:80-97, 132-154`).
8. **Ekonomik dekor:** Ore tile'ları için (`PlanetAtlas.TryGetTileOre`) `RuntimeMineBuilder.Build`
   town-edge'de çıkar (`:103-110`); Dungeon/Shrine dışı yerleşimlerde `SimFieldView` GameObject'i
   eklenir (nüfus → plots hesabı hâlâ log satırında ama plots artık dekor değil, canlı
   `PlantGrowth` mirror'ından render — REFORM #1, `:115-128`).
9. **Delve dallanması:** `kind == Dungeon` ise `RuntimeMineBuilder.Build` mağara ağzı +
   `RuntimeDungeonBuilder.Build` 5-10 odalı deterministik graf; sim tarafı
   `DomainSimulationAdapter.EnsureDungeonDwellers(DwellerSpots, BossSpot, ArchetypeName)` çağrılır
   — F10→F18 dwellers idempotent (`:159-177`).
10. **Kervan + bayrak + plaza:** `RuntimeCaravanBuilder.Build` (görünürlük mirror'a bağlı,
    `:181-182`), `BuildRegionBanner` (pole+flag, hue `regionValue*47%360`, `:187, 263-287`),
    plaza silindiri `PlazaFloor` (Ø14m, wall_showroomoverview textured), üstüne
    `TableTop+TableTrestleA/B+BenchNorth/South+WellRing+WellPost` primitive'leri
    (`:191-227`).
11. **Işık + spawn + rig:** `RuntimeLightingRig.Apply(root, biome)` (`:229`), spawn
    `(layout.PlayerSpawnX, 0.2, layout.PlayerSpawnZ)` — **Dungeon override (W30):**
    `RuntimeDungeonLayoutInfo.RoomCount > 0` ise `StartRoomWorld + up*0.4` (crest sonrası ilk oda
    merkezi; eski EntryWorld mine mound collider'ına oturuyordu). `RuntimePlayerSpawn.Record` +
    `RuntimePlayerRig.Build` (`:231-243`).
12. **Rig eklentileri:** `RuntimeAudioDirector.Attach(PlayerRig)`,
    `RuntimeMusicDirector.Attach(PlayerRig)`, `RuntimeWaterIndex.Clear()`,
    `SwimView.Attach(PlayerRig)` (`:246-254`).
13. **READINESS log:** tek satır özet (kind, buildings, geo=REAL/LEGACY, localShore, npcCap,
    rig konumu) — playtest logları bu satıra karşı diff'lenir (`:258-260`).

### RuntimeBuildingBuilder alt-akışı (per placement)

1. Root `Building`, `placement.OriginX/Z`'e taşınır; `BuildingAccessibilityVolume` eklenir
   (halfX+margin/halfZ+margin push-outside sözleşmesi, spawner NPC'lerini kabuğun içine sokmasın
   diye — `RuntimeBuildingBuilder.cs:17-25`, `BuildingAccessibilityVolume.cs:16-45`).
2. Duvar materyali `RuntimeMaterialPalette.Textured(WallTextureId(materialIndex),
   WallColor(materialIndex), tiling: 2f)` (`:28-31`).
3. `ChooseEntranceSide`: |OriginX| > |OriginZ| ise Ox≥0 → West aksi East; değilse Oz≥0 → South
   aksi North (kapı hep merkeze bakar, `:399-404`).
4. **W31 varyasyon rolü** (`varRoll = hash(origin.x*4, origin.z*4)`): `varPick < 0.30 ∧
   size>4×4` → **UpperStorey** (`AddSlab` üst kutu + roof, `:102-113`); `0.30 ≤ varPick <
   0.55` VEYA storey band yetersizse → **hasWing = true** (aşağıda); `0.55 ≤ varPick < 0.75` →
   **Awning** (kapı üstü sundurma + iki direk, `:154-178`); kalan yalın (`:41-53`).
5. **Duvarlar (AddWallX/AddWallZ):** entrance yönündeki duvar `withDoor=true` iki segment +
   lintel (W17 playtest fix) alır; aynı kapılı-duvar mantığı `hasWing ∧ wingDoor` için de
   çalışır — WING duvarındaki paylaşılan duvara doorway açar (W31 fix; wing artık girilebilir
   oda, kapalı dekoratif kutu değil, `:57-64, 430-476`).
6. **Doorstep basamakları (W32):** entrance + varsa wing kapısı için `AddDoorstep` — üç kademe,
   tepe kotları 0/-0.3/-0.6, dışa doğru 0.42m adımlarla — kabuk terrain'in TILE MERKEZ
   yüksekliğinde oturduğundan eğimli tile'ta 1m'ye kadar düşen kapı eşiğini step limitinin altına
   böler (`:66-72, 406-428`).
7. **Çatı + ridge + baca:** `Roof` slab, `RoofRidge` alt-slab, `Chimney` deterministik rooftop
   stack (~%75, `chimneyRoll = hash(origin.x*8, origin.z*8)`, `:77-98`).
8. **hasWing branch (W31 HOLLOW WING v2):** yan wing için ayrı `WingFloor+WingRoof` slab'ları
   ve UÇ (far), YAN-A, YAN-B duvarları — döşemesi 0.03 yükselti, tavanı `max(H*0.62, 2.4)`
   (kapıdan asla kısa değil). Eski branch tek katı kübdü (`:114-153`).
9. **Zemin + partition (P1-1 / W31 gate fix):** `Floor` slab; `partitioned = size > 4.8×4.8`
   (eski >6f gate hiç açılmıyordu — generator 3.5-5.69m üretiyor, gate PATCH'lenmeden ölü
   koddu). Partition duvar setine (segW+segW + lintel) iç kapı 1.2m×2.0m açılır — entrance ekseni
   dikeyse Z=%20 depth'te, aksi durumda X=%20 depth'te (`:180-223`).
10. **Furnish (W17):** `Hash01(state)`'e göre 2-3 slot (partitioned ise merkez slot iptal, bed
    kind'ı crate'e demote — dar arka odaya 1.8m yatak sığmaz). Kind 0=BedFrame+BedBlanket,
    1=TableTop+TableLeg, 2=Crate+CrateLid (`:306-364`).
11. **Hearth + Door + Windows:** noktasal `HearthLight` (renk 1,0.78,0.55; range = maxSize*1.2;
    shadows None, `:366-377`); `DoorHinge` GameObject'ine `RuntimeDoorView` bağlanır + panel
    (collider ölür — swing sırasında oyuncuyu kilitlemesin, `:235-265`); `AddWindows` iki yan
    duvara + arka duvara `WindowFrame*+Window*` slab çifti — cam 0.72,0.82,0.92; frame
    0.24,0.17,0.10; outward offset 0.16m (üç kez raporlanan "pencereler yok" bug'ının kesin
    fix'i, `:267-302`).

## LLD - Veri Modeli (file:line)

### Presentation katmanı (Unity'li)
- `Assets/Scripts/Presentation/Ember/WorldDirector/WorldSceneDirector.cs:19-322` — statik
  facade, tek public giriş `Realize(IWorldViewReadModel)`; private yardımcılar
  `BuildRegionBanner`, `NpcCapFor`, `ResolveHomeTile`, `ResolveKind`.
- `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeBuildingBuilder.cs:11-497` — statik
  builder + `DoorSide { North, South, East, West }` private enum (`:489-495`); sabitler
  `WallThickness=0.25f`, `DoorWidth=1.6f`, `DoorHeight=2.2f` (`:13-15`).
- `Assets/Scripts/Presentation/Ember/WorldDirector/BuildingAccessibilityVolume.cs:10-46` —
  `MonoBehaviour`, `_halfX/_halfZ/_margin` alanları, `Configure(sizeX, sizeZ, margin)` +
  `TryPushOutside(pos, out adjusted)` sözleşmesi.
- `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeDungeonLayoutInfo.cs:9-52` — statik
  alanlar `RoomCount`, `EntryWorld`, `StartRoomWorld`, `BossRoomWorld`, `ChestWorld`,
  `FootprintCenterWorld`, `FootprintExtentMeters`, `List<Vector3> DwellerSpots`, `BossSpot`,
  `TrapWorld`, `KeyWorld`, `BossDoorWorld`, `ArchetypeName = "Mağara"`; iki writer
  `Record(...)` ve `RecordArchetype(...)`.
- `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeFunctionalInteriors.cs:8-20` — statik
  `RuntimeInteriorInfo { TavernWorld, TempleWorld, ShopWorld, Record(...) }`; aynı dosyada
  `ScreenRequestSignal { Request(str), Consume() }` (`:24-29`).
- `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimePlayerSpawn.cs:8-13` — statik
  `Position = (0, 0.2, 0)`, `Record(spawn)`.
- `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeNpcDensity.cs` — statik `Cap` alanı
  (kullanım `WorldSceneDirector.cs:58, 260`).

### Simulation katmanı (Unity'siz, deterministik)
- `Assets/Scripts/Simulation/WorldDirector/SettlementContext.cs` — `readonly struct`
  `(Name, Kind, Biome, Seed)`.
- `Assets/Scripts/Simulation/WorldDirector/BuildingPlacement.cs:9-27` — `readonly struct`,
  `OriginX/OriginZ/SizeX/SizeZ/Height/MaterialIndex` alanları (Unity referansı yok — bilinçli).
- `Assets/Scripts/Simulation/WorldDirector/SettlementLayout.cs:10-34` — `sealed class`
  `(IReadOnlyList<BuildingPlacement> Buildings, GroundRadius, PlayerSpawnX, PlayerSpawnZ,
  PlayerFacingDeg)`.
- `Assets/Scripts/Simulation/WorldDirector/ISettlementLayoutStrategy.cs` — `Plan(in
  SettlementContext) → SettlementLayout` sözleşmesi.
- `Assets/Scripts/Simulation/WorldDirector/SettlementLayoutStrategyFactory.cs:10-34` — statik
  `For(kind)` — üç strateji singleton'u (`Village`, `Compact`, `Streets`).
- `Assets/Scripts/Simulation/WorldDirector/VillageLayoutStrategy.cs:13-` — City/Town/Village
  ölçekli halka planlaması (sabitler `CentralPlazaRadius=7`, `RingSpacingMeters=13`,
  `MinimumArcSpacingMeters=12`, `MaxRings=8`, `DefaultStreetClearance=4.5`).
- `Assets/Scripts/Simulation/WorldDirector/CompactLayoutStrategy.cs:8-13` —
  `VillageLayoutStrategy(min=1, max=3, ringRadius=5)` sarar.
- `Assets/Scripts/Simulation/WorldDirector/StreetLayoutStrategy.cs:14-` — radial avenue
  ızgarası; sabitler `PlazaRadius=8`, `StreetHalfWidth=3.5`, `ParcelStep=9`, `Clearance=2.5`;
  City 4-5 avenue × 4-6 parsel/yön, Town 3-4 × 3-4; `heightBoost` City=3.2, Town=1.0.
- `Assets/Scripts/Simulation/WorldDirector/WorldGeoSampler.cs` — `TryCreate(map, tile, seed,
  out sampler)`; `SeaLevelMeters`, `HasLocalShore` sinyalleri.

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

- `public static void Realize(IWorldViewReadModel view)` —
  `WorldSceneDirector.cs:21` — dünya gerçekleştirme facade'ı; tüm alt-adımları sırayla koşar.
- `private static void BuildRegionBanner(Transform parent, ulong regionValue)` —
  `WorldSceneDirector.cs:263` — plaza kenarına deterministik renkli direk+bayrak diker.
- `private static int NpcCapFor(SettlementKind kind)` —
  `WorldSceneDirector.cs:290` — kind → billboard sınırı (City=24 … Dungeon=14).
- `private static RegionTile ResolveHomeTile(OverlandMap map, GridPosition tilePosition)` —
  `WorldSceneDirector.cs:305` — tile lookup + merkez fallback.
- `private static SettlementKind ResolveKind(OverlandMap map, GridPosition tilePosition)` —
  `WorldSceneDirector.cs:311` — settlement listesinden Kind eşleştirir; yoksa Village.
- `static void AttachFunctionalRole(GameObject building, int roleIndex)` —
  `WorldSceneDirector.cs:132` (Realize içinde local func) — glowing sign cube + trigger view
  (Tavern/Temple/Shop) ekler.
- `public static GameObject Build(Transform parent, BuildingPlacement placement)` —
  `RuntimeBuildingBuilder.cs:17` — bir kabuk (duvar + çatı + zemin + partition + furnish +
  hearth + door + windows + doorstep) inşa eder, root GameObject döner.
- `private static void AddDoor(Transform root, BuildingPlacement placement, DoorSide entrance)` —
  `RuntimeBuildingBuilder.cs:235` — hinge + kapak paneli (collider'sız); `RuntimeDoorView`
  bağlar.
- `private static void AddWindows(Transform root, BuildingPlacement placement, DoorSide
  entrance)` — `RuntimeBuildingBuilder.cs:267` — iki yan + arka duvara frame+pane slab çifti;
  outward 0.16m offset.
- `private static void Furnish(Transform root, BuildingPlacement placement, DoorSide entrance,
  bool partitioned)` — `RuntimeBuildingBuilder.cs:306` — origin-hash'li seed ile 2-3 kind
  (bed/table/crate) DISTINCT slotlara yerleştirir.
- `private static void AddHearthLight(Transform root, BuildingPlacement placement)` —
  `RuntimeBuildingBuilder.cs:366` — noktasal ışık, gölgesiz, range = maxSize×1.2.
- `private static float Hash01(ref uint state)` —
  `RuntimeBuildingBuilder.cs:379` — inline xor-shift; furnish + varyasyon rolleri için.
- `private static void AddSlab / AddWall / AddWallX / AddWallZ / AddDoorstep` —
  `RuntimeBuildingBuilder.cs:388-476` — primitive Cube üretici yardımcılar; `AddWallX/Z`
  `withDoor=true` ise iki segment + lintel bırakır; `AddDoorstep` üç-kademe basamak dizer.
- `private static DoorSide ChooseEntranceSide(BuildingPlacement placement)` —
  `RuntimeBuildingBuilder.cs:399` — kapıyı plaza merkezine baktırır.
- `void BuildingAccessibilityVolume.Configure(float sizeX, float sizeZ, float margin)` —
  `BuildingAccessibilityVolume.cs:16` — half-extent + margin alanlarını kilitler.
- `bool BuildingAccessibilityVolume.TryPushOutside(Vector3 worldPosition, out Vector3
  adjusted)` — `BuildingAccessibilityVolume.cs:23` — eğer world nokta hacmin içindeyse en yakın
  yüze iter, dışarıdaysa false döner.
- `RuntimeDungeonLayoutInfo.Record(int roomCount, Vector3 entry, Vector3 startRoom, Vector3
  bossRoom, Vector3 chest, Vector3 fpCenter, float fpExtent, List<Vector3> dwellerSpots, Vector3
  bossSpot, Vector3 trap, Vector3 key, Vector3 bossDoor)` — `RuntimeDungeonLayoutInfo.cs:33` —
  tek yazar (`RuntimeDungeonBuilder`), tüm anchor'ları statik alanlara yazar.
- `RuntimeDungeonLayoutInfo.RecordArchetype(string archetypeName)` —
  `RuntimeDungeonLayoutInfo.cs:28` — F29 arketip adı (Mağara/Kripta/Harabe).
- `RuntimeInteriorInfo.Record(Vector3 tavern, Vector3 temple, Vector3 shop)` —
  `RuntimeFunctionalInteriors.cs:14` — WorldSceneDirector tarafından ilk üç binanın dünya
  konumlarını yazar.
- `RuntimePlayerSpawn.Record(Vector3 spawn)` —
  `RuntimePlayerSpawn.cs:12` — ölüm-uyanma teleportu için rig konumunu pin'ler.
- `ISettlementLayoutStrategy.Plan(in SettlementContext context) → SettlementLayout` —
  `VillageLayoutStrategy.cs:32`, `CompactLayoutStrategy.cs:12`, `StreetLayoutStrategy.cs:21` —
  strateji girişleri; sim tarafı Unity'siz kalır.

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Bu sistem sim veri modeline (WorldState/Actors/…) YAZMAZ; sahne graf'ına ve statik
"runtime info" kanallarına yazar. Registry'ye kayıtlı SIM alanı yok (repo grep'inde `WorldDirector`
karşılığı `FieldOwnershipRegistry` girdisi görülmedi — dogrulanmadi).

**Yazdığı runtime kanallar (sahibi: `WorldSceneDirector`):**
- `RuntimeNpcDensity.Cap` (int; `WorldSceneDirector.cs:58`) — sonra `EmberGeneratedActorSpawner`
  ve `EmberProofScreenshotDriver` okur.
- `RuntimeInteriorInfo.TavernWorld/TempleWorld/ShopWorld` (Vector3; `:92`) — sonra
  `EmberProofScreenshotDriver` + `DomainSimulationAdapter.WorldProjection` okur.
- `RuntimePlayerSpawn.Position` (Vector3; `:242`) — sonra `RuntimeMainQuestFinale` / ölüm-uyanma
  akışı okur.
- `EmberCrpg.Simulation.Diagnostics.EmberLog.Sink` (delegate, ??= Debug.Log; `:52`) — proje
  standardı log seam'i, sim katmanının Unity-agnostik logger'ı.

**Yazdığı runtime kanallar (sahibi: `RuntimeBuildingBuilder`):**
- Yeni `GameObject`'ler (`Building`, `Wall`, `Roof`, `Floor`, `WingFloor/Roof`, `UpperStorey`,
  `Awning`, `Chimney`, `WindowFrame*/Window*`, `DoorHinge/DoorPanel`, `HearthLight`,
  `BedFrame/BedBlanket`, `TableTop/TableLeg`, `Crate/CrateLid`, `Doorstep0..2`) — parent
  `GeneratedLocation/Building` altında.
- `BuildingAccessibilityVolume` bileşeni (`RuntimeBuildingBuilder.cs:22-25`).

**Yazdığı runtime kanallar (sahibi: `RuntimeDungeonBuilder`):**
- `RuntimeDungeonLayoutInfo.*` alanları — `RuntimeDungeonBuilder.cs:52, 267`; tek yazar
  garantisi.

**Okuduğu (ama YAZMADIĞI) alanlar / servisler:**
- `IWorldViewReadModel.Overland / PlayerOverlandTile / StartingSettlementName` —
  `WorldSceneDirector.cs:29, 45-48`.
- `OverlandMap.Settlements` (SettlementRecord listesi, `ResolveKind`) — `:311-319`.
- `PlanetAtlas.TryGetTileOre(map, x, y, out iron, out coal)` — `:103-105`.
- `RegionTile.Biome / PropVariationSeed / RegionId` — `:47-48, 187`.
- `RuntimeDungeonLayoutInfo.RoomCount / StartRoomWorld / DwellerSpots / BossSpot /
  ArchetypeName` — Realize DUNGEON dalında OKUR (`:174, 236-241`).
- `EmberDomainAdapterLocator.Current as DomainSimulationAdapter` — `PinHostInsideTavern`,
  `EnsureDungeonDwellers` — `:93-96, 169-176`.

## LLD - Ürettiği/Tükettiği Olaylar

Bu sistem `WorldEventKind` üretmez veya tüketmez — üretim/observer akışının ÖNCESİNDE koşar
(sahne açılışı, tek atım). Domain event çıkış noktaları yok.

**Ürettiği log satırları (playtest / shipcheck diff kaynağı):**
- `"[WorldDirector] directing settlement '{name}' kind=... biome=... seed=..."` (`:54`).
- `"[WorldDirector] npc billboard cap for {kind}: {cap}"` (`:59`).
- `"[WorldDirector] terrain bound to world geography (REAL — sea at {SeaLevelMeters:0.#}m
  rel.)"` | `"[WorldDirector] no geography snapshot — legacy Perlin terrain (PARTIAL)."`
  (`:72-74`).
- `"[WorldDirector] functional interiors: tavern/temple/shop on buildings 0/1/2."` (`:96`).
- `"[WorldDirector] {N} buildings built"` (`:98`).
- `"[WorldDirector] {coal|iron} mine realized at town edge (iron=..., coal=...)."` (`:109`).
- `"[WorldDirector] fields={N} plots for pop={P} ({Kind}) — farm belt at the town edge."`
  (`:127`).
- `"[WorldDirector] delve dwellers ensured: +{N} across {R} rooms (idempotent — corpses stay
  down)."` (`:176`).
- `"[WorldDirector] trade cart realized (visibility bound to the caravan mirror)."` (`:182`).
- `"[WorldDirector] region banner raised (region ..., hue ...)."` (`:286`).
- `"[WorldDirector] realize complete for '{name}': kind=..., buildings=..., geo=REAL/LEGACY,
  localShore=..., npcCap=..., rig at ..."` — **READINESS satırı**, tek başına özet, (`:259-260`).
- `"[Building] furnished {pieces} pieces at (x,z)"` — per building (`RuntimeBuildingBuilder.cs:363`).

**Bir sinyal üretir (`ScreenRequestSignal`) ama Realize bunu kullanmaz** — F26 world-prop → UI
istekleri için (`RuntimeFunctionalInteriors.cs:24-29`).

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

**Deterministik plan (sim, Unity'siz):**
- `Assets/Tests/EditMode/WorldDirector/SettlementLayoutStrategyFactoryTests.cs` — City/Town →
  Streets, Village → Ring, Hamlet/Inn/Shrine/Dungeon → Compact, unknown → Village
  (`:12-30`).
- `Assets/Tests/EditMode/WorldDirector/SettlementLayoutDeterminismTests.cs` — aynı seed
  identical layout (`:14`), farklı seed farklı layout (`:35`), binalar ground plane içinde
  (`:53`), street clearance korunur (`:70`).
- `Assets/Tests/EditMode/WorldDirector/StreetLayoutStrategyTests.cs` — aynı seed identical
  (`:13`), City > Town bina sayısı (`:26`), plaza clear + çift-örtüşme yok (`:33`).

**Presentation (bağlı sistemler; Realize'ı doğrudan koşan test bulunamadı):**
- `Assets/Tests/EditMode/Presentation/EmberWorldHostAdapterBindingTests.cs` — Realize
  adapter'ının narrow-role hydration'ı (`:10, 25`).
- `Assets/Tests/EditMode/Presentation/VisualLayer/WorldEventTailSnapshotTests.cs` — visual olay
  tail'inin sözleşmesi (yakın komşu).
- `Assets/Tests/EditMode/Presentation/WorldHostInputPolicyTests.cs` — dış input policy.

**Doğrudan Pinleme boşluğu (dogrulanmadi):** `WorldSceneDirector.Realize`, `RuntimeBuildingBuilder.Build`,
`BuildingAccessibilityVolume.TryPushOutside`, `RuntimeInteriorInfo.Record`,
`RuntimePlayerSpawn.Record` için Assets/Tests grep'inde eşleşen test dosyası bulunamadı.
Realize'ı bir NUnit yolundan koşan pin yok — proof harness (20-proof-harness dokümanı) tek
canlı guardrail.

**W32-W36 hikâye testleri:** W33/W34 story test'leri sim slice'larını (Farm/Sleep/Work) pinliyor;
bu SİSTEMİ (world-realize) doğrudan pinleyen W32+ hikâye testi eklenmedi (dogrulanmadi — grep
`WorldSceneDirector|RuntimeBuildingBuilder|BuildingAccessibilityVolume`, Assets/Tests, 0
eşleşme).

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

Git log (`Assets/Scripts/Presentation/Ember/WorldDirector/WorldSceneDirector.cs` +
`RuntimeBuildingBuilder.cs` + `Assets/Scripts/Simulation/WorldDirector/`, `--since=2026-07-05`):

- **W32 — `959ccb99` `feat(world)+docs(ruh): doorsteps bridge sloped doorways`.** Kapı eşiği
  üç-kademe basamakla (`AddDoorstep`, `RuntimeBuildingBuilder.cs:406-428`) toprağın kotuna iner;
  entrance + wing kapısı ayrı ayrı köprülenir (`:70-72`). "kapi yukarda gorunuyor, bazen
  girilmiyor" playtest bug'ının fix'i. **Doorstep tiers W32'de landed — task prompt'undaki "W35"
  ifadesi git ile eşleşmiyor (dogrulanmadi ama commit tarih pin'i W32'yi işaret ediyor).**
- **W31 — `1e9b474b` `fix(soul): W31 - eight live wounds + the full HLD/LLD systems atlas`.**
  Winged house paylaşılan duvarına build-time'da doorway açan `hasWing/wingDoor` hoist'ı
  (`RuntimeBuildingBuilder.cs:38-64`); L-wing artık HOLLOW ANNEX (WingFloor+WingRoof + üç duvar,
  `:114-153`) — eski sealed cube ölür. Partition gate `>6f` → `>4.8f` (dead-code autopsy;
  generator 3.5-5.69m üretiyor, eski gate hiç açılmıyordu, `:188`). Furnish partitioned dallanma:
  merkez slot iptal, bed→crate demote (`:337, 346`).
- **W30 — `8c16b572` `fix(world): W30 - the four wounds close`.** Dungeon spawn override
  `EntryWorld` → `StartRoomWorld + up*0.4` (`WorldSceneDirector.cs:236-241`); eski
  proof-camera anchor mine mound collider'ıyla çakışıyordu — CharacterController depenetration
  oyuncuyu çatıya fırlatıyordu. `RuntimeDungeonLayoutInfo.Record` imzasına
  `StartRoomWorld` eklenmesi bu değişikliğin sim tarafı.
- **W33-W36 → değişiklik YOK.** Sim katmanı slice'ları (Farm/Sleep/Work) bu sistemi güncellemedi.
  `WorldSceneDirector.cs`, `RuntimeBuildingBuilder.cs`, `Assets/Scripts/Simulation/WorldDirector/`
  git log'unda `--since=2026-07-15` sıfır commit.

Referans (W32 öncesi büyük hareketler, bağlam için):
- `79d9eaca` P1-1 real interior partitions with doorways (WallWithGap portu — W31'in temelini
  attı).
- `46806f5b` skyline stops repeating — storeys/wings/awnings varyasyon rolleri (`:41-178`).
- `6953e3ac` doorways with lintels + windows that read (`AddWallX/Z` lintel + `AddWindows`
  outward offset).
- `5738d49f` table + trestle + bench + well plaza propları
  (`WorldSceneDirector.cs:216-227`).

## Bilinen Borçlar + Kaçak Kapıları

1. **Realize'ın kendisi PIN'SİZ.** `WorldSceneDirector.Realize` grep'te sıfır test eşleşmesi
   veriyor (Assets/Tests). Sim strateji planı pinli, ama planı → GameObject çevirimini test
   koşan bir edit-mode ya da play-mode test yok. Tek canlı guardrail proof-harness screenshot
   akışı (20-proof-harness) — memory'deki "verify at render layer" ilkesi bu sistem için
   düğüm noktası.
2. **`RuntimeBuildingBuilder.Build` monolit — 200+ satırlık tek metot.** Varyasyon rolü,
   duvar/roof/wing/awning branch'ları, partition, furnish, door, windows tek `Build` içinde;
   `hasWing` / `varPick` / `partitioned` flag'leri lokal değişken çorbası oluşturuyor.
   Regression yüzeyi geniş; birim testi bir alt-adımı izole edemiyor (dogrulanmadi ama
   dosya satır sayısı 497 ve tek public metot bunu doğrular).
3. **`EmberDomainAdapterLocator.Current` casting kaçak kapısı.** `WorldSceneDirector.cs:93,
   169-170` `as DomainSimulationAdapter` — locator'ın somut tipi değişirse `PinHostInsideTavern`
   ve `EnsureDungeonDwellers` sessizce null'a düşer (log yok). Realize devam eder, delve DWELLER
   spawn'ı sessizce 0 döner.
4. **W31 dead-code autopsy hâlâ patch, gate değil.** `partitioned = SizeX > 4.8f && SizeZ >
   4.8f` (`RuntimeBuildingBuilder.cs:188`) — generator'ın min/max footprint'i değişirse gate yine
   ölü koda düşer. Threshold `BuildingPlacement` boyutlarıyla birlikte değişmiyor; regenerator
   parametreleri değişince atlas + gate manuel senkron gerektirir.
5. **W30 spawn override `RoomCount > 0` şartına bağlı.** `RuntimeDungeonLayoutInfo` ilk çağrıdan
   önce OKUNUYOR olabilir mi? Realize'da `RuntimeDungeonBuilder.Build` DAHA ÖNCE çağrılıyor
   (`WorldSceneDirector.cs:166 → 236-241`), sıra doğru — ama başka giriş noktası (test harness'i,
   partial fail) statik alanı temizlemezse ESKİ oyunun `StartRoomWorld`'ü yeni sahnede yanlış
   spawn üretebilir (statik alan life-cycle'ı domain reload'a bağlı, dogrulanmadi).
6. **`AttachFunctionalRole` role-index hard-coded 0/1/2 = tavern/temple/shop.** Layout ilk üç
   binayı köşe/plaza-yakın diye üretmiyor (VillageLayoutStrategy ring'te açı sırası, StreetLayout
   avenue sırası). Kind identity oyuncuya sağlam bir yer sunmuyor — "tavern nerede?" playtest
   sorusu için deterministik ama insan-okuyabilir bir seçim yok (dogrulanmadi bug — playtest
   log grep'inde bulunamadı, ama tasarım borcu net).
7. **Pop → plots hesabı ölü satır.** `WorldSceneDirector.cs:117-127` `plots` hesaplanıyor,
   log'lanıyor ama HİÇBİR yere iletilmiyor — REFORM #1'de field belt kaldırıldı, `SimFieldView`
   plots argümanı almıyor. Kod okuyanı yanıltır (log "farm belt at the town edge" hâlâ diyor).
8. **`RuntimeMaterialPalette` tekilliği doğrulanmadı.** Tüm builder'lar palette'i `Solid` /
   `Textured` üstünden okuyor; palette invalidation stratejisi ve texture-atlas kaynağı bu
   dokümanın kapsamı dışında ama Realize'ın deterministik "same seed → same look" iddiası
   palette'in seed-agnostik olmasına bağlı (dogrulanmadi).
9. **`BuildingAccessibilityVolume` yalnız runtime NPC push-out için.** Sim tarafı actor
   placement bu hacmi bilmez ("does not affect deterministic simulation placement" —
   `BuildingAccessibilityVolume.cs:6-8`); NPC yürüyüşü fiziksel controller'a bağımlı, bu da
   NPC'nin duvarı geçmesini engellemek için hem billboard cull hem push-out olması gerektiği
   anlamına gelir. Kaçak: yeni bir spawner tipi bu bileşeni sormayı unutursa NPC binada gömülür.
10. **Failsafe pad'in de deterministik olmama riski.** `map == null` failsafe (`:29-43`)
    `TerrainStreamer.Initialize(1u, BiomeKind.Plains, sampler=null)` çağırıyor; seed sabit ama
    sampler yok, streamer varsayılana düşer. Bu path bir kez tetiklenmiş oturum + sonraki
    ideal-restore arası state'i karıştırabilir (dogrulanmadi ama BROKEN log'u belirteci
    bulunması gereken bir hikâye).

# 11-worldgen-overland

Dünya üretimi hattı: küresel gezegen simülasyonu (`PlanetGenerator`) → `GeneratedWorld` sözleşmesi
(`PlanetToWorldMapper`) → gezilebilir overland grid (`OverlandWorldgen`) → runtime `WorldState`
hidrasyonu (`DomainSimulationAdapter.Worldgen.*`). Tüm satırlar koddan doğrulandı; doğrulanamayanlar
açıkça `dogrulanmadi` etiketiyle işaretli.

## HLD - Ne ve Neden

Amaç: ana menü sihirbazının üç serbest-metin cevabını (mood / calling / start) deterministik bir
uint seed'e katlayıp (`FoldSeed`, FNV-1a — `DomainSimulationAdapter.Worldgen.Selection.cs:99-115`)
aynı cevaplardan HER ZAMAN aynı dünyayı üretmek — "share your seed" bir replay özelliği olsun diye
(`DomainSimulationAdapter.Worldgen.cs:28-32`). Ölçek hedefi Daggerfall-stili: ~50 bölge, ~200
yerleşim, ~20 faction, ~750 NPC, [900K, 1.1M] nüfus, çok-yüzyıllık tarih
(`WorldgenService.cs:8-15`); overland 16x16 varsayılanda 640x640 km = 409.600 km² — Daggerfall'ın
~2 katı (`OverlandParameters.cs:8-12`). Canlı oyun artık düz (flat) üreticiyi değil, KÜRESEL
gezegen hattını kullanır: level-5 icosphere (~10.242 tile) üzerinde plakalar → yükselti → iklim →
hidroloji → erozyon → kaynak → yerleşim aşamaları koşar, sonra `PlanetToWorldMapper` bunu AYNI
`GeneratedWorld` şekline indirger (`DomainSimulationAdapter.Worldgen.cs:41-47`,
`PlanetWorldService.cs:15-17`). Oyuncuya görünen yüzü: karakter yaratmadaki "gezegen oluşuyor"
reveal haritası, M-tuşu dünya haritası, HUD'daki tile koordinatı, gün-sayılı fast travel (1 tile
kenarı 40 km = 1 gün yol, `DomainSimulationAdapter.Travel.cs:47-51`) ve NPC'lerin gerçekten
yaşadığı kasabalar. Mimari felsefe: worldgen SAF ve Unity'siz (`WorldgenService.cs:22`), sonuç
immutable `GeneratedWorld` bundle'ı; presentation adapter'ı bu bundle'ı runtime `WorldState`
store'larına HİDRATE eder — koordinat birleşmesi (tile × 40000) bu hidrasyonda yapılır, böylece
domain grid'i ile yürünebilir dünya TEK koordinat uzayını paylaşır
(`DomainSimulationAdapter.Worldgen.Hydration.cs:54-63`).

## HLD - Akis

1. **Sihirbaz → intent (menü sahnesi):** oyuncunun cevapları `EmberWorldGenIntent.Pending`
   statik slotuna yazılır — sahneler arası tek-atımlık el değiştirme (`EmberWorldGenIntent.cs:3-15`).
   Reveal ekranı gezegeni STREAMING observer ile önceden üretebilir ve
   `PlanetWorldContext`'e cache'ler (`PlanetWorldService.cs:8-11, 19-33`).
2. **Bootstrap tetiği:** `EmberWorldHost.Awake` pending intent'i tüketip
   `SeedWorld(mood, calling, start, worldSeed?)` çağırır (`EmberWorldHost.cs:86-93`). Kadans:
   yalnız New Game sahne açılışında BİR KEZ; tick sistemi değildir. Fast travel sahne reload'unda
   adapter `EmberWorldContinuity` üzerinden taşınır, yeniden seed edilmez
   (`EmberWorldContinuity.cs:5-11`).
3. **SeedWorld (`DomainSimulationAdapter.Worldgen.cs:26-87`):** seed katlanır (worldSeed verilmişse
   o), `_world.RoomSeed = (int)seed` (`:35` — runtime kronik bu tohumdan beslenir),
   `WorldGenesisMapper` mood/calling/start'ı `WorldStyle`/`WorldGenre`/tercih edilen yerleşim
   boyutuna çevirir (`Selection.cs:84-97`), `WorldgenParameters.For(style, genre)` knob setini
   kurar (`WorldgenParameters.cs:98-166` — stil/tür başına bölge-şehir-faction-NPC sapmaları,
   `historyYears: 1200`).
4. **Gezegen üretimi:** `PlanetWorldService.GetOrGenerate(seed, parameters)` — seed başına TEK
   üretim, `PlanetWorldContext.Instance` singleton cache (`PlanetWorldService.cs:19-33`,
   `PlanetWorldContext.cs:14-41`). `PlanetToWorldMapper.Map` zinciri
   (`PlanetToWorldMapper.cs:20-60`): kara tile'larından suitability + uzaklık skoruyla bölge
   tohumları seçilip BFS ile bölgeler büyütülür (`:80-197`), 128x64 equirect coğrafya raster'ına
   projekte edilir (`GeographyWidth/Height`, `:17-18`; projeksiyon `:358-429`), bölge/yerleşim/
   faction kayıtları eşlenir (`:494+`; yerleşim tile'ı = icosphere tile'ının en yakın land cell'i,
   `:550-583`), `WorldHistorySimulator` 1200 yılı simüle eder (`:32-38`), tarih projeksiyonu ÖLEN
   yerleşimleri düşürür ve nihai tile/tier'ı damgalar (`WorldgenService.HistoryProjection.cs:89-100`),
   `PlanetNpcSeeder.Seed` hayatta kalan yerleşimlere NPC tohumlarını dağıtır (`:1027-1069`).
   Zengin `PlanetField` sidecar olarak `world.PlanetData`'da kalır (`:56-58`,
   `GeneratedWorld.cs:155-161` — save'e YAZILMAZ).
5. **Başlangıç seçimi:** `SelectStartingRegion/Settlement/Faction` — start metni ad-substring'iyle
   eşleşirse o, yoksa tercih edilen boyuttaki ilk yerleşim, yoksa `[0]`
   (`Selection.cs:25-82`).
6. **Hidrasyon (`HydrateGeneratedWorld`, `Hydration.cs:25-33`):** sırayla `HydrateSites` →
   `HydrateFactions` → `HydrateNpcs` → `HydrateHistory` → `SeedWorldQuests` →
   `MovePlayerToStartingSettlement`.
   - **HydrateSites (`Hydration.cs:35-84`):** bölge siteleri sentetik (i%10)*96 grid'ine; yerleşim
     siteleri KOORDİNAT BİRLEŞMESİ ile `x = TileX*40000 + 20000` (tile merkezli, 1 cell ≈ 1 m;
     `:54-63`) — eski (i%32)*12 kompakt yerleşim yalnız tile'sız legacy kayıtlara kalır (`:67-68`).
     Site yarıçapı boyuta göre 28/24/18/14 (`:72`). Her yerleşime 150 buğdaylık larder stockpile
     (`:75-80`), sonra `SeedStartingProductionSites` (`Worldgen.Production.cs:32`).
   - **HydrateFactions (`Hydration.cs:86-118`):** ilk üç faction'a craft/trade/law ekseni tag'leri
     garanti edilir (runtime kronik ölmesin diye), ilişki tohumları + başlangıç faction'ına +15.
   - **HydrateNpcs (`Worldgen.Npcs.cs:25-68`):** her `NpcSeedRecord` →
     `ActorId(10000 + NpcId)`; ev hücresi ve gündüz çapası site bound'ları içinde iki AYRI hash'le
     dağıtılır (`:72-99`), rol → iş tercihi; `GrantStartingJobPreference` bir işçiye Smith verir
     (`:107-141`).
   - **HydrateHistory (`Worldgen.Player.cs:25-39`):** üretilen tarih olayları
     `WorldEventLog`'a yıl→dakika çevrimiyle append edilir; kind eşlemesi
     `ToRuntimeEventKind` (`Worldgen.NpcStats.cs:57-71`).
7. **Overland projeksiyonu:** `_world.Overland = OverlandWorldgen.Generate(generated,
   new OverlandParameters(geo.Width, geo.Height))` — AYNI GeneratedWorld'den, boyutlar coğrafyadan
   türetilir; ayrı bir Default regen DEĞİL (W1 düzeltmesi, `Worldgen.cs:67-77`).
   `Generate(world, parameters)` (`OverlandWorldgen.cs:27-47`): boyut uyuşmazlığında throw
   (`:49-57`), `ProjectSettlements` her yerleşimi tarih tile'ına koyar (tile yoksa / denizdeyse /
   sınır dışıysa throw, `OverlandWorldgen.Settlements.cs:25-30`), boyut+biome+stable-roll ile
   `SettlementKind` sınıflanır (`:78-107`), `EnsureMinimumDungeons` en az 3 delve garanti eder
   (`:41-65`), tile RNG tohumları `seed ^ 0xA511E9B3` (`OverlandWorldgen.cs:98-105`), coğrafya ve
   `PlanetField` sidecar store'lara asılır (`:44-45`).
8. **Görünürleşme:** `WorldSceneDirector.Realize` başlangıç yerleşimini 3D'ye kurar
   (`EmberWorldHost.cs:101-107`); reveal ve M-haritası `PlanetAtlas.TryRender` (icosphere-kaynaklı
   atlas, marker'lar veri-üstü pin; `OverlandMapPlanetStore.cs:48-72, 105-135`), sidecar yoksa
   `OverlandMapImageSampler.Sample`'a düşer (`CharacterCreationController.Transitions.cs:308-360`).
9. **Save/Load:** `NpcSeeds` ve `WorldProfile` slice-json'a girer (`WorldSaveMapper.cs:85-86,
   191-192`); `Overland` ve `PlanetData` GİRMEZ — load'da canlı oturumun haritası korunur
   (`DomainSimulationAdapter.Save.cs:42-49`), soğuk load'da yeniden üretim YOK (borçlara bak).

## LLD - Veri Modeli

| Tip | Alanlar | Kaynak |
|---|---|---|
| `GeneratedWorld` | `Seed:uint`, `Regions`, `Settlements`, `Factions`, `FactionRelations`, `Npcs`, `History`, `NotableFigures` (hepsi ReadOnlyCollection-sarılı), `Geography:WorldGeography`, `PlanetData:PlanetField` (mutable sidecar, save dışı), `TotalPopulation` (türetilmiş) | `GeneratedWorld.cs:88-183` |
| `SettlementRecord` | `Id`, `Region`, `Name`, `Population>0`, `Size≠None`, `TileX/TileY` (−1/−1 = legacy konumsuz; kısmi negatif throw), `HasTilePosition` | `SettlementRecord.cs:13-54` |
| `RegionRecord` | `Id`, `Name`, `PopulationLow/High`, `Biome:WorldBiomeKind`, `TileX/TileY`, `HasTilePosition` | `RegionRecord.cs:13-53` |
| `NpcSeedRecord` | `Id:NpcId`, `Home:SettlementId`, `Faction:FactionId`, `Name`, `BirthYear`, `Role≠None`, `PortraitAssetPath` — ctor tüm boş sentinelleri reddeder ("her NPC bir yerleşimde yaşar") | `NpcSeedRecord.cs:18-64` |
| `FactionRelationSeed` | `(FactionA, FactionB, Reputation)` — kanonik low-then-high sıralama, duplicate çiftler çöker | `GeneratedWorld.cs:23-52` |
| `WorldProfile` | `Style`, `Genre`, `Seed:uint`, `TargetPopulation`, `RegionCount`, `FactionCount`, `HistoryYears`, `MoodKeyword`, `PlayerCallingKeyword`, `StartLocationKeyword` | `WorldProfile.cs:41-50` |
| `WorldGeography` | `Width/Height`, `Elevation/Temperature/Moisture:double[]`, `LandMask:bool[]`, `OverlandBiomes`, `WorldBiomes`, `RegionIds` + `IsLandAt/OverlandBiomeAt/RegionAt` erişimcileri | `WorldGeography.cs:12-124` |
| `OverlandMap` | `Width/Height`, `Tiles:RegionTile[]` (tam kapsama + duplicate koordinat/yerleşim doğrulaması ctor'da), `Settlements` + id sözlüğü; `TileAt/TryGetSettlement/DistanceBetween` (Chebyshev) / `TryGetNearestSettlement` | `OverlandMap.cs:10-159` |
| `RegionTile` | `(x, y, regionId, biome, settlementIds, tileSeed, climate)` — BuildTiles ctor çağrısından; alan listesi `RegionTile.cs`'ten tek tek dogrulanmadi | `OverlandWorldgen.cs:84-91` |
| `OverlandSettlement` | `(Id, Kind:SettlementKind, TilePosition, Name, TemplatePackTag)` — ctor kullanımından | `OverlandWorldgen.Settlements.cs:34` |
| `OverlandParameters` | `Width=16, Height=16, BiomeSeedCount=12, SettlementDensity=0.30, RegionEdgeKm=40`; `RegionAreaKm2`, `TotalAreaKm2` | `OverlandParameters.cs:14-56` |
| `WorldgenParameters` | bölge/kapital/şehir/kasaba/köy/faction/NPC/tarih-yılı/başlangıç-yılı + `Style/Genre/TargetPopulation`; `For(style, genre)` matrisi | `WorldgenParameters.cs:16-166` |
| `SiteRecord` | `Id`, `Kind`, `Name`, `MinBound/MaxBound:GridPosition` (inclusive), `Contains` | `SiteRecord.cs:15-48` |
| `WorldState` alanları | `Sites:SiteStore` (`:38`), `Overland:OverlandMap` (`:42-44` "deterministik, load'da yeniden üretilebilir" NOTU — kod bunu yapmıyor), `NpcSeeds:List<NpcSeedRecord>` (`:51`), `WorldProfile` (`:52`), `RoomSeed` (`:26`); `CopyFrom` hepsini referans-kopyalar (`:216-228`) | `WorldState.cs` |
| Save şekilleri | `NpcSeedSaveData{id, home, faction, name, birthYear, role, portrait}` (`WorldSaveData.cs:116-125`), `WorldProfileSaveData{style, genre, seed, ...}` (`:128+`); restore geçersiz satırları sessizce filtreler | `WorldSaveMapper.Narrative.cs:203-234` |

Sabitler: `RegionSiteOffset=100_000`, `SettlementSiteOffset=200_000`, `GeneratedNpcActorOffset=10_000`
(`DomainSimulationAdapter.cs:56-58`); koordinat birleşmesi `x=TileX*40000+20000, y=TileY*40000+20000`
(`Hydration.cs:62-63`); site yarıçapı Capital 28 / City 24 / Town 18 / diğer 14 (`:72`);
`FallbackSeed=42` (seed==0 için, `OverlandWorldgen.cs:15,22,34`); `MinimumDungeons=3`
(`Settlements.cs:46`); `GeographyWidth=128, GeographyHeight=64` (`PlanetToWorldMapper.cs:17-18`);
`GameSubdivisionLevel=5` ≈ 10.242 tile (`PlanetWorldService.cs:15-17`); larder = 150 buğday
(`Hydration.cs:78-79`); travel senkron tavanı 14 gün (`Travel.cs:111`).

## LLD - Fonksiyon Haritasi

| İmza | Konum | Ne yapar |
|---|---|---|
| `DomainSimulationAdapter.SeedWorld(mood, calling, start, worldSeed?)` | `DomainSimulationAdapter.Worldgen.cs:26` | Ana orkestratör: seed katla → gezegen üret/cache'ten al → seçimler → hidrasyon → overland → "Domain Seeded" logu → `ConfigureMainQuest`. |
| `FoldSeed(mood, calling, start) : uint` | `Worldgen.Selection.cs:99` | FNV-1a-32, birim ayraçlı; 0 çıkarsa 2463534242'ye iter (XorShift zero-seed reroute'unu korur). |
| `PlanetWorldService.GetOrGenerate(seed, parameters, observer?)` | `PlanetWorldService.cs:19` | Seed başına tek gezegen+dünya; observer'lı çağrı reveal için streaming üretir; `PlanetWorldContext.Set` cache. |
| `PlanetGenerator.Generate(seed, planetParameters)` | `Planet/PlanetGenerator.cs` (imza `PlanetWorldService.cs:47`) | Aşamalı deterministik gezegen fiziği (plaka→iklim→hidroloji→erozyon→kaynak→yerleşim; aşama sınıfları `Planet/*Stage.cs`). |
| `PlanetToWorldMapper.Map(field, parameters) : GeneratedWorld` | `PlanetToWorldMapper.cs:20` | Küre → GeneratedWorld köprüsü; bölge kümeleme + 128x64 projeksiyon + tarih simülasyonu + NPC tohumlama; `world.PlanetData = field`. |
| `PlanetWorldRecordMapper.MapSettlements(field, regionMap, projection)` | `PlanetToWorldMapper.cs:550` | `SettlementStage` çıktısını `SettlementRecord`'a çevirir; tile = `projection.CellForTile` land-cell garantili. |
| `PlanetNpcSeeder.Seed(seed, parameters, settlements, factions)` | `PlanetToWorldMapper.cs:1029` | Nüfus-ağırlıklı NPC dağıtımı; heceli adlar, deterministik faction ataması, boyuta göre rol ruleti (`:1180-1211`). |
| `WorldgenService.Generate(seed, parameters) : GeneratedWorld` | `WorldgenService.cs:56` | LEGACY düz üretici: tek XorShift, sıkı çağrı sırası (bölge→yerleşim→faction→ilişki→tarih→NPC). Canlı oyun yolu değil; testler + `OverlandWorldgen.Generate(seed, ...)` overload'u kullanır. |
| `ProjectHistoryState(historyResult)` | `WorldgenService.HistoryProjection.cs:93-100` civarı | Tarih sonu durumundan nihai yerleşim listesi: nüfus≤0 düşer, `TileX/TileY` + tier damgalanır. |
| `OverlandWorldgen.Generate(GeneratedWorld, OverlandParameters) : OverlandMap` | `OverlandWorldgen.cs:27` | Coğrafya raster'ı + yerleşim projeksiyonu + tile tohumları → `OverlandMap`; sidecar register (`:44-45`). |
| `OverlandWorldgen.Generate(uint seed, OverlandParameters)` | `OverlandWorldgen.cs:17` | İkinci overload: içeride `WorldgenService.Generate(seed, Default)` koşturur — DÜZ üretici, gezegen yolu DEĞİL. |
| `ProjectSettlements(settlements, geography)` | `OverlandWorldgen.Settlements.cs:14` | Tarih tile'ına yerleştirme + üç invariant throw'u + kind sınıflama + `EnsureMinimumDungeons`. |
| `HydrateSites / HydrateFactions / HydrateNpcs / HydrateHistory` | `Hydration.cs:35/86`, `Worldgen.Npcs.cs:25`, `Worldgen.Player.cs:25` | GeneratedWorld → WorldState hidrasyon dörtlüsü (akışa bak). |
| `RegionSiteId(RegionId)` / `SettlementSiteId(SettlementId)` | `Worldgen.Player.cs:65-66` | Id uzayı köprüsü: 100k / 200k offset'li `SiteId`. |
| `HomeCellFor(siteId, npcId)` / `DayAnchorFor(...)` | `Worldgen.Npcs.cs:72/86` | Site bound'ları içinde iki farklı Knuth-hash dağılımı — kalabalık tek tile'a yığılmaz. |
| `TryBeginTravelToSettlement(name, out days, out msg)` | `Travel.cs:31` | Chebyshev tile mesafesi = gün; oyuncu aktörü hedef site merkezine `MoveTo` (`:56-57`) — tek konum-doğrusu. |
| `NearestSettlementToPlayer()` | `Travel.cs:117` | Load sonrası `_currentSettlement` boşken oyuncu pozisyonundan türetme — birleşik koordinat uzayının okuyucusu. |
| `ResolvePlayerOverlandTile()` | `DomainSimulationAdapter.Overland.cs:22` | M-haritası/HUD için oyuncunun tile'ı: mevcut yerleşim → merkeze en yakın → merkez. |
| `PlanetAtlas.TryRender(map, w, h, out image)` | `OverlandMapPlanetStore.cs:48` | Icosphere-kaynaklı atlas render + boyut-başına cache + dikey flip; sidecar yoksa false. |
| `PlanetAtlas.TryGetTileAnchorPercent(map, x, y, ...)` | `OverlandMapPlanetStore.cs:115` | Pin'i, cell merkezinin eşlendiği icosphere TILE'ının lat/lon'una çipalar — pin denize düşmez. |
| `WorldGenesisMapper.ToStyle/ToGenre/ToPreferredSettlementSize` | `WorldGenesisMapper.cs` (çağrı `Selection.cs:84-97`) | Sihirbaz metni → stil/tür/boyut. İç eşleme detayı bu dokümanda dogrulanmadi. |

## LLD - Yazdigi/Okudugu Alanlar

Bu sistem tick sistemi DEĞİL, seed-anında tek-atımlık yazıcıdır; `FieldOwnershipRegistry` ledger'ında
hiçbir satırı yok (ledger yalnız `World.GuardPursuits/Stockpiles/Rumors/SiteUnrest` deklare ediyor —
`FieldOwnershipRegistry.cs:38,43,51,52`). Yazdıkları (hepsi `SeedWorld` çatısı altında):

- `World.RoomSeed` (`Worldgen.cs:35`), `World.WorldProfile` (`:54-64`), `World.Overland` (`:75-77`)
- `World.Sites` (bölge+yerleşim `SiteRecord` add, `Hydration.cs:45,73`), `World.Stockpiles`
  (yerleşim larder'ları, `:78-80`; üretim tohumları `Worldgen.Production.cs:32+`)
- `World.Factions` (KOMPLE yeni `FactionStore`, `Hydration.cs:88` — var olanı değiştirmez, değiştirir)
- `World.Actors` (NPC aktörleri add + oyuncu `MoveTo`, `Worldgen.Npcs.cs:64`,
  `Worldgen.Player.cs:41-46`), `World.NpcSeeds` (`Worldgen.Npcs.cs:28`)
- `World.Events` (tarih olayları append, `Worldgen.Player.cs:32-37`)
- Adapter durumu: `GeneratedWorld` (tek atama `Worldgen.cs:48`), `StartingRegion/Settlement/Faction`
  (`Worldgen.State.cs:28-33`), `_currentSettlement` (travel, `Travel.cs:59`),
  `_billboardOriginResolved` reset (`Worldgen.cs:52`)
- Statik sidecar'lar: `PlanetWorldContext.Instance` (`PlanetWorldContext.cs:29-34`),
  `OverlandMapGeographyStore`/`OverlandMapPlanetStore` (`OverlandWorldgen.cs:44-45`)

Okudukları: `EmberWorldGenIntent.Pending` (tetik), sihirbaz string'leri, `generated.*` bundle'ı.
Tüketicileri (okuyucular): fast travel + HUD + M-haritası (`Travel.cs`, `Overland.cs`,
`EmberWorldHost.Input.cs:58-60`), dünya kotaları/karşılaşmalar `World.NpcSeeds`'ten lazy aktör
üretir (`DomainSimulationAdapter.WorldEncounter.cs:185,239,520`), diyalog binding NpcId'yi
`ActorId − 10000` ile geri çözer (`Dialog.Binding.cs:60-65`), quest üretici
`NpcSeeds + Overland.Settlements` okur (`WorldQuests.cs:51-54`), `WorldSceneDirector.Realize`
başlangıç yerleşimini sahneye kurar (`EmberWorldHost.cs:106-107`).

## LLD - Urettigi/Tukettigi Olaylar

**Üretilen:**
- `WorldEventLog`'a tarih tohum olayları — kind eşlemesi: `FactionWar/FactionAlliance →
  FactionReputationChanged`, `TradeRouteOpened → TradeCompleted`, `Calamity → ShortageDetected`,
  diğer her şey → `StorytellerCheckpoint` (`Worldgen.NpcStats.cs:57-71`); hepsi
  `fallbackSite` = başlangıç yerleşimi + boş actor ile (`Worldgen.Player.cs:32-37`).
- Log tag'leri: `"Domain Seeded: seed=... regions=... settlements=... npcs=... pop=...
  history=... startingRegion/Settlement/Faction=..."` (`Worldgen.cs:79-83`).
- Yerleşim üretim invariant ihlallerinde exception (log değil, çökme):
  "has no authoritative geography tile" / "out-of-bounds geography tile" / "not on a land tile"
  (`OverlandWorldgen.Settlements.cs:26-30`) ve parametre-boyut uyuşmazlığı
  (`OverlandWorldgen.cs:53-55`).

**Tüketilen:** WorldEventKind tüketmez — üretim zinciri olay-öncesi çalışır. Reveal akışının
`WorldgenEventProjector` görünür-olay projeksiyonu ayrı bir presentation katmanıdır
(`Worldgen/WorldgenEventProjector.cs`; test `WorldgenServiceTests.cs:282-306` pinliyor — iç
detayı bu dokümanda dogrulanmadi).

## Testler

- `Assets/Tests/EditMode/Worldgen/WorldgenServiceTests.cs` — aynı seed aynı dünya (`:29`), farklı
  seed (`:60`), nüfus tarihe uyar (`:72`), terk edilen yerleşimler düşer + figürler (`:84`), NPC
  rosteri hayatta kalanlarda + rol çeşitliliği (`:112`), ad benzersizliği (`:145,:160`), tarih
  determinizmi (`:179`), yerleşim→gerçek bölge (`:200`), OTORITER LAND TILE invariantı (`:216`),
  seed42 dump (`:242`), event projector (`:282,:306`).
- `Assets/Tests/EditMode/Worldgen/WorldStyleMatrixTests.cs` — Daggerfall ölçeği (`:11`), stil/tür
  profillerinin sapmaları (`:27,:43,:54,:79`), her (stil,tür) çifti deterministik (`:90`).
- `Assets/Tests/EditMode/Worldgen/WorldHistorySimulatorTests.cs` — tarih hash determinizmi
  (`:15,:26`), yüzlerce nedensel olay (`:36`), kurulan yerleşim→geçerli bölge (`:48`), ilişki
  matrisi sınırı (`:72`), sistem-başına izole determinizm (`:90`).
- `Assets/Tests/EditMode/Worldgen/Planet/PlanetGeneratorTests.cs` — icosphere komşuluk (`:28`),
  digest determinizmi (`:52`), aşama sırası (`:66`), plaka kapsaması (`:105`), kara/okyanus oranı
  (`:123`), biome kapsaması (`:133`), yağmur gölgesi (`:149`), nehirler denize iner (`:159`),
  erozyon (`:178`), tektonik dağlar (`:192`), kaynak yoğunlaşması (`:257`), yerleşim aşaması
  (`:306`), seed42 okunabilirlik örnekleri (`:204,:228,:353`).
- `Assets/Tests/EditMode/Worldgen/PlanetIntegration/PlanetToWorldMapperTests.cs` — stabil digest
  (`:35`), yerleşim→geçerli projekte bölge (`:51`), kara oranı gezegeni izler (`:71`), tarih
  mevcut simülatörden (`:83`), seed42 sayımları (`:92`).
- `Assets/Tests/EditMode/Overland/OverlandWorldgenTests.cs` — harita/biome determinizmi
  (`:20,:30,:40`), seed42 golden master (`:51`), kara oranı (`:64`), biome kapsaması (`:78`), dağ
  sıraları (`:96`), tek-tile ada düzeltmesi (`:121`), HER yerleşim tarih tile'ına projekte
  (`:138`), verilen dünyanın id'leri korunur (`:166`), mesafe yardımcıları (`:187`).
- `Assets/Tests/EditMode/Overland/OverlandScaleTests.cs` — 16x16 varsayılan (`:17`), 40 km kenar
  (`:27`), ≥2x Daggerfall (`:36`), ölçekleme (`:47`).
- `Assets/Tests/EditMode/Overland/OverlandMapImageSamplerTests.cs` — örnekleyici determinizmi +
  tek-grid projeksiyon sözleşmesi (`:11-:96`); `PlanetPinAlignmentTests.cs` — her yerleşim pini
  render edilen atlasın KARA pikseline düşer (`:20`).
- `Assets/Tests/EditMode/Worldgen/NpcSeedSaveRoundTripTests.cs` (`:13`) ve
  `WorldProfileSaveRoundTripTests.cs` (`:12`) — save round-trip; `WorldGenesisMapperTests.cs` —
  mood/calling id eşlemeleri (`:13-:27`).
- `Assets/Tests/PlayMode/Worldgen/WorldgenViewVisibleTest.cs` — reveal görünürlüğü;
  `Assets/Editor/Ember/Forge/WorldgenSmokeTest.cs` — editor smoke (iç kapsamı dogrulanmadi).
- KOORDİNAT BİRLEŞMESİNİN KENDİSİ (tile*40000+20000 site yerleşimi, `Hydration.cs:62-63`) için
  doğrudan test bulunamadı — Assets/Tests grep'inde `40000` eşleşmesi yok; davranış yalnız
  hidrasyon üstünden dolaylı pinli (dogrulanmadi: adlandırılmış bir kapsam testi olabilir ama
  grep'te görülmedi).

## Bilinen Borclar + Kacak Kapilari

1. **Soğuk load'da overland YOK — "regenerable" notu yalan söylüyor.** `WorldState.cs:42-43`
   "deterministik, load'da yeniden üretilebilir" der; ama save `Overland`'ı yazmaz ve
   `RestoreStateJson` yalnız CANLI oturumun haritasını korur (`Save.cs:42-49`). Taze process +
   Load: `_world.Overland == null` kalır → travel "There is no overland world to travel"
   (`Travel.cs:36-38`), M-haritası boş. `WorldProfile.Seed` save'te DURUYOR
   (`WorldSaveMapper.cs:86,192`) — yeniden üretim mümkün ama hiçbir kod yolu yapmıyor.
2. **`GeneratedWorld` adapter özelliği load'da restore edilmiyor.** Tek atama `SeedWorld`'de
   (`Worldgen.cs:48`; repo grep'inde başka atama yok). Soğuk load sonrası
   `ResolveStartingSettlementName`'in GeneratedWorld fallback'i (`Overland.cs:50-56`) ve HUD tile
   satırı (`Hud.cs:28-32`) sessizce devre dışı kalır. NpcSeeds save'den gelir ama kardeş
   bundle'ları gelmez — yarım restorasyon.
3. **`PlanetWorldContext` tek-seed'lik statik singleton.** Seed farklıysa cache ıskalar ve yeniden
   üretir — doğru; ama `Clear()` çağıran bir akış grep'te görülmedi (dogrulanmadi), eski
   `PlanetField` process ömrü boyunca bellekte kalır (~10K tile alan yapıları).
4. **İki `Generate` overload'u iki FARKLI üretici demek.** `Generate(uint, params)` içeride DÜZ
   `WorldgenService.Generate(seed, Default)` koşturur (`OverlandWorldgen.cs:17-25`) — gezegen
   yolundaki canlı dünyayla AYNI seed'den FARKLI harita üretir. Boyut uyuşmazlığı throw'u
   (`:49-57`) yanlış kullanım için tek emniyettir; karışıklık daha önce gerçek bug çıkardı
   ("reveal/map settlement-count mismatch", `Worldgen.cs:67-70` yorumu).
5. **Legacy kompakt yerleşim hâlâ kodda.** Tile'sız `SettlementRecord` için (i%32)*12 grid'i
   (`Hydration.cs:67-68`) — bugünkü iki üretici de tile damgaladığından ölü yol olması gerekir,
   ama save'den gelen eski kayıtlar için canlı bir kaçak kapısı: o dünyalarda tüm kasabalar
   birbirinin üstünde.
6. **Bölge siteleri birleşik koordinat uzayında DEĞİL.** Yerleşimler tile*40000'e taşındı ama
   bölge siteleri sentetik (i%10)*96 grid'inde kaldı (`Hydration.cs:43-45`) — bölge sitesi
   bound'ları gerçek coğrafyayla ilgisiz; `Contains` tabanlı herhangi bir bölge sorgusu fiktif
   sonuç verir.
7. **Reveal ikinci bir `OverlandMap` instance'ı üretir.** `Transitions.cs:311-313` cache'li
   GeneratedWorld'den kendi haritasını kurar; `PlanetAtlas` cache'i map-instance-anahtarlı
   (`OverlandMapPlanetStore.cs:45-46`) olduğundan reveal ve oyun-içi harita ayrı ayrı render
   edilip cache'lenir — içerik deterministik-özdeş, iş ve bellek çift.
8. **Tarih olayları tek siteye ve boş aktöre çivili.** `HydrateHistory` HER tarih olayını
   `fallbackSite`'a (başlangıç yerleşimi) yazar (`Worldgen.Player.cs:28-37`) — 1200 yıllık
   kroniğin tamamı, olayın gerçek bölgesi/yerleşimi ne olursa olsun oyuncunun doğduğu kasabada
   "olmuş" görünür. Dedikodu/diyalog katmanı site-local öncelik kullandığından bu çarpıklık
   oyuncuya sızar.
9. **`EnsureMinimumDungeons` SON küçük yerleşimleri sessizce delve'e çevirir.**
   (`Settlements.cs:48-65`) Shrine/Inn/Hamlet demote edilebilir (City/Town asla); üretilen ad ve
   nüfus kaydı yerleşim gibi kalır — NPC'ler zindana "evli" olabilir (NpcSeeds Home hâlâ o
   SettlementId; kasıt/etkisi dogrulanmadi).
10. **Save restore geçersiz NpcSeed satırlarını SESSİZCE düşürür** (`Narrative.cs:224-229`)
    — id/home/faction 0 veya role None ise satır yok olur; log yok. `GeneratedNpcActorOffset`
    aritmetiği (ActorId−10000 → NpcId) düşen satır için sahipsiz aktör bırakabilir (senaryo
    dogrulanmadi).
11. **`FallbackSeed=42` ve `FoldSeed` 0-itmesi** iki ayrı sıfır-seed kaçağı
    (`OverlandWorldgen.cs:15`, `Selection.cs:110-113`) — determinist ama "seed 0" isteyen bir
    kullanıcı sessizce 42/2463534242 dünyası alır.
12. **Senkron travel 14 gün tavanlı, asenkron değil** (`Travel.cs:104-114` vs `:66-70`) — proof
    driver'lar ile oyun UI'ı farklı dünya-yaşı biriktirir; kıta-ötesi yolculukta senkron yol
    varışa kadar olan günlerin bir kısmını hiç simüle etmez.
13. **Sınıf (a)-(g) hata-ailesi eşlemesi repo içinde bulunamadı** (07 numaralı dokümandaki notla
    aynı durum) — bu borçların aile harfleri atanamadı, dogrulanmadi.

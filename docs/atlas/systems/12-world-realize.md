# 12-world-realize

> Kapsam: WorldSceneDirector.Realize, deterministik yerleşim planı (SettlementLayoutStrategy ailesi),
> RuntimeBuildingBuilder (katlar/kanatlar/iç bölmeler/kapılar/pencereler), RuntimeMineBuilder,
> RuntimeFieldBuilder/SimFieldView, RuntimeWeatherController, RuntimeLightingRig, SkyController, plaza.
> Kanıt biçimi: `dosya:satır`. Aksi belirtilmedikçe yollar `Assets/Scripts/` (builder'lar
> `Presentation/Ember/WorldDirector/`, plan katmanı `Simulation/WorldDirector/`) köklüdür.
> Teren streaming (TerrainStreamer/RuntimeTerrainBuilder) ve zindan üretimi (RuntimeDungeonBuilder)
> ayrı sistem dosyalarının konusudur; burada yalnız Realize'ın onları tetiklediği dikişler anlatılır.

## HLD - Ne ve Neden

Dünya-gerçekleştirme sistemi, "New Game dünyayı ÜRETİR, sen içinde durursun" vaadinin motorudur:
oyuncunun overland haritada durduğu tile, sahne yüklendiği anda editör bake'i OLMADAN, doğrudan
dünya verisinden canlı Unity geometrisine çevrilir (`WorldSceneDirector.cs:9-18`). Aynı seed aynı
kasabayı bayt-aynı yeniden üretir; bunun için plan katmanı (Simulation.WorldDirector) Unity'siz saf
fonksiyonlardır (`ISettlementLayoutStrategy.cs:129-137`) ve sunum katmanı bu planı primitive
küplerden (AssetDatabase'siz) inşa eder (`RuntimeBuildingBuilder.cs:6-10`). Oyuncuya görünen etki:
plaza + masa/kuyu/sancak, sokaklar boyunca kapılı-pencereli-bacalı evler, ilk üç binada işlevli
tavern/temple/shop, kasaba kenarında maden ağzı ve sim bitkilerinin hücre-hücre büyüdüğü tarlalar,
üstünde sim saatine bağlı gökyüzü + güne-deterministik hava. Felsefe Daggerfall Unity'den:
"deterministik olanı yeniden üret, yalnız deltayı sakla" — layout bir SAKLANMAZ, her sahne
yüklemede seed'den yeniden planlanır (`VillageLayoutStrategy.cs:7-11`). Director geometri sahibi
değildir; NPC'ler SONRADAN, bu adımın yarattığı "PlayerRig" çapasına host spawner tarafından
eklenir (`WorldSceneDirector.cs:13-14`). Canlı-dünya bağı statik ayna kanallarıyla kurulur:
adapter her tick RuntimeFieldMirror/RuntimeCaravanMirror'a yazar, sahnedeki view'lar poll eder
(`RuntimeFieldBuilder.cs:53-57`; `DomainSimulationAdapter.Clock.cs:116-147`).

## HLD - Akis

1. **Tetik (tek atış, sahne yükünde):** `EmberWorldHost` boot'ta SeedWorld + `AdvanceTick(0)`
   sonrası, YALNIZ aktif sahne `EmberScenes.GeneratedWorld` ise `WorldSceneDirector.Realize(_worldView)`
   çağırır — baked dilim sahneleri dokunulmaz kalır (`Bootstrap/EmberWorldHost.cs:106-107`).
   Fast travel bu sahneyi yeniden yükler → Realize yeniden koşar.
2. **Korkuluklar:** read model null → uyarı + çık (`WorldSceneDirector.cs:23-27`); overland null →
   "BROKEN" failsafe: Perlin fallback pad + ışık + rig, oyuncu karanlık boşlukta kalmaz
   (`WorldSceneDirector.cs:32-43`).
3. **Girdi çözümleme:** home tile (`ResolveHomeTile`, `WorldSceneDirector.cs:305-309` — tile yoksa
   harita ortası), settlement kind (`ResolveKind`, `:311-321` — eşleşme yoksa Village), seed =
   `homeTile.PropVariationSeed` (0 ise 1; `:48`). `EmberLog.Sink` yoksa `Debug.Log` bağlanır (`:51-52`).
4. **Nüfus kimliği:** kind → NPC billboard tavanı `RuntimeNpcDensity.Cap`'e yazılır (City 24 …
   Shrine 4, Dungeon 14; `:58-59, 290-303`) — spawner director'dan SONRA yaratıldığı için statik kanal.
5. **Plan:** `SettlementLayoutStrategyFactory.For(kind).Plan(new SettlementContext(name, kind, biome, seed))`
   (`:61-62`). City/Town → `StreetLayoutStrategy` (radyal caddeler), Village → ring,
   Hamlet/Inn/Shrine/Dungeon → `CompactLayoutStrategy` (1-3 bina) (`SettlementLayoutStrategyFactory.cs:107-124`).
6. **Zemin:** `WorldGeoSampler.TryCreate` harita geo anlık görüntüsüne bağlanır (başarısızsa legacy
   Perlin — log "PARTIAL"; `:71-74`), `TerrainStreamer.Initialize(seed, biome, geoSampler)` ile
   Daggerfall-tarzı akan arazi kurulur (`:76-78`). (Detay: teren sistemi dosyası.)
7. **Binalar:** her `BuildingPlacement` için `RuntimeBuildingBuilder.Build`; ilk ÜÇ kabuk işlevsel
   rol alır (tavern/temple/shop + parlayan tabela küpü `AttachFunctionalRole`, `:82-97, 132-154`);
   üçü de varsa `RuntimeInteriorInfo.Record` + adapter'a `PinHostInsideTavern` (`:90-96`).
8. **Maden:** `PlanetAtlas.TryGetTileOre` iron>0.5 veya coal>0.5 derse kasaba kenarına
   (`GroundRadius+14f`, açı = seed%360) maden ağzı (`:103-110`; `RuntimeMineBuilder.cs:13-33`).
9. **Tarlalar:** Dungeon/Shrine dışında `SimFieldView` bileşeni eklenir — REFORM #1: kutup-dekor
   kuşağı emekli, ekinler sim bitkilerinin projeksiyon hücrelerinde, bitki-başına sahne
   (`:115-128`; `RuntimeFieldBuilder.cs:190-251`).
10. **Dungeon dalı:** kind==Dungeon ise delve ölçekli mağara ağzı (`RuntimeMineBuilder.Build(root, 9f, …)`)
    + `RuntimeDungeonBuilder.Build` (5-10 odalı graf) + adapter'dan dweller'lar (`:159-177`; detay:
    zindan dosyası).
11. **Plaza kalbi:** ticaret arabası (`RuntimeCaravanBuilder.Build`, görünürlük kervan aynasına bağlı;
    `:181-182`; `RuntimeCaravanView.cs:136-158`), bölge sancağı (RegionId → deterministik HSV;
    `:187, 263-287`), taş plaza diski (`:191-198`), masa+sıralar+kuyu prop seti (`:204-227`).
12. **Işık + gök + hava:** `RuntimeLightingRig.Apply` flat ambient (%35 biome tonu) + yönlü güneş
    kurar, güneşe `SkyController.Bind` ve `RuntimeWeatherController.Bind(biome)` takar
    (`:229`; `RuntimeLightingRig.cs:13-39`).
13. **Spawn + rig:** spawn = layout plaza merkezi; Dungeon'da `RuntimeDungeonLayoutInfo.StartRoomWorld+0.4y`
    (tavan-spawn canlı hatası düzeltmesi; `:231-241`), `RuntimePlayerSpawn.Record` (F15 ölüm-ekranı
    dönüşü) + `RuntimePlayerRig.Build` (`:242-243`; `RuntimePlayerRig.cs:17-57` — idempotent).
14. **Ses/müzik/yüzme:** `RuntimeAudioDirector.Attach`, `RuntimeMusicDirector.Attach`,
    `RuntimeWaterIndex.Clear()` + `SwimView.Attach` (`:246-254`).
15. **Hazırlık raporu:** playtest loguna tek özet satır (kind, bina sayısı, geo REAL/LEGACY,
    localShore, npcCap, rig konumu; `:258-260`).
16. **Kadans (Realize sonrası, sürekli):** eklenen MonoBehaviour'lar frame kadansında poll eder ve
    sim gerçeğini AYNALARDAN okur: `SkyController.Update` her frame `RuntimeFieldMirror.MinutesOfDay`
    (`SkyController.cs:54-88, 200-207`); `RuntimeWeatherController.Update` gün değişince yeni hava
    uygular (`RuntimeWeatherController.cs:86-97`); `CropStalkView` 2 sn'de bir, `SimFieldView` 1.5 sn'de
    bir stamp kontrolü (`RuntimeFieldBuilder.cs:114-137, 204-227`); `CaravanCartView` 2 sn
    (`RuntimeCaravanView.cs:124-133`); `RuntimeDoorView` 4 Hz mesafe (`RuntimeDoorView.cs:172-201`);
    tavern/temple/shop tetikleri 0.4 sn + E tuşu (`RuntimeFunctionalInteriors.cs:81-100`).
    Aynaları dolduran tek yazar adapter'ın tick'idir (`DomainSimulationAdapter.Clock.cs:116-147`).

## LLD - Veri Modeli

**Plan katmanı (Unity'siz, deterministik):**
- `SettlementContext` (readonly struct): `Name` (yalnız log), `Kind`, `Biome`, `Seed`
  (= PropVariationSeed) — `Simulation/WorldDirector/SettlementContext.cs:37-55`.
- `BuildingPlacement` (readonly struct): `OriginX/OriginZ` (metre, merkez), `SizeX/SizeZ`,
  `Height`, `MaterialIndex` (soyut palet slotu; UnityEngine.Color plan katmanına girmez) —
  `BuildingPlacement.cs:9-27`.
- `SettlementLayout`: `Buildings` (IReadOnlyList), `GroundRadius` (yarı-uzam), `PlayerSpawnX/Z`,
  `PlayerFacingDeg` — `SettlementLayout.cs:66-90`.
- `GeoSample` (readonly struct): `ElevationMeters`, `IsWater`, `SandBlend01`, `WaterSurfaceMeters` —
  `WorldGeoSampler.cs:8-24`.

**Sunum katmanı — statik ayna/kanal tipleri (tek-yazar sözleşmeli):**
- `RuntimeFieldMirror`: `HourOfDay`, `MinutesOfDay` (varsayılan 08:00 — tick öncesi kare sabah
  görünsün), `WorldDay`, `PlantCount`, `StageIndex` (0-2), `PlantCell{Id,LocalX,LocalZ,Stage}`
  dizisi + `PlantsStamp` — `RuntimeFieldBuilder.cs:58-92`. Yazar: adapter tick
  (`DomainSimulationAdapter.Clock.cs:116-132`).
- `RuntimeWeatherMirror`: `Kind` ("clear|rain|snow|fog"), `Raining`, `FogFactor` (0-1; URP fog
  varyantları player build'den strip olduğu için okunabilir sis gök rengi+güneşte yaşar) —
  `RuntimeWeatherController.cs:47-56`.
- `RuntimeCaravanMirror`: `AtSiteCount` — `RuntimeCaravanView.cs:111-116`; yazar adapter
  (`DomainSimulationAdapter.Clock.cs:147`).
- `RuntimeNpcDensity`: `Cap` (0 = ayarsız → spawner kendi varsayılanı) — `RuntimeNpcDensity.cs:94-99`.
- `RuntimePlayerSpawn`: `Position` — `RuntimePlayerSpawn.cs:80-85`.
- `RuntimeInteriorInfo`: `TavernWorld/TempleWorld/ShopWorld`; `ScreenRequestSignal` (tek-bayrak
  tek-tüketici, `Request/Consume`) — `RuntimeFunctionalInteriors.cs:8-29`.
- `RuntimeDungeonLayoutInfo`: `RoomCount`, `EntryWorld`, `StartRoomWorld`, `BossRoomWorld`,
  `ChestWorld`, `DwellerSpots`, `BossSpot`, `ArchetypeName` vd. — `RuntimeDungeonLayoutInfo.cs:9-26`
  (yazar zindan builder'ı; Realize spawn/dweller için okur).
- `RuntimeBuildingBuilder.DoorSide` (private enum): North/South/East/West — `RuntimeBuildingBuilder.cs:414-420`.
- Sabitler: `WallThickness=0.25`, `DoorWidth=1.6`, `DoorHeight=2.2` — `RuntimeBuildingBuilder.cs:13-15`.

## LLD - Fonksiyon Haritasi

**Direktör:**
- `WorldSceneDirector.Realize(IWorldViewReadModel view)` — `WorldSceneDirector.cs:21`; tüm akışın
  sahibi (yukarıdaki 2-15).
- `ResolveHomeTile(OverlandMap, GridPosition): RegionTile` — `:305`; tile yoksa harita merkezi.
- `ResolveKind(OverlandMap, GridPosition): SettlementKind` — `:311`; settlement listesinde lineer arama.
- `NpcCapFor(SettlementKind): int` — `:290`; kind → billboard tavanı.
- `BuildRegionBanner(Transform, ulong regionValue)` — `:263`; hue = (region*47)%360, direk+bayrak.
- `AttachFunctionalRole(GameObject, int)` (yerel statik fn) — `:132-154`; tabela küpü + point light +
  `RuntimeTavernView`/`RuntimeTempleView`/`RuntimeShopCounterView` ekler.

**Plan stratejileri (Simulation/WorldDirector/):**
- `SettlementLayoutStrategyFactory.For(SettlementKind): ISettlementLayoutStrategy` —
  `SettlementLayoutStrategyFactory.cs:107`; kind → strateji (OCP).
- `VillageLayoutStrategy.Plan(in SettlementContext): SettlementLayout` — `VillageLayoutStrategy.cs:32`;
  XorShiftRng(seed) ile halka yerleşimi: kind ölçeği (City 26-40 bina/16m/+2.5m boy; `:43-49`),
  dominant malzeme kimliği (%60; `:50, 79`), plaza-bloke ve örtüşme redleri (`BlocksPlaza :96`,
  `Overlaps :114` — sokak payı 4.5m), en çok 8 halka (`:19, 62`).
- `StreetLayoutStrategy.Plan` — `StreetLayoutStrategy.cs:145`; radyal 3-5 cadde, cadde başına iki
  yanlı 3-6 parsel, 3.5m temiz şerit (`:140-186`); v2 (cross-street, parsel/kapı graf düğümleri)
  açıkça "queued" (`:136`).
- `CompactLayoutStrategy.Plan` — `CompactLayoutStrategy.cs:217-219`; VillageLayoutStrategy(1..3, r=5)
  kompozisyonu.
- `WorldGeoSampler.TryCreate(OverlandMap, GridPosition, uint, out sampler): bool` —
  `WorldGeoSampler.cs:66-73`; geo snapshot yoksa false (legacy Perlin yolu).

**Bina builder'ı:**
- `RuntimeBuildingBuilder.Build(Transform, BuildingPlacement): GameObject` — `RuntimeBuildingBuilder.cs:17`;
  kabuk + çatı + varyant + iç bölme + mobilya + kapı + pencereler.
- `ChooseEntranceSide(placement): DoorSide` — `:348`; kapı DAİMA yerleşim merkezine bakar (cadde kuralı).
- `AddWallX/AddWallZ(…, bool withDoor)` — `:355, 380`; kapılı duvar iki segment + LİNTEL
  ("kapının üst tarafı açık" playtest düzeltmesi; `:373-377, 398-400`).
- Çatı + saçak basamağı (`Roof`/`RoofRidge`) — `:48-57`; baca: pozisyon-hash'li ~3/4 olasılık —
  `:58-69`.
- **Silüet varyantları** (`varRoll`; `:70-133`): <0.30 **ikinci kat** (geri çekilmiş üst kutu + kendi
  çatısı; `:76-87`), <0.55 **L-kanat** (giriş duvarını asla kapatmayan yan ek; 0.62→0.78 "kanat
  duvar içine sızdı" otopsi düzeltmesi; `:88-108`), <0.75 **kapı sundurması** (iki direkli ahşap
  saçak; `:109-133`).
- **İç bölme** (`SizeX>6 && SizeZ>6`): P1 WallWithGap portu — iki segman + lintelli GERÇEK kapı
  boşluklu ara duvar, girişe göre %20 derinlikte — `:137-175`.
- `Furnish(root, placement, entrance)` — `:258-313`; NEGATİF float→uint UB'sine karşı kantize-int
  seed (`:260-264`), arka duvarda çakışmasız 3 slot, yatak/masa/sandık şekilleri (`:282-311`).
- `AddDoor` — `:187-217`; menteşe + collider'sız panel; `RuntimeDoorView` −90°/1.5s yaklaşınca açar
  (`RuntimeDoorView.cs:157-203`).
- `AddWindows` — `:219-254`; kapısız HER duvara koyu çerçeve + parlak cam; 0.03→0.16 ofset "3x
  pencereler yok" canlı hatasının düzeltmesi (`:236-240`).
- `AddHearthLight` — `:315-326`; gölgesiz sıcak point light.
- `Hash01(ref uint)` — `:328`; xorshift 0-1.
- `BuildingAccessibilityVolume.Configure/TryPushOutside` — `BuildingAccessibilityVolume.cs:116, 123`;
  NPC billboard'ları ayak-izinden dışarı iter (sim yerleşimini ETKİLEMEZ).

**Çevre builder'ları:**
- `RuntimeMineBuilder.Build(Transform, float distance, float angleDeg, bool coal): GameObject` —
  `RuntimeMineBuilder.cs:13`; höyük+ağız+kiriş+cevher arabası; ağız kasabaya döner (`:19`).
- `RuntimeCaravanBuilder.Build(Transform)` — `RuntimeCaravanView.cs:139`; plaza kenarında araba;
  `CaravanCartView.Update` ayna 0 ise çocukları gizler (`:124-133`).
- `RuntimeLightingRig.Apply(Transform, BiomeKind)` — `RuntimeLightingRig.cs:13`; flat ambient
  (nötr taban + %35 biome; R2 "nane yeşili" düzeltmesi `:16-21`), soft-shadow güneş, SkyController +
  WeatherController takılır (`:36-38`).
- `SkyController.Bind(Light)` / `Update` — `SkyController.cs:38, 54`; sim dakikasından gün-kesri
  (`DayFraction :200-207`; adapter yoksa 2 dakikalık gün), gök rengi = kamera clear-colour
  (skybox shader'ları build'den strip olur; `:14-17, 80-83`), güneş pitch/intensity (`:68-73`),
  gece: 140 yıldızlı altın-açı kubbesi + üretilmiş ay sprite'ı, kamerayı izler (`EnsureCelestials
  :93-142`, `BuildMoonSprite :144-163`, `UpdateCelestials :165-195`).
- `RuntimeWeatherController.Bind(BiomeKind)` / `Update` — `RuntimeWeatherController.cs:84, 86`;
  `Pick(day)` = hash(day,biome) → biome+mevsim ağırlıklı deterministik seçim (`:99-128`);
  `Apply` yağmur/kar/sis/açık: 130 küplük manuel havuz (ParticleSystem player build'de HİÇ
  çizmedi — proof bulgusu; `:77-79, 190-209`), Linear fog + FogFactor + yağmur loop sesi
  (`:130-188`); `Rehome/FollowCamera` kamera-merkezli 30m kolon geri dönüşümü (`:234-268`).
- `RuntimePlayerRig.Build(Vector3, Quaternion, float fov=70)` — `RuntimePlayerRig.cs:17`;
  idempotent (`:19-21`), CharacterController + EyeCamera + isimle 5 kontrolcü (`AddControllerByName
  :60-70` — bulunamazsa yalnız uyarı).

**Ölü ama derlenen builder'lar (çağıran YOK — grep ile doğrulandı):**
- `RuntimeGroundBuilder.Build/BuildBoundary` — `RuntimeGroundBuilder.cs:222, 263`; TerrainStreamer
  gelince emekli oldu, hiçbir çağrı kalmadı.
- `RuntimeFieldBuilder.BuildBelt/Build` — `RuntimeFieldBuilder.cs:148, 159`; REFORM #1 ile kutup
  kuşağı emekli — ancak AYNI dosyadaki `RuntimeFieldMirror`/`CropStalkView`/`SimFieldView` canlıdır.

## LLD - Yazdigi/Okudugu Alanlar

Bu sistem sunum katmanıdır; `FieldOwnershipRegistry`'de kaydı YOKTUR ve sim alanlarına doğrudan
yazmaz. Tek sim-mutasyon dikişi: `hostAdapter?.PinHostInsideTavern(functionalWorld[0])` — adapter
üzerinden hancı aktörün konum pin'i (`WorldSceneDirector.cs:93-95`). Sahiplik dili burada statik
kanalların "tek yazar / tek okur" sözleşmesidir:

**Yazdıkları (yazar bu sistem):**
- `RuntimeNpcDensity.Cap` ← Realize (`WorldSceneDirector.cs:58`); okur: EmberGeneratedActorSpawner.
- `RuntimeInteriorInfo.TavernWorld/TempleWorld/ShopWorld` ← Realize (`:92`); okur: proof çapaları,
  hancı pin'i.
- `RuntimePlayerSpawn.Position` ← Realize (`:242`); okur: F15 ölüm-ekranı dönüşü.
- `RuntimeWeatherMirror.Kind/FogFactor` ← WeatherController (`RuntimeWeatherController.cs:138,
  152-183`); okur: SkyController (`SkyController.cs:66`), müzik.
- `RenderSettings.ambientLight/ambientMode/fog*` ← LightingRig (`RuntimeLightingRig.cs:15-23`),
  SkyController her frame (`SkyController.cs:75`), WeatherController hava değişiminde
  (`RuntimeWeatherController.cs:153-186`) — ÜÇ yazarlı paylaşım, aşağıda borç #6.
- Kamera `clearFlags/backgroundColor` ← SkyController (`SkyController.cs:51, 83`).
- `EmberLog.Sink` ← Realize, yalnız null ise (`WorldSceneDirector.cs:51-52`).
- `RuntimeWaterIndex` ← `Clear()` ile sıfırlama (`:253`; seviyeleri streamer doldurur).

**Okudukları (yazar başka sistem):**
- `IWorldViewReadModel.Overland/PlayerOverlandTile/StartingSettlementName` (`:29, 45-47`).
- `RegionTile.PropVariationSeed/Biome/RegionId` (`:48, 61, 187`).
- `PlanetAtlas.TryGetTileOre` cevher katmanı (`:103-105`).
- `RuntimeFieldMirror.MinutesOfDay/WorldDay/Plants/StageIndex` — yazar adapter tick
  (`DomainSimulationAdapter.Clock.cs:116-132`); okurlar SkyController/WeatherController/stalk'lar.
- `RuntimeCaravanMirror.AtSiteCount` — yazar adapter (`DomainSimulationAdapter.Clock.cs:147`).
- `RuntimeDungeonLayoutInfo.*` — yazar RuntimeDungeonBuilder; Realize spawn + dweller için okur
  (`WorldSceneDirector.cs:172-176, 236-241`).

## LLD - Urettigi/Tukettigi Olaylar

- **WorldEventKind üretmez ve tüketmez** — klasörde `WorldEventKind` referansı yok (grep ile
  doğrulandı); sim gerçeği olaylarla değil, yukarıdaki tick-aynalarıyla gelir.
- **Sinyal:** `ScreenRequestSignal.Request("trade")` — shop tezgâhından UI denetleyicisine
  tek-bayrak istek (`RuntimeFunctionalInteriors.cs:24-29, 75`).
- **Log etiketleri (shipcheck/playtest bunlara pinlenir):** `[WorldDirector]` (realize özeti,
  bina sayısı, maden, banner, dweller; `WorldSceneDirector.cs:25, 35, 54, 59, 72-74, 96-98, 109,
  127, 176, 182, 259-260, 286`), `[Building]` (mobilya; `RuntimeBuildingBuilder.cs:312`),
  `[Weather]` (gün/mevsim/seçim; `RuntimeWeatherController.cs:140`), `[Sky]` (celestial kurulum;
  `SkyController.cs:141`), `[Shop]` (tezgâh; `RuntimeFunctionalInteriors.cs:76`).
- **Kaçak kapı/olay dışı girdiler:** `--ember-weather clear|rain|snow|fog` komut satırı pin'i
  (`RuntimeWeatherController.cs:132-136`) ve `RuntimeWeatherController.ProofForce(kind)` statik
  override'ı (`:67-69`) — proof sürücüleri için.

## Testler

Hepsi `Assets/Tests/EditMode/WorldDirector/` altında; SADECE plan katmanı pinli:
- `SettlementLayoutDeterminismTests.cs` — aynı seed birebir aynı layout (`:15-32`), farklı seed
  farklı (`:36-50`), binalar zemin içinde (`:54-67`), sokak payı korunur (`:71-80`).
- `StreetLayoutStrategyTests.cs` — aynı-seed determinizm (`:14`), City>Town bina sayısı (`:27`),
  plaza temiz + çift örtüşme yok (`:34`).
- `SettlementLayoutStrategyFactoryTests.cs` — kind→strateji eşlemesi (`:12, 21, 30`).
- `WorldGeoSamplerTests.cs` — projeksiyon ölçeği, TryCreate, determinizm, düz-kuru pad, süreklilik,
  su bayrağı (`:27-92`).
- `WorldGeoSamplerShoreTests.cs` — kıyı yerleşimlerinde yürünebilir su + worldgen zinciri
  determinizmi (`:22, 61`).
- **Boşluk:** `WorldSceneDirector.Realize`, `RuntimeBuildingBuilder` ve TÜM Runtime* builder'lar
  için doğrudan test YOK; sahne tarafını pinleyen tek şey proof-screenshot sürücüsünün
  GeneratedWorld akışı (`Diagnostics/EmberProofScreenshotDriver.cs:307`) ve `[WorldDirector]`
  log satırlarına bakan shipcheck'lerdir.

## Bilinen Borclar + Kacak Kapilari

Aile harfleri `docs/SYSTEMS_ATLAS.md:52-60` (a)-(g) sınıflamasına göre.

1. **Negatif-float→uint UB, düzeltilen hatanın kopyaları — sınıf (f)/(g).** `Furnish` bu tuzağı
   açıkça belgeleyip kantize-int'e geçti (`RuntimeBuildingBuilder.cs:260-264`), ama AYNI desen iki
   yerde hâlâ ham float cast'iyle duruyor: baca seed'i `(uint)(placement.OriginX * 8f)`
   (`:60-61`) ve silüet-varyant seed'i `(uint)(placement.OriginX * 4f)` (`:73-74`). Negatif
   koordinatlı binalarda (kasabanın yarısı!) cast 0'a çökebilir → o yarıda baca/varyant zarı tek
   değere yapışır. "Skyline stops repeating" vaadinin yarım kalma riski; davranış platform-bağımlı,
   oyunda ne kadar görünür olduğu **doğrulanmadı**.
2. **"İlk üç bina" tek-nokta tasarımı — sınıf (a)** (SYSTEMS_ATLAS §2'nin kendi tespiti):
   tavern/temple/shop rolleri listede İLK üç kabuğa yapışır (`WorldSceneDirector.cs:82-97`).
   Compact yerleşim 1-3 bina üretir (`CompactLayoutStrategy.cs:217`): 1-2 binalı Inn/Hamlet'te
   roller yine 0/1 indekslerine takılır ama `RuntimeInteriorInfo.Record` + hancı pin'i
   `Count>=3` şartına takılıp ÇALIŞMAZ (`:90-96`) — tabelası yanan ama proof-çapası/hancısı
   olmayan tavern mümkün. **Doğrulanmadı** (canlıda 1-2 binalı seed'e denk gelinmedi), kod yolu net.
3. **Dungeon + zengin cevher = çifte höyük — sınıf (a).** Maden bloğu kind'e bakmaz (`:103-110`);
   Dungeon dalı ayrıca delve ağzı kurar (`:159-166`). Cevheri zengin bir Dungeon tile'ında iki
   ayrı mağara ağzı yan yana doğar. **Doğrulanmadı** (böyle bir tile'ın oluşup oluşmadığı veriye bağlı).
4. **Ölü kod çifti:** `RuntimeGroundBuilder` (tamamı) ve `RuntimeFieldBuilder.BuildBelt/Build`
   sıfır çağıranla derleniyor (grep kanıtı; `RuntimeGroundBuilder.cs:222`,
   `RuntimeFieldBuilder.cs:148-187`). İkincisi canlı tiplerle aynı dosyada saklandığı için
   fark edilmesi zor. Ayrıca `plots` hesabı (DF oranları) artık YALNIZ log satırı üretiyor —
   SimFieldView onu hiç okumuyor (`WorldSceneDirector.cs:117-127`).
5. **`GameObject.Find("PlayerRig")` string bağı — sınıf (g).** Realize üç kez (`:246-254`),
   kapı/iç-mekân tetikleri poll'da (`RuntimeDoorView.cs:179`, `RuntimeFunctionalInteriors.cs:90`)
   isimle arar; rig adı değişirse ses/müzik/yüzme/kapılar sessizce ölür. Aynı şekilde
   `AddControllerByName` tip bulamazsa yalnız uyarı loglar (`RuntimePlayerRig.cs:60-70`).
6. **RenderSettings'in üç yazarı — sınıf (c) sunum kopyası.** Ambient'i LightingRig kurar
   (`RuntimeLightingRig.cs:19-22`), SkyController her frame `_baseAmbient` üzerinden ezer
   (`SkyController.cs:41, 75`), fog'u WeatherController hava değişiminde yazar
   (`RuntimeWeatherController.cs:153-186`). Sıra bugün tutuyor; SkyController `Bind` ÖNCESİ ambient
   değişirse `_baseAmbient` bayatlar. Tek-yazar registrisi yok (bilinçli: presentation).
7. **Statik kanalların yaşam süresi — sınıf (f).** Ayna/kanal tipleri process-static:
   `RuntimeWaterIndex.Clear()` çağrılır (`WorldSceneDirector.cs:253`) ama `RuntimeNpcDensity`,
   `RuntimeInteriorInfo`, `RuntimeDungeonLayoutInfo`, `RuntimeWeatherMirror` travel-reload'da
   ÖNCEKİ lokasyonun değerlerini yeni yazım olana dek taşır. Bugün tüketiciler kind korumalı
   (ör. spawn guard `kind==Dungeon`, `:236`) — yeni tüketici eklerken en bilinen tuzak.
8. **Katlar/kanatlar cephe yalanı (dürüstçe):** ikinci kat ve L-kanat İÇİ OLMAYAN dolu slablar —
   merdiven yok, kanat odaya bağlı değil ("odalar arasında kapı yok" otopsisi kanadı 0.78 ile
   tamamen DIŞARI itti; gerçek çok-oda P1 WallWithGap portuna havale; `RuntimeBuildingBuilder.cs:88-102`).
   İç bölme yalnız `SizeX>6 && SizeZ>6` kabuklarda (`:140`) — köy evlerinin çoğu (3.5-5.7m;
   `VillageLayoutStrategy.cs:76-77`) TEK oda kalır, bölme fiilen büyük Town/City parsellerine özgü.
9. **Sundurma direklerinin collider'ı duruyor — sınıf (d) riski.** `AddSlab` primitive collider'ı
   korur; kapı paneli collider'ı bilinçli silinirken (`:210-211`) sundurma direkleri kapının 1m
   önünde çarpışmalı dikilir (`:128-132`). Kapı genişliği 1.6m, direk arası ~1.9m — geçilebilir
   ama NPC itme hunisi. **Doğrulanmadı** (canlı şikâyet yok).
10. **Kaçak kapılar (bilinçli):** `--ember-weather` CLI pin'i (`RuntimeWeatherController.cs:132-136`),
    `ProofForce` statik override (`:67-69`; null'a geri almayı unutmak havayı kalıcı sabitler),
    adapter'sız gök için 2-dakikalık gün fallback'i (`SkyController.cs:206`), overland'sız BROKEN
    pad (`WorldSceneDirector.cs:32-43`), `EmberLog.Sink` init'inin buraya gömülü olması (`:51-52` —
    logger kurulumunun doğal evi değil).
11. **Açık v2 kuyruğu (kodun kendi beyanı):** StreetLayout cross-street/parsel-graf
    (`StreetLayoutStrategy.cs:136`), asset-gate readiness v2 "loading screen arkasında blokla"
    (`WorldSceneDirector.cs:256-257`), bölge sancağında faction inceltmesi (`:184-186`).

# 06-plants-harvest

> Kapsam: bitki buyume + hasat + sim-gorsel tarla birlesmesi. Ana dosyalar:
> `Assets/Scripts/Simulation/Process/PlantGrowthSystem.cs`, `HarvestSystem.cs`, `PlantingSystem.cs`,
> `HarvestHandsService.cs`, `FarmingJobRequestFactory.cs`,
> `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs` (PlantGrowthStep/HarvestStep),
> `Assets/Scripts/Presentation/Ember/WorldDirector/RuntimeFieldBuilder.cs` (RuntimeFieldMirror + CropStalkView + SimFieldView),
> `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Clock.cs` (PublishFieldMirror).

## HLD - Ne ve Neden

Bitki sistemi, yerlesimin tarlalarindaki ekinleri deterministik gunluk adimlarla buyutur
(seed -> sprout -> ripe), olgunlasan ekini YAKINDAKI BIR KOYLUNUN ELIYLE hasat edip site
stokuna 2 birim urun ekler ve ayni gun tohuma geri diker — boylece buyume -> stok -> fiyat
zinciri her gun kendini besler (shipcheck "FLAT" bulgusunun cozumu, `DefaultTickSystems.cs:419-421`).
Oyuncuya gorunen etkisi: kasaba kenarindaki tarla parselleri sim'in GERCEK PlantComponent'lerinin
projeksiyonunda durur; her sap kendi bitkisinin evresini giyer, tohum aniziden yesil filize,
oradan olgun altina saniyeler icinde yukselir/alcalir — hasat "glitch" degil hasat gibi okunur
(`RuntimeFieldBuilder.cs:81-90`). Felsefe iki reform uzerine kurulu: REFORM #1 "tek mekansal
otorite" — gorsel tarla SIM tarlasidir, dekoratif polar kusak emekliye ayrildi
(`RuntimeFieldBuilder.cs:143-147`, `WorldSceneDirector.cs:122-126`); M6 "ekinler birden yok
olmasin" — hasat fiat ile degil ELLE olur, kimse yoksa parsel olgun bekler
(`HarvestHandsService.cs:7-12`). Sim katmani saf ve Unity'siz; sunum katmani statik mirror
kanalindan okur, geri yazmaz. Onemli gercek: sistemin YASAYAN yolu (HarvestStep) ile ATOMIK
yolu (PlantingSystem/HarvestSystem/FarmingJobRequestFactory) ayri dunyalardir — ikincisi test
altinda tutulan ama tick'e baglanmamis, buyuk olcude uyuyan bir boru hattidir (asagida "Borclar").

## HLD - Akis

Gunluk sim akisi (hepsi `TickCadence.Daily`, WorldTickComposer -> WorldTickRegistry uzerinden;
kayit listesi `DefaultTickSystems.cs:42-64`):

1. **econ.plantgrowth @ Daily:20** (`DefaultTickSystems.cs:470-508`): sezon `SeasonCalendar`'dan
   cozulur (bulunamazsa Spring varsayilir, satir 493-495), katalogdaki her tur icin
   `PlantGrowthSystem.AdvanceOneDay` cagrilir; `isSnowing` SABIT `false` gecilir (satir 505 —
   canli hava durumu sim'e bagli degil).
2. `AdvanceOneDay` (`PlantGrowthSystem.cs:17-79`): tur uyusan her bitkinin `DaysInStage`'i +1;
   esik asilirsa sonraki evreye gecirir ve `PlantStageAdvanced` olayi yazar (satir 58-74).
   Son evre (`DaysToNextStage == 0`) atlanir (satir 42-43).
3. **world.harvest @ Daily:25** (`DefaultTickSystems.cs:422-468`): `StageId == "ripe"` olan
   bitkiler snapshot'lanir (satir 431-435), her biri icin `HarvestHandsService.FindHarvester`
   ile 2 hucre (Chebyshev) icindeki en yakin canli sivil aranir (satir 442); kimse yoksa parsel
   olgun BEKLER (satir 443). Toplayan varsa site stokuna `pile.Add(SpeciesId, 2)` (satir 457),
   toplayicinin ActorId'siyle `PlantHarvested` olayi (satir 460-462) ve AYNI bitki Id'siyle
   "seed" evresine geri dikim (satir 463-465).
4. **econ.shortage_response @ Daily:27** (`DefaultTickSystems.cs:343-348`,
   `ShortageResponseSystem.cs:27-61`): her stokta gida etiketi < 4 ise `ShortageDetected` +
   JobBoard'a `FarmingJobRequestFactory.CreatePlantingJob` (satir 51-53). (Bu is bir ciftciyi
   tarlaya YURUTUR ama bitki DIKMEZ — bkz. Borclar #2.)
5. **econ.prices @ Daily:30** (`DefaultTickSystems.cs:518-526`): hasadin sistirdigi stok fiyati
   ayni gun icinde gunceller — 20 -> 25 -> 30 ayni-gun zinciri bilerek (yorum: satir 424).
6. **Sunum, her tick**: `DomainSimulationAdapter.AdvanceTick` (`Clock.cs:6-13`) composer'i
   ilerlettikten sonra `PublishFieldMirror()` cagirir (`Clock.cs:81-147`): aktif yerlesimin
   bitkileri taranir, evre sayimi + FNV hash cikartilir (satir 88-113); baskin evre
   `RuntimeFieldMirror.Publish` ile (satir 114-117), bitki hucre listesi SADECE hash
   degistiginde `PublishPlants` ile yayinlanir (satir 119-122). Saat/gun kanallari da ayni
   yerden beslenir (satir 125-132).
7. **Gorsel, Unity Update**: `SimFieldView` 1.5 sn'de bir `PlantsStamp`'i yoklar
   (`RuntimeFieldBuilder.cs:157-180`), yeni hucre icin parsel kurar, olen Id'yi budar;
   her `CropStalkView` 2 sn'de bir evresini yoklar ve boyunu 0.18 birim/sn ile hedefe surer
   (`RuntimeFieldBuilder.cs:67-90`).

Tohumlama (tick disinda, bir kez): `WorldFactory.SeedWorldAnchors` Site 5'e 3 bugday
bitkisi + 320 bugday larder'i koyar (`WorldFactory.cs:135`, `138-145`);
`DomainSimulationAdapter.SeedStartingProductionSites` baslangic yerlesimine 1 Field worksite +
1 soil + 1 seed bugday ekler (`DomainSimulationAdapter.Worldgen.Production.cs:46-55`).

## LLD - Veri Modeli

**PlantComponent** (`Assets/Scripts/Domain/Process/PlantComponent.cs:9-54`) — immutable:
- `Id: WorldComponentId`, `SiteId: SiteId`, `Position: GridPosition` (satir 38-40)
- `SpeciesId: string`, `StageId: PlantStageId`, `DaysInStage: int` (satir 41-43)
- `WithStage(stageId)` gun sayacini sifirlar (satir 45-48); `WithDaysInStage` (satir 50-53)

**SoilComponent** (`Assets/Scripts/Domain/Process/SoilComponent.cs:13-69`) — immutable:
- `Id`, `SiteId`, `Position`, `Fertility: int (0-100)`, `Moisture: int (0-100)`,
  `PlantId: WorldComponentId`, `HasPlant` (satir 36-42)
- `WithMoisture` / `WithPlant` / `WithoutPlant` (satir 44-59). NOT: Fertility/Moisture hicbir
  canli sistem tarafindan okunmaz/yazilmaz — susleme verisi (bkz. Borclar #6).

**PlantSpeciesDef** (`Assets/Scripts/Domain/Process/PlantSpeciesDef.cs:10-101`):
- `SpeciesId`, `SeedItemTag`, `HarvestItemTag` (satir 54-56), `Stages`, `GrowthRules`,
  `FirstStage` (satir 57-59)
- `TryGetStage` (61-74), `TryGetNextStage` — evre siralamasi liste sirasidir (76-89),
  `CanGrow(season, isSnowing)` — ILK eslesen kural karar verir (91-100)

**PlantGrowthStageDef** (`Assets/Scripts/Domain/Process/PlantGrowthStageDef.cs:6-27`):
- `Id: PlantStageId`, `DisplayName`, `DaysToNextStage: int` (0 = son evre),
  `IsHarvestable: bool` (satir 23-26)

**PlantGrowthRule** (`Assets/Scripts/Domain/Process/PlantGrowthRule.cs:4-27`):
- `Season` (`Season.None` = joker, satir 17-20), `AllowsGrowth`, `BlockedBySnow` (satir 13-15)

**StockpileComponent** (`Assets/Scripts/Domain/Process/StockpileComponent.cs:12-101`):
- `SiteId` + etiket->adet sozlugu; `Add` int-tasma korumali (satir 51-57), `Remove` sifir
  altina inmez (64-75), `Entries` kanonik sirali (86-94)

**Kanonik tur katalogu** (`WorldTickComposer.cs:104-121`): tek tur "wheat"
(seed 1 gun -> sprout 1 gun -> ripe hasatlik; `Season.None` kurali = her mevsim, kar engeli yok).
`SeedItemTag="wheat_seed"`, `HarvestItemTag="wheat_grain"` — ikisi de canli yolda KULLANILMAZ
(bkz. Borclar #3).

**Sunum mirror'i** (`RuntimeFieldBuilder.cs:11-45`): statik kanal `RuntimeFieldMirror` —
`HourOfDay` (14), `MinutesOfDay` (19, varsayilan 08:00), `WorldDay` (22), `PlantCount` (24),
`StageIndex 0/1/2` (27), `PlantCell{Id, LocalX, LocalZ, Stage}` dizisi + monoton `PlantsStamp`
(37-44).

**Save**: `PlantComponentSaveData` (`WorldSaveData.WorldProcess.cs:62-71`) ve
`SoilComponentSaveData` (50-59); mapper cift yonlu
(`WorldSaveMapper.Process.cs:89-99` soil, `117-127` plant) — bitki/soil durumu save'de yasar.

## LLD - Fonksiyon Haritasi

- `PlantGrowthSystem.AdvanceOneDay(species, plants, eventLog, now, season, isSnowing) -> int`
  (`PlantGrowthSystem.cs:17-79`) — bir turun tum bitkilerini bir gun ilerletir, evre atlayan
  sayisini dondurur; bilinmeyen evre exception (satir 41).
- `HarvestHandsService.FindHarvester(world, plant) -> ActorRecord`
  (`HarvestHandsService.cs:18-36`) — 2 hucre (Chebyshev, `ReachCells` satir 15) icindeki en
  yakin canli dusman-olmayan aktor; yoksa null.
- `DefaultTickSystems.HarvestStep.Run(in TickContext)` (`DefaultTickSystems.cs:426-467`) —
  canli hasat: ripe filtre + eller + stok + olay + ayni-Id tohum reset.
- `DefaultTickSystems.PlantGrowthStep.Run(in TickContext)` (`DefaultTickSystems.cs:487-507`) —
  sezonu cozer, katalog turlerini dongude buyutur.
- `PlantingSystem.TryPlant(species, soils, plants, soilId, plantId, inventory, eventLog, now) -> bool`
  (`PlantingSystem.cs:15-74`) — 1 tohum tuketir, bos soile bitki takar, `PlantPlanted` yazar.
  URETIMDE CAGIRAN YOK (yalniz testler).
- `HarvestSystem.TryHarvest(species, plants, soils, plantId, stockpile, eventLog, now, createHarvestItem) -> bool`
  (`HarvestSystem.cs:17-83`) — `IsHarvestable` evredeki bitkiyi `InventoryState`'e urun olarak
  ekler, bitkiyi siler, soili bosaltir, `PlantHarvested` yazar. URETIMDE CAGIRAN YOK.
- `FarmingJobRequestFactory.CreatePlantingJob / CreateHarvestJob(...) -> JobRequest`
  (`FarmingJobRequestFactory.cs:19-39`) — RecipeId 5101/5102, `WorksiteKind.Field` +
  `JobKind.Farmer` isleri (satir 53-62). CreateHarvestJob'i ureten uretim kodu yok.
- `ShortageResponseSystem.Tick(world, stamp) -> int` (`ShortageResponseSystem.cs:27-61`) —
  kitlik -> planting is ilani; gida etiketleri CANLI bitki turlerinden turetilir
  (`FoodTags`, satir 92-100), pozisyon mevcut bir bitkinin hucresi (`FieldPositionFor`, 82-90).
- `DomainSimulationAdapter.PublishFieldMirror()` (`DomainSimulationAdapter.Clock.cs:81-147`) —
  site sayimi + <=64 hucre projeksiyonu (satir 97-98 cap) + hash-gate yayin.
- `RuntimeFieldMirror.Publish(plantCount, stageIndex)` (`RuntimeFieldBuilder.cs:29-33`) /
  `PublishPlants(cells)` (40-44) — statik yayin; StageIndex 0-2'ye clamp'lenir.
- `SimFieldView.Update()` (`RuntimeFieldBuilder.cs:157-180`) + `BuildPlot(cell)` (182-203) —
  Id-anahtarli parsel yasam dongusu (kur/guncelle/buda).
- `CropStalkView.Update()` (`RuntimeFieldBuilder.cs:67-91`) — evre -> renk/boy;
  `ExternalStage >= 0` ise sim hucresi kazanir, degilse settlement-baskin mirror (satir 72).
- `RuntimeFieldBuilder.BuildBelt / Build` (`RuntimeFieldBuilder.cs:101-140`) — OLU KOD:
  polar dekor kusagi; repo'da hicbir cagiran kalmadi (dogrulandi: grep sifir sonuc).

## LLD - Yazdigi/Okudugu Alanlar

FieldOwnershipRegistry diliyle (`Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs:15-54`):

Yazilan:
- `World.Plants` — yazarlar: `econ.plantgrowth@Daily:20` (evre/gun ilerletme) ve
  `world.harvest@Daily:25` (ayni-Id seed reset). **DEFTERDE KAYITLI DEGIL** — registry'de
  `World.Plants` satiri yok; iki gunluk yazar fiilen dekларasyonsuz calisiyor (Borclar #1).
- `World.Stockpiles` — `world.harvest@Daily:25` kayitli (`FieldOwnershipRegistry.cs:43-50`);
  diger kayitli yazarlar (eatOnArrival/consumption/ambient/trade) bu sistemin disi.
- `World.Events` — PlantStageAdvanced / PlantHarvested / PlantPlanted / ShortageDetected append.
- `World.Jobs` — `econ.shortage_response@Daily:27` planting is ilani (registry'de bu alan da yok).
- (Uyuyan yol: `World.Soils` — yalniz PlantingSystem/HarvestSystem yazardi; canli tick'te
  Soils'a YAZAN YOK, worldgen tohumlamasi sonrasi donmus veridir.)

Okunan:
- `World.Plants.Rows` (HarvestStep, PlantGrowthSystem, ShortageResponse.FoodTags,
  PublishFieldMirror, WorldProjection.DescribeActivity `DomainSimulationAdapter.WorldProjection.cs:128-138`)
- `World.Actors.Records` (HarvestHandsService — canli/rol filtresi)
- `World.Stockpiles` (HarvestStep site-pile bul/olustur `DefaultTickSystems.cs:445-455`)
- `World.Time` (mirror saat/gun kanallari `Clock.cs:125-132`)
- SeasonCalendar + PlantSpeciesDef katalogu (composer'a gomulu, `WorldTickComposer.cs:88-121`)
- Sunum tarafi: yalniz `RuntimeFieldMirror` statikleri (sim'e geri yazim yok).

## LLD - Urettigi/Tukettigi Olaylar

Uretilen (`Assets/Scripts/Domain/World/WorldEventKind.cs`):
- `PlantPlanted = 11` (satir 25) — yalniz uyuyan PlantingSystem yazar (`PlantingSystem.cs:58-72`);
  canli tick'te hic uretilmez.
- `PlantStageAdvanced = 12` (satir 26) — `PlantGrowthSystem.cs:60-74`,
  log tag `plant_stage_advanced:{site}:{plant}:{stage}` + ReasonTrace `plant_growth`.
- `PlantHarvested = 13` (satir 27) — iki uretici: canli `HarvestStep`
  (`DefaultTickSystems.cs:460-462`, toplayici ActorId'li, ReasonTrace'siz kisa form) ve uyuyan
  `HarvestSystem` (`HarvestSystem.cs:67-81`, ReasonTrace'li `plant_harvest` formu).
- `ShortageDetected = 19` (satir 33) + `JobAssigned` "restock_job_posted reason:shortage"
  (`ShortageResponseSystem.cs:42-57`).

Tuketilen:
- `PlantHarvested` -> NPC ustunde yuzen hasat ikonu
  (`DomainSimulationAdapter.Clock.cs:66-70` -> `NpcEventEchoFeed.KindHarvest`).
- `PlantPlanted`/`PlantHarvested` -> anlati fiilleri "planted"/"harvested"
  (`WorldEventNarrator.cs:44-45`).
- Sistemin kendisi hicbir WorldEvent TUKETMEZ; sim->gorsel akisi olay degil mirror iledir.

## Testler

- `Assets/Tests/EditMode/Process/PlantGrowthSystemTests.cs` — gun sayaci/evre siniri/olay
  (satir 17), sezon-kar blogu (51), tur filtresi + son evre atlama (67), null/bilinmeyen evre (82).
- `Assets/Tests/EditMode/Process/PlantingSystemTests.cs` — tohum tuket + soil bagla + olay (18),
  mutasyonsuz red yollari (60), null redler (84). (Uyuyan sistemi pinler.)
- `Assets/Tests/EditMode/Process/HarvestSystemTests.cs` — ripe -> stok + soil temizle (18),
  ham bitki reddi (57), stok kabul etmezse mutasyonsuz (72), kotu factory (88). (Uyuyan sistem.)
- `Assets/Tests/EditMode/Process/HarvestHandsServiceTests.cs` — komsu koylu eller (26),
  kimse yoksa bekler (36), dusman asla toplamaz + en yakin kazanir (46).
- `Assets/Tests/EditMode/Process/PlantDefinitionTests.cs` — bugday def sirali evreler (12),
  bahar/yaz + kar blogu kurallari (27), gecersiz satir redleri (38).
- `Assets/Tests/EditMode/Process/FarmingJobIntegrationTests.cs` — planting isi ciftciye atanir
  (18), Field worksite yoksa harvest isi bekler (35), 5101/5102 ayrimi (50).
- `Assets/Tests/EditMode/Composition/WorldTickRegistryTests.cs:81-82` —
  `Daily:20:econ.plantgrowth` + `Daily:25:world.harvest` kadans/sira pinleri.
- `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs` — golden digest;
  2026-06-11 (HarvestStep) ve 2026-07-23 (M6 eller) re-baseline notlari (satir 16-24),
  fixture'da plant+soil tohumu (101-102).
- `Assets/Tests/EditMode/Composition/WorldLivesOverNTicksTests.cs:35-55` — "ekin YASADI:
  evre ilerledi VEYA tam buyu->hasat dongusu" makro-canlilik pini.
- `Assets/Tests/EditMode/Composition/FieldOwnershipRegistryTests.cs` — defter lint'i
  (bu sistemin borcunu YAKALAMIYOR, bkz. Borclar #1).

## Bilinen Borclar + Kacak Kapilari

1. **`World.Plants` sahipsiz — aile (c), kadans yazar catismasi tohumu.**
   `econ.plantgrowth@Daily:20` ve `world.harvest@Daily:25` ikisi de `world.Plants`'a yazar ama
   `FieldOwnershipRegistry.Writers`'da `World.Plants` satiri yok (`FieldOwnershipRegistry.cs:16-53`);
   `FieldOwnershipRegistryTests.CoreMutableFields` listesi de bu alani iceremiyor
   (`FieldOwnershipRegistryTests.cs:36-40`). REFORM #2'nin "dekлаre edilmemis ikinci yazar CI
   olayi olur" vaadi bu alan icin bos. Ustune, testteki `knownIds` "world.growth" /
   "world.shortage" iceriyor (`FieldOwnershipRegistryTests.cs:20-21`) ama gercek id'ler
   `econ.plantgrowth` (`DefaultTickSystems.cs:480`) ve `econ.shortage_response` (satir 348) —
   lint listesi gercek kayitla senkron degil, sadece tesadufen patlamiyor.
2. **Planting isi bitki DIKMEZ — aile (a)/(g), kapanmayan dongu.** ShortageResponse planting
   isi ilan eder (`ShortageResponseSystem.cs:51-53`), JobAssignment isi tamamlar ve
   `JobCompleted` yazar (`JobAssignmentSystem.Tick.cs:127-135`) ama tamamlama recipe-craft
   yoludur; hicbir yerde `PlantingSystem.TryPlant` cagrilmaz (dogrulandi: uretimde sifir
   cagiran). Yani kitlik cascade'i ciftciyi tarlaya yurutur, is "biter", yeni PlantComponent
   DOGMAZ. Ekonomiyi ayakta tutan sey HarvestStep'in ayni-Id yeniden dikimi
   (`DefaultTickSystems.cs:463-465`) — parsel sayisi dunya kurulumundaki sayida sonsuza dek
   sabittir; tarla buyumez, kucul(e)mez.
3. **Cift hasat yolu + etiket capraz-uyumsuzlugu — aile (a), tek-otorite ihlali.**
   Canli HarvestStep stok etiketine `SpeciesId` ("wheat") ekler (`DefaultTickSystems.cs:457`);
   uyuyan HarvestSystem `HarvestItemTag` ("wheat_grain") uretir (`HarvestSystem.cs:54-57`,
   katalog: `WorldTickComposer.cs:108-109`). "wheat_grain" repo'da baska hicbir uretim kodunda
   gecmiyor — uyuyan yol bir gun tick'e baglanirsa hasat urunu tuketim/fiyat sistemlerinin
   bakmadigi bir etikete akar. `SeedItemTag="wheat_seed"` de ayni sekilde olu veri.
4. **HarvestStep evreyi string ile tanir — aile (g).** `row.Value.StageId.Value == "ripe"`
   (`DefaultTickSystems.cs:434`) ve mirror sayimi "ripe"/"sprout" stringleri
   (`Clock.cs:92`, `104-109`); veri modelindeki `IsHarvestable` bayragi
   (`PlantGrowthStageDef.cs:26`) canli yolda okunmaz. Ikinci bir tur farkli evre adlariyla
   eklendiginde sessizce ne hasat edilir ne gorsellesir (hepsi stage 0 gorunur).
5. **Soils canli dongunun disinda — aile (a).** HarvestStep soil'a dokunmaz; WorldFactory'nin
   3 bitkisinin (`WorldFactory.cs:138-145`) soil karsiligi hic yok; tek soil worldgen'de
   (`Worldgen.Production.cs:54-55`). Uyuyan HarvestSystem ise soil eslesmesi ZORUNLU kilar
   (`HarvestSystem.cs:50-52`) — bugun baglansaydi factory bitkilerinde `return false` ile
   sessizce hasat reddederdi. `Fertility/Moisture` hicbir sistemce okunmuyor.
6. **`isSnowing: false` hardcode — aile (b) akrabasi (sim-hava kopuk).**
   `DefaultTickSystems.cs:505` — kar sim'e hic girmez; `PlantGrowthRule.BlockedBySnow` ve
   testteki kar senaryolari calisan koda degil, olu parametreye karsi yesil. Hava tamamen
   sunum kurgusu oldugundan (SYSTEMS_ATLAS bulgu 5) gorselde kar yagarken ekin buyumeye devam eder.
7. **Mirror 64-hucre cap'i — aile (g), unchecked kesme.** `Clock.cs:97-98`: 64'ten sonraki
   bitkiler gorsel projeksiyona girmez (hash yine hepsini sayar). Bugun parsel sayisi sabit-3/4
   oldugu icin gorunmez; #2 cozulup tarla buyurse sessiz kirpma olur.
8. **Olu kod: `RuntimeFieldBuilder.BuildBelt/Build` + legacy mirror modu.** Polar kusak emekli
   (cagiran sifir), ama sinif ve `CropStalkView`'in `ExternalStage == -1` legacy dali
   (`RuntimeFieldBuilder.cs:72`) duruyor; `WorldSceneDirector.cs:117-121` hala kullanilmayan
   `plots` sayisini hesaplayip log'luyor (satir 127) — DF-orani anlatisi gorselde artik temsili
   degil.
9. **Kacak kapilar (bilincli):** sezon cozulmezse Spring varsayimi
   (`DefaultTickSystems.cs:493-495`); `PlantCell.Stage` clamp'i yalniz `Publish` yolunda,
   `PublishPlants` hucreleri clamp'siz (`RuntimeFieldBuilder.cs:32` vs 40-44 — bugun guvenli,
   cunku tek uretici Clock.cs 0-2 uretir); statik mirror kanallari domain-reload/senaryo
   gecisinde sifirlanmaz (bilerek: director adapter'dan once kosar,
   `RuntimeFieldBuilder.cs:5-9`).

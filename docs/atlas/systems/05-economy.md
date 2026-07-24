# 05-economy

> Kapsam: Site-ekonomisi (StockpileComponent, PriceLedger, TradeRouteDef/CaravanInstance, kitlik tespiti/cevabi) + is atamalari (JobBoard/JobAssignmentSystem) + oyuncu-tuccar ekonomisi (SettlementTradeService/MerchantTradeService). Tum satir referanslari bu commit anindaki dosyalara gore verilmistir.

## HLD - Ne ve Neden

Ekonomi sistemi iki ayri ekonomiden olusur ve bunlar birbirine tek bir ince kopruyle baglidir. **Site-ekonomisi** deterministik simulasyon tarafidir: her yerlesimin site bazli bir stok yigini (`StockpileComponent`), site+esya bazli bir fiyat defteri (`PriceLedger`), rotalar uzerinde mal tasiyan kervanlar (`CaravanSystem`) ve stok esiklerine gore fiyat guncelleyen gunluk bir adim (`PriceUpdateSystem`) vardir. Kitlik dustugunde `ShortageResponseSystem` is panosuna ekim isi atar; bosta, karni tok ve morali yerinde bir ciftci (`JobAssignmentSystem`) isi ustlenir — "kitlik → is → ekim → hasat → stok → fiyat" dongusu (CAN SUYU H1+H3 kaskadi, `ShortageResponseSystem.cs:7-11`). **Oyuncu-tuccar ekonomisi** ise UI-suruluyor: `PlayerGold`/`MerchantGold` ve envanter takaslari (`SettlementTradeService`), Presence stat'ina gore alis/satis marjlari. Iki ekonomi yalnizca `LivePriceOr` koprusuyle temas eder: sim'in gunluk yazdigi canli site fiyati, tuccar ekranindaki statik `base_price`'in yerine gecer (`DomainSimulationAdapter.Trade.cs:70-86`). Felsefe: tum site-ekonomisi saf Domain verisi + stateless sistemlerdir (Unity tipi yok), her mutasyon `WorldEventLog`'a iz birakir, digest golden testleri byte-stabil kalir. Oyuncuya gorunen etki: pazar fiyatlari stoklara gore oynar, meydandan kervan arabasi izlenebilir (`RuntimeCaravanView.cs:8-10`), koyluler tarlaya iseyakalanir sekilde yuruyup calisir.

## HLD - Akis

Tick kaydi `DefaultTickSystems.Create` icinde kurulur (`DefaultTickSystems.cs:41-64`); kadans+sira etiketleri `"systemId@Cadence:Order"` bicimindedir.

1. **`econ.jobs` @Hourly:10** (`DefaultTickSystems.cs:123-208`): (a) `TryAssignNext` dongusuyle bekleyen isler bosta aktorlere claim edilir, aktore `ActorScheduleState.Assigned` yazilir ve `JobAssigned` olayi dusulur (139-162). (b) Claim edilmis her is icin `ProductionRecipeRegistry.Resolve` ile tarif cozulur; tarif kayitli degilse **sessizce atlanir** (172-183). (c) `TickAssignedJobs` aktif tarif emirlerini ilerletir, biten islere `JobCompleted` yazar, aktoru Idle'a dondurur (195-207).
2. **`world.caravans` @Daily:10** (`DefaultTickSystems.cs:401-417`): `CaravanSystem.Tick` her Idle/Arrived olmayan kervani bir adim ilerletir; `StepsSinceDeparture >= CadenceDays` olunca origin stokundan yukler, hedef stokuna bosaltir, `CaravanArrived` yazar (`CaravanSystem.cs:29-98`).
3. **`world.harvest` @Daily:25** (`DefaultTickSystems.cs:422-460`): "ripe" bitkiler, yakinda el varsa (`HarvestHandsService.FindHarvester`, 442-443), site stokuna 2 birim urun ekler ve tohuma geri doner. Stok yoksa stockpile burada yaratilir (445-455).
4. **`econ.shortage_response` @Daily:27** (`DefaultTickSystems.cs:343-354`): `ShortageResponseSystem.Tick` her stok yiginini bitki-turevli gida etiketlerine karsi tarar; stok < 4 ise `ShortageDetected` yazar ve site icin bekleyen ekim isi yoksa `FarmingJobRequestFactory.CreatePlantingJob` ile is panosuna is atar (`ShortageResponseSystem.cs:27-61`). Sira 27, hasat(25) sonrasi / fiyat(30) oncesi bilincli secim (`DefaultTickSystems.cs:341-342`).
5. **`econ.prices` @Daily:30** (`DefaultTickSystems.cs:518-551`): her stok yigininin **sifir-olmayan** girdileri icin `PriceUpdateSystem.Recompute` cagrilir; stok < `LowStockThreshold`(4) ise fiyat +`PriceStep`(1), stok > `HighStockThreshold`(64) ise -1; degisim varsa `PriceChanged` yazilir (`PriceUpdateSystem.cs:33-60`, esikler `EmberRuntimeOptions.cs:82-84`).
6. **Oyuncu ticareti (tick disi, UI tetikler)**: `DomainSimulationAdapter.ExecuteTrade` → `SettlementTradeService.TryBuy/TrySell`; fiyat tabani = canli site fiyati (varsa) → itibar indirimi (rep>=5 → %10, `Trade.cs:75-78`) → Presence marji (alis 1.20x, satis 0.55x, ±0.18 delta; `SettlementTradeService.cs:57-65,121-127`).
7. **Baslangic tohumu**: `WorldFactory` site 1'e 8 iron, site 5'e 100 coin + bugday tarlalari eker; iron fiyatini 10'a set eder; 1→5 iron x2 / 2 gun rotasi ve `EnRoute` bir kervan yaratir (`WorldFactory.cs:128-150`).
8. **Kayit/yukleme**: ekonomi dilimi `WorldSaveMapper.Economy.cs` uzerinden `PriceLedgerSaveData/StockpileSaveData/TradeRouteSaveData/CaravanSaveData`'ya cift yonlu maplenir (asagida).

## LLD - Veri Modeli

### StockpileComponent — `Assets/Scripts/Domain/Process/StockpileComponent.cs`
- `SiteId SiteId` (ctor bos SiteId'yi reddeder) — :16-23
- `Dictionary<string,int> _counts` (tag→adet) — :14
- `int Count` (sifir-olmayan tag sayisi) — :26-35; `Get(itemTag)` — :38-42
- `Add(itemTag, qty)`: negatif atar; long'a terfi + `int.MaxValue` clamp (Codex audit A-P1 overflow fix) — :45-58
- `int Remove(itemTag, qty)`: fiilen silinen adedi doner, sifirin altina inmez — :64-75
- `Entries`: yalnizca `>0` satirlar, Ordinal tag sirali (byte-stabil save/digest icin) — :86-94

### PriceLedger — `Assets/Scripts/Domain/Process/PriceLedger.cs`
- `Dictionary<PriceKey,int> _prices`; `PriceKey=(SiteId, ItemTag)` Ordinal — :15, :95-109
- `SetPrice` — :18-25; `GetPrice` (kayitsiz=0) — :28-33
- `AdjustPrice(delta)`: long terfi + [0, int.MaxValue] clamp (PR#152 wrap fix) — :39-56
- `Contains` — :59-64; `Count` — :67; `Entries` (SiteId.Value, sonra Ordinal tag sirali) — :75-84
- `PriceLedgerEntry{SiteId, ItemTag, Price}` — :113-129

### TradeRouteDef / TradeRouteId — `Assets/Scripts/Domain/World/TradeRouteDef.cs`
- `TradeRouteDef{Id, OriginSiteId, DestinationSiteId, ItemTag, QuantityPerCaravan, CadenceDays}`; origin!=destination, qty>0, cadence>0 zorunlu — :13-49
- `TradeRouteId(ulong)`, 0 = bos sentinel — :63-81

### CaravanInstance / CaravanState / CaravanId — `Assets/Scripts/Domain/World/CaravanInstance.cs`
- `CaravanState`: string-kodlu durum ("yeni durumlar data olarak gelir, enum dali degil"): `loading/en_route/arrived/unloading/idle` — :10-47 (sabitler :19-23)
- `CaravanId(ulong)` — :52-70
- `CaravanInstance{Id, RouteId, CurrentSiteId, PayloadRemaining, StepsSinceDeparture, State}` — :76-108
- `AdvanceStep()` (adim++, EnRoute) — :111-115; `Load(qty)` — :117-124; `Arrive(siteId)` — :127-133; `Unload(qty)` (payload 0 olunca **Idle**) — :136-144. `StepsSinceDeparture`'i sifirlayan API **yok**.

### Is (job) tarafi
- `JobId(ulong)`: 0 = bos sentinel — `Domain/Process/JobId.cs:14-89`
- `JobRequest{Id, RecipeId, SiteId, WorksitePosition, WorksiteKind, Kind, Priority, Quantity, RequesterId}` — immutable, tum alanlar ctor'da dogrulanir — `Domain/Process/JobRequest.cs:18-85`
- `JobBoard` (pending/claimed yasam dongusu) — `Domain/Process/JobBoard.cs:18` (ic yapisi bu belgede detaylanmadi)
- `FarmingJobRequestFactory.PlantCropRecipeId = RecipeId(5101)`, `HarvestCropRecipeId = RecipeId(5102)` — `Simulation/Process/FarmingJobRequestFactory.cs:16-17`

### WorldState uzerindeki ekonomi kokleri — `Assets/Scripts/Domain/World/WorldState.cs`
- `Prices` (PriceLedger), `Stockpiles` (List), `TradeRoutes` (List), `Caravans` (List) — :45-48
- `Jobs` (JobBoard), `Worksites` (WorksiteStore), `Plants`, `Soils` — :59-62 (SOUL-01 notu: bu store'lar save-bridge yan-store'larindan world root'a tasindi, :54-58)
- `FindStockpile(siteId)` — :315-318; `FindTradeRoute(routeId)` — :320-323

### Konfig — `Assets/Scripts/Domain/Configuration/EmberRuntimeOptions.cs`
- `LowStockThreshold=4`, `HighStockThreshold=64`, `PriceStep=1` — :82-84; normalize min-clamp — :256-258

### Icerik (JSON DTO) — `Assets/Scripts/Data/Content/EconomyColonyCaravanDtos.cs`
- `EconomyConfigDto{default_store_inventory, commodities}` — :10-16; `CommodityDto.base_price` — :19-24; `StoreInventoryItemDto.item_def_id` — :30-32; `CaravansDocumentDto/CaravanDto` — :67-72 (bu DTO'larin runtime CaravanInstance'a baglandigi dogrulanmadi; tuccar stogu ve base fiyat kullanimi `DomainSimulationAdapter.Trade.cs:93-96,129-140`'ta dogrulandi)

### Save modeli — `Assets/Scripts/Data/Save/WorldSaveData.Economy.cs`
- `PriceLedgerSaveData` — :23; `StockpileSaveData(+EntrySaveData)` — :31-42; `TradeRouteSaveData` — :45-53; `CaravanSaveData` — :56

## LLD - Fonksiyon Haritasi

| Fonksiyon | Konum | Ozet |
|---|---|---|
| `PriceUpdateSystem.Recompute(ledger, stockpile, itemTag, low, high, delta, now, events)` | `Simulation/World/PriceUpdateSystem.cs:15-61` | Stok esiklerine gore fiyati ±delta oynatir, degisim olduysa `PriceChanged` yazar. |
| `CaravanSystem.Tick(caravans, resolveRoute, resolveStockpile, now, events)` | `Simulation/World/CaravanSystem.cs:17-99` | Idle/Arrived olmayan kervanlari ilerletir; varista origin'den yukler (43-65), hedefe bosaltir (87-97); origin bos ya da hedef stoku cozulmezse `caravan_stuck` yazar (55-64, 76-85). |
| `ShortageDetector.Check(stockpile, itemTag, threshold, now, events)` | `Simulation/World/ShortageDetector.cs:21-51` | Esik altina **gecis aninda bir kez** `ShortageDetected` yazar (PR#162 spam fix). Uretimde cagiran yok — yalniz testler (asagida borc #7). |
| `ShortageResponseSystem.Tick(world, stamp)` | `Simulation/World/ShortageResponseSystem.cs:24-61` | Gunluk tarama: gida stogu < 4 → `ShortageDetected` + (bekleyen ekim isi yoksa) `CreatePlantingJob` ile is panosuna is; posted sayisini doner. Stateless — JobId `(site, gun%512)` turevli (:46). |
| `TradeService.TryTrade(ledger, buyerPile, sellerPile, itemTag, qty, now, events, ...)` | `Simulation/World/TradeService.cs:16-87` | Atomik site-arasi takas: fiyatli-ama-defterde-kayitsiz esyayi reddeder (52-53), opsiyonel para birimi bacagi (57-72) ve faction itibar deltasi (74-78); `TradeCompleted` yazar. Uretimde cagiran yok (borc #9). |
| `JobAssignmentSystem.TryAssignNext(actors, board, worksites, [eventLog, now,] out result)` | `Simulation/Process/JobAssignmentSystem.cs:32-73, 82-143` | Deterministik eslesme: aktor tercih onceligi → istek onceligi → aktor sirasi → is sirasi (`Candidate.CompareTo`, :295-310); claim + `ActorScheduleState.Assigned` yazimi. |
| `JobAssignmentSystem.CanActorWorkJob(...)` | `JobAssignmentSystem.cs:151-165, 172-191` | Canli+bosta+tercih acik+aktif worksite (+tarif preflight'i klonlanmis envanterde) kontrolu. |
| `JobAssignmentSystem.StartRecipeForClaim(...)` | `JobAssignmentSystem.cs:199-258` | Claim edilmis is icin `RecipeSystem.TryStart` ile aktif emir baslatir, `_activeOrders`'a kaydeder. |
| `JobAssignmentSystem.TickAssignedJobs(...)` | `Simulation/Process/JobAssignmentSystem.Tick.cs:37-157` | Aktif emirleri tick'ler; coklu-adet islerde sonraki yurutmeyi baslatir (104-125); bitince `board.Complete` + `JobCompleted` + aktoru Idle'a dondurur (127-153). |
| `JobAssignmentSystem.IsRefusing(actor)` | `JobAssignmentSystem.Tick.cs:358-375` | Hunger >= 80 veya `Mood.IsLow` → is reddi; secim sirasinda `JobRefused` olayi (:285-303). |
| `FarmingJobRequestFactory.CreatePlantingJob / CreateHarvestJob` | `Simulation/Process/FarmingJobRequestFactory.cs:19-39` | `WorksiteKind.Field` + `JobKind.Farmer` tipli is uretir. |
| `SettlementTradeService.TryBuy / TrySell` | `Simulation/Inventory/SettlementTradeService.cs:67-119` | PlayerGold/MerchantGold + iki envanter arasi tek-birim takas; rollback'li hata yollari. |
| `SettlementTradeService.ComputeBuyPrice / ComputeSellPrice` | `SettlementTradeService.cs:57-65` | base*(1.20+presenceDelta) / base*(0.55+presenceDelta*0.5); presenceDelta=[-0.18,+0.18] (:121-127). |
| `MerchantTradeService.TradeGateWrit(world)` | `Simulation/Inventory/MerchantTradeService.cs:18-72` | Slice'a ozel EmberShard↔GateWrit takasi; NPC hafizasina `TradedWith` + `TransactionRecord` yazar (49-62). |
| `TradeRefusalHook.ShouldRefuse(...)` | `Simulation/Narrative/TradeRefusalHook.cs:23-59` | Faction War/Hostile veya yakin-tarihli suc anisi → red + gerekce. Uretimde cagiran yok (borc #8). |
| `DomainSimulationAdapter.ExecuteTrade(request)` | `Presentation/Ember/Adapters/DomainSimulationAdapter.Trade.cs:28-43` | UI→sim ticaret koprusu; `LivePriceOr` (:80-86) canli site fiyatini, `ApplyReputationDiscount` (:75-78) itibar indirimini uygular. |
| `WorldFactory` ekonomi tohumu | `Simulation/World/WorldFactory.cs:128-150` | Baslangic stoklari, iron fiyati (10), rota 1→5 (iron x2 / 2 gun) ve tek `EnRoute` kervan. |
| `WorldSaveMapper.To*/From*` ekonomi | `Data/Save/SliceJson/WorldSaveMapper.Economy.cs:19-136` | Ledger/stok/rota/kervan cift yonlu save mapping (`ToPriceLedgerData`:19, `ToStockpiles`:60, `ToTradeRoutes`:96, `ToCaravans`:126). |
| `WorldStateDigest.AppendJobs/AppendPrices/AppendStockpiles/AppendCaravans` | `Simulation/Composition/WorldStateDigest.cs:44-47, 163, 195, 228` | Golden-test digest'inin JOBS/PRICES/STOCKPILES/CARAVANS bolumleri — determinizm pin'i. |

## LLD - Yazdigi/Okudugu Alanlar

FieldOwnershipRegistry dili (`Simulation/Composition/FieldOwnershipRegistry.cs`):

**Deklare edilenler:**
- `World.Stockpiles` ← `world.harvest@Daily:25`, `living.eatOnArrival@PerTick:22`, `living.consumption@Hourly:35`, `living.ambient@Hourly:50`, `econ.trade@Daily:28` — :43-50

**Fiili yazarlar (kod dogrulamali):**
- `World.Stockpiles`: `world.caravans@Daily:10` (origin `Remove` `CaravanSystem.cs:46`, hedef `Add` :88) — **deklare EDILMEMIS** (borc #2); `world.harvest@Daily:25` (`DefaultTickSystems.cs:454-457`).
- `World.Prices`: tek yazar `econ.prices@Daily:30` (`PriceUpdateSystem.AdjustPrice` uzerinden) + worldgen tohumu (`WorldFactory.cs:146`) + save hidrasyonu (`WorldSaveMapper.Economy.cs:38`) — registry'de `World.Prices` satiri **hic yok**.
- `World.Jobs`: `econ.shortage_response@Daily:27` (`world.Jobs.Add`, `ShortageResponseSystem.cs:53`), `econ.jobs@Hourly:10` (claim/complete, `DefaultTickSystems.cs:139`, `JobAssignmentSystem.Tick.cs:127`) + worldgen is tohumu (`Worldgen.Production.cs:63-64`) — registry'de satiri yok.
- `World.Caravans`: `world.caravans@Daily:10` (durum/adim/payload mutasyonu) — registry'de satiri yok.
- `Actor.ScheduleState`: `econ.jobs@Hourly:10` (`Assigned` yazimi `DefaultTickSystems.cs:143-147`, `TryClaimCandidate` `JobAssignmentSystem.Tick.cs:252-256`; is bitiminde `Idle` `JobAssignmentSystem.Tick.cs:152`) — registry'de satiri yok.
- `World.PlayerInventory`: `econ.jobs@Hourly:10` — NPC tarif girdi/ciktilari **oyuncu envanterinden** akar (`DefaultTickSystems.cs:190, 196-207`; borc #5).
- `World.PlayerGold / World.MerchantGold / MerchantInventory`: tick disi, UI yolu (`SettlementTradeService.cs:87-88, 115-116`).

**Okudugu alanlar:** `World.Plants` (gida etiketleri + tarla pozisyonu, `ShortageResponseSystem.cs:82-100`), `World.Actors` (bosta/canli/tercih/ihtiyac, `JobAssignmentSystem.Tick.cs:226-243`), `World.Worksites` (aktif worksite esleme, `JobAssignmentSystem.Tick.cs:347-353`), `World.TradeRoutes` (`WorldState.FindTradeRoute` uzerinden `DefaultTickSystems.cs:415`), `EmberRuntimeOptions.Tick` esikleri (`DefaultTickSystems.cs:23-25`).

## LLD - Urettigi/Tukettigi Olaylar

`WorldEventKind` (`Assets/Scripts/Domain/World/WorldEventKind.cs`):

| Olay | Deger | Ureten | Log tag ornekleri |
|---|---|---|---|
| `JobAssigned` | 5 (:19) | `econ.jobs` (`DefaultTickSystems.cs:149-161`; ayrica `JobAssignmentSystem.Tick.cs:265-283`) ve `econ.shortage_response` ("restock_job_posted reason:shortage", `ShortageResponseSystem.cs:55-57` — anlamsal olarak "posted", "assigned" degil; borc #10) | `job_assigned:<id>` |
| `JobCompleted` | 6 (:20) | `TickAssignedJobs` (`JobAssignmentSystem.Tick.cs:130-142`) | `job_completed:<id>` + ReasonTrace |
| `JobRefused` | 8 (:22) | aday secimi sirasinda (`JobAssignmentSystem.Tick.cs:285-303`) | `job_refused:<id>`, reason:hunger_or_low_mood |
| `CaravanArrived` | 16 (:30) | `CaravanSystem` — hem gercek varis (`caravan_arrived ... delivered:N`, :92-97) hem stall (`caravan_stuck ... reason:origin_empty|destination_unavailable`, :57-63, :78-84) ayni kind'i kullanir | |
| `PriceChanged` | 17 (:31) | `PriceUpdateSystem` (:55-60) | `price_up|price_down item:<tag> from:<a> to:<b> stock:<n>` |
| `TradeCompleted` | 18 (:32) | `TradeService.TryTrade` (:80-85) — uretim cagirani olmadigi icin fiilen yalniz testlerde gorulur | `trade item:... qty:... unit:...` |
| `ShortageDetected` | 19 (:33) | `ShortageResponseSystem` (:42-44); `ShortageDetector` (:45-50, uretim-disi) | `shortage item:<tag> stock:<n> threshold:4` |
| `TradeRefused` | 23 (:37) | **hicbir yerde emit edilmiyor** (borc #8) | — |

Tuketiciler: `WorldEventNarrator` / `RuntimeHistorySystem` / digest ve golden testler bu olaylari okur (bu belgede yalniz varliklari not edildi; tuketim semantigi 0x-history/narrative atlas sayfalarinin konusu).

## Testler

- `Assets/Tests/EditMode/Process/StockpileComponentTests.cs`, `PriceLedgerTests.cs` — domain sozlesmeleri
- `Assets/Tests/EditMode/World/CaravanSystemTests.cs` — kervan yasam dongusu
- `Assets/Tests/EditMode/World/TradeBundleTests.cs` — ShortageDetector gecis-tekil emisyonu (:57-76) + TryTrade (:91-109)
- `Assets/Tests/EditMode/World/TradeServiceMissingPriceTests.cs` — fiyatsiz-esya red kurali
- `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`, `JobAssignmentCompetitionTests.cs`, `JobPriorityTests.cs`, `JobNeedsRefusalTests.cs`, `JobEventLogTests.cs`, `FarmingJobIntegrationTests.cs` — atama determinizmi, oncelik, red, olay satirlari
- `Assets/Tests/EditMode/Inventory/SettlementTradeServiceTests.cs`, `MerchantTradeServiceTests.cs` — oyuncu-tuccar yolu
- `Assets/Tests/EditMode/Narrative/TradeRefusalHookTests.cs` — refusal kurallari (uretim baglantisi olmadan)
- `Assets/Tests/EditMode/Acceptance/FazSixToTwelveBackendAcceptanceTests.cs` — trade+shortage kabul zinciri (:50, :69, :169)
- `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs`, `SaveLoadDigestRoundtripTests.cs`, `JsonSliceSaveServiceTests.cs` — PRICES/STOCKPILES/CARAVANS/JOBS digest ve save roundtrip pinleri
- `Assets/Tests/EditMode/Composition/FieldOwnershipRegistryTests.cs` — sahiplik lint'i (asagida borc #1'e bakiniz)
- `Assets/Tests/EditMode/Composition/WorldLivesOverNTicksTests.cs`, `LiveScaleCatchupPerfPinTests.cs`, `Living/ColonyNeedsAcceptanceReplayTests.cs` — uzun-kosum/marathon pinleri (CAN SUYU)

## Bilinen Borclar + Kacak Kapilari

1. **Hayalet yazar `econ.trade@Daily:28`**: `FieldOwnershipRegistry.cs:49` `World.Stockpiles` icin bu yazari deklare eder ama tick kaydinda `econ.trade` diye bir adim yok (tum repo'da tek gecis o satir). Lint testi yakalayamaz cunku `FieldOwnershipRegistryTests.cs:14-22`'deki `knownIds` beyaz listesi **kendisi bayat**: `econ.trade`, `econ.caravan`, `world.shortage`, `world.history` icerir; gercek id'ler `world.caravans` (`DefaultTickSystems.cs:406`), `econ.shortage_response` (:348), `world.runtime_history` (:514). Lint, tick registry'ye degil sabit listeye bakiyor — "executable documentation" iddiasi (registry :7-10) su an bos.
2. **Deklare edilmemis stok yazari**: `CaravanSystem` origin'den `Remove` (`CaravanSystem.cs:46`) ve hedefe `Add` (:88) yapar; `world.caravans@Daily:10` `World.Stockpiles` sahiplik satirinda yok (`FieldOwnershipRegistry.cs:43-50`). Registry'nin yakalamak icin kuruldugu "undeclared second writer" sinifinin bizzat ornegi. (Muhtemelen #1'deki `econ.trade` bunu temsil ediyordu ve id hic guncellenmedi.)
3. **Kervanlar tek atimlik**: Teslimattan sonra `Unload` payload'i sifirlar ve durum `Idle` olur (`CaravanInstance.cs:142`); `CaravanSystem.Tick` Idle kervanlari atlar (`CaravanSystem.cs:32`) ve hicbir sistem yeniden sefer baslatmaz ya da `StepsSinceDeparture`'i sifirlamaz (resetleyen API da yok, `CaravanInstance.cs:103-144`). `TradeRouteDef.CadenceDays` fiilen "kervan kalkis kadansi" degil (dokstring `TradeRouteDef.cs:8-9` oyle soyluyor), **ilk ve tek seferin yol suresi**. Tekrarlayan ticaret yazilmamis.
4. **Ekim tarifi (RecipeId 5101) kayitsiz**: `ProductionRecipeRegistry` yalnizca 1001 (smelt) ve 1002 (bread) icerir; `Resolve` bilinmeyen id'de atar (`ProductionRecipeRegistry.cs:17-18, 49-54`). `econ.jobs` bunu yakalayip **sessizce atlar** ("content packs may claim jobs whose recipes land in a later authoring pass", `DefaultTickSystems.cs:177-183`). Sonuc: `ShortageResponseSystem`'in ektigi restock isi claim edilebilir (yuruyus hedefi olur) ama tarifi hicbir zaman baslamaz ve `JobCompleted` uretmez; bekleyen is `HasPendingPlanting` guard'i (`ShortageResponseSystem.cs:40, 73-80`) uzerinden o site icin **kalici** yeni-is-ve-olay bastirici olarak calisir. Claim'in gerceklesip gerceklesmedigi de tarla worksite pozisyonunun (`Worldgen.Production.cs:47`) is istegindeki bitki pozisyonuyla (`ShortageResponseSystem.cs:82-89`) birebir eslesmesine bagli (`TryGetActiveMatchingWorksite`, `JobAssignmentSystem.Tick.cs:347-353`) — sahada eslesme durumu **dogrulanmadi**. "Scarcity → work → replanting → harvest: the loop answers itself" iddiasinin (:7-11) tarif bacagi kapali; fiili yeniden-ekimi `world.harvest`'in "replant at seed" davranisi tasiyor (`DefaultTickSystems.cs:419-421`).
5. **NPC uretimi oyuncu envanterinden akar**: `econ.jobs` tarif girdilerini `world.PlayerInventory`'den tuketir ve ciktilarini oraya yazar (`DefaultTickSystems.cs:190, 196-207`). Koy uretimi = oyuncu cantasi; site stoklariyla iliskisi yok. SOUL-01 koprusunun bilinen kalintisi, sinif olarak "yanlis sahiplik/yanlis magaza" ailesine girer.
6. **Sifir stokta fiyat donar**: `PriceStepSystem` yalnizca `stockpile.Entries` uzerinde doner (`DefaultTickSystems.cs:534-548`) ve `Entries` sifir satirlari eler (`StockpileComponent.cs:90-93`). Bir esyanin stogu 0'a dusunce fiyati **tam kitlik aninda** guncellenmez olur — kitlik fiyat sinyali en cok gerektigi anda susar. (Stok 1-3 arasi normal yukselir; 0'da durur.)
7. **`ShortageDetector` uretim yetimi**: gecis-tekil emisyon semantigi olan sinif (`ShortageDetector.cs:17-51`) yalnizca testlerden cagriliyor (`TradeBundleTests.cs:64,76`, `FazSixToTwelveBackendAcceptanceTests.cs:69`); uretim taramasi `ShortageResponseSystem` icinde farkli semantikle (bekleyen-is-guard'li, gunluk yeniden-emisyonlu) yeniden yazilmis. Iki "shortage" tanimi drift halinde.
8. **`TradeRefused` olayi hic emit edilmiyor**: kind tanimli (`WorldEventKind.cs:37`), `TradeRefusalHook` gerekce uretiyor (`TradeRefusalHook.cs:23-59`) ama uretimde ne hook'u cagiran ne olayi yazan var (tek kullanicilar testler). Phase 9 Atom 11 yarim baglanmis.
9. **`TradeService` (site-arasi ticaret) uretim yetimi**: para-birimi bacakli, itibar-deltali atomik takas (`TradeService.cs:16-87`) yalnizca testlerden cagriliyor. Siteler arasi mal akisini fiilen yalniz kervanlar (parasiz, tek yonlu `Remove`→`Add`) yapiyor; `TradeCompleted` uretimde dogmaz.
10. **Olay-kind istismari**: `ShortageResponseSystem` is **postalamayi** `JobAssigned` kind'iyla loglar (`ShortageResponseSystem.cs:55-57`, mesaj "restock_job_posted") ve `CaravanSystem` stall'i `CaravanArrived` kind'iyla loglar (`CaravanSystem.cs:57-63, 78-84`, mesaj "caravan_stuck"). Kind'a gore filtreleyen tuketiciler (narrator/digest sayaclari) yanlis siniflar.
11. **JobId sarma penceresi**: restock JobId formulu `7_700_000 + site*512 + (gun%512)` (`ShortageResponseSystem.cs:46`) — 512 gun sonra ayni id'ler geri doner; `world.Jobs.Contains` guard'i (:47) eski ayni-id'li is hala panoda ise yeni isi sessizce yutar. Marathon-olcekli kosumlarda teorik carpisma.
12. **Iki ekonomi tek yonlu bagli**: oyuncu ticareti site fiyatini okur (`LivePriceOr`, `Trade.cs:80-86`) ama oyuncu alim/satimi site stoklarina/fiyatlarina **geri yazmaz** (`SettlementTradeService` yalnizca envanter+gold mutasyonu yapar). Oyuncu pazari bosaltamaz, fiyat oynatamaz.
13. **Escape hatch — sessiz tarif atlama**: #4'teki `catch (KeyNotFoundException) { continue; }` (`DefaultTickSystems.cs:177-183`) bilincli bir kacak kapisi; log'suz oldugu icin kayitsiz-tarif isleri gozlemlenemez sekilde birikir.

*(Not: gorev tanimindaki "(a)-(g) hata ailesi" siniflandirmasinin tanim dosyasi bu tarama sirasinda bulunamadi — borclar aile harfine baglanmadan duz yazildi; siniflandirma eklenmek istenirse #1-2 "deklarasyon/lint drifti", #3-4-8-9 "yarim-bagli sistem", #5-6-10 "yanlis sahiplik/yanlis sinyal", #11-13 "determinizm/gozlemlenebilirlik" kumelerine oturur.)*

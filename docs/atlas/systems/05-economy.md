# 05-economy

> Kapsam: Site-ekonomisi (StockpileComponent, PriceLedger, PriceUpdateSystem/B08, CaravanSystem/B07, TradeService, ShortageResponseSystem) + W33 B06 kopru (`IRecipeInventory` + `StockpileRecipeInventory` + `InventoryRecipeAdapter`) + oyuncu-tuccar ekonomisi (SettlementTradeService/MerchantTradeService). Referanslar 2026-07-25 commit'ine gore verildi.

## HLD - Ne ve Neden

Ekonomi iki ayri ekonomiden olusur, aralarindaki tek kopru fiyat okumasidir. **Site-ekonomisi** deterministik simulasyon tarafidir: her yerlesim icin bir tag-adet stok kutugu (`StockpileComponent`), (site, esya) bazli bir fiyat defteri (`PriceLedger`), rotalar uzerinde adim adim mal tasiyan kervanlar (`CaravanSystem`, W32/W36 B07 ile tek atimlik degil - Idle olunca kendini yeniden silahlandirir) ve stok esiklerine gore gunluk fiyat guncelleyen `PriceUpdateSystem` vardir; W32/W36 B08 patch'i ile bu adim yalniz *var olan* girdileri degil, defterin bildigi TUM (site, esya) satirlarini de yeniden hesaplar - stok dibe vurup Entries'ten dustuk unutulmaz. Kitlik saptandiginda `ShortageResponseSystem` STATELESS bir sekilde is panosuna ekim isi asar; bosta ciftci `JobAssignmentSystem` uzerinden alir, MoveToPlot -> PlantSeed -> HaulCrop zinciri stogu yerine koyar (CAN SUYU H1+H3 kaskadi). W33 B06 kopruculugu ile tarif motoru (`RecipeSystem`) artik `InventoryState` degil `IRecipeInventory` konusur; ayni motor hem oyuncu envanterinden hem site stokundan urun/gida uretebilir - koy uretimi player-bag'inda pisen bir simulasyon degildir artik. **Oyuncu-tuccar ekonomisi** ise UI-tetiklidir: `PlayerGold`/`MerchantGold` + envanter takaslari (`SettlementTradeService`), Presence stat'ina bagli alis/satis marjlari ve MerchantTradeService'in Sprint 2 dar takas kurallari (Ember Shard -> Gate Writ). Ikisi arasindaki tek temas noktasi `LivePriceOr` koprusudur (adapter tarafi), sim'in gunluk yazdigi canli fiyat statik `base_price` yerine gecer.

## HLD - Akis

Ekonomi ile ilgili tum tick adimlari `DefaultTickSystems.Create` icinde kayitlidir; kadans+sira etiketleri `"systemId@Cadence:Order"` bicimindedir.

1. **`econ.jobs` @Hourly:10** (`DefaultTickSystems.cs:152-208`): `JobAssignmentSystem.TryAssignNext` bekleyen isi uygun ciftci/isciye claim eder, `JobAssigned` yazar. W34-C sonrasi bu adim **stogu HIC OKUYUP YAZMAZ**; ne recipe input tuketir ne output basar - o is `PerformWorkAdvancer`'a devrildi. Adim hala olu-claimant taramasi + ghost tarif iptallerinden sorumludur.
2. **`world.caravans` @Daily:10** (`DefaultTickSystems.cs:449-465`, `CaravanSystem.cs:14-104`): her Idle kervan bu tick'te `Depart` cagirir (B07: yeniden silahlanma), sonraki tick'ten itibaren `AdvanceStep`. `StepsSinceDeparture >= route.CadenceDays` olunca origin stogundan yukleme; yukleme 0 ise **stuck event** emit edip stall (Codex A/P2: onceden delivered:0 basarili gorunuyordu). Hedef stok null ise yine stuck ve `Arrive` cagrilmaz (PR#161 bot fix: eskisi arriveled sonra null-check yapip payload'i limbo'da birakiyordu). Basarili teslimatta `CaravanArrived` yazilir, payload sifirlanir - kervan `Unload` icinde Idle'a dusup sonraki tick tekrar Depart eder.
3. **`econ.shortage_response` @Daily:27** (`DefaultTickSystems.cs:391-402`, `ShortageResponseSystem.cs:14-107`): STATELESS - registrar tek instance'i paylasir, hicbir alan tutulmaz. Her stokta bitki-turevli gida etiketlerini tarar; `stock < ShortageThreshold (4)` ise `ShortageDetected` yazar, site icin bekleyen ekim isi YOKSA + bos toprak plot varsa + ilk sivil requester bulundu ise (`RestockJobIdBase + siteId*512 + day%512`) deterministik JobId ile `FarmingJobRequestFactory.CreatePlantingJob` uretir, `JobAssigned` yazar. Sira 27; hasat sonrasi/fiyat oncesi kasitli slot secim: sweep hasat sonrasi post-truth stogu gorur ve fiyat guncellemesinden once is asilmis olur.
4. **`econ.prices` @Daily:30** (`DefaultTickSystems.cs:516-573`, `PriceUpdateSystem.cs`): her stok icin (a) `Entries` uzerinden var olan girdileri repricele; (b) **B08 - Faz 2**: ayni site icin defterin bildigi ama Entries'te olmayan (yani stogu tukenmis) tag'leri de yeniden hesapla. Recompute: `count < LowStockThreshold (4)` -> +`PriceStep (1)`; `count > HighStockThreshold (64)` -> -1; degisim varsa `PriceChanged` yazilir (yon+eski+yeni+stok).
5. **W33 B06 kopru (tick disi, `PerformWorkAdvancer` icinden)**: `WorkOperations.SiteIo(world, siteId)` cagrisi `FarmOperations.FindOrCreatePile`'i vurup site stogu uzerine `StockpileRecipeInventory` sarar; `RecipeSystem.TryFund` + `Tick` bunu `IRecipeInventory` olarak konusur. `CloneForPreflight` bir Dictionary probe uretir - live pile preflight sirasinda hic dokunulmaz. Oyuncu envanterinde uretim yolu ise `InventoryRecipeAdapter` uzerinden: `TryAccept` bir `Func<string,InventoryItem>` mint factory ile birim item basar; kapasite dolulugu preflight'in yakaladigi gercek retdir.
6. **Oyuncu ticareti (tick disi, UI tetikler)**: `DomainSimulationAdapter.ExecuteTrade` -> `SettlementTradeService.TryBuy/TrySell`; fiyat: canli site fiyati -> itibar indirimi -> Presence marji (alis 1.20x, satis 0.55x, +/-0.18 delta). `MerchantTradeService.TradeGateWrit` ise Sprint 2 dar takas kurali - Ember Shard karsiligi Gate Writ, ManhattanDistance <=2 ve NPC hafiza sartlariyla.

## LLD - Veri Modeli (file:line)

### `StockpileComponent` - `Assets/Scripts/Domain/Process/StockpileComponent.cs`
- `SiteId SiteId { get; }` (ctor bos SiteId reddeder) - :16-22
- `Dictionary<string,int> _counts` (tag -> adet) - :14
- `int Count` (yalniz `>0` tag'ler) - :26-35
- `int Get(string tag)` (bilinmiyor = 0) - :38-42
- `void Add(string tag, int qty)`: negatif atar; **Codex A-P1 overflow fix** - long'a terfi + `int.MaxValue` clamp - :45-58
- `int Remove(string tag, int qty)`: fiilen silinen, sifirin altina inmez - :64-75
- `bool Contains(string tag)` - :78
- `IEnumerable<KeyValuePair<string,int>> Entries`: **Codex A/P3** - Ordinal tag sirali, yalniz `>0` (byte-stabil digest/save) - :86-94

### `PriceLedger` - `Assets/Scripts/Domain/Process/PriceLedger.cs`
- `Dictionary<PriceKey,int> _prices`; `PriceKey=(SiteId, string ItemTag)` Ordinal - :15, :92-108
- `void SetPrice(site, tag, price)`: negatif fiyat atar - :18-25
- `int GetPrice(site, tag)` (kayitsiz = 0) - :28-33
- `int AdjustPrice(site, tag, delta)`: **PR#152 wrap fix** - long terfi + [0, int.MaxValue] clamp - :39-58
- `bool Contains(site, tag)` - :61-66
- `int Count` - :69
- `IEnumerable<PriceLedgerEntry> Entries`: SiteId.Value, sonra Ordinal tag sirali - :77-86
- `readonly struct PriceLedgerEntry(SiteId, string, int)` - :112-128

### `TradeRouteDef` / `TradeRouteId` - `Assets/Scripts/Domain/World/TradeRouteDef.cs`
- `TradeRouteDef{Id, OriginSiteId, DestinationSiteId, ItemTag, QuantityPerCaravan, CadenceDays}`; origin != destination, qty > 0, cadence > 0 ctor'da dogrulanir.

### `CaravanInstance` / `CaravanState` - `Assets/Scripts/Domain/World/CaravanInstance.cs`
- `CaravanState`: string-kodlu, `loading / en_route / arrived / unloading / idle` - :10-47 (sabitler :19-23). Enum degil - "yeni durumlar data olarak gelir, kod dali degil".
- `CaravanInstance{Id, RouteId, CurrentSiteId, PayloadRemaining, StepsSinceDeparture, State}` - :76-108
- `void Depart()`: **B07** - `StepsSinceDeparture=0` + `State=Loading` - :110-116
- `void AdvanceStep()`: adim++, `State=EnRoute` - :119-123
- `void Load(int qty)`: payload += qty; qty>0 ise `EnRoute` - :125-132
- `void Arrive(SiteId)`: `CurrentSiteId=siteId`, `State=Arrived` - :135-141
- `int Unload(int qty)`: payload sifirlaninca `State=Idle` (kervan bir sonraki Depart'a hazir) - :144-152

### `IRecipeInventory` (W33 B06) - `Assets/Scripts/Domain/Process/IRecipeInventory.cs`
- `int CountOf(string tag)` - :12
- `bool TryConsume(string tag, int qty)` - all-or-nothing, kismi tuketim YASAK - :15
- `bool TryAccept(string tag, int qty)` - kapasite reddi mumkun - :18
- `IRecipeInventory CloneForPreflight()` - live container'a dokunmayan bagimsiz kopya - :22

### `StockpileRecipeInventory` (W33 B06) - `Assets/Scripts/Domain/Process/StockpileRecipeInventory.cs`
- Ctor: `StockpileComponent pile` sarar - :11-14
- `TryConsume`: `pile.Get() < qty` ise false; sonra `pile.Remove(qty) == qty` (atomicity BU adapter'in gorevi cunku Remove remove-up-to) - :21-26
- `TryAccept`: negatif reddeder, aksi durumda `pile.Add(qty)`; kapasite yok - :29-35
- `CloneForPreflight`: `Entries` uzerinden Dictionary'ye kopya, `CountsProbe` doner (dictionary-backed) - :38-71

### `InventoryRecipeAdapter` (W33 B06) - `Assets/Scripts/Domain/Inventory/InventoryRecipeAdapter.cs`
- Ctor: `InventoryState inventory, Func<string,InventoryItem> mint` - :17-22
- `CountOf`: `IsEquipment==false` + `TemplateId` Ordinal esitligi olan `Quantity` toplami - :24-31
- `TryConsume`: `qty>=0 && TryRemoveStackable(tag, qty)` - :33-34
- `TryAccept`: mint yoksa false; qty adet `mint(tag)` cagirir, her birinin Quantity==1 + TemplateId eslesme + TryAdd guardini test eder - :37-49
- `CloneForPreflight`: `inventory.Clone()` uzerine ayni mint - :51-53

### `WorldState` uzerindeki ekonomi kokleri - `Assets/Scripts/Domain/World/WorldState.cs`
- `PriceLedger Prices` - :45
- `List<StockpileComponent> Stockpiles` - :46
- `List<TradeRouteDef> TradeRoutes` - :47
- `List<CaravanInstance> Caravans` - :48
- `JobBoard Jobs` - :61
- `InventoryState MerchantInventory` - :202; `int PlayerGold` - :263; `bool MerchantStoreSeeded` - :265
- `StockpileComponent FindStockpile(SiteId)` - :393
- `TradeRouteDef FindTradeRoute(TradeRouteId)` - :398-400

### Konfig - `Assets/Scripts/Domain/Configuration/EmberRuntimeOptions.cs`
- `LowStockThreshold = 4` - :82 (`Math.Max(1, ...)` normalizasyon :256)
- `HighStockThreshold = 64` - :83 (`Math.Max(2, ...)` :257)
- `PriceStep = 1` - :84 (`Math.Max(1, ...)` :258)

## LLD - Fonksiyon Haritasi (imza + file:line + 1 cumle)

### Site-ekonomisi
- `PriceUpdateSystem.Recompute(ledger, stockpile, itemTag, low, high, delta, now, events)` - `Assets/Scripts/Simulation/World/PriceUpdateSystem.cs:16-60` - stok esiklerine gore fiyati +/-delta ayarlar, degisim varsa `PriceChanged` yazar.
- `CaravanSystem.Tick(caravans, resolveRoute, resolveStockpile, now, events)` - `Assets/Scripts/Simulation/World/CaravanSystem.cs:14-104` - her kervani bir adim ilerletir, Idle olani Depart eder (B07), yukleme/teslimat basarisiz olursa stuck event yazip stall eder.
- `CaravanInstance.Depart()` - `Assets/Scripts/Domain/World/CaravanInstance.cs:112-116` - **B07** re-arm: Idle kervani sifirlar ve Loading'e sokar; artik tek atimlik degil.
- `TradeService.TryTrade(ledger, buyerPile, sellerPile, tag, qty, now, events, currencyTag=null, ...)` - `Assets/Scripts/Simulation/World/TradeService.cs:16-84` - iki stok arasi atomik takas; **Codex A/P2** currencyTag verili ama ledger'da satir yoksa reddeder (silent free trade fix); opsiyonel faction reputation delta uygular; `TradeCompleted` emit.
- `ShortageResponseSystem.Tick(world, stamp)` - `Assets/Scripts/Simulation/World/ShortageResponseSystem.cs:23-66` - STATELESS gunluk sweep; kitlik saptar, `ShortageDetected` yazar, dedup+plot+requester gate'lerinden gecerse `CreatePlantingJob` posts + `JobAssigned` yazar.
- `PriceStepSystem.Run(context)` - `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs:527-573` - her stok icin once Entries'in var olan girdileri, sonra **B08 patch** ile defterin bildigi ama Entries'ten dusen kuruk girdileri Recompute eder.
- `ShortageResponseStep` - `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs:391-402` - `econ.shortage_response @Daily:27` adaptor; sira 27 secimi hasat (25) sonrasi / fiyat (30) oncesi kasitli.
- `CaravanStep.Run(context)` - `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs:459-464` - `CaravanSystem.Tick` cagirisi; `world.FindTradeRoute` + `world.FindStockpile` cozunurluklerini enjekte eder.

### W33 B06 kopru
- `WorkOperations.SiteIo(world, siteId) : IRecipeInventory` - `Assets/Scripts/Simulation/Living/Actions/WorkOperations.cs:46-50` - `FarmOperations.FindOrCreatePile` uzerine `StockpileRecipeInventory` sarar; empty siteId'de null.
- `RecipeSystem.TryStartFor(recipe, siteId, position, io, actorId, out order)` - `Assets/Scripts/Simulation/Process/RecipeSystem.cs:66-72` - W33 B06 tag-count overload'i; koy uretimi site stogundan tuketmek icin.
- `RecipeSystem.TryFund(recipe, io) : bool` - `Assets/Scripts/Simulation/Process/RecipeSystem.cs:107` - PerformWork'un fund gate'i; ayni tick'te Tick basariyla donmezse row inputs hakkinda yalan soylemis olur.
- `RecipeSystem.Tick(order, io, eventLog) : bool` - `Assets/Scripts/Simulation/Process/RecipeSystem.cs:183` - tag-count tick; preflight clone tum outputs'un kabul edilebilir oldugunu KANITLAR, sonra gercek accept'e gecer (all-or-nothing).
- `StockpileRecipeInventory.CloneForPreflight()` - `Assets/Scripts/Domain/Process/StockpileRecipeInventory.cs:38-44` - live pile'a dokunmayan Dictionary probe uretir (preflight kural).

### Oyuncu-tuccar ekonomisi
- `SettlementTradeService.EnsureMerchantStock(world, seedItems)` - `Assets/Scripts/Simulation/Inventory/SettlementTradeService.cs:36-56` - MerchantStoreSeeded false ise tohum listesini InventoryItem'lara ekler.
- `SettlementTradeService.ComputeBuyPrice(base, presence)` / `ComputeSellPrice(...)` - :58-66 - Presence normalize (+/-0.18) + faktor (alis 1.20, satis 0.55).
- `SettlementTradeService.TryBuy(world, tag, unitPrice) : TradeOperationResult` - :68-93 - stock var mi + altin yeter mi + envanter alir mi kontrol; basarida `PlayerGold-=`, `MerchantGold+=`, LastNarrative set.
- `SettlementTradeService.TrySell(...)` - :95-120 - simetrik; equip edilmis item satisi reddeder.
- `MerchantTradeService.TradeGateWrit(world) : string` - `Assets/Scripts/Simulation/Inventory/MerchantTradeService.cs:18-74` - Sprint 2 dar takas: ManhattanDistance <=2, Familiarity metnini secer, EmberShard -> GateWrit atomik takasi (herhangi bir adim basarisiz olursa geri sarim).

## LLD - Yazdigi/Okudugu Alanlar (FieldOwnershipRegistry dilinde)

Sadece **ic loop'ta** yazilan alanlar bildirilir (`FieldOwnershipRegistry.cs`). Boot/komut/UI-driven mutation UNDECLARED kalir - registry "kim in-loop yaziyor" kontratidir, "her byte nerede yasar" indeksi degil.

- `World.Stockpiles` yazarlari (`FieldOwnershipRegistry.cs:55-64`):
  - `living.action_advance@PerTick:22` - W32 TakeFood decrement + failure return; W33 HaulCrop deposit + PlantSeed seed take; **W34 PerformWork fund + mint (B06 site pile IO)**.
  - `living.decision@PerTick:18` - **W34 orphan work-order refund** inputs geri site pile'a doner.
  - `living.ambient@Hourly:50` - vermin theft.
  - `world.caravans@Daily:10` - **B03**: CaravanSystem'in Load/Unload (Remove/Add) - GERCEK gunluk trader; `econ.trade` bir hayaletti.
  - Retired: `world.harvest@Daily:25` (W33), `econ.jobs@Hourly:10` (W34 - recipe input+output moved to PerformWork on advance slot).
- `World.Jobs` yazarlari (`FieldOwnershipRegistry.cs:72-77`) - long-standing multi-writer reality W34'te DECLARED:
  - `econ.jobs@Hourly:10` - claim / dead-claimant sweep / ghost-cancel
  - `living.action_advance@PerTick:22` - Complete/Cancel (W33 PlantSeed'den beri de-facto yazar)
  - `econ.shortage_response@Daily:27` - shortage cascade planting job'lari asar
- `World.WorkOrders` (`FieldOwnershipRegistry.cs:66-70`):
  - `living.decision@PerTick:18` - orphan sweep + refund
  - `living.action_advance@PerTick:22` - birth / funding / counter / removal
- **DECLARED DEGIL** (bilincli): `World.Prices` (yalnizca `econ.prices` yazar - tek-yazar), `World.Caravans` (yalnizca `world.caravans`), `World.TradeRoutes` (yalnizca WorldFactory boot + save load), `World.MerchantInventory` / `World.PlayerGold` / `World.MerchantGold` (UI-driven, tick disi).

Okumalar (yayilma icin bilinmesi gerekenler): `PriceStepSystem` -> `world.Stockpiles`+`world.Prices`; `CaravanStep` -> `world.Caravans`+`world.TradeRoutes`+`world.Stockpiles` (Find'ler); `ShortageResponseSystem` -> `world.Stockpiles`+`world.Plants`+`world.Soils`+`world.Actors`+`world.Jobs`; `SettlementTradeService.TryBuy/TrySell` -> `world.PlayerInventory`+`world.MerchantInventory`+`world.PlayerGold`+`world.MerchantGold`+`world.PlayerEquipment`.

## LLD - Urettigi/Tukettigi Olaylar

Uretilenler (`Assets/Scripts/Domain/World/WorldEventKind.cs`):
- `JobAssigned = 5` - econ.jobs assign + econ.shortage_response cascade post + JobBoard.Add sonrasi.
- `JobCompleted = 6` - econ.jobs / PerformWorkAdvancer.
- `JobRefused = 8` - JobAssignmentSystem gate fail.
- `CaravanArrived = 16` - Basarili teslimatta VE stuck event (origin_empty / destination_unavailable) icin ayni kind kullanilir; ayirt payload'ta `caravan_arrived` vs `caravan_stuck` prefix.
- `PriceChanged = 17` - `price_up`/`price_down item:X from:Y to:Z stock:N`.
- `TradeCompleted = 18` - `trade item:X qty:N unit:P buyer_site:B seller_site:S`.
- `ShortageDetected = 19` - `shortage item:X stock:N threshold:4`.
- `TradeRefused = 23` - (henuz TradeService emit etmiyor - referans icin listelendi).

Tuketen: `WorldTickDigestGolden` (byte-stabil), `RuntimeHistorySystem` (H4 - event -> relation drift + monthly chronicle), `FarmStoryChainTests` (kaskad zinciri pin), `LivingWorldGateTests` (CAN SUYU H1+H3 gate), `RumorMillSystem` (dolayli).

## Testler

Bu sistemi pinleyen test dosyalari:
- `Assets/Tests/EditMode/Process/StockpileComponentTests.cs` - Add/Remove/Get/Entries/ctor gates (7 test).
- `Assets/Tests/EditMode/Process/PriceLedgerTests.cs` - Set/Get/Adjust/Contains/site+item bagimsizligi/blank reddi (7 test).
- `Assets/Tests/EditMode/Process/SmeltIronCompletesTests.cs` - `Advance_OverOneDeterministicDay_CompletesSmeltAndProducesIronIngot` (W33 B06 sonrasi InventoryRecipeAdapter pini).
- `Assets/Tests/EditMode/World/CaravanSystemTests.cs` - `Tick_AdvancesEnRouteCaravan_NoArrivalUntilCadenceReached`, `Tick_AtCadence_ArrivesAndDeliversToStockpile`, `Tick_IdleCaravan_NoOp`, `Tick_NullRoute_SkipsCaravan`.
- `Assets/Tests/EditMode/World/TradeBundleTests.cs` - `Trade_HappyPath_MovesStock_EmitsEvent`, `Trade_InsufficientStock_ReturnsFalse_NoEvent` + `ShortageDetected` sondaji.
- `Assets/Tests/EditMode/World/TradeServiceMissingPriceTests.cs` - Codex A/P2 fix'ini pinler: `TryTrade_NoPriceRow_RejectsCurrencyTrade`, `TryTrade_NoCurrencyTag_AllowedEvenWithoutPriceRow`, `TryTrade_PriceRowPresent_StillChargesCurrency`.
- `Assets/Tests/EditMode/Inventory/SettlementTradeServiceTests.cs` - TryBuy/TrySell/EnsureMerchantStock/ComputeBuy(Sell)Price.
- `Assets/Tests/EditMode/Inventory/MerchantTradeServiceTests.cs` - Sprint 2 GateWrit takas kurallari.
- `Assets/Tests/EditMode/Actions/FarmStoryChainTests.cs` - **W33 hikaye testi**: ShortageDetected -> PlantPlanted -> ... zincirini pinler.
- `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs:316` - CAN SUYU H1+H3 gate; ShortageDetected zincirin canli yasadigini kanitlar.
- `Assets/Tests/EditMode/Composition/WorldTickRegistryTests.cs:68` - `Daily:27:econ.shortage_response` slot sirasini pinler.
- `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs` - byte-stabil digest (Prices/Stockpiles/Caravans blok cikislari).
- `Assets/Tests/EditMode/Save/SaveLoadDigestRoundtripTests.cs`, `JsonSliceSaveServiceTests.cs` - ekonomi dilimi cift-yon save roundtrip.
- `Assets/Tests/EditMode/Acceptance/FazSixToTwelveBackendAcceptanceTests.cs` - Faz-6..12 backend acceptance (stockpile + trade + caravan).

## W32-W36 Degisiklikleri

- **W32 (CAN SUYU H1+H3)** - `ShortageResponseSystem` **stateless** ve `Tick(WorldState, GameTime)` catchup imzasina cekildi; instance-state leak golden'i kirmisti (bkz. `Tick(...)` uzerindeki dokuman blogu `ShortageResponseSystem.cs:23-27`). Cascade kaydi: `econ.shortage_response @Daily:27`.
- **W33 (B06 kopru)** - `IRecipeInventory` + `StockpileRecipeInventory` + `InventoryRecipeAdapter` ile RecipeSystem `InventoryState`'ten TAG-COUNT seam'ine gecti. `world.harvest@Daily:25` RETIRED (`DefaultTickSystems.cs:72-73`); hasat artik `HaulCrop` action strip'ten dusuyor. `FarmingJobRequestFactory` (5101 PlantCrop / 5102 HarvestCrop) devreye girdi. B06 bridge kapandi.
- **W34 (WORK slice)** - `econ.jobs@Hourly:10` **stogu artik hic okumaz/yazmaz** (`DefaultTickSystems.cs:250` bloku: `TickAssignedJobs RETIRED`); recipe input+output slot'u `PerformWorkAdvancer @PerTick:22`'ye tasindi. `World.WorkOrders` ledger'i (`WorkOrderLedger`) yaratildi; orphan sweep (living.decision) inputs'i site pile'a REFUND ediyor. `FieldOwnershipRegistry.cs`'e World.WorkOrders ve World.Jobs cok-yazar gercekligi DECLARED.
- **W35 (B03/B04)** - `world.caravans@Daily:10` `World.Stockpiles` yazari olarak REGISTRY'YE deklare edildi (yorum: "REAL daily trader; econ.trade was a ghost"). Reverse-lint tuttu; ekonomi hayalet-adim yalanlarindan temizlendi.
- **W36 (B07/B08 wound-close)** - **B07**: `CaravanInstance.Depart()` re-arm eklendi (`CaravanInstance.cs:110-116`) + `CaravanSystem.Tick` Idle branch'i `Depart` cagirir hale getirildi (`CaravanSystem.cs:32-37`); kervanlar artik tek atimlik degil. **B08**: `PriceStepSystem` fiyati Entries'ten dusen (drained) tag'ler icin de repriceler (`DefaultTickSystems.cs:534-572`); "fiyat kitlikta donuyor" kapandi. Bunlarla ayni turnada **B06** patch'i (SiteIo/StockpileRecipeInventory) confirmed fixed olarak isaretlendi ve W36 batch main'e f6c9e2d0 olarak dustu.

## Bilinen Borclar + Kacak Kapilari

- **`CaravanArrived` cift-anlam**: Basarili teslimat da stuck-event de ayni WorldEventKind'i kullaniyor; ayrim yalniz payload prefix'inde (`caravan_arrived` vs `caravan_stuck`). Hikaye-testleri kind'i sayarsa yaniltir - payload'a bakmali.
- **`TradeService` tick disi**: Kimse cagirmiyor; sadece test-scope + gelecege birakildi (composer'da kayit yok). `TradeRefused = 23` enum'da var ama emit eden yok.
- **World.Prices/Caravans UNDECLARED**: FieldOwnershipRegistry'de yok cunku tek-yazarlar (`econ.prices` ve `world.caravans`). Yeni bir yazar eklenirse reverse-lint yakalamaz - bilincli tek-yazar varsayimi.
- **`ShortageResponseSystem` requester secimi**: `FirstCivilianId` deterministik ama zayif - koyde tum siviller olduyse hicbir kitlik isi asilmaz ("nobody left to want food - the colony is gone" kommenti). Bir felaket sonrasi ekonomi kendini onaramaz.
- **`FreeSoilPositionFor` tarama**: `world.Soils.Rows` tam tarama ve tam ilk-bos plot; buyuk koylerde O(soils) her gun. Kirlanma esiginin altinda ama W37+ olcek testinde gorulmesi kolay.
- **Ledger-Digest bagimliligi**: PriceLedger.Entries siralamasi Ordinal ve stable; buna dokunan bir refactor `WorldTickDigestGoldenTests`'i sessizce kirar (byte-stabil kontrat).
- **Oyuncu-tuccar ekonomisi UI'ya bagli**: `MerchantStoreSeeded` bool'u sim durumunda ama seeding trigger'i `EnsureMerchantStock` cagrisina bagli - sahne actigi yerden cagrilmazsa merchant bos kalir. `LivePriceOr` kopru sadece adapter tarafindan cagriliyor; sim asla PlayerGold'a bakmaz.
- **B07 sonrasi caravan starvation**: Kervan Idle -> Depart sonrasi CadenceDays boyunca origin stoklandiginda payload cikacak; ama origin bos ise `caravan_stuck reason:origin_empty` yazip stall eder. Su an bunun kesif metrikleri yok; sadece test kanit.

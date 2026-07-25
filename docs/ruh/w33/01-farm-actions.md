# W33 / 01 — Farm Eylem Dağarcığı: MoveToPlot, PlantSeed, HarvestCrop, HaulCrop

> RUH_TESHIS §9 ikinci dikey dilim: `seed item → plot rezervasyonu → çiftçinin yürüyüşü →
> Plant action → gerçek plant instance → growth → Harvest action → crop item aktörün elinde →
> Haul action → stockpile → meal`. Bu belge W32 EAT diliminin (5049d445) kalıplarını
> YENİDEN KULLANIR, yeniden icat etmez: aynı struct, aynı ledger, aynı advancer şablonu,
> aynı log dilbilgisi. Anayasa değişmedi: determinizm, all-zero-extends-Idle save uyumu,
> chunking invariance hakemliği, düşük LOC, kısıt açıklayan yorumlar.

## Karar özeti

| Konu | Karar |
|---|---|
| Intent | `ActorIntent`'e append: `Plant = 2`, `Harvest = 3` ("FarmIntent" çifti; tek `Farm` değeri DEĞİL — §2.1) |
| Action | `ActorActionType`'a append: `MoveToPlot = 4`, `PlantSeed = 5`, `HarvestCrop = 6`, `HaulCrop = 7` |
| Failure | `ActionFailureReason`'a append: `PlotTaken = 7`, `CropGone = 8`; `ActionLogReason`'a append: `PlotTaken = 10`, `CropGone = 11` |
| Plot rezervasyonu | `ReservationLedger` DEĞİŞMEDEN kullanılır; tag anahtarı `"plot:{soilId}"`, `pileCount: 1` → hücre başına münhasırlık (§4) |
| Taşınan ürün | W32 taşıma mekanizması genişler: tag'i rezervasyon satırı taşır (`"carry:{cropTag}"` satırı), ADEDİ yeni `CarriedUnits` int alanı taşır (§6) |
| Tohum kaynağı | Site `StockpileComponent`'i — `PlayerInventory` DEĞİL (RUH_TESHIS §8 madde 6). `TryPlant`'a delege-enjeksiyonlu overload (§7.2) |
| Tohum tag'i | Dilimde `SpeciesId` ("wheat") — "tohumluk buğday" kuralı; `SeedItemTag`'in canlı kaynağı yok (§7.2 kanıt) |
| Hasat çıktısı | Önce ELE (`CarriedUnits`), sonra HaulCrop ile pile'a; `world.harvest` fiat adımı EMEKLİ OLUR (§8) |
| Save | `ActorSaveData`'ya 1 alan: `actionCarriedUnits` (default 0 = Idle); rezervasyon satır şeması değişmez (§9) |
| Digest | Aktör satırına `CarriedUnits` eklenir; goldenlar bir kez yeniden baseline'lanır (§9.3) |

---

## 1. Kanıt: bugünkü çiftlik gerçeği (okunan kod)

- `Assets/Scripts/Domain/World/WorldState.cs:59-60` — `Plants` ve `Soils` ComponentStore'ları
  var; `:179` `Reservations` ledger'ı W32'den yaşıyor; `:143` `PlayerInventory` bir
  `InventoryState`.
- `Assets/Scripts/Simulation/World/WorldFactory.cs:135-144` — canlı dünya: site 5'te
  `stallStock.Add("wheat", 320)` + üç `PlantComponent` (id 9001-9003, pozisyon (7..9, 4)).
  DİKKAT: bu plotların ARKASINDA SoilComponent YOK — `TryPlant`'ın soil-tabanlı akışı canlı
  dünyada hiç çalışmadı (§9.4'te iyileştirme).
- `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs:454-503` — `world.harvest@Daily:25`
  fiat adımı: ripe plot → `pile.Add(p.SpeciesId, 2)` + `PlantStageId("seed")` ile bedava replant.
  Eller `HarvestHandsService.FindHarvester` ile YALNIZCA seçilir; yürüyüş, iş, taşıma yok.
  RUH_TESHIS'in yasakladığı teleport tam olarak budur ("Plant output doğrudan stockpile'a
  teleport olmaz", §10).
- `DefaultTickSystems.cs:503-541` — `econ.plantgrowth@Daily:20`; büyüme W33'te DEĞİŞMEZ.
- `Assets/Scripts/Simulation/Process/PlantingSystem.cs:16-30` — imza:
  `TryPlant(PlantSpeciesDef species, ComponentStore<SoilComponent> soils, ComponentStore<PlantComponent> plants,
  WorldComponentId soilId, WorldComponentId plantId, InventoryState inventory, WorldEventLog eventLog, GameTime now)`.
  Tohum `inventory.TryRemoveStackable(species.SeedItemTag, 1)` ile düşülür — bir `InventoryState`
  ister; oysa kasabanın malı `StockpileComponent` sayaçlarında (§7.2 çözer).
- `Assets/Scripts/Simulation/Process/HarvestSystem.cs` — `TryHarvest` çıktıyı DOĞRUDAN bir
  `InventoryState`'e ekler; "önce el" kuralına aykırı, advancer bunu ÇAĞIRMAZ (§7.3).
- `Assets/Scripts/Simulation/Composition/WorldTickComposer.cs:106-121` — wheat türü:
  `SeedItemTag = "wheat_seed"`, `HarvestItemTag = "wheat_grain"`, evreler `seed → sprout → ripe`
  (gün başına bir evre), `ripe.IsHarvestable = true`.
- `Assets/Scripts/Simulation/Living/FoodPileCache.cs:27-35` — yemek tag evreni `SpeciesId`
  üzerinden ("wheat"); pile'larda `"wheat_seed"` veya `"wheat_grain"` HİÇ dolaşmıyor.
- W32 mirası (aynen yeniden kullanılacak parçalar): `ActorActionState` struct + geçişleri,
  `ReservationLedger` (satır = site + tag + aktör + TTL, aktör başına EN FAZLA 1 satır),
  `ActionAdvancer` şablonu (probe → step → `TransitionTo` tek dikiş), `ActionLifecycleSystem`
  Decide(@PerTick:18)/Advance(@PerTick:22) tek-yazar çifti, `ActionLogManager` faz-sınırı
  log dilbilgisi.

---

## 2. Sözcük dağarcığı genişlemesi (hepsi append-only)

Save'e int yazılan her enum'da değerler SABİTTİR; silme/yeniden numaralama yasak
(`ActorActionState.cs` başlık yorumu). Tüm eklemeler mevcut son değerin ARKASINA gelir.

```csharp
// Assets/Scripts/Domain/Actors/ActorActionState.cs — mevcut üyeler aynen kalır.
public enum ActorIntent { None = 0, Eat = 1, Plant = 2, Harvest = 3 }

public enum ActorActionType
{
    None = 0, MoveToFood = 1, TakeFood = 2, ConsumeFood = 3,
    MoveToPlot = 4, PlantSeed = 5, HarvestCrop = 6, HaulCrop = 7,
}

public enum ActionFailureReason
{
    None = 0, NoFoodFound = 1, ReservationLost = 2, Unreachable = 3,
    Interrupted = 4, TimedOut = 5, SourceDrained = 6,
    PlotTaken = 7,   // plot'ta beklenmeyen bitki: benden önce ekilmiş / doğrulama kaybı
    CropGone = 8,    // hasat hedefi bitki yok ya da artık harvestable değil
}

// Assets/Scripts/Domain/Actors/Actions/ActionLogEntry.cs
public enum ActionLogReason
{
    /* 0..9 W32 aynen */, PlotTaken = 10, CropGone = 11,
}
```

`ActionAdvancer.ToLogReason`'a iki satır: `PlotTaken → ActionLogReason.PlotTaken`,
`CropGone → ActionLogReason.CropGone`. Hikâye hammaddesi kaba `TargetGone`'a EZİLMEZ —
"Ayşe'nin plotunu Mehmet kapmış" ile "ekin çoktan toplanmış" iki ayrı cümledir.

### 2.1 Neden tek `Farm` intent'i değil

`ActionLifecycleSystem.NextLink` zincirin sıradaki halkasını türetir ve W33'te
`MoveToPlot`'un ardılı intent'e bağlıdır (ek → `PlantSeed`, hasat → `HarvestCrop`).
Tek `Farm` değeri bu çatalı çözmek için ek bir saved alt-mod alanı isterdi — daha çok save
yüzeyi, daha çok TryRestore kuralı. İki intent değeri MEVCUT alanı bedavaya kullanır:

```csharp
// ActionLifecycleSystem — W32 imzası genişler: NextLink(intent, action).
// EAT satırları aynen; chain SAVED DEĞİLDİR, intent'ten türetilir (W32-01 §8 kuralı).
private static ActorActionType NextLink(ActorIntent intent, ActorActionType current) => (intent, current) switch
{
    (ActorIntent.Eat, ActorActionType.MoveToFood) => ActorActionType.TakeFood,
    (ActorIntent.Eat, ActorActionType.TakeFood) => ActorActionType.ConsumeFood,
    (ActorIntent.Plant, ActorActionType.MoveToPlot) => ActorActionType.PlantSeed,
    (ActorIntent.Harvest, ActorActionType.MoveToPlot) => ActorActionType.HarvestCrop,
    (ActorIntent.Harvest, ActorActionType.HarvestCrop) => ActorActionType.HaulCrop,
    _ => ActorActionType.None,
};
```

`ActionAdvancerRegistry` kurucusundaki dizi boyu `(int)ActorActionType.ConsumeFood + 1`
→ `(int)ActorActionType.HaulCrop + 1` (tek satır; kayıt sırası davranışı etkilemez).

---

## 3. Alan anlamları: farm eylemlerinde `ActorActionState`

W32 kuralı korunur: struct YALNIZCA id taşır, string TAŞIMAZ; string gerçeği rezervasyon
satırındadır (`TakeFoodAdvancer` design note'u: "the claim row carries the taken unit's tag").
Yeni TEK alan `CarriedUnits`'tir.

| Alan | EAT anlamı (W32) | FARM anlamı (W33) |
|---|---|---|
| `CurrentIntent` | `Eat` | `Plant` veya `Harvest` |
| `TargetSiteId` | rezerve pile'ın sitesi | tarlanın (soil hücresinin) sitesi; HaulCrop'ta varış pile'ının sitesi de budur (site-yerel depo) |
| `TargetItemId` | TakeFood'da mint edilen birim | HER ZAMAN `Empty` — plot kimliği rezervasyon anahtarından parse edilir (§4.2); yeni alan icat edilmez |
| `ReservationId` | pile claim satırı | plot satırı (MoveToPlot/PlantSeed/HarvestCrop) → carry satırı (HaulCrop; §6) |
| `ProgressTicks` | faz içi sayaç | aynı — yalnız Running'de, yalnız advancer artırır |
| `CarriedUnits` (YENİ) | hep 0 | HarvestCrop commit'inden HaulCrop deposit'ine kadar eldeki ürün adedi; diğer her yerde 0 |

```csharp
// ActorActionState'e eklenen alan + geçiş — CarryingItem kalıbının aynısı.
/// <summary>Eldeki hasat adedi; yalnız HarvestCrop/HaulCrop yaşam aralığında sıfırdan farklı.</summary>
public int CarriedUnits { get; }
public ActorActionState WithCarriedUnits(int units); // aktif eylem ister; units < 0 exception
```

"None ⇒ tümü sıfır" invariantı genişler: kurucu assert'ine ve `TryRestore`'a
`carriedUnits` katılır (§9.2). `default(ActorActionState)` yine Idle'dır — yeni alanın
default'u 0 olduğundan W32 VE W32-öncesi save'ler değişiklik hissetmeden yüklenir.

---

## 4. Plot rezervasyonu: `ReservationLedger`'ın soil anahtarıyla yeniden kullanımı

### 4.1 Anahtar kodlaması

Ledger'ın satırı `(SiteId: ulong, ItemTag: string, ActorId, UntilMinutes)`; kapasite sorgusu
`pileCount - ReservedCount(site, tag) > 0` (`ReservationLedger.TryReserve`). Ledger'a TEK
satır kod eklenmez; anlam tamamen anahtar seçimiyle kurulur:

| Satır türü | `SiteId` | `ItemTag` | `TryReserve.pileCount` | Anlam |
|---|---|---|---|---|
| Plot satırı | tarlanın sitesi | `"plot:" + soilId.Value` (invariant ondalık) | `1` | Bu soil hücresi bu aktöre ayrıldı; kapasite 1 ⇒ ikinci istekte `1 - 1 <= 0` → false → münhasırlık YAPISAL |
| Carry satırı | varış pile'ının sitesi | `"carry:" + cropTag` | `int.MaxValue` | Elde `CarriedUnits` adet `cropTag`, şu pile'a borçlu; kapasite anlamsız, satır tag+TTL taşıyıcısıdır |

Kodlama kuralları (FarmOperations'ta tek ev, §7.1):

- **Namespace çakışmazlığı.** Canlı item tag evreni çıplak kimliklerdir ("wheat", "iron",
  "coin" — `WorldFactory.cs:131-135`); `"plot:"`/`"carry:"` önekleri bu evrene giremez ve
  `FoodPileCache.FoodTags` önekli tag üretemez. KISIT yorumu her iki üretim noktasına yazılır:
  önekli bir tag'in `StockpileComponent`'e sızması effektif-stok matematiğini bozar.
- **Carry neden `cropTag` değil de `"carry:" + cropTag`:** çıplak `("site", "wheat")` carry
  satırı `ReservedCount(site, "wheat")`'i şişirir ve yemek kararının effektif stok hesabını
  (`ActionLifecycleSystem.TryDecideEat`) YANLIŞ yönde düşürürdü — taşınan ürün mevcut stoğu
  tüketmez, gelecek stok ekler.
- **`soilId` neden pozisyon değil:** `WorldComponentId` save-boyunca stabil, tekil ve
  `TryPlant`'ın zaten istediği anahtar; pozisyon iki plot'un taşınması/yeniden döşenmesinde
  kimlik karışması riski taşır.
- **Format:** `ulong.ToString(CultureInfo.InvariantCulture)`; ledger tag'i asla parse etmez,
  yalnız FarmOperations parse eder (§4.2). Padding gerekmez — eşitlik bayt eşitliğidir.

### 4.2 Anahtar çözümü (parse) — ekstra alan yerine

Her advancer adımı ZATEN `TryGetByActor` ile satırı çekip `row.Id == state.ReservationId`
doğruluyor (W32 şablonu). Plot advancer'ları soil kimliğini AYNI satırdan çözer:

```csharp
// FarmOperations — deterministik, alloc'suz; başarısız parse = bozuk satır = ReservationLost.
public static bool TryParsePlotKey(string itemTag, out WorldComponentId soilId);
public static bool TryParseCarryKey(string itemTag, out string cropTag);
```

Böylece `ActorActionState`'e ne string ne ikinci ulong alan girer; save/digest yüzeyi
`CarriedUnits` ile sınırlı kalır (düşük LOC + all-zero disiplini).

### 4.3 TTL

W32-02 §4.3 formül ailesi aynen: `until = now + yürüyüş(Chebyshev) + işTickleri + 60 slack`.
Plot satırı zincir başında (Decide) alınır ve zincir sonunda ya commit'te el değiştirir
(§6) ya da Fail kapısında TEK sefer bırakılır. `SweepExpired` güvenlik ağı değişmez;
süpürülen carry satırının ürünü taşıyıcısıyla birlikte kaybolur (ölüm/mis-TTL istisnası —
W32'nin bilinçli kabulüyle aynı sınıf, loglanır, nadir).

---

## 5. Zincirler ve faz süreleri

İki zincir, W32'nin "geçiş tick tüketir" tekdüzeliğiyle:

```text
Plant  : Decide(rezerv) → MoveToPlot (değişken) → PlantSeed (2 tick) → Idle
Harvest: Decide(rezerv) → MoveToPlot (değişken) → HarvestCrop (2 tick) → HaulCrop (değişken) → Idle
```

| Eylem | Süre | Sabitin evi | Bitiş koşulu |
|---|---|---|---|
| `MoveToPlot` | 1 hücre/tick, Chebyshev | `MovementService.StepToward` (mevcut) | aktör soil hücresinin ÜZERİNDE — plot rezerve olduğundan Gate8 istifi imkânsız, ring koltuk gerekmez |
| `PlantSeed` | `PlantDurationTicks = 2` | `PlantSeedAdvancer` (tek ev, `ConsumeDurationTicks` emsali) | son tick'te atomik commit (§7.2) |
| `HarvestCrop` | `HarvestDurationTicks = 2` | `HarvestCropAdvancer` | son tick'te atomik commit: bitki sökülür + `CarriedUnits` dolar + satır takası (§6) |
| `HaulCrop` | 1 hücre/tick site merkezine | — | `FoodOperations.WithinEatReach` menziline girince AYNI adımda deposit commit + Succeeded |

`HaulCrop` bilinçli olarak yürüyüş + terminal commit'i birleştirir: ayrı bir `DepositCrop`
üyesi 1-tick'lik gövde için enum + advancer + kayıt maliyeti öderdi (düşük LOC kısıtı;
emsal: `ConsumeFood` da faydayı son tick'inde commit'ler). Yemek zamanlaması bozulmaz;
her farm advancer'ı de `ActionAdvancer.Advance` şablonundan geçer, pursuit probe'u bedava alır.

Örnek zaman çizgisi (plot 3 hücre uzakta): T karar+rezerv+MoveToPlot start,
T+1..T+3 yürüyüş (T+3 varış = Succeeded geçişi), T+4 HarvestCrop ilk tick, T+5 commit
(eller dolu), T+6.. haul yürüyüşü, menzile giriş tick'inde deposit + Completed, sonraki
tick Idle'a devri teslim. Chunking invariance hakemi bu çizgiyi tek-tick replay'de bire bir
ister — advancer'larda çağrılar arası statik durum YASAK (W32 şablonu bunu yapısal kılar).

---

## 6. Taşınan ürün: W32 mekanizmasının genişletilmesi

Karar: **yeni bir "el envanteri" YOK; W32'nin ikilisi genişler** — tag'i rezervasyon satırı,
adedi `ActorActionState.CarriedUnits` taşır.

HarvestCrop commit'i (tek Step içinde, atomik):

1. Doğrula: satır plot satırı, soil çözülüyor, `soil.PlantId` dolu, tür evresi
   `IsHarvestable` (değilse `CropGone` fail).
2. `plants.Remove(plantId)` + `soils.Replace(soilId, soil.WithoutPlant())` — bitki dünyadan
   çıkar, plot boşalır.
3. `PlantHarvested` event'i AKTÖR adına (fiat adımın `by:{hands.Id}` satırının halefi;
   RumorMill/anlatı tüketicileri kopmaz).
4. Satır takası: `Release(plotRow.Id)` → `TryReserve(TargetSiteId, "carry:" + cropTag, actor,
   until, int.MaxValue, out newId)`. Aktör başına tek-satır kuralı sayesinde release
   ÖNCE gelmek zorundadır; iki çağrı aynı Step içinde olduğundan hiçbir sistem ara durumu
   gözlemleyemez. Yeni id `state.Start(...)` yoluna değil, `WithCarriedUnits(HarvestYieldUnits)`
   + ReservationId güncellemesiyle AYNI transition'a yazılır.
5. `HarvestYieldUnits = 2` — fiat adımın verimiyle bire bir (ekonomi kalibrasyonu oynamaz);
   `cropTag = plant.SpeciesId` — pile evreni SpeciesId'dir (§1 kanıt), `HarvestItemTag`
   ("wheat_grain") item-instance dünyasına REZERVE kalır.

HaulCrop deposit commit'i: varış pile'ı `FindPile(TargetSiteId)`, yoksa fiat adımın
yaptığı gibi OLUŞTURULUR (`DefaultTickSystems.cs:486-490` emsali); `pile.Add(cropTag,
CarriedUnits)`, `Release(carryRow.Id)`, `WithCarriedUnits(0)`, Succeeded.

Fail kapısı genişlemesi (`ActionAdvancer.Fail`): mevcut ConsumeFood iade dalının yanına
carry dalı gelir — satır `"carry:"` önekliyse `FindPile(row.SiteId)?.Add(cropTag,
state.CarriedUnits)` + `WithCarriedUnits(0)` sonra `Failed(reason)`. Madde korunumu >
gerçekçilik: düşürülen ürün "bağlı olduğu pile'a süpürülür" (W32'nin ConsumeFood iadesi de
aynı sınıf bir geri-ışınlamadır; RUH_TESHIS'in yasağı NORMAL yol içindir, hata yolu değil).

---

## 7. Advancer tasarımı ve `TryPlant` çağrısı

### 7.1 FarmOperations — FoodOperations'ın aynası

`Assets/Scripts/Simulation/Living/Actions/FarmOperations.cs` (internal static):
`TryParsePlotKey` / `TryParseCarryKey` / `PlotKey(soilId)` / `CarryKey(cropTag)`,
`FindSoil(world, soilId)`, `SpeciesFor(catalog, speciesId)`. Pile bulma ve menzil için
MEVCUT `FoodOperations.FindPile` + `WithinEatReach` yeniden kullanılır — deposit menzili ==
yemek menzili, tek sabit, formül çatallanmaz (`FoodOperations` design note'u ile aynı gerekçe).

Tür kataloğu enjeksiyonu: `ActionLifecycleSystem` kurucusu
`IReadOnlyList<PlantSpeciesDef>` alır; `DefaultTickSystems.cs:43` kuruluş noktasında
`PlantGrowthStep`'e giden liste (`WorldTickComposer.BuildDefaultPlantSpecies`) AYNEN buraya
da verilir — iki sistem tek katalog okur, tür gerçeği çatallanamaz.

### 7.2 PlantSeedAdvancer — "gerçek envanter: kimin?"

Cevap: **kimsenin `InventoryState`'i değil; tarla sitesinin `StockpileComponent`'i.**
`WorldState.PlayerInventory` OYUNCUNUN cebidir — NPC üretimini oradan beslemek RUH_TESHIS
§8 madde 6'nın şikâyetinin ta kendisidir ("NPC üretimi player inventory yerine
worksitenin/stockpile'ın gerçek container'ını kullanır"). Kasabanın tohumu, W32'de ekmeğin
yaşadığı yerde yaşar: site pile'ının tag sayaçlarında.

Empedans çözümü — `PlantingSystem.TryPlant` bir `InventoryState` istiyor; stockpile ise
sayaç. Bitki yaratma + event bloğunu KOPYALAMAK yerine tohum-tüketim dikişi delege olur:

```csharp
// PlantingSystem — yeni overload; mevcut imza tek satırla buna delege eder,
// mevcut testler ve InventoryState çağrıcıları davranış değişmeden derlenir.
public bool TryPlant(PlantSpeciesDef species, ComponentStore<SoilComponent> soils,
    ComponentStore<PlantComponent> plants, WorldComponentId soilId, WorldComponentId plantId,
    Func<bool> takeSeed, WorldEventLog eventLog, GameTime now);
// eski imza: takeSeed = () => inventory.TryRemoveStackable(species.SeedItemTag, 1)
```

`PlantSeedAdvancer` commit'i (son tick):

```csharp
// KISIT (tohumluk buğday kuralı): dilimde tohum tag'i = species.SpeciesId. Kanıt: pile
// evreni SpeciesId'dir (WorldFactory "wheat" 320; fiat hasat SpeciesId ekler) ve
// "wheat_seed"in CANLI hiçbir kaynağı yok — SeedItemTag'i şart koşmak, fiat replant
// emekli olur olmaz ekonomiyi ilk ekimde açlığa kilitlerdi. Ekin kendi tohumudur;
// "tohumluğunu yeme" gerilimi bedavaya doğar (aynı (site,"wheat") sayacını yemek
// rezervasyonlarıyla paylaşır). Gerçek tohum item'ları ileri dilim (§11).
var pile = FoodOperations.FindPile(world, state.TargetSiteId.Value);
if (pile == null || pile.Get(seedTag) <= 0) { Fail(world, actor, SourceDrained, stamp); return; }
if (soil.HasPlant) { Fail(world, actor, PlotTaken, stamp); return; }
var planted = _planting.TryPlant(species, world.Soils, world.Plants, soilId,
    FarmOperations.PlantIdFor(soilId), () => pile.Remove(seedTag, 1) == 1, world.Events, stamp);
if (!planted) { Fail(world, actor, PlotTaken, stamp); return; } // yarış: doğrulama ile commit arası
world.Reservations.Release(row.Id);                              // plot görevi bitti
TransitionTo(world, actor, progressed.Succeeded(), ActionLogReason.Completed, stamp);
```

Tohum yolculuğu dilim yumuşaklığı: çiftçi tohumu AYNI SİTENİN pile'ından "yanında getirmiş"
sayılır (pile'a ayrı yürüyüş yok); tohum REZERVE EDİLMEZ (aktör başına tek satır kuralı plot
satırına harcandı) — commit anında yeniden doğrulanır, kaybeden `SourceDrained` ile yeniden
planlar (W32 mid-route drain yumuşaklığının aynısı, KISIT yorumu koda yazılır).

`plantId` türetimi: `FarmOperations.PlantIdFor(soilId) = new WorldComponentId(PlantIdBase +
soilId.Value)`, `PlantIdBase = 500_000UL`. Bir plot'ta aynı anda en fazla bir bitki
(`soil.HasPlant` kapısı) olduğundan kimlik çakışmasızdır, replant aynı id'yi güvenle yeniden
kullanır (store'dan silinmişti), yeni bir persistent sayaç (NextComponentId benzeri) ve save
alanı GEREKMEZ. Fabrika id'leri (9001-9003) taban altında kalır — ilk hasatla emekli olurlar.

### 7.3 HarvestCropAdvancer / HaulCropAdvancer

`HarvestSystem.TryHarvest` ÇAĞRILMAZ: çıktıyı doğrudan `InventoryState`'e basar; RUH_TESHIS
§8 madde 8 çıktının ÖNCE elde doğmasını emreder. Sökme + doğrulama §6 commit listesindeki
beş adımdır; `HarvestSystem`/eski `TryPlant` imzası test-and-tool yüzeyi olarak yaşamaya
devam eder. Her iki advancer'ın adım başı doğrulama sırası W32 kalıbıdır: satır → dünya
nesnesi → menzil → iş; ihlalde `Fail` (tek sefer release + iade + `Failed(reason)`).

Ara-yol doğrulamaları (`MoveToPlot`, her adım — MoveToFood'un mid-route drain emsali):
satır/parse kaybı → `ReservationLost`; soil yok → `ReservationLost`; intent `Plant` iken
`soil.HasPlant` → `PlotTaken`, site pile'ında tohum kalmadı → `SourceDrained`; intent
`Harvest` iken bitki yok/evre harvestable değil → `CropGone`. Interrupt politikası tüm
farm eylemlerinde `Interruptible` (tarla tehlikelidir; av probe'u yemekteki gibi işi keser,
carry iadesi §6 ile madde korunur).

---

## 8. Fiat adımın emekliliği: `world.harvest`

W32'nin `living.eatOnArrival` emekliliği neyse (W32-02 §5.2), W33'te `HarvestStep` odur.
Kalırsa iki yazar aynı bitkiyi yönetir: fiat adım günlük 25'te söker-replantlarken action
zinciri T+5'te `CropGone` yer — tek-yazar tezi (RUH_TESHIS §8 madde 10) ihlal olur.
Emeklilik paketi: adım kaydı silinir; replant görevi Plant zincirine geçer (Decide: boş +
rezervesiz soil ve sitede tohum varken `Plant` intent'i — üretim kuralları W33/02'nin
konusu); `PlantHarvested` event satırı aktörlü biçimiyle HarvestCrop commit'inden akmaya
devam eder. Geçiş riski §12'de.

---

## 9. Save + digest (all-zero deseni)

### 9.1 ActorSaveData

`WorldSaveData.ActorDungeon.cs` W32 blokunun (:128-137) sonuna TEK alan:

```csharp
public int actionCarriedUnits; // W33: eldeki hasat adedi; 0 = eller boş (Idle ile bit uyumlu)
```

`ActorSaveMapper` yazma blokuna (:72-81) `actionCarriedUnits = actor.ActionState.CarriedUnits`;
`ActorActionStateSaveReader.Read` yeni parametreyi `TryRestore`'a geçirir. Presence bayrağı,
schema bump YOK: W32 ve öncesi save'lerde alan 0 deserialize olur, 0 = "eller boş" zaten
doğru gerçektir. Rezervasyon satır şeması DEĞİŞMEZ — `"plot:..."`/`"carry:..."` mevcut
string `ItemTag` kolonundan geçer (digest `AppendStringField` dahil, `WorldStateDigest.cs:481`).

### 9.2 TryRestore genişlemesi

Aralıklar: `intent <= Harvest`, `action <= HaulCrop`, `failureReason <= CropGone`.
Çapraz kurallar (yeni):

- `carriedUnits < 0` → false; `action == None && carriedUnits != 0` → false ("None ⇒ tümü sıfır").
- `carriedUnits > 0` yalnız `action ∈ {HarvestCrop, HaulCrop}` iken geçerli — PlantSeed veya
  EAT eylemi elinde ürünle restore edilemez (geçiş-erişilemez durum, W32 kuralı: bozuk blok
  sessizce yarım yüklenmez, Idle'a normalize edilir).
- Sarkan `actionReservationId` W32'deki gibi BURADA çözülmez; ilk advancement ledger'a
  karşı doğrular ve `Failed(ReservationLost)` üretir (`ActorSaveMapper.cs:166-168` yorumu).

### 9.3 Digest + golden

`WorldStateDigest` aktör satırına `StartedAtMinutes`'ten sonra `AppendIntField(CarriedUnits)`
(W32 zihin-alanları yorumunun devamı: eldeki ürün ayrışması chunking hakemine görünmezse iki
dünya aynı digest'i paylaşır). Plants/Soils/Reservations bölümleri ZATEN digest'te
(`:42-43`, `:50`) — plot/carry satırları bedava izlenir. Goldenlar bir kez, meşru olarak
yeniden baseline'lanır (W32 emsali). Golden roundtrip dünyasına non-default bir farm durumu
eklenir: elleri dolu (CarriedUnits=2, carry satırlı) bir HaulCrop aktörü — en çok alan
dolduran nokta.

### 9.4 Soil iyileştirmesi (fabrika + yükleme)

- `WorldFactory` üç plot'un altına `SoilComponent` döşer (id `8001-8003`, site 5, aynı
  pozisyonlar, fertility/moisture 50, `WithPlant(9001..9003)` bağlı) — plot anahtarının
  gösterdiği satır artık canlı dünyada VAR.
- `WorldState.EnsureInvariants` öksüz bitkileri iyileştirir: pozisyonunda soil olmayan her
  bitki için deterministik soil sentezlenir (`id = OrphanSoilBase(600_000) + plantId.Value`,
  bitki-id sırasıyla) — eski save'ler fiat-emekliliğinden sonra sonsuza dek "ripe bekleyen
  ama asla hedef olamayan" bitkiyle kalmaz (`RebuildIndexes` ile aynı normalize-on-load ailesi).

---

## 10. Hikâye testleri (Assets/Tests/EditMode/Actions, W32 T-serisinin devamı)

| # | Hikâye | Sözleşme |
|---|---|---|
| F1 | Son plot iki çiftçiye verilmez | ikinci `TryReserve("plot:N", pileCount:1)` false; ikinci aktör intent üretmez |
| F2 | Tohumsuz ekim olmaz | pile'da 0 birim → PlantSeed commit `SourceDrained`; bitki DOĞMAZ (input yoksa output yok, RUH_TESHIS §10) |
| F3 | Ekim başarısızlığı tohum yakmaz | `takeSeed` çağrısı yalnız tüm kapılardan sonra; fail yolunda pile sayacı değişmemiş |
| F4 | Uzaktan hasat yok | plot hücresine varmadan HarvestCrop koşamaz (zincir yapısal); nudge ile menzil dışına itilen haul `Fail` + iade |
| F5 | Kesilen haul ürün kaybetmez/çoğaltmaz | pursuit interrupt mid-haul → pile toplamı + eller = hasat öncesi bitki değeri (madde korunumu, W32 T5'in farm ikizi) |
| F6 | "Ekin çoktan toplanmış" | başka el bitkiyi söktü → MoveToPlot/HarvestCrop `CropGone`, log'da `CropGone` |
| F7 | "Plotumu kapmışlar" | doğrulama-commit yarışı → `PlotTaken`; plot satırı tek sefer released |
| F8 | Zincir ortasında save/load | HaulCrop + CarriedUnits=2 roundtrip: aynı id, aynı faz, aynı carry satırı; digest eşit |

PerTick bandındaki her yeni advancer `CadenceChunkingInvarianceTests` sözleşmesine
OTOMATİK girer — hakem değişmedi.

---

## 11. İleri notlar (bu dilimin DIŞI)

- Gerçek tohum item'ları (`SeedItemTag` ekonomisi): TakeSeed bacağı + tohum carry — carry
  satırı deseni hazır, tek tag sabiti değişir.
- Yere düşen ürün (ground drop): count-dünyasında zemin pile'ı; Fail iadesinin
  "varış pile'ına süpürme" yumuşaklığını kaldırır.
- Çok-birimli rezervasyon satırları (Units kolonu): bugün gerekmedi — carry adedi
  `CarriedUnits`'te, plot kapasitesi 1.
- Sulama/gübre eylemleri: `Moisture/Fertility` alanları duruyor; aynı dağarcık kalıbıyla
  (`WaterPlot = 8, ...`) açılır.

## 12. Riskler

- **Fiat emekliliği + karar boşluğu:** `HarvestStep` emekli olup Decide farm intent'i
  üretmezse ekonomi durur. W33/02 (karar kuralları) bu belgeyle AYNI dilimde inmek zorunda;
  gate/marathon koşusu "pile toplamı gün N'de artıyor" kanıtı ister (render-layer doğrulama
  protokolü geçerli).
- **Tohumluk = yemeklik geriliminin ayarı:** açlar ve ekiciler aynı `(site,"wheat")`
  sayacını tüketir; kıtlıkta ekim hiç başlayamayabilir (bilinçli hikâye — ama gate eşiği
  buna göre kalibre edilmeli).
- **Digest/golden yeniden baseline:** meşru ama tek seferde, izole commit'te yapılmalı
  (W29 dersinin tekrarı: baseline gürültüsü gerçek regresyonu gizler).

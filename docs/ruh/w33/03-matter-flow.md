# W33 — DOC 3: Madde Akışı (Matter Flow) — FARM Dikey Dilimi: Hasadın Fiziksel Yolculuğu

> RUH_TESHIS §2.8'in hükmü: *"Madde dünyada yolculuk yapmaz; sayaçlar arasında teleport olur."*
> Bu doküman o cümleyi emekli eder. Ürün artık bitkiden **aktörün eline**, elden **yürüyerek**
> stockpile'a gider; bitki seed'e yalnızca **gerçek bir HarvestCrop tamamlanışıyla** döner.
> Desen envanteri W32 EAT diliminden AYNEN devralınır (yeniden icat yok): ActorActionState +
> tek-yazar lifecycle + IActionAdvancer Strategy + ActionLogManager + ReservationLedger.

---

## 0. Kapsam ve komşu doküman varsayımları

Bu doküman şunları tasarlar:

1. **Taşınan yük**: hasat verimi aktörün üzerinde (carried, kapasiteli) — yeni saved alanlar.
2. **CropOperations**: bitki→el→pile madde mutasyonlarının TEK doğrulanmış kapısı.
3. **Üç advancer**: `MoveToPlot → HarvestCrop → HaulCrop` faz makinesi ve her fazın madde etkisi.
4. **Teleport emeklilikleri**: `HarvestStep`'in satır-içi `+2`-ve-reset'i, self-replant,
   `HarvestHandsService` (dosya:satır ile, §1 ve §10).
5. **Madde korunumu invariantları**: DOC 6 (story tests) bu listeden yazılır (§9).

**DOC 1 varsayımları** (W33 state genişlemesi): enum'lar append-only büyür —
`ActorIntent.Harvest = 2`; `ActorActionType.MoveToPlot = 4, HarvestCrop = 5, HaulCrop = 6`.
`ActorActionState.TargetItemId` "eylemin konusu olan şeyin id'si" olarak genelleşir: farm
zincirinde **PlantComponent.Id** taşır (W32'de TakeFood'un mint ettiği yemek birimiydi).
**PİN**: `ActorActionState.TryRestore` aralık bekçileri (`ActorActionState.cs:169-170`,
`intent > ActorIntent.Eat` / `action > ActorActionType.ConsumeFood`) genişletilmek ZORUNDA —
unutulursa save'deki her farm eylemi sessizce Idle'a normalize olur (madde yine korunur, §6.3,
ama hikâye kaybolur).

**DOC 2 varsayımları** (karar + plot rezervasyonu): boşta, tok, sivil aktör + kendi
yerleşkesinde `ripe` plot → `Harvest` intenti. Plot claim'i mevcut `ReservationLedger`
üzerinden, **tag namespace ile**: `tag = "plot:{plantId}"`, `pileCount = 1` geçilir —
`TryReserve`'ün `pileCount - ReservedCount <= 0` bekçisi (`ReservationLedger.cs:36-39`) plot
başına EN FAZLA BİR claim'i bedavaya verir. Açlık hasattan önce gelir (aç aktör önce yer);
**eller doluysa** karar katmanı yeni hasat DEĞİL, doğrudan `HaulCrop`'tan başlayan zinciri
kurar (§6.2 — trigger `CarriedCropTag != null` alanının kendisidir, yeni state yok).
TTL formül ailesi W32-02 §4.3'tür: `until = now + walk + HarvestDurationTicks + 60`.

Bu katmanın sözleşmesi tek cümledir:

> **Bir birim ürün ya bitkidedir (hasat edilmemiş potansiyel) ya bir aktörün elindedir ya
> pile'dadır ya da tüketilmiştir; iki yerde birden asla, hiçbir yerde birden asla — ve yer
> değiştirmenin tek yolu, tamamlanmış bir eylemin doğrulanmış işlemidir.**

---

## 1. Bugünkü teleportlar ve emeklilik listesi

RUH_TESHIS §2.8 hasat tarafını `DefaultTickSystems.cs:437-465` diye işaret ediyordu; kod
o günden beri kaydı — bugünkü gerçek satırlar aşağıda. Üç teleport yaşıyor:

| # | Teleport | Dosya:satır (bugün) | Akıbet |
|---|---|---|---|
| T1 | **Satır-içi `+2`**: ripe plot doğrudan `pile.Add(species, 2)` — ürün ne yerde, ne elde, ne yolda | `DefaultTickSystems.cs:490` (sınıf `HarvestStep` `:452-501`, kayıt `:65`) | SİLİNİR; `+2` fiziksel mint olarak `CropOperations.CompleteHarvest`'e (ELE), pile'a giriş `DepositCarried`'e (TESLİMATTA) taşınır §4 |
| T2 | **Self-replant**: bitki fiat ile `"seed"`'e reset | `DefaultTickSystems.cs:496-498` (`world.Plants.Replace(..., "seed", 0)`) | SİLİNİR; ripe→seed geçişinin TEK yazarı `CompleteHarvest` olur §4.2 |
| T3 | **Fiat eller**: "yakında biri var mı?" proximity taraması — irade yok, yürüyüş yok | `HarvestHandsService.cs` (tüm dosya, `FindHarvester :20-37`) | DOSYA SİLİNİR; `ReachCells = 2` sabiti anlamıyla `CropOperations.HarvestReachCells`'e taşınır; "kim hasat eder" sorusunun cevabı DOC 2'nin KARARI olur |

Yaşayanlar (bilinçli):

- **`PlantHarvested` olay satırı** aynen kalır — `"harvested species:{s} qty:2 by:{actorId}"`
  grameri artık `CompleteHarvest`'ten yazılır. Tüketiciler kind'a bakar, kırılmaz:
  `RumorMillSystem.cs:57`, echo feed `DomainSimulationAdapter.Clock.cs:66`,
  `Dialog.Text.cs:180`, `WorldEventNarrator.cs:45`.
- **`HarvestSystem.cs`** (Phase 5 köprüsü, `Simulation/Process/`): canlı akışta ÇAĞIRANI YOK
  (yalnız `HarvestSystemTests`). Bu dilimde dokunulmaz — davranış-nötr temizlik adayı olarak
  not edilir, silme ayrı iştir.
- **M6 davranışı korunur**: kimse gelmezse plot `ripe` BEKLER. Bugün bunu proximity bekçisi
  yapıyordu (`:475-476` `continue`); yarın "hiçbir aktör Harvest intenti almadı" hâli aynı
  sonucu doğal yoldan verir.

---

## 2. Taşınan yük: `ActorRecord.CarriedCrop*`

W32'de eller yoktu — alınan yemek birimi claim satırının tag'inde yaşıyordu, çünkü take→consume
tek site içinde, tek zincirdeydi. Farm zinciri **siteler arası taşıma + replan'a dayanıklılık**
istiyor: yük artık aktörün üzerinde birinci sınıf, SAVED durumdur.

`ActorRecord.cs` alan bloğuna (`:68-88` civarı) iki alan + iki dar mutator:

```csharp
/// <summary>Elde taşınan ürün tag'i; null = eller boş. Tek tag taşınır (karışık yük yok).</summary>
// CONSTRAINT (save/backward-compat): default(null, 0) == empty hands — pre-W33 saves
// deserialize missing fields to null/0 and load hands-empty (all-zero-extends-Idle rule).
// CONSTRAINT (single writer): only CropOperations mutates these — matter moves through
// validated ops, never through a bare setter.
public string CarriedCropTag { get; private set; }
public int CarriedCropUnits { get; private set; }

internal void PickUpCrop(string tag, int units) { CarriedCropTag = tag; CarriedCropUnits = units; }
internal void DropAllCrop() { CarriedCropTag = null; CarriedCropUnits = 0; }
```

Kapasite — sabitlerin tek evi `CropOperations` (§4):

```csharp
public const int HarvestYieldUnits = 2;   // verbatim today's "+2" — economy math unchanged
public const int CarryCapacityUnits = 2;  // == yield: one plot per trip; batching is future work
```

`CarryCapacityUnits == HarvestYieldUnits` bilinçli: hasat ancak **boş elle** başlar/tamamlanır,
zincir kendiliğinden hasat→taşı→hasat ritmine oturur; çoklu-plot toplama (§11) kapasiteyi
büyütmekten ibaret kalır.

**Save kolonları** (`ActorSaveMapper.cs` yazım bloğu `:72-82` bitişiği + `FromSave`):
`carriedCropTag` (string, boş = yok), `carriedCropUnits` (int). Normalizasyon kuralı
`ActorActionStateSaveReader` (`:186-205`) ile aynı felsefe — bozuk blok yarım yüklenmez:
`units < 0`, `units > CarryCapacityUnits` veya `units > 0 && tag boş` ⇒ eller boşa normalize
(fail-safe; kayıp loglanmaz çünkü yalnız bozuk save üretebilir). Golden roundtrip testi her
alan düşüşünde kırılır (W32 kuralı).

---

## 3. Zincir ve faz başına madde mutasyonu

`ActionLifecycleSystem.NextLink` (`ActionLifecycleSystem.cs:95-100`) iki satır büyür — sistemin
kendisine DOKUNULMAZ (OCP, W32-03 §3 aynen):

```csharp
ActorActionType.MoveToPlot  => ActorActionType.HarvestCrop,
ActorActionType.HarvestCrop => ActorActionType.HaulCrop,
// HaulCrop => None (chain end); a hands-full replan starts a NEW chain AT HaulCrop (§6.2)
```

| Eylem | Süre | Madde mutasyonu | Başarısızlık dalı |
|---|---|---|---|
| `MoveToPlot` | yürüyüş (adım/tick) | **YOK** — dünya değişmez | claim düştü ⇒ `ReservationLost`; bitki yok/ripe değil ⇒ `SourceDrained`; pursuit ⇒ `Interrupted` |
| `HarvestCrop` | `HarvestDurationTicks = 2` | Tamamlanışta TEK atomik commit (§4.2): bitki ripe→seed + verim ELE + event + claim release | reach dışına itilme ⇒ `Unreachable`; claim/bitki kontrolleri her adımda; **tamamlanmadıysa hiçbir şey mint edilmez, bitki ripe kalır** |
| `HaulCrop` | yürüyüş (adım/tick) | Varışta `DepositCarried`: eller → pile, eller boşalır | eller boş (yalnız bozuk yol) ⇒ `SourceDrained`; pursuit ⇒ `Interrupted` — **yük elde KALIR** §6.2 |

Yeni `ActionFailureReason` değeri YOK — mevcut altı sebep farm zincirini eksiksiz kapsar
(enum append-only kredisi harcanmaz).

Zaman çizgisi (karar tick'i T0; W32-03 §4 kuralları: karar@18 + advance@22 aynı tick,
geçişler tick tüketir, handover'dan doğan link İLK adımını handover tick'inde atar):

```text
T0        : decide@18 — plot claim + Start(MoveToPlot); advance@22 ilk adım
T0+1..A   : adım adım plota yürüyüş; A'da reach (Chebyshev <= 2) → Succeeded
A+1       : handover → HarvestCrop, progress 1/2
A+2       : progress 2/2 → CompleteHarvest commit'i → Succeeded
            (eller: {species, 2}; bitki: seed; event: PlantHarvested; claim: released)
A+3..D    : handover → HaulCrop; adım adım kendi pile'ına yürüyüş; D'de reach →
            DepositCarried + Succeeded  (pile += 2; eller boş)
D+1       : handover → Idle (zincir biter; sonraki karar serbest)
```

Stok artık günlük fiat sınırında değil, **teslimat tick'inde** doğar — golden kayması sınıfı
"zamanlama", "toplam" değil (§10.3).

---

## 4. CropOperations — doğrulanmış dünya işlemleri

`Assets/Scripts/Simulation/Living/Actions/CropOperations.cs` (yeni; `FoodOperations`'ın kardeşi,
aynı klasör, aynı internal-static kalıp).

```csharp
// Design note:
// W33-03 §4: validated world mutations for the FARM chain. CONSTRAINT: the ONLY code that
// turns a ripe plant into carried units into pile stock — and the ONLY ripe->seed writer.
// Reach semantics are IMPORTED (HarvestHandsService.ReachCells = 2, retired file; site-centre
// truth = NeedConsumptionSystem.TryGetSiteCentre) so behaviour shifts only by phase timing.
internal static class CropOperations
{
    public const int HarvestYieldUnits = 2;    // verbatim retired "+2" (DefaultTickSystems.cs:490)
    public const int CarryCapacityUnits = 2;   // == yield: one plot per trip (W33)
    public const int HarvestDurationTicks = 2; // picking + bundling; single home of the constant
    public const int HarvestReachCells = 2;    // verbatim retired HarvestHandsService.ReachCells
    public const int DepositReachCells = 2;    // same ring as EatReachCells — one reach culture

    public static PlantComponent FindPlant(WorldState w, ulong plantId) { /* Plants.Rows scan */ }
    public static bool IsRipe(PlantComponent p) => p != null && p.StageId.Value == "ripe";
    public static bool WithinHarvestReach(ActorRecord a, PlantComponent p) { /* Chebyshev <= 2 */ }
    public static bool WithinDepositReach(WorldState w, ActorRecord a, ulong siteId)
    { /* TryGetSiteCentre + Chebyshev <= DepositReachCells; siteless: permissive (bare tests) */ }
}
```

### 4.1 Neden `Simulation/Living/Actions` altında

W32'nin fiilî yerleşimi budur (`FoodOperations.cs` orada yaşıyor); advancer'larla aynı asamble,
internal görünürlük korunur. Domain saf kalır: `CropOperations` yalnız Domain tiplerine dokunur,
Unity/IO/RNG yok.

### 4.2 `CompleteHarvest` — dilimin kalbi, tek atomik commit

```csharp
// CONSTRAINT (atomicity): mint + replant + event + claim-release close in the SAME step —
// a chunk boundary can never observe "yield minted but plant still ripe" or the reverse.
// CONSTRAINT (single writer): this is the ONLY ripe->seed transition in the codebase.
public static void CompleteHarvest(WorldState w, ActorRecord actor, PlantComponent plant,
    ulong reservationRowId, GameTime stamp)
{
    // CanCarry violation here is transition-unreachable (decision gates hands-empty, cap ==
    // yield): silent fixup would be a determinism leak — die loudly, the constitution's way.
    if (actor.CarriedCropUnits + HarvestYieldUnits > CarryCapacityUnits
        || (actor.CarriedCropTag != null && actor.CarriedCropTag != plant.SpeciesId))
        throw new InvalidOperationException("CompleteHarvest invariant: hands cannot take the yield.");

    actor.PickUpCrop(plant.SpeciesId, actor.CarriedCropUnits + HarvestYieldUnits); // mint -> HANDS
    w.Plants.Replace(plant.Id, plant.WithStage(new PlantStageId("seed")));         // the ONE replant
    w.Events?.Append(new WorldEvent(stamp, WorldEventKind.PlantHarvested, actor.Id, plant.SiteId,
        $"harvested species:{plant.SpeciesId} qty:{HarvestYieldUnits} by:{actor.Id.Value}")); // verbatim grammar
    w.Reservations?.Release(reservationRowId); // plot free; regrowth is biology's job from here
}
```

`WithStage` (`PlantComponent.cs:45-48`) `DaysInStage`'i zaten sıfırlar — emekli `Replace(...,
"seed", 0)` satırıyla bit-eşdeğer bitki durumu.

### 4.3 `DepositCarried` — eller → pile

```csharp
// CONSTRAINT: Add-only, cannot fail for capacity (StockpileComponent.Add is total) — deposit
// needs NO reservation. Find-or-create mirrors the retired HarvestStep block (:477-487) so
// a pileless site still receives its first stock.
public static void DepositCarried(WorldState w, ActorRecord actor, ulong siteId)
{
    var pile = FoodOperations.FindPile(w, siteId)
        ?? AddNewPile(w, siteId);                       // new StockpileComponent(siteId)
    pile.Add(actor.CarriedCropTag, actor.CarriedCropUnits);
    actor.DropAllCrop();                                // hands empty in the SAME step
}
```

Teslimat olayı `ActionLogManager` terminal dikişinden akar (§5.3) — burada ikinci bir event
yazılmaz (tek log kapısı kuralı, W32-04).

---

## 5. Üç advancer — W32 şablonu birebir

Kayıt (`ActionLifecycleSystem` ctor `:22-28`): registry üç satır büyür; `ActionAdvancerRegistry`
ve `ActionAdvancer` (Template Method: pursuit probe → Step → TransitionTo/Fail) DEĞİŞMEZ.
`Fail`'in ConsumeFood-özel pile-iade satırı (`ActionAdvancer.cs:74-75`) eat'e özgü kalır ve
farm'a DOĞRU davranır: farm eylemleri pile'dan bir şey almadığı için iade edilecek şey yoktur;
plot claim'i varsa `Release` aynı kapıdan düşer.

### 5.1 `MoveToPlotAdvancer` (~40 satır)

`MoveToFoodAdvancer` kalıbı: her adımda (1) claim hâlâ benim mi (`TryGetByActor` + id eşleşmesi;
değilse `ReservationLost`), (2) bitki var ve ripe mi (`FindPlant(TargetItemId)` + `IsRipe`;
değilse `SourceDrained` — normalde imkânsız, claim ripe'ı kilitler; bekçi ucuz ve gürültülü),
(3) `MovementService.StepToward(actor.Position, plant.Position)` bir hücre,
(4) `WithinHarvestReach` ⇒ `Succeeded` (seat ringi YOK — plot başına tek claim'li tek aktör,
Gate8'in istif problemi burada doğamaz).

### 5.2 `HarvestCropAdvancer` (~55 satır)

`ConsumeFoodAdvancer` kalıbı: her adımda claim + bitki + reach doğrula (reach dışına witness
itmesi ⇒ `Unreachable`, fail & replan — W32'nin fiilî gemisiyle aynı politika),
`state.Advanced()`; `ProgressTicks == HarvestDurationTicks` olduğunda
`CropOperations.CompleteHarvest(...)` + `Succeeded` AYNI adımda.

### 5.3 `HaulCropAdvancer` (~45 satır)

**Claim'siz ilk advancer**: `CompleteHarvest` claim'i çoktan bıraktı; state'teki
`ReservationId` ölü bir id taşıyabilir veya (hands-full replan zincirinde) `Empty`'dir —
**doğrulanMAZ**. Adım: eller boşsa `SourceDrained` (yalnız bozuk save üretebilir);
`MovementService.StepToward(actor.Position, siteCentre(TargetSiteId))`;
`WithinDepositReach` ⇒ `DepositCarried` + `Succeeded` aynı adımda (ilk temas teslim eder —
seat/işgal semantiği yok, mevduatın sırası önemsiz).

Log dikişi: `ActionLogManager.Record`'un terminal-tamamlanma özel hâli
(`ActionLogManager.cs:32-36`, bugün yalnız `ConsumeFood`) `HaulCrop`'u da kapsar — tek koşul
satırı büyür, event grameri `"farm:haul completed qty:{n} tag:{tag} target=site:{id} t={m}"`.
Haul başına net +1 story-yüzeyi olayı; faz sınırları her zamanki gibi yalnız ring'e akar.

---

## 6. Kesilme ve başarısızlık: minimal dürüst kural

Mandanın sorusu — *"kesilen taşıyıcının ürünü bitkiye mi döner, yere mi düşer?"* — ikisi de değil:

### 6.1 Tamamlanmadan önce (MoveToPlot / HarvestCrop)

Hiçbir şey mint edilmedi ⇒ konservasyon bedava. Bitki **ripe kalır** (progress kaybı dürüst
maliyettir), claim `Fail` kapısından düşer, replan bir sonraki karar tick'inde. Bitkiye
"iade" edilecek bir şey yoktur çünkü bitkiden bir şey alınmamıştır.

### 6.2 Tamamlandıktan sonra (HaulCrop kesintisi) — KURAL: **yük elde kalır**

Ürün KOPARILMIŞTIR — bitki bir konteyner değildir, geri konamaz (ripe'a dönüş biyolojiyi
geri sarmak olurdu: hem yalan hem dup kaynağı). Yer kaydı da YOK: dünyada item-entity
altyapısı bulunmuyor (stockpile'lar count-based; "yerde duran çuval" yeni bir kayıt türü,
yeni save şeması, yeni toplama eylemi demek — dilim dışı, §11). En basit deterministik ve
dürüst kural:

> **Kesilen/başarısız HaulCrop yükü DÜŞÜRMEZ; birimler aktörün elinde kalır.
> `CarriedCropTag != null` hâli kendisi bir karar tetiğidir: aktör tekrar boşa düştüğünde
> karar katmanı bulunduğu yerden yeni bir HaulCrop-zinciri başlatır.**

Sıfır yeni alan, sıfır kayıp, sıfır çoğalma; W32-03 §4'ün CarriedFood devam sözleşmesinin
farm karşılığı — üstelik yürüyüş kaldığı yerden sürer (progress cezası yok, mesafe zaten
konumda kodlu).

### 6.3 Ölüm taşırken

Taşıyıcı ölürse birimler **taşıyıcısıyla gömülür** — vermin hırsızlığı gibi bir SINK'tir
(sıçan yer, mezar yutar; ikisi de pile'a dönmez). Ceset yağması altyapısı bu dilimde yok;
teleport-iade ("ölünce stok pile'a ışınlanır") tam da emekli ettiğimiz günahtır. Invariant
muhasebesi CANLI aktörlerin elleri üzerinden yürür (§9); save normalizasyonu (§0 PİN) bir
zinciri Idle'a düşürse bile eller dolu kalır ve §6.2 kuralı toparlar.

### 6.4 Sıçanlar ellere uzanamaz

`AmbientLifeSystem` yalnız pile'dan çalar (`AmbientLifeSystem.cs:44-58`, `pile.Remove(tag,1)`).
Eldeki yük yapısal olarak vermin menzili dışındadır — kod gerekmez, invariant bedavadır.

---

## 7. Sıçanlar ve efektif stok — DEĞİŞİKLİK YOK

`AmbientLifeSystem` (`living.ambient@Hourly:50`, kayıt `DefaultTickSystems.cs:58`) aynen kalır:

- **Teslimat rezervasyonsuzdur**: `DepositCarried` Add-only, kapasite reddi yok — sıçanla
  yarışacağı bir "son birim" senaryosu deposit tarafında doğamaz.
- **Teslimat SONRASI hırsızlık** zaten W32'nin kapsadığı hikâyedir: pile'a inen ürün eat
  zincirinin efektif-stok matematiğine girer; sıçan claim'li son birimi yerse mevcut
  `SourceDrained` dalları yakalar (`MoveToFoodAdvancer.cs:28-34`, `TakeFoodAdvancer.cs:30-35`).
- **Tag namespace disjoint invariantı**: plot claim'leri `"plot:{plantId}"` tag'iyle yazılır;
  yemek kararının `ReservedCount(site, species)` sorguları species tag'leriyle çalışır
  (`FoodPileCache.FoodTags`, `"wheat"` + canlı türler). İki evren kesişmez ⇒ plot claim'leri
  efektif yemek stokunu ASLA saptırmaz. (DOC 6'ya test: plot claim'i açıkken effective-stock
  hesabı değişmemeli.)
- Sıralama notu: vermin Hourly:50, advance PerTick:22 — saat sınırı tick'inde hırsızlık
  teslimattan SONRA vurur; bir sonraki tick'in doğrulamaları yakalar (W32'nin pull modeli).

---

## 8. SimFieldView doğrulaması — DEĞİŞİKLİK GEREKMİYOR

Veri zinciri: `DomainSimulationAdapter.Clock.cs:87-131` (`PublishFieldMirror`) her tick
`world.Plants.Rows`'tan stage sayımı + hücre listesi çıkarır → değişim hash'i
(`plantsHash`, `:126-130`) → `RuntimeFieldMirror.PublishPlants` (`RuntimeFieldBuilder.cs:40-44`)
→ `SimFieldView.Update` (`:148-186`) stalk kurar/budar, `ExternalStage` yazar →
`CropStalkView` yüksekliği yumuşakça sürer.

W33'ün değiştirdiği tek şey stage'in **NE ZAMAN** döndüğüdür (günlük fiat yerine hasat
tamamlanış tick'i); **NE** okunduğu değil: aynı `PlantComponent` satırları, aynı stage
string'leri (`"seed"/"sprout"/"ripe"`), aynı yayın şeması. Doğrulama sonucu:

- `SimFieldView`/`CropStalkView`/`RuntimeFieldMirror` **hiçbir satır değişmez**.
- Görsel yan ürün bedava: ripe plot artık bir çiftçi gelene dek altın renkte BEKLER; hasat
  anında sap `"PLAYTEST FIX"` solma animasyonuyla iner (`RuntimeFieldBuilder.cs:81-86`) —
  o yorumdaki *"the walking harvester that triggers it is roadmap M6"* cümlesi bu dilimle
  kelimenin tam anlamıyla gerçek olur.
- Render-katmanı kanıt kuralı (proof harness): shipped iddiası stage bekleyen ripe plot +
  yürüyen çiftçi ekran görüntüsüyle verilir, data-layer log'uyla değil.

---

## 9. Madde korunumu invariantları (DOC 6'nın hammaddesi)

Tür `s` için her tick sınırında muhasebe:

```text
ΔPile(s) + ΔHands_alive(s) =  Mint(s)              // yalnız CompleteHarvest, +2/adet
                            − Meals(s)             // ConsumeFood commit'i (W32)
                            − Vermin(s)            // sıçan hırsızlığı (pile'dan)
                            − Buried(s)            // taşıyıcı ölümü (ellerden)
```

Testlenebilir maddeler:

- **I1 — Input olmadan output yok** (RUH_TESHIS §10): farm kaynaklı stok artışı
  `== HarvestYieldUnits × tamamlanmış HarvestCrop sayısı`. Sıfır tamamlanma ⇒ sıfır mint.
  Özelde: hiçbir `pile.Add` farm yolundan, önünde `PlantHarvested` olmadan gelemez.
- **I2 — Tek replant yazarı**: ripe→seed geçiş sayısı `== PlantHarvested sayısı ==
  HarvestCrop tamamlanma sayısı`. Fiat reset kalmadığının kanıtı: hasatçısız dünyada bitki
  sonsuza dek ripe kalır (M6 hikâyesi test olur).
- **I3 — Kesinti korunumu**: HaulCrop herhangi bir tick'te kesilirse
  `Pile + Hands` toplamı değişmez (dup yok, kayıp yok); aktör yeniden boşa düşünce yük
  eninde sonunda pile'a iner (hands-full replan kuralı).
- **I4 — Kapasite**: her sınırda `CarriedCropUnits ∈ {0, CarryCapacityUnits}` ve tag'siz
  birim yok; `CompleteHarvest` kapasite ihlalinde gürültüyle ölür (unreachable-guard).
- **I5 — Chunking hakemi**: N tick'i tek parça vs herhangi bir bölmeleme — (piles, hands,
  plant stages, event trace) bire bir aynı. `ActionPhaseChunkingInvarianceTests` farm
  zinciriyle genişletilir; advancer'lar stateless, tüm ilerleme saved state'te (§2, §3).
- **I6 — Zincir bütünlüğü**: her `PlantHarvested` olayının öncesinde aynı aktör için
  `MoveToPlot→HarvestCrop` faz izi vardır (ActionLogRing'den okunur) — teleportun mezar taşı.
- **I7 — Save/load taşıma ortası**: HaulCrop ortasında save+load ⇒ eller aynen, teslimat
  tamamlanır, toplamlar değişmez. Pre-W33 save ⇒ eller boş, sıfır crash (all-zero kuralı).
- **I8 — Namespace disjoint**: açık plot claim'i, aynı site'taki yemek kararının efektif
  stok hesabını değiştirmez (§7).

---

## 10. Emeklilik / kayıt / ledger diff'leri

### 10.1 `DefaultTickSystems`

```csharp
  new PlantGrowthStep(...),        // econ.plantgrowth@Daily:20 — değişmez
- new HarvestStep(),               // :65 — world.harvest@Daily:25 SİLİNİR (sınıf :452-501 ile)
  new ShortageResponseStep(),      // econ.shortage_response@Daily:27 — değişmez
  ...                              // fiyatlar Daily:30 pile'ı olduğu gibi okur — zincir sağlam
```

Farm zinciri mevcut slotlarda akar: karar `living.decision@PerTick:18`, ilerleme
`living.action_advance@PerTick:22` — YENİ tick slotu yok (W32 altyapı reuse'unun kârı).

### 10.2 `FieldOwnershipRegistry` (GateContractLintTests zorlar)

```csharp
["World.Stockpiles"]  : - "world.harvest@Daily:25"
                          // living.action_advance@PerTick:22 zaten listede (W32 TakeFood) —
                          // yorum güncellenir: "+ W33 HaulCrop deposit"
["World.Plants"]      : + YENİ SATIR: { "econ.plantgrowth@Daily:20",
                          "living.action_advance@PerTick:22" }  // CompleteHarvest replant
["Actor.CarriedCrop"] : + YENİ SATIR: { "living.action_advance@PerTick:22" }  // tek yazar
```

(`World.Plants` bugün ledger'da hiç yoktu — iki yazarlı gerçek artık denetlenebilir kayda girer.)

### 10.3 Golden / ekonomi sonuçları (belgelenmiş kayma sınıfı)

- Stok **teslimat tick'inde** ve `+2` patlamalarıyla doğar; Daily:25 fiat çizelgesi ölür.
  Aynı gün fiyata yansıma, teslimatın Daily:30'dan önce bitmesine bağlıdır — kayma sınıfı
  ZAMANLAMA'dır.
- Uzun vadeli throughput artık çiftçi yürüyüşüyle sınırlıdır: eller azsa toplam DÜŞEBİLİR.
  Bu dürüst ekonomidir; kıtlık cevabı zaten var (`ShortageResponseStep@Daily:27`) ve ilk kez
  gerçek bir maliyeti dengeliyor olur. Golden yeniden-baseline'ı bu iki sınıf dışında fark
  gösteriyorsa şüphe testten yana kullanılır.

---

## 11. LOC bütçesi ve genelleşecek noktalar

Yeni: `CropOperations.cs` (~85), `MoveToPlotAdvancer.cs` (~40), `HarvestCropAdvancer.cs` (~55),
`HaulCropAdvancer.cs` (~45). Dokunulan: `ActorRecord` (+10), `ActorSaveMapper` (+8),
`ActorActionState.TryRestore` bekçileri (+2, DOC 1), `NextLink` (+2), lifecycle ctor (+3),
`ActionLogManager` terminal koşulu (+1), `FieldOwnershipRegistry` (+4). Silinen: `HarvestStep`
(~50) + `HarvestHandsService.cs` (~38). Net ≈ +255 / −90.

Bilinçli ertelemeler: çoklu-plot batching (kapasite > verim + karar döngüsü), yer-item kaydı
(drop/pickup altyapısı), ceset yağması, tohum item'ı + `SowSeed` eylemi (RUH_TESHIS slice-2
zincirinin ekim yarısı — bu doküman hasat SONRASI madde akışını kapatır; ekim tarafı kendi
dokümanının işidir), pathfinding (düz-hat `MovementService` adımı korunur ki golden farkı
yalnız faz zamanlaması olsun).

> Çember kapanışı: bitki → el → pile → (W32) el → mide. Ürün ilk kez dünyada YÜRÜYOR;
> sayaçlar artık yalnızca gerçeği sayıyor.

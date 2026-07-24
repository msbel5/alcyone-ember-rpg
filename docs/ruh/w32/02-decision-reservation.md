# W32 / Doc 02 — DecisionSystem + ReservationLedger (EAT dikey dilimi)

> Teşhis referansı: `docs/RUH_TESHIS.md` §2.4 (ScheduleSystem karar değil, her tick yeniden
> hesaplanan yönlendirme), §6 (DecisionSystem yalnızca idle/interrupted aktör için intent seçer;
> ReservationSystem kaynağı ayırır), §7 (Ayşe'nin hedef yemek zinciri), §9 (ilk dikey dilim,
> adım 3-4: "Decision `EatIntent` oluştursun; gerçek bir food unit rezerve edilsin").
>
> Kapsam: SADECE EAT dilimi. Rest/work/idle kararları ScheduleSystem'de kalır (bkz. §5).
> Bağımlılıklar: Doc 01 (ActorMindState + inheritance tabanlı action hiyerarşisi:
> `EmberActionBase` → `MoveToFoodAction`), Doc 03 (action ilerletme / consume commit),
> W32 ortak LogManager kesiti (§8'de kullandığım asgari kontrat yeniden yazılmıştır).

---

## 1. Mevcut durumun kanıtı (okunan gerçek kod)

Bugün "aç sivili yemeğe yürütme" kararı her tick, kalıcı iz bırakmadan yeniden hesaplanır:

- `Assets/Scripts/Simulation/Living/ScheduleSystem.cs:51-81` — `Advance` her aktörü tarar,
  `ChooseTarget` (:77) hedefi seçer, `StepToward` + `actor.MoveTo(next)` (:78-80) tek karede
  uygular. Karar hiçbir yere yazılmaz; aktör NEDEN yürüdüğünü taşımaz.
- `ScheduleSystem.cs:129-142` — eat skoru (`Hunger >= HungerEatThreshold` ise `eat = Hunger`,
  :133-135) ve eat kazanırsa `Seat(foodSpot.Value, seatOrdinal)` (:141-142). Utility masası her
  çağrıda sıfırdan kurulur (teşhis §2.4: "kalıcı karar yoktur").
- `Assets/Scripts/Simulation/Living/NeedConsumptionSystem.cs:180-188` — yemek fiili değil, iki
  sayaç mutasyonu: `best.Remove(bestTag, 1)` + hunger tabana çekilir; event sonradan yazılır.
- `NeedConsumptionSystem.cs:67-89` (`TickArrivals`, PerTick) ve `:47-49` (Hourly dal) — masaya
  varan İLK tarayan yer; rezervasyon yok, "son ekmek" için iki aktör aynı tick yarışırsa ilk
  iterasyon sırası kazanır ve kaybeden hiçbir bilgi taşımaz (teşhis §5 sonu).
- `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs:211-234` — `ScheduleStep`
  (`living.schedule@PerTick:20`, :219) her tick `NeedConsumptionSystem.FoodSpots(world)` (:230)
  üretip `Advance`'e verir (:231). `EatOnArrivalStep` (`living.eatOnArrival@PerTick:22`,
  :254-263) varış anında yedirir.
- Kadans gerçeği: `WorldTickComposer.cs:242-266` — PerTick bandı her tick koşar, Hourly bandı
  aynı tick içinde PerTick'ten SONRA sınırda koşar. `living.needs@Hourly:30`
  (`DefaultTickSystems.cs:356-361`, oranlar `NeedsSystem.cs:21-28`) açlığı saat başı yükseltir;
  yani eşik aşımı, bir sonraki tick'in PerTick bandında görülür (deterministik 1 tick gecikme).

Stok gerçeği: `Domain/Process/StockpileComponent.cs:12-75` — stok, tag→count sözlüğüdür
(`Get` :38-42, `Remove` :64-75). Item instance YOK. Bu yüzden rezervasyon bu dilimde bir
"adet claim"idir, item-id claim'i değildir (ileriye genelleme: §9).

---

## 2. Hedef akış (bu dokümanın dilimi)

```text
NeedsSystem (Hourly:30) açlığı 55 eşiğinin üstüne iter
→ living.decision@PerTick:18: aktör actionsız + aç → EatIntent
→ ReservationLedger: en yakın erişilebilir yemekli pile'dan 1 adet tag rezerve edilir
→ actor.Mind: Intent=Eat, CurrentAction=MoveToFoodAction(seat, siteId, tag, resId)
→ (Doc 03) hareket YALNIZCA aktif Move action üzerinden; varışta Take/Consume fazları
→ Consume commit: stok -1 + hunger düşer + rezervasyon 'consumed' ile bırakılır (atomik faz)
→ başarısızlık/ölüm/expiry: rezervasyon TEK sefer bırakılır, sebep loglanır
→ UI CurrentAction'ı verbatim okur (teşhis §6/§9 madde 10)
```

"Son ekmek" hikayesi ilk kez mümkün olur: Ayşe rezerve eder → Mehmet'in decision'ı efektif
stok 0 gördüğü için o pile'ı ELEyip bir sonrakine bakar veya intent üretmez → Mehmet'in
başarısız arayışı artık sistemin görebildiği bir durumdur.

---

## 3. DecisionSystem tasarımı

### 3.1 Yerleşim ve kadans

- Dosya: `Assets/Scripts/Simulation/Living/DecisionSystem.cs` (pure Simulation; Unity/IO/RNG yok —
  determinizm anayasası).
- Kayıt: `DefaultTickSystems` içinde `DecisionStep : StepBase("living.decision", TickCadence.PerTick, 18)`.
  18 < 20 (`living.schedule`) ve < 19 (Doc 03'ün önerilen `living.action_move@PerTick:19`):
  aynı tick karar → hareket → (actionsız kalanlar için) schedule sırası korunur.
- PerTick olduğu için `CadenceChunkingInvarianceTests` sözleşmesine otomatik girer
  (`WorldTickComposer.cs:242-249` tick-tek-tek replay); sistemde çağrılar arası statik durum YOK.

### 3.2 Aday filtresi — O(1)-ish kuralı

950 aktörde her tick tarama kaçınılmaz (mevcut `ScheduleSystem.Advance` ve `TickArrivals`
zaten tarıyor); pahalı işi aday sayısına indiririz. Aktör başına ucuz kapılar, ucuzdan pahalıya:

1. `actor == null || !actor.IsAlive` → geç (alan okuma).
2. `actor.Role == Player || Enemy` → geç (EAT dilimi sivil pilotu; `NeedConsumptionSystem.cs:45` ile aynı küme).
3. `actor.Mind.CurrentAction` aktif (Running) → geç — TEK null/enum kontrolü. Karar yalnız
   `CurrentAction == null` veya faz `Completed/Failed` (terminal) olan aktör için verilir
   (teşhis §6: "DecisionSystem yalnızca idle/interrupted aktör için intent seçer").
4. Kural ön-koşulu: `actor.Needs.Hunger.Value < NeedConsumptionSystem.HungerEatThreshold` → geç.

Kapı 1-4 alan okumasıdır; pile taraması SADECE "aç + boşta" aktör için koşar. Karar verilen
aktör bir action sahiplenir ve sonraki tick'lerde kapı 3'te elenir → pile taraması aktör başına
yemek döngüsü başına ~1 kez amortize olur (O(1)-ish şartı).

### 3.3 Kural stratejisi (SOLID, düşük satır)

```csharp
// Strategy (OCP): dilimde tek kural (Eat). Rest/Work kuralları sonra AYNI dizinin
// üyesi olur; sıra SABİT dizi sırasıdır (deterministik öncelik, RNG yok).
public interface IDecisionRule
{
    // true dönerse intent + action yazılmış ve gerekli rezervasyon alınmıştır.
    bool TryDecide(WorldState world, ActorRecord actor, in TickContext ctx, FoodPileCache cache);
}

public sealed class DecisionSystem
{
    private readonly IDecisionRule[] _rules; // ctor: { new EatDecisionRule() }
    public void Tick(WorldState world, in TickContext ctx) { /* §3.2 kapıları + §4.4 sweep */ }
}
```

`EatDecisionRule.TryDecide` (tamamı ~40 satır hedef):

```csharp
// KISIT: yalnız Hunger >= HungerEatThreshold (55, NeedConsumptionSystem.cs:16 sabiti
// YENİDEN KULLANILIR — eşik iki yerde çatallanamaz) ve bilinen/erişilebilir yemekli
// pile varsa. "Bilinen" = FoodPileCache'te olan (dilimde algı = global bilgi; §9).
// 1) cache üzerinden en yakın pile: Chebyshev(site merkezi), efektifStok > 0 şartıyla
//    efektifStok = pile.Get(tag) - world.Reservations.ReservedCount(siteId, tag)
//    eşitlikte pile sırası (stockpile listesi sırası) kazanır — TickArrivals'ın
//    first-wins tie-break sözleşmesinin aynısı (NeedConsumptionSystem.cs:149-151).
// 2) world.Reservations.TryReserve(...) — başarısızsa (yarış aynı tick içinde başka
//    aktörle) bir SONRAKİ pile denenir; hiçbiri olmazsa intent üretilmez (aktör
//    ScheduleSystem'in rest/work/idle akışına düşer, log yok: spam dersi
//    NeedConsumptionSystem.cs:51-52).
// 3) seat = CommunalSeat.For(siteCentre, seatOrdinal)  — ScheduleSystem.SeatOffsets
//    (:176-186) buraya taşınır; diner'lar masa ÇEVRESİNE dağılmaya devam eder.
// 4) actor.Mind.Begin(EatIntent, new MoveToFoodAction(seat, siteId, tag, resId));
// 5) LogManager: IntentChosen + ResourceReserved (§8).
```

Not — `seatOrdinal`: `ScheduleSystem.Advance` bugün ordinali tarama sırasından üretir (:58, :77).
Deterministik ve kalıcı olması için seat ordinali `actor.Id.Value % 16` olarak hesaplanır
(insertion-order sayacı yerine kimlikten türetilir; kaydetmeye gerek kalmaz).

### 3.4 FoodPileCache — mevcut cache'in yeniden kullanımı

`NeedConsumptionSystem.FoodPileEntry` (:91-99) + `BuildFoodPileCache` (:101-126) +
`FoodTags` (:334-343) `Assets/Scripts/Simulation/Living/FoodPileCache.cs` altına ÇIKARILIR
(internal sealed class; pile referansı + site merkezi + tag listesi). İki tüketici olur:

- `NeedConsumptionSystem` (mevcut çağrılar aynen; net satır AZALIR),
- `DecisionSystem` (tick başına EN FAZLA 1 kez, LAZY kurulum: ilk aday aktör görülünce).

Aday yoksa (herkes tok/actionlı — normal rejim) tick maliyeti saf kapı taramasıdır ve sıfır
allocation'dır. Cache tick içinde bayatlamaz: PerTick bandında stok yazan tek sistem bugün
`living.eatOnArrival@PerTick:22`'dir ve dilim sonunda emekli olur (§5.2); rezervasyonlar zaten
efektif stok hesabına girer.

---

## 4. ReservationLedger tasarımı

### 4.1 Veri modeli (Domain, saf)

Stok adet-tabanlı olduğu için rezervasyon da adet-tabanlıdır: satır = "şu sitedeki şu tag'den
1 adet, şu aktör adına, şu dakikaya kadar ayrılmıştır".

```csharp
// Assets/Scripts/Domain/Process/ReservationRecord.cs — PursuitRecord üslubu (public alanlı POCO)
public sealed class ReservationRecord
{
    public ulong Id;            // ledger'ın KALICI sayacından; save'e girer (§4.5)
    public ulong SiteId;        // StockpileComponent.SiteId
    public string ItemTag;      // "wheat" vb. (FoodTags evreni)
    public ulong ActorId;
    public long UntilMinutes;   // GameTime.TotalMinutes; PursuitRecord.UntilMinutes emsali
}

// Assets/Scripts/Domain/Process/ReservationLedger.cs — WorldState alanı (§4.2)
public sealed class ReservationLedger
{
    public List<ReservationRecord> Rows = new();  // insertion order = save/enum sırası
    public ulong NextId = 1;                      // deterministik kimlik; save'e girer
    // Türetilmiş indeksler (SAVE'E GİRMEZ; load sonrası RebuildIndexes ile kurulur):
    // (site,tag) -> aktif adet ; actorId -> rowIndex. O(1) sorgu bunlardan gelir.
}
```

API (her metot deterministik, exception'sız Try-kalıbı):

| Metot | Sözleşme |
|---|---|
| `int ReservedCount(ulong siteId, string tag)` | Aktif satır adedi; O(1) sözlük. |
| `bool TryReserve(siteId, tag, actorId, untilMinutes, pileCount, out ulong id)` | `pileCount - ReservedCount <= 0` → false (SON ADET iki kez verilemez — çekirdek invariant). Aktör zaten satır tutuyorsa → false (aktör başına EN FAZLA 1 rezervasyon; EAT dilimi invariantı). |
| `bool Release(ulong id)` | Satırı kaldırır, indeksleri düşer; yoksa false (çifte release görünür hata değil, no-op — idempotent). |
| `bool TryGetByActor(actorId, out ReservationRecord row)` | Doc 03'ün varışta doğrulaması için. |
| `int SweepExpired(long nowMinutes, ICollection<ReservationRecord> removed)` | `UntilMinutes < now` satırlarını Rows sırasıyla kaldırır (deterministik). |
| `void RebuildIndexes()` | Load/EnsureInvariants sonrası. |

### 4.2 WorldState bağlantısı

`WorldState.cs`'e `GuardPursuits` (:173) emsalinde eklenir:

```csharp
public ReservationLedger Reservations = new ReservationLedger();
```

`EnsureInvariants()` içine `Reservations ??= new ReservationLedger();` + `RebuildIndexes()`
(bozuk save'de null store NRE dersi — WorldState.cs:64-71 yorumu). `CopyFrom` referans kopyalar
(GuardPursuits :252 ile aynı).

### 4.3 Yaşam döngüsü — release üç kapıdan, TEK sefer

| Kapı | Kim | Ne zaman |
|---|---|---|
| `consumed` | Doc 03 ConsumeAction commit fazı | Stok `Remove(tag,1)` + hunger düşüşü + `Release(resId)` AYNI fazda — teşhis §10: "Hunger, Consume tamamlanmadan düşmez", "kesilen eylem item'ı kaybetmez/çoğaltmaz". |
| `failed` | Doc 03 action fail yolu | Varışta `pile.Get(tag)==0` (rezervasyona rağmen stok haricî yazarca eridiyse: `living.ambient@Hourly:50` vermin, `econ.trade@Daily:28` — FieldOwnershipRegistry.cs:43-50), yol tıkalı, aktör öldü. Action `Failed(reason)` olur; DecisionSystem bir SONRAKİ tick yeniden karar verir. |
| `expired` | DecisionSystem.Tick başı sweep | Emniyet ağı: fail yolunun kaçırdığı her satır. Ölü aktör satırları da burada bırakılır. |

TTL deterministik hesaplanır, sabit değil mesafe-ölçekli:
`UntilMinutes = now + Chebyshev(actorPos, seat) + ConsumeDurationMinutes(Doc 01 sabiti) + 60 slack`.
(1 tick = 1 oyun dakikası = 1 kare adım — WorldTickComposer.MinutesPerTick; yürüyüş süresi mesafeye eşittir.)

### 4.4 Sweep'in yeri

`DecisionSystem.Tick` girişinde: `world.Reservations.SweepExpired(ctx.Stamp.TotalMinutes, buf)`
+ her düşen satır için `LogManager` `ReservationReleased reason:expired`. Sweep + claim aynı
sistemde olduğundan PerTick bandında ledger'ın yazarları net iki kimliktir (§6).

### 4.5 Save eşlemesi — pursuit paralel-dizi emsali birebir

`WorldSaveData.cs:75-77` (`pursuitGuardIds/TargetIds/UntilMinutes`) emsalinde:

```csharp
// WorldSaveData.cs — W32 EAT: rezervasyonlar paralel dizilerle (yeni alan = eski save'de null → boş ledger, geriye uyumlu)
public ulong[]  reservationIds;
public ulong[]  reservationSiteIds;
public string[] reservationItemTags;
public ulong[]  reservationActorIds;
public long[]   reservationUntilMinutes;
public ulong    reservationNextId;      // sayaç kaydedilmezse load sonrası ID çakışır → determinizm kırığı
```

Mapper: `WorldSaveMapper.ToData` (:96 pursuit satırının yanına, `Rows` sırasıyla) ve `ToWorld`
(:203-212 kalıbı: null-korumalı, uzunluk-min döngüsü, sonda `RebuildIndexes()`).
Golden kanıt: `WorldSaveMapperGoldenRoundtripTests.cs:23-36` temsili dünyasına 1 rezervasyon
satırı eklenir — reflection diff (:42-50) alan düşüren mapper'ı sonsuza dek yakalar.

---

## 5. ScheduleSystem ve NeedConsumptionSystem'in kaybettikleri

### 5.1 ScheduleSystem — aç sivili yönlendirmeyi BIRAKIR

Satır satır:

- `ScheduleSystem.cs:133-135` — eat skoru SİLİNİR (`NeedConsumptionSystem.HungerEatThreshold`
  karşılaştırması dahil). Utility masası rest/work/idle'a düşer.
- `ScheduleSystem.cs:141-142` — eat dalı + `Seat(...)` çağrısı SİLİNİR.
- `ScheduleSystem.cs:176-186` — `SeatOffsets` + `Seat` `CommunalSeat` yardımcı sınıfına TAŞINIR
  (davranış: DecisionSystem hedef üretirken kullanır; §3.3).
- `ScheduleSystem.cs:35-47` — `foodSpot`/`foodSpots` overload'ları ve `NearestSpot` (:158-169)
  SİLİNİR (tek çağıran ScheduleStep'ti).
- `ScheduleSystem.cs:59-62` bölgesine YENİ kapı: `if (actor.Mind?.CurrentAction is running) continue;`
  — aktif action'lı aktörün karesine schedule DOKUNAMAZ (Actor.Position çok-yazarlığının
  daralması; teşhis §2.3). Hareket yalnız Doc 03'ün Move ilerletmesinden gelir.
- `DefaultTickSystems.cs:230-231` — `ScheduleStep` artık `FoodSpots(world)` HESAPLAMAZ ve
  geçirmez; çağrı `_schedule.Advance(actors, stamp, pursuits)` biçimine iner.
  (`NeedConsumptionSystem.FoodSpots` :246-268 son çağıranını kaybeder → cache'e katlanır/ölür.)

ScheduleSystem'in KORUDUKLARI (bu dilimde): guard/enemy klasik rotası (:126-127, :150-156),
pursuit çözümü (:74-75, :86-106), actionsız sivil için rest/work/idle yürüyüşü. Bunlar sonraki
dilimlerde kendi kurallarına/actionlarına taşınır (§9).

### 5.2 NeedConsumptionSystem — anlık yeme yolları kapanır

- `DefaultTickSystems.cs:254-263` `EatOnArrivalStep` (`living.eatOnArrival@PerTick:22`) —
  dilim yandığında kayıttan ÇIKAR (`TickArrivals` :67-89 ölü koda düşer, silinir). Varış yemeği
  artık ConsumeAction'dır.
- `NeedConsumptionSystem.cs:47-49` Hourly eat dalı — geçiş süresince kapı eklenir:
  `if (actor.Mind?.CurrentAction != null || world.Reservations.TryGetByActor(...)) continue;`
  (çifte beslenme yasağı). Dilim tamamlanınca dal tamamen silinir; Hourly Tick yalnız uyku/
  metabolizma yarısı olarak kalır (:53-59).
- `TryEatCached`/`TryEat`/`FindNearestFoodPile` (:128-190, :192-210, :284-314) dilim sonunda
  silinir; site-merkezi/`TryGetSiteCentre` (:213-226) FoodPileCache'e taşınır.

---

## 6. FieldOwnershipRegistry değişen satırlar

`FieldOwnershipRegistry.cs:18-50` diff'i (lint testi `FieldOwnershipRegistryTests` AYNI
commit'te güncellenir; Doc 03'ün kesin sistem kimlikleri yer tutucudur):

```csharp
["Actor.Position"] = {
    "living.action_move@PerTick:19",      // + YENİ (Doc 03): aktif Move action tek adımı
    "living.schedule@PerTick:20",         // DARALIR: yalnız actionsız aktörler (§5.1 kapısı)
    "living.companion_follow@PerTick:21",
    "living.predation@Hourly:40", "living.witness@Hourly:45", "living.ambient@Hourly:50",
},
["Actor.Needs"] = {
    "living.needs@Hourly:30",
    // "living.eatOnArrival@PerTick:22"   // - SİLİNİR (§5.2)
    "living.action_resolve@PerTick:19",   // + YENİ (Doc 03): Consume commit hunger düşürür
    "living.consumption@Hourly:35",       // DARALIR: yalnız uyku/fatigue yarısı
},
["Actor.Mind"] = {                        // + YENİ ALAN (Doc 01)
    "living.decision@PerTick:18",         // intent + action START
    "living.action_move@PerTick:19",      // faz ilerletme / terminal işaretleme
},
["World.Reservations"] = {                // + YENİ ALAN (bu doküman)
    "living.decision@PerTick:18",         // claim + expiry sweep
    "living.action_resolve@PerTick:19",   // consumed/failed release
},
["World.Stockpiles"] = {
    "world.harvest@Daily:25",
    // "living.eatOnArrival@PerTick:22"   // - SİLİNİR
    "living.action_resolve@PerTick:19",   // + YENİ: Consume commit stok düşürür
    "living.consumption@Hourly:35",       // geçiş süresince; dilim sonunda satırdan düşer
    "living.ambient@Hourly:50", "econ.trade@Daily:28",
},
```

Net kazanç (teşhis §2.3): `Actor.Needs`/`World.Stockpiles` üzerindeki PerTick anonim yazar,
yerini faz-sahibi tek yazara bırakır; `Actor.Position`'da schedule daralır.

---

## 7. 950-aktör performans bütçesi

- Kapı taraması: 950 × 4 alan okuması / tick — bugünkü `ScheduleSystem.Advance` (:59-81) +
  `TickArrivals` (:81-87) çifte taramasından daha ucuz; eatOnArrival kalkınca tarama sayısı
  NET AZALIR.
- Pahalı yol (pile tarama + reserve): yalnız "aç + boşta" aktör; karar sonrası aktör actionlı
  → amortize aktör başına yemek başına 1. En kötü durum (kıtlık: herkes aç, stok yok) dahi
  O(A × P): mevcut `TickArrivals`'ın bugünkü en-kötüsü ile aynı sınıf; P (yemekli pile) onlar
  mertebesinde. TICKPERF dersleri (`NeedConsumptionSystem.cs:71-77` hoist, `:131-134` fail-fast)
  cache paylaşımıyla korunur.
- Ledger: tüm sorgular O(1) sözlük; sweep O(aktif rezervasyon) ≤ aktör sayısı.
- Allocation: aday yokken 0; adaylıkta tick başına 1 cache (bugünkü `TickArrivals` :78-79 zaten
  her tick tahsis ediyor — regresyon yok, çoğu tick iyileşme).
- `TickPerf` eşiği (`WorldTickComposer.cs:216-222`, >12ms uyarı) mevcut izleme ile yeterli;
  yeni ölçüm altyapısı gerekmez.

---

## 8. Loglama — tek LogManager'dan, her faz

W32 ortak kesitinden kullandığım asgari kontrat (kısıt: Domain/Simulation saf kalır; IO yok):

```csharp
// LogManager.Phase: (1) WorldEventLog'a DETERMİNİSTİK yapısal event (digest'e girer),
// (2) EmberLog debug aynası (yetkisiz, determinizme etkisiz). Stamp DAİMA ctx.Stamp'tir
// (kadans-sınırı damgası — NeedConsumptionSystem.cs:29-31 catchup dersi).
LogManager.Phase(world, ctx.Stamp, WorldEventKind.IntentChosen,     actorId, siteId, "intent:eat hunger:62");
LogManager.Phase(world, ctx.Stamp, WorldEventKind.ResourceReserved, actorId, siteId, "res:12 tag:wheat until:1420");
LogManager.Phase(world, ctx.Stamp, WorldEventKind.ReservationReleased, actorId, siteId, "res:12 reason:consumed|failed|expired");
```

`WorldEventKind` (WorldEventKind.cs:12-42) SONUNA eklenir (append-only; eski save uyumu):
`IntentChosen`, `ResourceReserved`, `ReservationReleased`. Karar ÜRETİLMEYEN tick'ler
loglanmaz (per-tick spam yasağı — `NeedConsumptionSystem.cs:51-52` dersi); teşhis §10 zinciri
`NeedThresholdCrossed → IntentChosen → ResourceReserved → …` için eşik-aşımı event'i Doc 04/05
(needs) kapsamındadır.

---

## 9. Kabul testleri ve sonraya genelleşenler

Yeni testler (teşhis §10'dan bu dokümanın payı):

1. `LastUnit_TwoHungryActors_OneReservationOneRefusal` — stok 1, iki aç aktör, aynı tick:
   deterministik olarak ilk aktör rezerve eder, ikinci intent alamaz; iki koşuda kimlikler aynı.
2. `Reservation_ReleasedExactlyOnce` — consumed sonrası expiry sweep'i no-op.
3. `Reservation_ExpiresAndFreesUnit` — TTL geçince efektif stok geri gelir; `reason:expired` loglanır.
4. `DeadActorReservation_SweptDeterministically`.
5. `Ledger_GoldenRoundtrip` — §4.5 satırı golden teste eklenir (mevcut reflection diff kanıtlar).
6. `DecisionSystem_SkipsActorsWithRunningAction` — kapı 3.
7. Chunking: decision kayıtlı registry ile `CadenceChunkingInvarianceTests` yeşil kalır
   (40-tick sıçrama = 40×1 tick; rezervasyon TTL'leri dakika-tabanlı olduğundan bölünmeye duyarsız).
8. Fallback harness yeşil kalır: dilim bayrağı kapalıyken (bkz. W32 geçiş planı) eski yol birebir.

Sonraya genelleşenler (ŞİMDİ YAPILMAZ):

- **Adet → instance rezervasyonu**: yemek `ItemStore`'da gerçek stack olduğunda `ReservationRecord`
  `(SiteId,ItemTag)` yerine `ThingId` taşır; API imzası bunun için `siteId+tag`'i tek
  "kaynak anahtarı" parametresinde tutar.
- **Yatak/tarla/worksite rezervasyonu**: aynı ledger, farklı tag evreni (teşhis §6 Bed→Reserve,
  Plant→Tend). `ReservationLedger` kasıtlı olarak "food" kelimesi içermez.
- **Algı sınırı**: "bilinen pile" bugün global; PerceivedFacts geldiğinde `FoodPileCache`
  aktör-görüşüne filtrelenir (imza `cache` parametresi bunun dikişi).
- **Rest/Work kuralları**: `IDecisionRule` dizisine eklenir; ScheduleSystem utility çekirdeği
  (:115-148) kural kural boşalır ve sistem yalnız klasik rota artığı kalınca silinir.

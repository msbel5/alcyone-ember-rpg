# W34 / 01 — SLEEP Dilimi: Rest Niyeti, MoveToBed, Sleep

> RUH_TESHIS §8 madde 4: "Yemek, uyku, iş, hasat typed action olur" — EAT (5049d445) ve
> FARM (61e340f3) dilimlerinden sonra sıra uykuda. §6 affordance listesi yatağı çoktan
> tarif etti: `Bed → Reserve, Sleep`. §10 kabul testi hakem: "Uyku toparlanması yalnızca
> aktif Sleep action ve uygun yatakta olur" ve "Activity etiketi CurrentAction ile bire bir
> aynıdır". Bu belge W32/W33 kalıplarını YENİDEN KULLANIR, yeniden icat etmez: aynı struct,
> aynı ledger, aynı advancer şablonu, aynı log dilbilgisi, aynı save/digest deseni.
> Anayasa değişmedi: determinizm, all-zero-extends-Idle save uyumu, chunking invariance
> hakemliği, düşük LOC, kısıt açıklayan yorumlar.

## Karar özeti

| Konu | Karar |
|---|---|
| Intent | `ActorIntent`'e append: `Rest = 4` |
| Action | `ActorActionType`'a append: `MoveToBed = 8`, `Sleep = 9` |
| Failure | YENİ DEĞER YOK — `Interrupted` (takip uyandırır), `ReservationLost` (satır/TTL), `Unreachable` (yataktan itilme), `TimedOut` (şafak yolda yakalar — W32 değerinin ilk canlı kullanımı). `ActionLogReason` da değişmez |
| Yatak | Aktörün KENDİ `Home` hücresi — yatak mobilya varlığı henüz YOK; HÜCRE yataktır (§3.4 gelecek yükseltme) |
| Yatak rezervasyonu | `ReservationLedger` DEĞİŞMEDEN; tag `"bed:{x}:{y}"` (hücre anahtarı, plot-key emsali), `SiteId: 0`, kapasite = o hücreyi `Home` bilen SAĞ aktör sayısı (§3.2) |
| Aile kuralı | "Aynı `Home` hücresi = aile" — worldgen'in ev ataması aile TANIMIDIR; ayrı veri modeli yok. Karar yalnız KENDİ `Home`'unu hedefler → yabancı yapısal olarak rezerve EDEMEZ (§3.2) |
| Toparlanma | Tick başına, YALNIZ Running Sleep'te: 3 tick'te 2 puan = saatte 40 — emekli fiat oranı birebir, tam-sayı aritmetiği (§5.3) |
| Karar uygunluğu | Gece penceresi + `Fatigue >= 1` + `CurrentAction == None` (mid-chain kapısı) + Player/Enemy hariç + guard'a canlı-takip muafiyeti (guards-eat emsali) (§4) |
| Bitiş | Şafak (06:00) Sleep'i `Succeeded` yapar; takip/kombat `Failed(Interrupted)` ile uyandırır (§5.3) |
| Emeklilik | `NeedConsumptionSystem.Tick` gece fiat'ı + `living.consumption@Hourly:35` adımı; `IsAsleepAtHome` projeksiyon tahmini; gece guess etiketleri; `NightCurfewView.Prowler` bayrağı (§6) |
| Save | `ActorSaveData`'ya SIFIR yeni alan — mevcut mind alanları yetiyor; yalnız `TryRestore` aralık tavanları yükselir (§7) |
| Digest | SIFIR yeni alan (intent/action/phase + fatigue zaten yazılıyor); davranış değiştiği için goldenlar bir kez, tarihli, EN SON yeniden baseline'lanır (§7) |

---

## 1. Kanıt: bugünkü uyku gerçeği (okunan kod)

- `Assets/Scripts/Simulation/Living/NeedConsumptionSystem.cs:26-47` — fiat'ın tamamı:
  `Tick(world, hourOfDay)` saatte bir koşar; `:29-30` gece penceresi
  (`hourOfDay >= 22 || hourOfDay < 6`, sabitler `:20-21`); `:39-45` koşulsuz çekirdek:
  `Fatigue > 0` olan her sivil ve HER GUARD (rol filtresi `:35` yalnız Player/Enemy atlar)
  yürüyüş yok, yatak yok, eylem yok — saat başına `Fatigue - 40`
  (`NightSleepFatigueRecovery`, `:19`) + mood yeniden değerlendirme. `:37-38` yorumu
  itiraf ediyor: kayıt bile YOK ("Sleep is intentionally UNLOGGED").
- `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs:362-374` — fiat'ın kompozer
  evi: `ConsumptionStep`, `living.consumption@Hourly:35` (`:367`), kayıt `:60`.
- `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldProjection.cs:93` —
  UI uykuyu TAHMİN ediyor: `sleeping: IsAsleepAtHome(actor)`. Gövde `:100-108`: rol kapısı
  (`:102`), saat penceresi (`:103-104`, 22/6 LİTERAL kopya — sabit bile paylaşılmamış),
  `Home`'a Chebyshev <= 1 (`:105-107`). `:99` yorumu kendi ölüm fermanını taşıyor:
  "GUESS(SLEEP slice): replace with ActionState.CurrentAction == Sleep."
- Aynı dosya `:131-133` — gece sözcük tahminleri: `"sleeping"` / `"heading home"` (`:132`)
  ve `"winding down"` (`:133`), üçü de `GUESS(SLEEP slice)` etiketli.
- `Assets/Scripts/Presentation/Ember/Views/ActorView.cs:137` — `SetSleeping`'i besleyen tel
  DOĞRULANDI: `_curfew?.SetSleeping(state.Sleeping)`; `state.Sleeping` projeksiyonun `:93`
  tahmininden gelir. Yani `NightCurfewView` ZATEN sim-push'tur (`NightCurfewView.cs:45-48`
  yorumu) — değişmesi gereken tel değil, telin taşıdığı ANLAM: tahmin yerine eylem gerçeği.
- `Assets/Scripts/Presentation/Ember/WorldDirector/NightCurfewView.cs:15,48` — `Prowler`
  bayrağı sim gerçeğini İKİNCİ kez tahmin ediyor (kim uyur kim uyumaz görüş kararı);
  atama `EmberGeneratedActorSpawner.cs:170-171` (sprite adından "guard"/düşman sezgisi).
- `Assets/Scripts/Simulation/Living/ScheduleSystem.cs:116-122` — gece eve yürüyüş ZATEN
  var: rest faydası (`Fatigue + NightRestBonus`) kazanınca hedef `actor.Home`; guard'lar
  için `:128-133` `ClassicTarget` gece koşulsuz `Home` döner. `:48-51` W32 kapısı: aktif
  eylem varsa schedule bacaklara DOKUNAMAZ — MoveToBed/Sleep bu kapının arkasında güvende.
- `Assets/Scripts/Simulation/Living/NeedsSystem.cs:22` — basınç yarısı: `FatigueIncreasePerTick = 6`
  (saatlik). Fiat dönemde gece net toparlanma 40-6=34/saatti; eylem dönemi aynı neti korur.
- W32/W33 mirası (aynen yeniden kullanılacak): `ActorActionState` struct + geçişleri
  (`ActorActionState.cs`), `ReservationLedger` (aktör başına EN FAZLA 1 satır `:37-38`,
  efektif stok `:35-36`, süpürme `:77-92`), `ActionAdvancer` şablonu (takip sondası →
  step → `TransitionTo` tek dikiş, `ActionAdvancer.cs:32-62`), `ActionLifecycleSystem`
  Decide(@PerTick:18)/Advance(@PerTick:22) tek-yazar çifti, `FarmOperations` anahtar
  kodek emsali (`"plot:"`/`"carry:"`, `FarmOperations.cs:26-32`), `ActionVerbTable`
  birebir-etiket sözlüğü.
- Uykuya değen ama BU dilime girmeyen ölü yol: `NeedRecoverySystem.Sleep` (tarif-tabanlı
  Phase-4 atomu) — kompozerden HİÇ çağrılmıyor (tek referansları kendi dosyaları);
  advancer ona delege ETMEZ (§10).

Teşhisin cümlesiyle: bugün uyku bir eylem değil, gece saatlerinde koşan bir çıkarma
işlemidir; UI ise uyuyanı saatten ve konumdan TAHMİN eder. RUH_TESHIS §2.9'un ta kendisi.

---

## 2. Sözcük dağarcığı genişlemesi (hepsi append-only)

Save'e int yazılan her enum'da değerler SABİTTİR; silme/yeniden numaralama yasak
(`ActorActionState.cs:6` başlık kuralı). Tüm eklemeler mevcut son değerin ARKASINA gelir.

```csharp
// Assets/Scripts/Domain/Actors/ActorActionState.cs — mevcut üyeler aynen kalır.
public enum ActorIntent { None = 0, Eat = 1, Plant = 2, Harvest = 3, Rest = 4 }

public enum ActorActionType
{
    None = 0, MoveToFood = 1, TakeFood = 2, ConsumeFood = 3,
    MoveToPlot = 4, PlantSeed = 5, HarvestCrop = 6, HaulCrop = 7,
    // W34 SLEEP slice (append-only; values are saved as ints and MUST stay fixed):
    MoveToBed = 8, Sleep = 9,
}
```

`ActionFailureReason` ve `ActionLogReason` DEĞİŞMEZ. Uykunun üç ölümü mevcut sözlükle
anlatılır ve üçü de ayrı cümledir:

| Olay | Reason | Hikâye |
|---|---|---|
| Takip/kombat uyandırdı | `Interrupted` | "Ayşe'yi gece yarısı kapı sesi uyandırdı" — `ActionAdvancer.Advance` sondası (`:36-40`) ZATEN üretir, bedava |
| Satır kayboldu / TTL süpürüldü | `ReservationLost` | mis-TTL sınıfı; deterministik, nadir |
| Yataktan itildi (witness nudge sınıfı) | `Unreachable` | `ConsumeFoodAdvancer.cs:38-42` erişim-doğrulama emsalinin yatağa uyarlanması |
| Şafak yolda yakaladı (MoveToBed bitmeden gece bitti) | `TimedOut` | W32'den beri enumda duran değerin ilk canlı kullanımı; `ToLogReason` default kolu (`ActionAdvancer.cs:101`) `InterruptPreempted`'e çevirir — yeni satır GEREKMEZ |

`ActionAdvancerRegistry.cs:15` dizi tavanı `(int)ActorActionType.HaulCrop + 1` →
`(int)ActorActionType.Sleep + 1` (tek satır; kayıt sırası davranışı etkilemez).
`ActionLifecycleSystem` kurucusuna iki advancer kaydı eklenir.

`ActionVerbTable`'a dört satır — emekli tahmin sözlüğü BİREBİR korunur, artık doğru
söyler (playtest sürekliliği: ekrandaki kelimeler değişmez, kaynağı değişir):

```csharp
// ActionVerbTable.Verb — W34: gece fiillerinin sahibi artık gerçek eylemler.
ActorActionType.MoveToBed => "heading home",
ActorActionType.Sleep => "sleeping",
// ActionVerbTable.KindName
ActorActionType.MoveToBed => "MoveToBed",
ActorActionType.Sleep => "Sleep",
```

---

## 3. Yatak = `Home` hücresi; rezervasyon anahtarı

### 3.1 Anahtar kodlaması

Yatak mobilya varlığı bu dilimde YOK — aktörün `Home` hücresi (`ActorRecord.cs:73`)
yatağın kendisidir. `ReservationLedger` satırı (site, tag) anahtarlıdır; yatağın sitesi
yoktur, adresi hücredir:

```csharp
// Assets/Scripts/Simulation/Living/Actions/SleepOperations.cs — FarmOperations'ın aynası.
// CONSTRAINT (namespace disjointness, FarmOperations.cs:8-13 emsali): "bed:" öneki HİÇBİR
// StockpileComponent tag'ine ya da FoodPileCache.FoodTags evrenine SIZAMAZ — pile'a ulaşan
// önekli tag efektif-stok matematiğini bozar. Ledger tag'i asla parse etmez; yalnız bu sınıf eder.
private const string BedPrefix = "bed:";
public static string BedKey(GridPosition home)
    => BedPrefix + home.X.ToString(CultureInfo.InvariantCulture)
     + ":" + home.Y.ToString(CultureInfo.InvariantCulture);
public static bool TryParseBedKey(string itemTag, out GridPosition home) { /* plot-key deseni */ }
```

Ledger satırının `SiteId`'si `0UL` — yatak site-kapsamlı hiçbir süpürmeye/sayıma
karışmaz; `ActionLogEntry` site alanı da 0 kalır (`ActorActionState.TargetSiteId` uyku
zincirinde `Empty` — hedef hücre her adımda `actor.Home`'dan CANLI okunur, W33'ün
"struct id taşır, adres taşımaz" kuralı; `MoveToPlotAdvancer.cs:9-10` emsali).

### 3.2 Aile paylaşım kuralı: rezidans

Görev tanımı: "iki aktör bir evi PAYLAŞABİLİR — kural: aile paylaşır, yabancı paylaşamaz;
en basit deterministik kural kazanır." Domain'de aile/hane veri modeli YOK ve bu dilim
eklemez. En basit deterministik kural — REZİDANS:

1. Karar yalnızca aktörün KENDİ `Home` hücresini hedefler. Yabancının o yatağa
   rezervasyon denemesi yapısal olarak İMKÂNSIZDIR — kod yolu yok.
2. "Aynı `Home` hücresini bilen aktörler" ailenin TANIMIDIR — worldgen ev atarken aileyi
   aynı hücreye koyar; ayrı bir `FamilyId` bu dilimde gereksiz kabuktur.
3. Kapasite = o hücreyi `Home` bilen SAĞ aktör sayısı (`Decide` sırasında tembel sayım —
   aktör döngüsü zaten dönüyor). `TryReserve(0UL, BedKey(home), actorId, until,
   residentCount, out id)` — aile üyelerinin her biri kendi satırını alır, sayı kapasiteyi
   aşamaz.

Rezervasyon "her zaman başarılı olacaksa niye var" DEĞİL — satır dört iş görür:
(a) zincirin her adımdaki standart doğrulama kapısı (satır yok/uyumsuz →
`ReservationLost`, W32 sözleşmesi); (b) aktör başına EN FAZLA 1 satır kuralı
(`ReservationLedger.cs:37-38`) uyku ile yemek/tarla zincirlerinin çakışmasını ledger
katında da keser; (c) TTL süpürmesi takılı kalmış gece durumlarını deterministik
temizler; (d) gelecekteki yabancı-misafir/han/yatak-mobilyası senaryolarının kapasite
kancası ŞİMDİDEN bu satırdır (§3.4).

### 3.3 TTL

W32-02 §4.3 mesafe-ölçekli deseni, süre "şafağa kadar":

```csharp
// 1 tick = 1 game minute (ActionLifecycleSystem.cs:189 emsali).
long walk = FarmOperations.Chebyshev(actor.Position, actor.Home);
long dawn = SleepOperations.MinutesUntilDawn(stamp); // ((6 - Hour + 24) % 24) * 60 - Minute
long until = stamp.TotalMinutes + walk + dawn + 60;  // yürüyüş + uyku + tampon
```

### 3.4 Gelecek: yatak mobilyası (dilim DIŞI, dikiş BURADA)

Yatak bir `ItemId`'li mobilya varlığı olduğunda değişecek olan TEK şey anahtar ve
kapasitedir: `"bed:{x}:{y}"` → `"bed:{itemId}"`, rezidans sayımı → mobilyanın kendi
kapasite alanı, arrival hücresi → mobilyanın hücresi. Zincir, advancer şablonu, faz
makinesi, save yüzeyi AYNEN kalır. `SleepOperations` kodek tekelinin varlık nedeni bu
dikişin tek dosyada kalmasıdır.

---

## 4. Zincir ve karar uygunluğu

Zincir sabittir, SAVED DEĞİLDİR, intent'ten türetilir (W32-01 §8 kuralı):

```csharp
// ActionLifecycleSystem.NextLink — iki satır eklenir (:127-136).
(ActorIntent.Rest, ActorActionType.MoveToBed) => ActorActionType.Sleep,
// (Rest, Sleep) → None: şafak tamamlar, Idle'a düşer, sabah kararı temiz başlar.
```

`Decide` (@PerTick:18) yeni kuralı — kod sırası öncelik sırasıdır (W33-02 §5 doktrini),
uyku EAT'ten SONRA gelir (açlık yatağı yener; aç uyuyan önce yer, ertesi karar tick'inde
yatar):

```csharp
// ActionLifecycleSystem.Decide — mevcut kapılardan geçen aktör için (:52-61 aynen:
// ölü/Player/Enemy atla, CurrentAction != None atla [mid-chain kapısı — görevin
// "not mid-chain" şartı ZATEN bu satırdır], guard canlı-takipte atla) eat kuralından sonra:
if (SleepOperations.IsNightHour(stamp.Hour)          // tek gerçek: NeedConsumptionSystem sabitleri
    && actor.Needs.Fatigue.Value >= SleepOperations.FatigueSleepThreshold)  // = 1: fiat'ın ">0" kapısı birebir
    TryDecideRest(world, actor, stamp);
```

- **Gece penceresi**: `NightStartHour`/`NightEndHour` sabitleri `NeedConsumptionSystem`'de
  KALIR (`:20-21`) ve `SleepOperations.IsNightHour` oradan okur — projeksiyonun `:104`
  literal kopyası da bu dilimle ölür, 22/6 gerçeği TEK dosyada yaşar
  (`HungerEatThreshold` ithalat emsali, `ActionLifecycleSystem.cs:62`).
- **Eşik**: `FatigueSleepThreshold = 1` — fiat'ın `Fatigue.Value > 0` kapısının
  (`NeedConsumptionSystem.cs:39`) birebir korunması; davranış-koruma en basit kuralı da
  seçmiş oluyor. Ayarlanabilir sabittir, karar tablosu değildir.
- **Roller**: fiat rol filtresi birebir (`:35`): Player/Enemy HARİÇ, GUARD DAHİL. Fiat
  guard yorgunluğunu da sildiği için guard'ı dışlamak sessiz bir gerileme olurdu (mood
  tabana kilitlenir). Guard'a guards-eat emsalindeki canlı-takip muafiyeti uygulanır
  (`HasLivePursuit`, `:61` — takip uykudan da üstündür). Görünür değişim: devriyesiz
  guard gece gerçekten eve gidip yatar; `ScheduleSystem.ClassicTarget` onu zaten eve
  yürütüyordu (`:128-133`), "on watch" etiketi yalan söylüyordu. Gece nöbet vardiyası
  GUARD dilimin işidir, bu dilimin değil.
- **TryDecideRest** gövdesi `TryDecideEat`/`TryDecidePlant` kalıbıdır: kapasite sayımı →
  `TryReserve` (§3) → başarısızsa sessiz düş (yatak dolu — bu tick değil) →
  `ForIntent(Rest).Start(MoveToBed, SiteId.Empty, ItemId.Empty, reservation,
  stamp.TotalMinutes, Interruptible)` → `TransitionTo(..., ReservationAcquired, stamp)`.

Bilinçli davranış değişimleri (fiat dönemine göre):

1. Uyuyan aktör gece acıkırsa KALKMAZ — `CurrentAction != None` kapısı karar sistemini
   şafağa kadar susturur; kahvaltı şafağın ilk karar tick'indedir. (Fiat dönemde 02:00'de
   "uyurken" yemeğe yürüyen aktör tuhaflığı vardı — o tuhaflık ölür.)
2. Toparlanma artık YÜRÜYÜŞÜ ve YATAĞI şart koşar: eve varamayan (takip, kilitli yol)
   aktör o gece toparlanamaz. RUH_TESHIS §10 kabulünün ta kendisi — kesinti artık hikâye
   üretir ("Mehmet dün gece uyuyamadı").

---

## 5. Advancer tasarımı

### 5.1 SleepOperations — FarmOperations'ın aynası

Tek dosyada: `BedKey`/`TryParseBedKey` kodeki (§3.1), `IsNightHour(int hour)`,
`MinutesUntilDawn(GameTime)`, `ResidentCount(WorldState, GridPosition)`,
`BedReachCells = 1` sabiti. Sabit gerekçesi: `IsAsleepAtHome`'un Chebyshev <= 1 emsali
(`WorldProjection.cs:105-107`) gerçek varış kuralı olur — aile üyeleri aynı hücreye
İSTİFLENMEZ, 3x3 "yatak odası" içinde yerleşir (MoveToPlot'un tam-hücre kuralı plotlar
1:1 kilitli olduğu için oradaydı; yatak hücresi paylaşımlıdır, tolerans şarttır).

### 5.2 MoveToBedAdvancer

`MoveToPlotAdvancer` iskeleti (`:29-77`), hedef `actor.Home`:

- Her adım doğrulama: satır var + `row.Id == state.ReservationId` +
  `TryParseBedKey(row.ItemTag) == actor.Home` (gece yarısı ev değişimi = satır yalanı →
  `ReservationLost`).
- Gece bitti mi? `!IsNightHour(stamp.Hour)` → `Fail(TimedOut)` — şafak yolda yakaladı,
  uyku anlamsız; satır `Fail` kapısında serbest kalır (`ActionAdvancer.cs:65-90`,
  taşınan yük yok, konservasyon dalları dokunulmaz).
- Yürüyüş: `MovementService.StepToward` ile tick başına bir hücre;
  `Chebyshev(Position, Home) <= BedReachCells` → `Succeeded` (`Arrived` log), değilse
  `Advanced()` (`ProgressTicked`).

### 5.3 SleepAdvancer

Çok saatlik Running eylem; toparlanma TICK BAŞINA ve YALNIZ burada:

```csharp
// Emekli fiat oranı birebir: NightSleepFatigueRecovery(40)/saat, tick'e serilmiş hali.
// 40/60 = 2/3 → her 3. Running tick'inde 2 puan. Tam-sayı, float YOK (determinizm anayasası);
// ProgressTicks state'te taşındığı için chunking sınırından bağımsız (stateless advancer kuralı).
public const int RecoveryPerStep = 2;
public const int TicksPerRecoveryStep = 3;
```

Step sırası (şablonun takip sondası `Advance`'ta zaten koştu — yakalanan uyuyan
`Fail(Interrupted)` ile UYANIR, bedava):

1. Satır doğrulama (§5.2 ile aynı üçlü) — düşerse `ReservationLost`.
2. Yatak erişimi: `Chebyshev(Position, Home) > BedReachCells` → `Fail(Unreachable)`
   (`ConsumeFoodAdvancer.cs:38-42` yerinden-oynatılan-yiyici emsali).
3. Şafak: `!IsNightHour(stamp.Hour)` → satırı serbest bırak, `Succeeded`
   (`Completed` log). Terminal devir W32 kuralıyla bir sonraki advancement'ta tüketilir,
   `NextLink(Rest, Sleep) = None` → Idle → bacaklar schedule'a döner
   (`ActionLifecycleSystem.cs:106-119`).
4. Değilse `Advanced()`; yeni `ProgressTicks % TicksPerRecoveryStep == 0` ise
   `Fatigue - RecoveryPerStep` uygula + mood yeniden değerlendir (fiat `:44` paritesi;
   `NeedValue` 0'da kıskaçlar, fiat da kıskaçlıyordu). Fatigue 0'a inse de aktör ŞAFAĞA
   KADAR yatakta kalır — fiat dönem görüntüsüyle birebir, ve gece yarısı amaçsız gezinen
   aktör üretmez.
5. Log dilbilgisi değişmez: faz SINIRLARI loglanır, tick ilerlemesi loglanmaz
   (`ActionAdvancer.cs:46-49` B21 dersi) — fiat'ın "spam korkusuyla hiç loglamama"
   çaresizliği, gecede tek `Started`/tek `Completed` satırına dönüşür. Ayrı bir
   `WorldEvent` YAYINLANMAZ (meal_eaten'ın aksine uykunun sayaç okuyucusu yok; en az LOC).

NeedsSystem'in saatlik +6 fatigue basıncı uykuda da işler — net toparlanma 34/saat,
fiat dönemin netiyle aynı (§1 kanıt). Basınç/toparlanma ayrımı RUH_TESHIS §6 sistem
listesindeki "NeedsSystem yalnızca basıncı artırır" cümlesine ilk kez tam oturur.

---

## 6. Emeklilikler

### 6.1 Fiat gece dalı

- `NeedConsumptionSystem.Tick` (`:26-47`) SİLİNİR; `NightSleepFatigueRecovery` sabiti
  SİLİNİR (tek dış tüketicisi `NeedConsumptionSystemTests.cs:53` pinidir — §9 göçü).
  Sınıf KALIR: `HungerEatThreshold`/`EatReachCells`/`MealHungerFloor`/`MealThirstRecovery`
  + `NightStartHour`/`NightEndHour` + `TryGetSiteCentre`/`FoodSpots` hâlâ eylem katmanının
  ithal ettiği tek-gerçeklerdir; sınıf başlık yorumu "sabitler + site-merkez gerçeği"
  rolüne yeniden yazılır.
- `DefaultTickSystems.ConsumptionStep` (`:362-374`) ve kaydı (`:60`) SİLİNİR —
  `living.consumption@Hourly:35` adımı ölür. Step kaydı FieldOwnershipRegistry'de
  fatigue yazarıysa satır SleepAdvancer'a devredilir (tek-yazar defteri güncel kalır).

### 6.2 Projeksiyon tahmini

- `WorldProjection.cs:93` → `sleeping: actor.ActionState.CurrentAction == ActorActionType.Sleep`.
  `IsAsleepAtHome` (`:97-108`) SİLİNİR — kendi `GUESS(SLEEP slice)` fermanının infazı.
- `DescribeScheduleWord` gece dalları (`:131-133`: `"sleeping"`, `"heading home"`,
  `"winding down"`) SİLİNİR. Kelimelerin ikisi `ActionVerbTable`'a taşındı (§2) ve artık
  yalnız gerçek MoveToBed/Sleep eylemlerinden doğar; `"winding down"` (20:00-22:00
  tahmini) VARİSSİZ ölür — W32 DOC5 §4 kuralı: yeni fiil = yeni eylem tipi, tahmin dalı
  değil. Etiket sözleşmesi (`ActivityLabelTruthTests`) iki yeni satırla genişler:
  RUH_TESHIS §10 "Activity etiketi CurrentAction ile bire bir aynıdır."

### 6.3 NightCurfewView sim-push semantiği

Tel doğrulandı (§1): `WorldProjection:93 → ActorViewState.Sleeping → ActorView.cs:137 →
SetSleeping`. `NightCurfewView`'ın kendisi tek satır bile değişmeden eylem gerçeğine
bağlanır — rewire tamamen `:93`'tedir. İKİ istisna:

- `Prowler` bayrağı (`NightCurfewView.cs:15`, `:48`; atama
  `EmberGeneratedActorSpawner.cs:168-171`) SİLİNİR. Varlık nedeni tahmin çağıydı: saat
  tahmini herkese "uyu" derken görüşün guard/haydutu ayıklaması gerekiyordu. Artık kim
  uyuyorsa SİM söylüyor: Enemy'ye Sleep kararı hiç verilmez, guard yalnız gerçekten
  yattığında `Sleeping=true` olur. Bayrak kalsaydı yatan guard'ın etiketi "sleeping"
  derken poz ayakta kalırdı — görüş simi bir kez daha İKİNCİ kez tahmin ederdi.
  (Runtime'da atanır, sahne serileştirmesinde `Prowler` alanı beklenmez; yine de silme
  commit'i sahne diff'i sıfır olmalı — §11 risk.)
- `_sleepingFromSim` adı ve 2sn poll iskeleti aynen kalır — presentation-only durum,
  digest'e girmez.

---

## 7. Save + digest (all-zero deseni)

- `ActorSaveData`'ya YENİ ALAN YOK. Uyku zinciri mevcut alanlarla anlatılır: intent,
  action, phase, reservation id, progress, startedAt (`ActorSaveMapper.cs:71-82` aynası
  değişmeden doğru kalır). `TargetItemId`/`TargetSiteId`/`CarriedUnits` uykuda hep
  0/Empty — default(struct) = Idle bit deseni bozulmaz, W31-öncesi save'ler Idle yükler.
- `TryRestore` aralık tavanları (`ActorActionState.cs:211-212`):
  `intent > ActorIntent.Harvest` → `> ActorIntent.Rest`;
  `action > ActorActionType.HaulCrop` → `> ActorActionType.Sleep`.
  `carriedUnits > 0` kuralı (`:228-229`) DEĞİŞMEZ — uyku el taşımaz, Harvest/Haul
  dışındaki taşıyıcı hâlâ transition-unreachable. Bozuk blok → Idle normalizasyonu
  (`ActorSaveMapper.cs:163-169`) aynen: yarım yüklenen uykucu sabah yeniden karar verir.
- Digest'e YENİ ALAN YOK: `CurrentIntent`/`CurrentAction`/`Phase`/`ProgressTicks` zaten
  satırda (`WorldStateDigest.cs:100-121`), fatigue zaten satırda (`:97`) — iki dünyanın
  uyku ayrışması bugünkü digest'te bile görünür. Chunking invariance hakemi
  (`ActionPhaseChunkingInvarianceTests` sınıfı) yeni zinciri bedavaya denetler; advancer
  stateless + tüm ilerleme state'te olduğu sürece kural ihlali yapısal olarak imkânsız.
- Goldenlar: gece davranışı değiştiği için (fatigue eğrileri, action log satırları,
  actor pozisyonları) marathon/proof goldenları BİR KEZ, tarihli, yeşile ulaştıktan
  SONRA EN SON yeniden baseline'lanır — W33-B protokolü.

---

## 8. Hikâye testleri (Assets/Tests/EditMode/Actions, T/F serilerinin devamı — S serisi)

Fixture: `SleepSliceWorld` (FarmSliceWorld deseni) — 1 sivil + `Home` hücresi + saat
kadranı; aile senaryosu için aynı `Home`'lu 2. sivil; takip senaryosu için pursuit satırı.

| # | Test | Pin |
|---|---|---|
| S1 | `SleepStoryChainTests` | 22:00, Fatigue 80: Rest → rezervasyon (`bed:` satırı) → MoveToBed yürüyüşü → `Arrived` → Sleep Running → 06:00 `Completed` → Idle; fatigue eğrisi 3-tick'te-2 merdiveni, satır serbest |
| S2 | `SleepRecoveryOnlyInBedTests` | RUH_TESHIS §10 kabulü: Sleep Running DIŞINDA hiçbir gece tick'i fatigue DÜŞÜRMEZ (fiat'ın mezar taşı); yatağa varamayan aktör sabaha yorgun çıkar |
| S3 | `SleepInterruptionTests` | Gece yarısı pursuit satırı: `Failed(Interrupted)` + satır serbest + kısmi toparlanma KALIR (o ana dek işlenen tickler geri alınmaz); replan ertesi karar tick'i |
| S4 | `SleepBedShareTests` | Aynı `Home`'lu iki sivil: iki `bed:` satırı yan yana (kapasite=2), ikisi de uyur; farklı `Home`'lu üçüncünün o hücreye satırı HİÇBİR kod yolundan doğamaz |
| S5 | `SleepDawnBoundaryTests` | 05:59 Running / 06:00 `Succeeded` sınır tick'i; MoveToBed 06:00'da `TimedOut` |
| S6 | `SleepChunkingPhaseTraceTests` | F6/EatChunking deseni: 1'li ve N'li chunk koşuları aynı faz izi + aynı digest |
| S7 | `ActivityLabelTruthTests` genişlemesi | MoveToBed→"heading home", Sleep→"sleeping" birebir; `Sleeping` bayrağı yalnız `CurrentAction == Sleep` iken true |
| S8 | Save roundtrip | Gece yarısı kaydet/yükle: Running Sleep aynı `ProgressTicks` ile sürer (W32 T4 deseni); bozuk intent=4/action=9 aralık kombinasyonları Idle'a normalize |

---

## 9. Pin göçleri

| Mevcut pin | Kader |
|---|---|
| `NeedConsumptionSystemTests.cs:53` (`60 - NightSleepFatigueRecovery`) | SİLİNİR — varisi S1/S2; sınıfın kalan testleri (site-merkez/food-spot) yaşar |
| `LivingWorldGateTests` Gate1 `avgFatigue < 75` (`:52-57` "nobody sleeps") | Kalır ama artık eylem katmanı geçirir; eşik tutmazsa gece uzunluğuna göre yeniden kalibre — davranış pini, mekanizma pini değil |
| `NeedRecoverySystemSleepTests` | DOKUNULMAZ — ölü `NeedRecoverySystem.Sleep` atomunu pinliyor, kompozerde çağrısı yok (§10 not) |
| Proof/marathon goldenları | §7 gereği tarihli re-baseline, EN SON |

---

## 10. İleri notlar (bu dilimin DIŞI)

- Yatak mobilyası: §3.4 dikişi. `"bed:{itemId}"` + mobilya kapasitesi + `Bed → Reserve,
  Sleep` affordance'ının gerçek varlık üstünde ifadesi.
- Guard gece vardiyası: guard'ın "kimisi uyur kimisi nöbette" bölünmesi GUARD diliminin
  kararıdır; bu dilim fiat'ın "her guard her gece toparlanır" gerçeğini eylemleştirir.
- `NeedRecoverySystem`/`NeedRecoveryRecipe` ölü çifti: kompozer çağrısı yok, advancer
  delege etmez; ayrı bir temizlik commit'inde silinmeye aday.
- Uyuyanın rüya/memory üretimi, gece olay tanıklığı kısıtları (uyuyan görmez) — RUMOR/
  MEMORY dilimlerine.
- 20:00-22:00 "winding down" davranışı (eve erken yönelme): istenirse ScheduleSystem
  fayda eğrisinin işi; etiket tahmini geri GELMEZ.

---

## 11. Riskler

1. **Golden çalkantısı**: gece fatigue eğrisi tick-granüler hale geliyor (saatlik -40
   basamağı → 3-tick'lik -2 merdiveni); mood ara değerleri değişir → NeedMood pinleri ve
   marathon digest'i oynar. Panzehir: re-baseline protokolü EN SON, tek commit, tarihli.
2. **Kapasite sayımı canlılığı**: `ResidentCount` karar anındaki sağ aktör sayısıdır;
   gece ölüm/ev değişimi satır fazlalığı yaratabilir (kapasite 2 → 1 düşer, mevcut 2
   satır yaşar). Zararsız: satırlar TTL ile ölür, doğrulama üçlüsü ev değişimini
   `ReservationLost`'a çevirir — ama S-serisine bilinçli edge eklemeye değer.
3. **Guard görünür değişimi**: playtest "geceleri sokakta guard kalmıyor" diyebilir.
   Bilinçli karar (§4); gerekirse GUARD dilimi vardiya getirir. Proof-screenshot
   sürüşlerinde gece karesi guard'sız çıkacak — ekran-katmanı doğrulama notu.
4. **`Prowler` silme yüzeyi**: alan public serialized; sahnede elle atanmış kopya varsa
   Unity import diff'i üretir. Silme commit'inde sahne/prefab diff'inin SIFIR olduğu
   doğrulanır (gitStatus temiz sahne kuralı).
5. **Şafak sınır tick'i**: `IsNightHour` yarım-açık pencere ([22,06)) — MoveToBed'in
   `TimedOut`'u ile Sleep'in `Succeeded`'i AYNI predikatı paylaşmalı; iki ayrı saat
   karşılaştırması yazılırsa off-by-one çatalı doğar. Panzehir: predikat yalnız
   `SleepOperations.IsNightHour`'da yaşar (kısıt yorumu şart).

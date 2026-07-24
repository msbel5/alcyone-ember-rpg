# W32 / DOC 6 — EAT Dilimi Hikâye Testleri (RUH_TESHIS §10 → Somut EditMode Testleri)

> Kaynak teşhis: `docs/RUH_TESHIS.md` §10 ("Yeni kabul testleri") + §7 (hedef yemek zinciri) + §9 (ilk dikey dilim).
> Kapsam: **YALNIZCA EAT dilimi.** Worksite/iş, uyku, tarım-teleport ve input-output invariantları §10'da
> listelidir ama bu dokümanda bilinçli olarak KAPSAM DIŞIDIR — ikinci dilimle gelirler (bkz. son bölüm).

---

## 0. Test Sözleşmesi — Bu Testlerin Varsaydığı API Yüzeyi

Adlandırma sahibi kardeş dokümanlardır (aksiyon hiyerarşisi ve dünya operasyonları 02–04'te tanımlanır).
Testlerin derlenmesi için varsayılan asgari yüzey — isim değişirse test metinleri mekanik olarak güncellenir,
**iddialar değişmez**:

```csharp
// Domain/Actors — aktör artık mevcut eylemini TAŞIR (RUH_TESHIS §6):
actor.ActionState            // ActorActionState; null değil, Idle bir değer (None)
    .ActionId                // ulong — deterministik sayaçtan; bölüm boyunca SABİT
    .Action                  // EmberAction (abstract base) → EatAction : EmberAction
    .Phase                   // EatAction fazları: MoveToFood → TakeFood → ConsumeFood
    .Progress                // faz içi tick sayacı (ConsumeFood süresi: EatAction.ConsumeDurationTicks)
    .FailureReason           // kesilme/başarısızlıkta dolu; aksi halde null
    .ReservationId           // aktif rezervasyonun kimliği (yoksa 0)

// Domain/World — dünya yalnızca doğrulanmış operasyonla değişir:
world.Reservations           // ReservationStore: (pileSiteId, foodTag, actorId, reservationId)
FoodOps.TryReserve(world, actor, out reservationId)        // en yakın erişilebilir/bilinen food unit
FoodOps.TryTakeReserved(world, actor)                      // pile → aktör eli; MESAFE DOĞRULAR
FoodOps.TryCompleteConsume(world, actor)                   // eldeki unit yok olur + hunger düşer; MESAFE DOĞRULAR
FoodOps.Interrupt(world, actor, reason)                    // fazına göre iade/serbest bırakma

// Composer adımları (DefaultTickSystems; kesin sıra DOC 04'ün malı):
"living.decision"     PerTick  // idle+aç sivil için EatIntent + plan üretir (ScheduleStep'in yemek yarısının yerine)
"living.actionAdvance" PerTick // aktif aksiyonun TEK fazını ilerletir (Move adımı, Take, Consume tick'i)

// Olay grameri (tek LogManager'dan geçen faz logu + deterministik WorldEventLog kaydı):
// NeedThresholdCrossed → IntentChosen → ResourceReserved → Arrived → ItemTransferred → MealConsumed
// + ActionPhaseChanged, ReservationReleased, ActionInterrupted
```

Testlerin okuduğu iz, `WorldEventLog` üzerinden şu yardımcıyla üretilir (test destek dosyası):

```csharp
// Assets/Tests/EditMode/Actions/Support/ActionTrace.cs
// KISIT: yalnız Events okur — render/diagnostik log ASLA kanıt değildir (verify-at-render-layer
// dersi UI tarafında ayrıca geçerli; domain iddiaları domain izinden okunur).
static string Of(WorldState w) => string.Join("\n",
    w.Events.Events.Where(e => e.Kind is ActionPhaseChanged or MealConsumed or ResourceReserved
                              or ReservationReleased or ActionInterrupted or ItemTransferred)
        .Select(e => $"{e.Tick.TotalMinutes}:{e.ActorId.Value}:{e.Reason}")); // reason: "eat id:{id} phase:{faz}"
```

Ortak kurulum (satır sayısını düşük tutmak için tek builder — SOLID: test fixture'ların tek dünya-kurma yolu):

```csharp
// Assets/Tests/EditMode/Actions/Support/EatSliceWorld.cs
// Site(1) sınırı (0,0)-(10,10) → merkez (5,5); pile o sitede; EatOnArrivalTests kurulumunun birebir taşınması.
static WorldState Build(int wheat = 10)            // site + stockpile(wheat) + EnsureInvariants
static ActorRecord Hungry(ulong id, int x, int y)  // Talker, hunger=80 (eşik 55'in üstü), diğer needs rahat
```

Bütün testler **EditMode**, saf Domain/Simulation (Unity API yok, IO yok, RNG yalnız seed'li) — determinizm
anayasasına tabi: aynı seed + aynı tick dizisi = aynı WorldState + aynı olay izi.

---

## 1. Hikâye Testleri (RUH_TESHIS §10 → kod)

### T1 — Aktör uzaktan yemek yiyemez

**Dosya:** `Assets/Tests/EditMode/Actions/EatAtDistanceTests.cs`

Bugünkü kod bunu yalnız "arama yarıçapı" olarak biliyor (`NeedConsumptionSystem.TryEatCached`
erişim dışıysa `false` döner — Assets/Scripts/Simulation/Living/NeedConsumptionSystem.cs:141-149).
Yeni dilimde mesafe, **operasyonun kendisinin doğruladığı** bir precondition olur: sistem sırası ne olursa
olsun uzaktan `Take`/`Consume` FİZİKSEL olarak reddedilir.

**Kurulum:**
```csharp
var world = EatSliceWorld.Build(wheat: 1);          // pile merkezi (5,5)
var far = EatSliceWorld.Hungry(7, 50, 50);          // Chebyshev 45 >> EatReachCells(2)
world.Actors.Add(far);
// Aksiyonu ZORLA ConsumeFood fazına kur (faz sırasını bypass eden saldırgan kurulum):
far.ActionState.Debug_Set(EatAction.At(Phase.ConsumeFood, reservationId));
```

**Kesin iddia:**
```csharp
Assert.That(FoodOps.TryTakeReserved(world, far), Is.False, "uzaktan alma REDDEDİLİR");
Assert.That(FoodOps.TryCompleteConsume(world, far), Is.False, "uzaktan yeme REDDEDİLİR");
Assert.That(far.Needs.Hunger.Value, Is.EqualTo(80), "hunger kımıldamadı");
Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(1), "stok kımıldamadı");
Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.MealConsumed), Is.False);
Assert.That(far.ActionState.FailureReason, Is.EqualTo("too_far"), "op reddi sebep bırakır");
```

İkinci vaka (entegrasyon): composer ile 2 saat ilerletilir; her `MealConsumed` olayında olayın tick'indeki
aktör pozisyonu pile merkezine `<= EatReachCells` olmalıdır (iz üzerinden; faz logu pozisyon damgalı).

---

### T2 — Aynı SON food unit iki aktöre rezerve edilemez

**Dosya:** `Assets/Tests/EditMode/Actions/EatReservationConflictTests.cs`

Bugün rezervasyon YOK: ilk yiyen sayaç düşürür, ikincisi sessiz boş tarama yaşar (teşhis §2.5/§5).
Yeni dilimde "Ayşe son ekmeği rezerve eder; Mehmet bulamaz ve BUNU BİLİR" hikâyesi doğar.

**Kurulum:**
```csharp
var world = EatSliceWorld.Build(wheat: 1);                   // SON unit
world.Actors.Add(EatSliceWorld.Hungry(1, 4, 4));             // A — store sırasında ÖNCE
world.Actors.Add(EatSliceWorld.Hungry(2, 4, 5));             // B — aynı uzaklık sınıfı
var composer = new WorldTickComposer();
composer.Advance(world, 1);                                  // decision + reservation aynı tick bandında
```

**Kesin iddia:**
```csharp
Assert.That(world.Reservations.Count, Is.EqualTo(1), "tek unit → tek rezervasyon");
Assert.That(world.Reservations.Single().ActorId.Value, Is.EqualTo(1UL),
    "kazanan DETERMİNİSTİK: store insertion sırası (seed değil, sıra kırar)");
var b = world.Actors.Get(new ActorId(2));
Assert.That(b.ActionState.Action, Is.Not.InstanceOf<EatAction>(),
    "B'ye EatAction verilmez — intent'i 'no_food_available' ile düşer/replan olur");
// Bölüm sonuna kadar koşulunca madde korunumu:
RunUntilQuiet(composer, world);                              // A yer, B yiyemez
Assert.That(MealsOf(world), Is.EqualTo(1), "tek unit tek boğazdan iner");
Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(0));
```

Ek vaka: A rezervasyonu aldıktan sonra B için `FoodOps.TryReserve` doğrudan çağrılır → `false`
(rezerve edilmiş unit görünmezdir), olay izinde B için `ResourceReserved` YOKTUR.

---

### T3 — Hunger YALNIZCA ConsumeFood tamamlanınca düşer

**Dosya:** `Assets/Tests/EditMode/Actions/EatHungerAtCompletionTests.cs`

Bugün varış anı = yemek (`EatOnArrivalTests.TickArrivals_...EatsThisVeryTick`) ve hunger tek atomda
tabana iner (NeedConsumptionSystem.cs:151-158). Yeni hikâye: varış yalnız fazı değiştirir; bedel
(item) ve fayda (hunger) yalnız tamamlanma tick'inde, TEK operasyonda el değiştirir.

**Kurulum:**
```csharp
var world = EatSliceWorld.Build();
world.Actors.Add(EatSliceWorld.Hungry(7, 5, 7));   // seat halkasında: MoveToFood kısa, Take 1 tick
var composer = new WorldTickComposer();
var trace = new List<(int tick, int hunger, Phase phase)>();
for (int t = 1; t <= 3 * EatAction.ConsumeDurationTicks + 20; t++)
{ composer.Advance(world, t); trace.Add((t, A().Needs.Hunger.Value, A().ActionState.Phase)); }
```

**Kesin iddia:**
```csharp
int drops = CountStrictDrops(trace.Select(s => s.hunger));   // ardışık azalma sayısı
Assert.That(drops, Is.EqualTo(1), "hunger TAM BİR kez düşer — faz ortasında sızıntı yok");
int dropTick = TickOfFirstDrop(trace);
int mealTick = world.Events.Events.Single(e => e.Kind == WorldEventKind.MealConsumed).Tick.TotalMinutes;
Assert.That(dropTick, Is.EqualTo(mealTick), "düşüş tick'i == MealConsumed tick'i");
Assert.That(trace.Where(s => s.tick < dropTick).All(s => s.hunger >= 80),
    "MoveToFood/TakeFood/Consume-sürerken hunger yalnız YÜKSELEBİLİR (NeedsSystem)");
Assert.That(trace.First(s => s.tick == dropTick).hunger,
    Is.EqualTo(NeedConsumptionSystem.MealHungerFloor), "doygunluğa yeme korunur");
// ConsumeFood gerçekten SÜRER:
Assert.That(trace.Count(s => s.phase == Phase.ConsumeFood),
    Is.GreaterThanOrEqualTo(EatAction.ConsumeDurationTicks), "yemek tek tick'lik teleport değil");
```

---

### T4 — Eylem tickler arasında AYNI id + faz ile sürer

**Dosya:** `Assets/Tests/EditMode/Actions/EatActionContinuityTests.cs`

Bugün karar her tick sıfırdan hesaplanır (`ScheduleSystem.Advance` → `ChooseTarget`,
ScheduleSystem.cs:58-80 + 115-147); "hangi eylemde kaldım?" bilgisi yok. Yeni hikâye: bölüm
(episode) boyunca `ActionId` sabittir, faz tek yönlü ilerler, hiçbir tick'te null'a düşmez.

**Kurulum:**
```csharp
var world = EatSliceWorld.Build();
world.Actors.Add(EatSliceWorld.Hungry(7, 20, 20));  // yürüyüş >= 13 tick: MoveToFood uzun yaşar
var composer = new WorldTickComposer();
var episode = new List<(ulong id, Phase phase)>();
for (int t = 1; t <= 200 && !MealDone(world); t++)
{ composer.Advance(world, t); if (A().ActionState.Action is EatAction) episode.Add((A().ActionState.ActionId, A().ActionState.Phase)); }
```

**Kesin iddia:**
```csharp
Assert.That(episode.Select(e => e.id).Distinct().Count(), Is.EqualTo(1),
    "IntentChosen'dan MealConsumed'a TEK ActionId — kimlik tickler arası sürer");
Assert.That(IsMonotone(episode.Select(e => (int)e.phase)), Is.True,
    "faz sırası MoveToFood→TakeFood→ConsumeFood; asla geri sarmaz, asla atlamaz");
Assert.That(episode.Count, Is.GreaterThanOrEqualTo(13), "bölüm gerçekten çok-tick'li yaşadı");
// Boşluk yok: bölüm başladıktan bitene kadar HER tick'te aktörün CurrentAction'ı vardı
Assert.That(gapTicks, Is.Zero, "hiçbir ara tick'te aktör 'eylemsiz' kalmadı");
```

---

### T5 — Kesilme rezervasyonu bırakır; item ne kaybolur ne çoğalır

**Dosya:** `Assets/Tests/EditMode/Actions/EatInterruptionConservationTests.cs`

Bugün kesilme kavramı yok (teşhis §2.5). Yeni hikâyede kesilme her fazda tanımlı ve madde korunumlu:

| Kesilme fazı | Beklenen dünya sonucu |
|---|---|
| MoveToFood | rezervasyon `Release` edilir; stok değişmemiştir |
| TakeFood sonrası (unit elde) | unit pile'a İADE edilir (slice kuralı: en yakın pile'a geri) |
| ConsumeFood sürerken | unit iade edilir (yarım yeme yok — slice basitleştirmesi, yorumla belgelenir) |

**Kurulum (parametrik):**
```csharp
[TestCase(Phase.MoveToFood)] [TestCase(Phase.TakeFood)] [TestCase(Phase.ConsumeFood)]
public void Interrupt_AtPhase_ConservesFoodAndFreesReservation(Phase at)
{
    var world = EatSliceWorld.Build(wheat: 1);
    world.Actors.Add(EatSliceWorld.Hungry(7, 12, 12));
    var composer = new WorldTickComposer();
    AdvanceUntilPhase(composer, world, at);                    // faz başlangıcına deterministik koşum
    int stockBefore = TotalFood(world);                        // pile + eldeki
    FoodOps.Interrupt(world, A(), "test_interrupt");           // tasarımın tek kesme kapısı
    composer.Advance(world, NextTick());                       // bir tick daha: temizlik oturur
```

**Kesin iddia:**
```csharp
    Assert.That(TotalFood(world), Is.EqualTo(stockBefore), "MADDE KORUNUMU: dup yok, kayıp yok");
    Assert.That(world.Reservations.Count, Is.Zero, "rezervasyon serbest");
    Assert.That(A().ActionState.Action, Is.Not.InstanceOf<EatAction>(), "eylem düştü");
    Assert.That(A().ActionState.FailureReason, Is.EqualTo("test_interrupt"), "sebep taşınır");
    Assert.That(A().Needs.Hunger.Value, Is.EqualTo(80), "yarım yemek fayda vermez");
    Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.MealConsumed), Is.False);
    // Serbest bırakmanın KANITI: ikinci aktör aynı son unit'i ŞİMDİ rezerve edebilir
    world.Actors.Add(EatSliceWorld.Hungry(8, 5, 6));
    Assert.That(FoodOps.TryReserve(world, B(), out _), Is.True, "unit tekrar dünyanın malı");
}
```

---

### T6 — UI etiketi CurrentAction ile BİRE BİR aynıdır

**Dosyalar:**
- `Assets/Tests/EditMode/Presentation/VisualLayer/ActivityLabelTruthTests.cs` (davranış)
- aynı dosyada lint testi (repo kültürü: `GateContractLintTests` kaynak-okuma deseni)

Bugün `DescribeActivity` saat+konum+bitki ipuçlarından fiil UYDURUyor
(DomainSimulationAdapter.WorldProjection.cs:110-142: 12-14 arası plaza yakınıysa "eating",
ripe bitki yakınıysa "harvesting"). Yeni tasarım: etiket metni AKSİYONUN KENDİSİNDE yaşar
(`EmberAction.ActivityLabel` — faz başına: MoveToFood→"to the tavern", TakeFood→"taking bread",
ConsumeFood→"eating"), projeksiyon onu OLDUĞU GİBİ okur.

**Kurulum + kesin iddia (davranış):**
```csharp
var actor = EatSliceWorld.Hungry(7, 5, 5);
foreach (Phase phase in new[] { Phase.MoveToFood, Phase.TakeFood, Phase.ConsumeFood })
{
    actor.ActionState.Debug_Set(EatAction.At(phase, 1));
    Assert.That(ActionActivityLabel.For(actor),                       // saf statik; DÜNYA/SAAT PARAMETRESİ YOK
        Is.EqualTo(actor.ActionState.Action.ActivityLabel),
        "etiket = aksiyonun beyanı; formatter'ın tahmin edecek girdisi imza gereği yok");
}
actor.ActionState.Debug_Clear();                                       // eylemsiz aktör
Assert.That(ActionActivityLabel.For(actor), Is.Null,
    "aksiyon yoksa etiket yok — plaza+12:30 kombinasyonu artık 'eating' ÜRETEMEZ");
```

**Kesin iddia (lint — tahmin dallarının ölümü):**
```csharp
var src = File.ReadAllText(".../DomainSimulationAdapter.WorldProjection.cs");
Assert.That(src, Does.Contain("ActionActivityLabel.For"), "projeksiyon tek doğru kaynağı okur");
foreach (var banned in new[] { "hour >= 12", "to the tavern\"", "tending the field", "harvesting\"" })
    Assert.That(CodeLinesOf(src).Any(l => l.Contains(banned)), Is.False,
        $"'{banned}' tahmin dalı hâlâ yaşıyor — görüntü fiil uyduruyor demektir");
```

---

### T7 — Aynı seed + chunking → BİREBİR aynı aksiyon-faz izi

**Dosya:** `Assets/Tests/EditMode/Actions/EatChunkingPhaseTraceTests.cs`

`CadenceChunkingInvarianceTests` olay-log eşitliğini zaten pinliyor; bu test aynı deseni yeni
aksiyon izine daraltır: W29 catch-up per-tick replay olduğundan, chunk İÇİNDE yaşanan faz
geçişleri de sınır damgalı ve birebir aynı olmalıdır (catch-up contract, NeedConsumptionSystem.cs:33-35 dersi).

**Kurulum:**
```csharp
const int totalTicks = 2 * 1440;
WorldState Run(int[] chunks)
{
    var world = new WorldFactory().Create(roomSeed: 4242);
    WorldFactory.SeedVillagers(world); world.EnsureInvariants();     // gerçek kadro: çok sayıda eat bölümü
    var composer = new WorldTickComposer(); int at = 0; int i = 0;
    while (at < totalTicks) { at = Math.Min(totalTicks, at + chunks[i++ % chunks.Length]); composer.Advance(world, at); }
    return world;
}
var tickByTick = Run(new[] { 1 });
var ragged    = Run(new[] { 1, 7, 13, 1, 40, 3, 61, 5, 127, 2 });    // mevcut testin chunk seti
```

**Kesin iddia:**
```csharp
Assert.That(ActionTrace.Of(ragged), Is.EqualTo(ActionTrace.Of(tickByTick)),
    "ragged ilerleme FARKLI bir aksiyon tarihi yazdı — bir faz yanlış saatte/yanlış damgayla ilerliyor");
// Son durum da aynı: her aktörün (ActionId, Phase, Progress, ReservationId) dörtlüsü eşit
Assert.That(ActionStateDigest(ragged), Is.EqualTo(ActionStateDigest(tickByTick)));
```

---

### T8 (kapak taşı) — §10 zinciri eksiksizdir

**Dosya:** `Assets/Tests/EditMode/Actions/EatStoryChainTests.cs`

`NeedThresholdCrossed → IntentChosen → ResourceReserved → Arrived → ItemTransferred → MealConsumed`
tek aktörün TEK bölümünde, DOĞRU SIRADA ve hepsi AYNI ActionId damgasıyla loglanır. Bu, teşhisin
"event çoğunlukla değişiklikten sonra yazılan yorum" yarasının kapanma kanıtıdır: olaylar artık
zincirin SEBEP halkalarıdır.

**Kurulum:** T4 ile aynı dünya (uzak aç aktör); bölüm bitene kadar koşulur.

**Kesin iddia:**
```csharp
var chain = world.Events.Events
    .Where(e => e.ActorId.Value == 7 && ReasonActionId(e) == episodeId)
    .Select(e => e.Kind).ToArray();
Assert.That(chain, Is.SupersetOf(new[] { NeedThresholdCrossed, IntentChosen, ResourceReserved,
    Arrived, ItemTransferred, MealConsumed }), "zincirde eksik halka var");
Assert.That(IndexOrder(chain), Is.Ordered, "halkalar sebep sırasında — sonradan yazılmış yorum değil");
// LogManager kanıtı: aynı bölümün HER faz geçişi tek LogManager kanalından da aktı
Assert.That(logSink.Lines.Count(l => l.Contains($"id:{episodeId}") && l.Contains("phase:")),
    Is.GreaterThanOrEqualTo(3), "MoveToFood/TakeFood/ConsumeFood geçişlerinin tamamı loglandı");
```

(`logSink`: `EmberLog.Sink`e test başında bağlanan liste — TickPerf'in kullandığı mevcut sink deseni,
WorldTickComposer.cs:214-218. Deterministik iddialar Events'ten, "her faz loglanır" direktifi sink'ten doğrulanır.)

---

## 2. DEĞİŞMESİ GEREKEN Mevcut Testler (pin envanteri + yeni hikâyeleri)

Grep tabanı: `meal_eaten | TickArrivals | NeedConsumptionSystem | HungerEatThreshold | Gate | chunking | digest`.

| # | Dosya | Bugünkü pin | Yeni beklenen hikâye |
|---|---|---|---|
| 1 | `Assets/Tests/EditMode/Living/EatOnArrivalTests.cs` | "Varışa yemek AYNI tick" (`TickArrivals_...EatsThisVeryTick`: hunger→floor anında) | **Ters döner:** varış tick'inde faz `TakeFood/ConsumeFood`e geçer, hunger DEĞİŞMEZ; `ConsumeDurationTicks` sonra düşer. İkinci test (`NotHungryOrNotThere_NoMeal`) ruhen yaşar: uzaktaki aç aktör için `MealConsumed` üretilmez — iddia `meals==0` sayacından "iz boş" biçimine taşınır. Dosya `Actions/` altına T3'ün komşusu olarak taşınabilir; W20 "reaching the table IS the meal" başlık yorumu tarihe not düşülerek silinir. |
| 2 | `Assets/Tests/EditMode/Living/NeedConsumptionSystemTests.cs` | `Tick(world,12)` saatlik anlık yemek: `meals==1`, stok 10→9, en yakın larder seçimi | Eat yarısı `FoodOps`+decision'a taşınır: en-yakın-pile seçimi `TryReserve`ın seçim testi olur; "saatlik anlık yemek" pini ÖLÜR. Uyku yarısı (gece fatigue) bu dilimde AYNEN kalır — dosya küçülür, silinmez. |
| 3 | `Assets/Tests/EditMode/Living/NeedRecoverySystemEatTests.cs` | `EatMeal` envanterden anlık yemek + `need_recovered` ReasonTrace zinciri | `EatMeal`, `TryCompleteConsume`ın iç uygulayıcısı olur (tek yazar). ReasonTrace zincirine `action_id:{id}` halkası eklenir; "anlıklık" artık YALNIZ tamamlanma anının uygulanışıdır, kararı değil. Null-guard testleri aynen kalır. |
| 4 | `Assets/Tests/EditMode/Living/ScheduleSystemTests.cs` | `Advance_HungerNotTheClock_SendsCiviliansToTheFoodSpot` + foodSpot/seat overload'ları: ScheduleSystem aç aktörü BİZZAT yürütür | Yemek yönlendirmesi ScheduleSystem'den ÇIKAR (teşhis §2.4'ün ölümü): bu test DecisionSystem testine dönüşür — "aç sivil `EatIntent` + planının ilk aksiyonu MoveToFood alır; yürütme MovementSystem/actionAdvance'ın işi". Work/home/pursuit/seat-dışı testler bu dilimde DEĞİŞMEZ (yalnız EAT dilimi). Seat halkası (SeatOffsets) EatAction hedef seçimine taşınırsa seat testi de onunla göçer. |
| 5 | `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` | Gate1/2/7 yemeği `e.Reason.StartsWith("meal_eaten")` ile sayar; Gate4 yorumu "meals resolve the tick a walker ARRIVES"; Gate8 seat fan-out | Sayaçlar `WorldEventKind.MealConsumed`a döner (tek satırlık lambda değişimi ×3). Gate1 eşikleri (avg hunger<70, meals≥3/villager) KORUNUR — ConsumeDurationTicks bunları bozacak kadar uzun olamaz; bozarsa süre yanlıştır, eşik değil. Gate4 yorum bloğu güncellenir (varış≠yemek artık; dalga iddiası `max-min>=5` aynen kalır, Consume süresi masada oturma süresini UZATIR — dalga güçlenir). Gate8 aynen: fan-out artık EatAction hedef seçiminin kanıtı. |
| 6 | `Assets/Tests/EditMode/CanSuyu/GateContractLintTests.cs` | Gate dosyasını kaynak-lint'ler (loop zorunlu, screenshot yasak) | Kod değişmez; Gate dosyası düzenlenirken lint'in yeşil kaldığı DOĞRULANIR (advance-loop desenleri korunmalı). |
| 7 | `Assets/Tests/EditMode/Composition/CadenceChunkingInvarianceTests.cs` | Olay-log eşitliği (tick-by-tick vs ragged) | Metin DEĞİŞMEZ; log yeni zincir olaylarını içerdiğinden pin kendiliğinden GÜÇLENİR. T7 aksiyon-izine daraltılmış kardeşidir; ikisi birlikte yaşar. |
| 8 | `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs` | `BaselineHash = "e56cb763..."` | **Zorunlu re-baseline** (hunger zamanlaması + yeni olaylar meşru tarih değişimi). Prosedür dosyadaki gelenek: aynı-seed çift koşum birebirken yakala, tarih+sebep yorumu ekle ("W32 EAT dilimi: varış≠yemek; zincir olayları loglanıyor"). |
| 9 | `Assets/Tests/EditMode/Composition/WorldTickRegistryTests.cs` | `DefaultRegistry_DeclaresCanonicalOrder` sabit üçlü listesi (`PerTick:22:living.eatOnArrival` dahil) | Liste güncellenir: `living.eatOnArrival` ÖLÜR; yerine `living.decision` ve `living.actionAdvance` (kesin order DOC 04). `living.consumption` yemek yarısını kaybeder, uyku yarısıyla kalır — id yaşar. |
| 10 | `Assets/Tests/EditMode/Composition/FieldOwnershipRegistryTests.cs` (+ `FieldOwnershipRegistry.cs:18-50`) | `Actor.Needs` 3 yazar, `World.Stockpiles` 5 yazar belgelenmiş | Registry satırları daralır: hunger-DÜŞÜŞ yazarı yalnız `FoodOps.TryCompleteConsume`; yeni tek-yazar satırları `Actor.ActionState` (actionAdvance) ve `World.Reservations` (reservation). Test, yeni satırları ve azalan yazar sayısını pinler — çok-yazarlılık artık lint edilen bir gerileme olur. |
| 11 | `Assets/Tests/EditMode/Composition/CatchupPerfPinTests.cs` | 14 gün catch-up < 5 sn | Eşik ve metin AYNEN KALIR — kırmızıya dönmesi dilimin perf regresyonu demektir (W30e dersi). |
| 12 | `Assets/Tests/EditMode/Composition/LiveScaleCatchupPerfPinTests.cs` | 800 aç sivil, 1 replay günü < 3 sn (EatOnArrival patlaması pini) | Pin kalır; sıcak döngü artık decision+reservation+actionAdvance. Rezervasyon araması pile-cache deseniyle O(aktör) tutulmak ZORUNDA — bu test onun bekçisidir; yorum bloğu yeni sıcak yola göre güncellenir. |
| 13 | `Assets/Tests/EditMode/Composition/WorldNpcDailyRhythmTests.cs` | 10:00/22:00 pozisyon örneklemesiyle günlük ritim | Eşikler büyük olasılıkla yaşar (hareket yine tick başına 1 hücre); yemek yürüyüşleri artık Move fazından geldiği için İNCELEME işaretli — kırmızıysa örnekleme saati değil, faz cadence'ı yanlıştır. |
| 14 | `Assets/Tests/EditMode/Composition/WorldLivesOverNTicksTests.cs` | Digest testiyle seed-hizalı canlılık koşumu | Değişiklik beklenmez; digest re-baseline'ıyla birlikte yeşil doğrulanır. |
| 15 | `Assets/Tests/EditMode/Save/WorldSaveMapperGoldenRoundtripTests.cs` | "Populate every NEWER collection" temsilî dünyası + WorldSaveData alan-alan reflection eşitliği | Temsilî dünyaya UÇUŞ ORTASI durum eklenir: bir aktör `ConsumeFood@progress=2` + canlı rezervasyon. `WorldSaveData`ya paralel diziler eklenir (`actorActionIds/actorActionTypes/actorActionPhases/actorActionProgress/actorReservationIds` + rezervasyon dizileri); mapper birini düşürürse reflection farkı BURADA patlar. |
| 16 | `Assets/Tests/EditMode/Save/SaveLoadDigestRoundtripTests.cs` | 1 gün koşum → save → load → digest birebir | Metin aynı kalır ama SÖZLEŞMESİ büyür: `WorldStateDigest.Compute` ActionState+Reservations alanlarını da içermek ZORUNDA (yoksa bu test yarım-uçuş kaybını göremez). Ek küçük iddia önerilir: load sonrası uçuş-ortası aktörün `(ActionId, Phase, Progress)` üçlüsü save öncesiyle eşit. |
| 17 | `Assets/Tests/EditMode/Presentation/VisualLayer/*SnapshotTests.cs` (özellikle `ColonyNeedsSnapshotTests`) | Snapshot satırları mevcut alanlarla | Yalnız GÖZDEN GEÇİRME: activity alanı taşıyan snapshot varsa kaynak `ActionActivityLabel`e döner. Davranış pini T6'dadır. |

**Değişmeyen bekçiler (bilinçli):** `Faz1AcceptanceReplayTests`, `WorldTickComposerReplayTests`,
`HarvestSystemTests`, job/recipe testleri — EAT dilimi bunların alanına yazmaz; hepsi yeşil kalmalıdır.
Fallback/proof harness (`--ember-proof-screenshots`) sözleşmesi değişmez; Gate'ler yeşilken harness da yeşildir.

---

## 3. Çalıştırma Sırası (uygulama haftası için)

1. Önce T1–T8 dosyaları KIRMIZI yazılır (yüzey derlenene kadar `Debug_Set` gibi test-kancaları DOC 02'ye sipariştir).
2. Dilim kodu indikçe sıra: T4 (süreklilik) → T3 (tamamlanma) → T2 (rezervasyon) → T1 (mesafe) → T5 (kesilme) → T6 (UI) → T7 (chunking) → T8 (zincir).
3. Bölüm-2 tablosu tek PR'da: 1–4 yeniden yazım, 5 lambda değişimi, 8 re-baseline EN SON (tüm davranış otururken bir kez).

## 4. Sonraki Dilime Devreden §10 Maddeleri (bilinçli kapsam dışı)

- "Aktör worksite dışında işi ilerletemez" + "Input olmadan output oluşmaz" → WORK dilimi (JobAssignmentSystem.Tick.cs:80-90 yarası).
- "Plant output stockpile'a teleport olmaz" → FARM dilimi (DefaultTickSystems.cs:437-465 yarası).
- "Uyku toparlanması yalnız aktif Sleep action + uygun yatakta" → SLEEP dilimi (NeedConsumptionSystem gece bloğu bu dilimde bilinçli dokunulmadan kalır).
- Genelleşen kalıplar: T4 (süreklilik), T5 (korunum) ve T7 (chunking-iz) şablonları her yeni aksiyon türü için
  parametrik olarak yeniden kullanılır — `EmberAction` hiyerarşisi büyüdükçe test şablonu sabit kalır.

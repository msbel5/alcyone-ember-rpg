# W32 — DOC 3: Eylem İlerletme Katmanı (Action Advancement) — EAT Dikey Dilimi

> RUH_TESHIS.md kararının üçüncü parçası: aktör niyet + mevcut eylemi taşır (DOC 1),
> karar/rezervasyon katmanı intent üretir (DOC 2), **bu doküman eylemi tick tick ilerleten
> katmanı** tasarlar. Kapsam SADECE EAT dilimidir; genelleşecek noktalar §14'te işaretlidir.

---

## 0. Kapsam ve sözleşme noktaları

Bu doküman şu üç şeyi tasarlar:

1. `IActionAdvancer` (Strategy) — eylem tipi başına bir ilerletici.
2. `ActionAdvancementSystem` (PerTick) — aktörün `CurrentAction`'ını **tek faz-adımı** ilerletir.
3. EAT faz makinesi: `MoveToFood → TakeFood(1 tick) → ConsumeFood(3 tick)`, kesilme ve
   başarısızlık dahil.

**DOC 1 varsayımları** (ActorMindState): aktörde `Mind.CurrentAction` (null = boşta),
`Mind.LastFailure` (sebep + damga) alanları var; mind state save/load DOC 1'de. Bu doküman
`ActorAction` tip hiyerarşisini ve EAT'e özgü save kolonlarını tanımlar.

**DOC 2 varsayımları** (Decision + Reservation): `DecisionSystem` yalnızca boşta/kesilen
aktör için intent seçer, hedef pile'ı bulur (bugünkü `FindNearestFoodPile` seçim matematiği
oraya taşınır — `NeedConsumptionSystem.cs:284-314`), `ReservationLedger` üzerinden pile+tag
rezerve eder, oturma hücresini (Seat ring, `ScheduleSystem.cs:176-186`) **eylem yaratılırken
bir kez** atar ve `EatAction`'ı `Mind.CurrentAction`'a yazar. Rezervasyon fiziksel düşüm
DEĞİLDİR (soft claim): pile başka tüketicilerce (vermin `living.ambient@Hourly:50`, hourly
tüketim) hâlâ boşaltılabilir — orta yolda boşalma bizim başarısızlık dalımızdır (§7).

Bu katmanın sözleşmesi tek cümledir:

> **Advancer, tek aktörün tek eylemini, tek tick'te, tek faz-adımı ilerletir; dünyayı yalnızca
> doğrulanmış işlemlerle değiştirir; her faz sınırını tek LogManager'dan loglar.**

---

## 1. Tick şeridindeki yeri

Bugünkü şerit (`DefaultTickSystems.cs:41-64` kayıt listesi, global Order sıralı):

```text
core.time@PerTick:10 → living.schedule@PerTick:20 → living.companion_follow@PerTick:21
→ living.eatOnArrival@PerTick:22 → living.needs@Hourly:30 → living.consumption@Hourly:35
→ living.predation@Hourly:40 → living.witness@Hourly:45 → ...
```

Hedef şerit (RUH_TESHIS §6 sırası: karar → rezervasyon → ilerletme):

```text
core.time@PerTick:10
living.decision@PerTick:18        // DOC 2 (YENİ): intent + rezervasyon + EatAction yaratımı
living.schedule@PerTick:20        // DEĞİŞİR: CurrentAction'ı olan aktörü ATLAR (§9)
living.companion_follow@PerTick:21
living.action_advance@PerTick:22  // YENİ: emekli eatOnArrival'ın slotunu DEVRALIR
living.needs@Hourly:30            // değişmez (ramp)
living.consumption@Hourly:35      // KÜÇÜLÜR: yalnız uyku/fatigue yarısı kalır (§9)
```

Slot devralma bilinçlidir: `Actor.Needs` ve `World.Stockpiles` yazımları tick içinde bugünkü
`living.eatOnArrival@PerTick:22` ile aynı noktada kalır; golden kayması yalnız faz gecikmesinden
gelir (§13), sıra karışmasından değil.

Zamanlama sonuçları (deterministik, belgelenmiş):

- Açlık `Hourly:30`'da yükselir; eşik aşımı **bir sonraki tick** `decision@18`'de görülür
  (bugün de schedule@20 needs@30'dan önce koştuğu için aynı 1-tick gecikme vardı).
- Saat sınırındaki tick'te `predation@Hourly:40` advancement@22'den SONRA vurur; kesilme
  kontrolü bir sonraki tick'in adım başında yakalar (§7) — deterministik pull modeli.

---

## 2. Tip hiyerarşisi (Domain, saf — Unity/IO/RNG yok)

Yol: `Assets/Scripts/Domain/Actions/` (yeni klasör). Kalıtım tabanlı hiyerarşi (kullanıcı
direktifi) + düşük satır sayısı: taban sınıf faz muhasebesini taşır, alt sınıf yalnız veri.

```csharp
// CONSTRAINT: actions store IDs and value types ONLY — never object references.
// Save/load rebuilds them from parallel arrays; a pile pointer would dangle after load.
public enum ActionKind : byte { None = 0, Eat = 1 }          // Sleep/Work/Harvest later

public enum ActionFailureReason : byte
{
    None = 0,
    SourceDrained = 1,       // pile emptied mid-route/mid-take
    InterruptedByCombat = 2, // health dropped since last step (predation strike)
    InterruptedByPursuit = 3 // actor became a guard-pursuit quarry
}

/// <summary>Kalıcı eylem: aktörün "şu an ne yapıyorum" gerçeği. UI bunu VERBATIM okur.</summary>
public abstract class ActorAction
{
    public abstract ActionKind Kind { get; }
    public int PhaseIndex;      // subclass enum'una map'lenir; save-friendly düz int
    public int TicksInPhase;    // faz içi ilerleme sayacı
    public int LastHealth;      // CONSTRAINT: interruption detection is PULL-based —
                                // set at creation, compared at every step start (§7)
    public abstract string PhaseName { get; } // UI/log etiketi — projection tahmin ETMEZ
}

public sealed class EatAction : ActorAction
{
    public const int PhaseMoveToFood = 0, PhaseTakeFood = 1, PhaseConsumeFood = 2;
    public const int ConsumeDurationTicks = 3; // "yemek 3 tick sürer" — tek yerde

    public override ActionKind Kind => ActionKind.Eat;
    public SiteId TargetSiteId;   // rezerve pile'ın sitesi (pile SiteId ile çözülür)
    public string FoodTag;        // rezerve tür ("wheat" vb.)
    public GridPosition SeatCell; // DOC 2 yaratımda BİR KEZ atar (commitment; Gate8)

    public override string PhaseName => PhaseIndex switch
    {
        PhaseMoveToFood => "moving_to_food",
        PhaseTakeFood => "taking_food",
        _ => "eating",
    };
}
```

### CarriedFood — minimal taşıma alanı (ActorRecord)

`ActorRecord.cs:66-90` alan bloğuna tek alan + iki dar mutator eklenir (madde korunumu
buradan geçer; anemik setter değil, doğrulamalı işlem §6'dan çağrılır):

```csharp
/// <summary>Elde taşınan yemek birimi; null = eller boş. Dilimde tek birim yeter —
/// count alanı Haul dilimiyle gelir. CONSTRAINT: only FoodOperations mutates this.</summary>
public string CarriedFoodTag { get; private set; }

internal void PickUpFood(string tag) { CarriedFoodTag = tag; }
internal string SwallowCarriedFood() { var t = CarriedFoodTag; CarriedFoodTag = null; return t; }
```

Teşhisin kabul kriteri buradan sağlanır: **kesilen eylem item'ı kaybetmez/çoğaltmaz** —
birim ya pile'dadır ya `CarriedFoodTag`'dedir ya da tüketilmiştir; iki yerde birden asla.

---

## 3. Strategy katmanı: IActionAdvancer + registry + system

Yol: `Assets/Scripts/Simulation/Actions/`. SOLID eşlemesi: SRP (advancer yalnız kendi eylem
tipini bilir), OCP (yeni eylem = yeni advancer + registry satırı; system'e DOKUNULMAZ),
DIP (system somut advancer değil arayüz görür), Strategy + Registry desenleri.

```csharp
public enum ActionStepResult : byte { Running = 0, Completed = 1, Failed = 2 }

/// <summary>Strategy: eylem tipi başına BİR ilerletici. CONSTRAINT: advancers are
/// STATELESS — every field of progress lives on the action/actor inside WorldState,
/// or chunked replay diverges (CadenceChunkingInvarianceTests is the enforcement).</summary>
public interface IActionAdvancer
{
    ActionKind Kind { get; }
    /// <summary>Exactly ONE phase-step. A step may finish its phase; the next phase's
    /// first step runs NEXT tick (uniform rule — transitions consume the tick).</summary>
    ActionStepResult Step(WorldState world, ActorRecord actor, ActorAction action, GameTime stamp);
}

/// <summary>Registry: sabit kayıt sırası (determinism); ActionKind → advancer.</summary>
public sealed class ActionAdvancerRegistry
{
    private readonly IActionAdvancer[] _byKind;               // index = (int)ActionKind
    public ActionAdvancerRegistry(params IActionAdvancer[] advancers) { /* fill array */ }
    public IActionAdvancer For(ActionKind kind) => _byKind[(int)kind];
}
```

```csharp
/// <summary>PerTick: her aktörün CurrentAction'ını tek faz-adımı ilerletir. Başka HİÇBİR
/// şey yapmaz — karar DOC 2'nin, dünya mutasyonu FoodOperations'ın işidir (SRP).</summary>
public sealed class ActionAdvancementSystem
{
    private readonly ActionAdvancerRegistry _registry; // stateless strategies — chunking-safe

    public void Tick(WorldState world, GameTime stamp)
    {
        // CONSTRAINT: insertion order iteration — the same deterministic order every
        // other Living system uses (ActorStore.Records).
        foreach (var actor in world.Actors.Records)
        {
            if (actor == null || !actor.IsAlive) continue;
            var action = actor.Mind?.CurrentAction;            // DOC 1 alanı
            if (action == null) continue;

            var result = _registry.For(action.Kind).Step(world, actor, action, stamp);
            if (result == ActionStepResult.Running) continue;

            if (result == ActionStepResult.Failed)
                world.Reservations.Release(actor.Id);          // DOC 2 ledger — idempotent
            actor.Mind.ClearAction(result);                    // LastFailure'ı da damgalar
            // Completed/Failed logging is inside the advancer via LogManager (§8) so the
            // reason string carries phase-local detail; the system stays generic.
        }
    }
}
```

`DefaultTickSystems` adaptörü (mevcut `StepBase` kalıbı, `DefaultTickSystems.cs:70-86`):

```csharp
// Emekli EatOnArrivalStep'in (PerTick:22) slotunu devralır — Needs/Stockpiles yazım noktası
// tick içinde sabit kalır. CONSTRAINT: stamp = context.Stamp (cadence BOUNDARY stamp; the
// catchup contract from NeedConsumptionSystem.cs:29-32 applies unchanged).
private sealed class ActionAdvancementStep : StepBase
{
    private readonly ActionAdvancementSystem _actions = new ActionAdvancementSystem(
        new ActionAdvancerRegistry(new EatActionAdvancer()));
    public ActionAdvancementStep() : base("living.action_advance", TickCadence.PerTick, 22) { }
    public override void Run(in TickContext context) => _actions.Tick(context.World, context.Stamp);
}
```

---

## 4. EatActionAdvancer — faz makinesi

Tek dosya, ~100 satır hedefi. Şablon: her `Step` önce kesilme kontrolü (§7), sonra faza göre
tek adım (Template Method'un mini hâli — taban sınıfa çıkarmak 3+ eylem gelince yapılır, §14).

```csharp
public sealed class EatActionAdvancer : IActionAdvancer
{
    public ActionKind Kind => ActionKind.Eat;
    private readonly NeedMoodEvaluator _mood = new NeedMoodEvaluator(); // stateless helper

    public ActionStepResult Step(WorldState world, ActorRecord actor, ActorAction a, GameTime stamp)
    {
        var eat = (EatAction)a;

        // 1) INTERRUPTION (pull, deterministic — §7): combat/pursuit outranks lunch.
        var interrupt = InterruptionProbe.Check(world, actor, eat);
        if (interrupt != ActionFailureReason.None)
            return LogManager.Failed(world, stamp, actor, eat, interrupt);

        switch (eat.PhaseIndex)
        {
            case EatAction.PhaseMoveToFood:
                // Mid-route drain: reservation is soft — verify the larder still feeds.
                if (!FoodOperations.PileStillHas(world, eat.TargetSiteId, eat.FoodTag))
                    return LogManager.Failed(world, stamp, actor, eat, ActionFailureReason.SourceDrained);
                actor.MoveTo(MovementService.StepToward(actor.Position, eat.SeatCell)); // §5
                if (FoodOperations.WithinEatReach(world, actor, eat.TargetSiteId))
                {   // arrival = transition; TakeFood executes NEXT tick (1-tick phase).
                    eat.PhaseIndex = EatAction.PhaseTakeFood; eat.TicksInPhase = 0;
                    LogManager.Phase(world, stamp, actor, eat);
                }
                return ActionStepResult.Running;

            case EatAction.PhaseTakeFood:
                // Validated op: reach + stock re-checked INSIDE the op (§6). Fails if a
                // faster mouth (or vermin) emptied the pile between arrival and take.
                if (!FoodOperations.TryTakePileUnit(world, actor, eat.TargetSiteId, eat.FoodTag))
                    return LogManager.Failed(world, stamp, actor, eat, ActionFailureReason.SourceDrained);
                world.Reservations.Release(actor.Id); // unit is physically in hand now
                eat.PhaseIndex = EatAction.PhaseConsumeFood; eat.TicksInPhase = 0;
                LogManager.Phase(world, stamp, actor, eat);
                return ActionStepResult.Running;

            default: // PhaseConsumeFood — 3 ticks; hunger drops ONLY at completion
                eat.TicksInPhase++;
                if (eat.TicksInPhase < EatAction.ConsumeDurationTicks)
                    return ActionStepResult.Running;
                FoodOperations.CompleteConsume(world, actor, eat, stamp, _mood); // §6
                return LogManager.Completed(world, stamp, actor, eat);
        }
    }
}
```

Zaman çizgisi (bugünle fark — golden kayması §13'ün girdisi):

```text
tick T   : MoveToFood son adımı — reach'e girer, faz = TakeFood        (bugün: yemek T'de biterdi)
tick T+1 : TakeFood — pile.Remove(tag,1) + CarriedFoodTag = tag        (stok düşümü +1 tick)
tick T+2 : ConsumeFood 1/3
tick T+3 : ConsumeFood 2/3
tick T+4 : ConsumeFood 3/3 — hunger→floor, thirst/mood, meal_eaten     (ihtiyaç düşümü +4 tick)
```

`CarriedFood` devam sözleşmesi: ConsumeFood sırasında kesilirse birim elde KALIR;
DecisionSystem "aç + elinde yemek var" durumunda `PhaseConsumeFood`'dan başlayan yeni
`EatAction` yaratır (walk/take atlanır; progress sıfırdan — yemek yeniden başlar). Birim
asla kaybolmaz/çoğalmaz.

---

## 5. Hareket kararı: MovementService (extract), ScheduleSystem reuse DEĞİL

**Karar: extract.** `ScheduleSystem.StepToward` (`ScheduleSystem.cs:191-196`) private'tır ve
Strategy advancer'ın router'a bağımlanması yön olarak yanlıştır (Simulation.Actions → Domain
olmalı; router'a değil). Ayrıca aynı Chebyshev tek-adım matematiği üç yerde daha inline
kopyadır: `CascadeSystems.cs:62-64` (predation), `:189-191` ve `:208-210` (witness). Tek eve
toplanır:

```csharp
// Assets/Scripts/Domain/Core/MovementService.cs — pure function, ZERO state.
// CONSTRAINT: the ONE home of grid stepping. Chebyshev 8-direction, one cell per axis
// per tick, monotone convergence, never overshoots (verbatim ScheduleSystem.cs:191-196).
public static class MovementService
{
    public static GridPosition StepToward(GridPosition from, GridPosition to) =>
        new GridPosition(from.X + System.Math.Sign(to.X - from.X),
                         from.Y + System.Math.Sign(to.Y - from.Y));
}
```

- `ScheduleSystem.StepToward` silinir; `ScheduleSystem.cs:78` `MovementService.StepToward`
  çağırır. Bit-identik: golden'lara hareketten sıfır katkı.
- CascadeSystems kopyalarının taşınması ayrı, davranış-nötr temizlik — dilim DIŞI, not edildi.
- Pathfinding (`PathfindingSystem.cs`, dormant — RUH_TESHIS §2.10) bilinçli olarak
  BAĞLANMAZ: dilim bugünkü düz-hat adımını korur ki golden farkı yalnız faz gecikmesi olsun.
  Engel/A* MovementService'in arkasına sonra takılır (§14).

---

## 6. Doğrulanmış dünya işlemleri: FoodOperations

RUH_TESHIS §6 "dünya yalnızca doğrulanmış işlemlerle değişir" ilkesinin dilimlik hâli.
Advancer pile'a asla doğrudan dokunmaz.

```csharp
// Assets/Scripts/Domain/Actions/FoodOperations.cs — validated world mutations for EAT.
// CONSTRAINT: the ONLY code that turns a pile unit into a carried unit into a meal.
// Reach constant and needs math are IMPORTED from NeedConsumptionSystem so behaviour
// and goldens shift only by phase timing, never by formula drift.
public static class FoodOperations
{
    public static bool PileStillHas(WorldState w, SiteId site, string tag) { /* pile by SiteId → Get(tag) > 0 */ }

    public static bool WithinEatReach(WorldState w, ActorRecord a, SiteId site)
    {   // NeedConsumptionSystem.TryGetSiteCentre (:213-226, the single site-centre truth)
        // + Chebyshev <= NeedConsumptionSystem.EatReachCells (2). Siteless piles: permissive,
        // same as today's WithinReach (:228-241) — bare test worlds keep working.
    }

    public static bool TryTakePileUnit(WorldState w, ActorRecord a, SiteId site, string tag)
    {   // invariants: within reach AND pile.Get(tag) > 0 AND a.CarriedFoodTag == null;
        // then pile.Remove(tag, 1); a.PickUpFood(tag). All-or-nothing.
    }

    public static void CompleteConsume(WorldState w, ActorRecord a, EatAction eat,
        GameTime stamp, NeedMoodEvaluator mood)
    {   // EXACTLY today's TryEatCached mutation block (NeedConsumptionSystem.cs:180-188):
        // hunger → MealHungerFloor, thirst -= MealThirstRecovery, ApplyNeeds, ApplyMood,
        // and the SAME event line so rumors/dialogue/Gate9 keep reading it:
        //   Append(stamp, WorldEventKind.NeedChanged, a.Id, eat.TargetSiteId,
        //          $"meal_eaten item:{tag} hunger:{fed.Hunger.Value}")
        // a.SwallowCarriedFood() retires the unit — matter conservation closes here.
    }
}
```

---

## 7. Kesilme (interruption) ve başarısızlık → replan

Push/callback yok; **her adım başında deterministik pull probe** (sıra bağımsız, chunking-güvenli):

```csharp
public static class InterruptionProbe
{
    public static ActionFailureReason Check(WorldState w, ActorRecord a, ActorAction action)
    {
        // 1) Pursuit quarry: an armed chase (PursuitRecord.cs:10-12) targeting me.
        //    Lunch yields to being hunted; scan is O(pursuits), tiny list.
        foreach (var p in w.GuardPursuits)
            if (p.TargetId == a.Id.Value && w.Time.TotalMinutes <= p.UntilMinutes)
                return ActionFailureReason.InterruptedByPursuit;
        // 2) Combat: predation strike (CascadeSystems.cs:87-88 slams health) lands at
        //    Hourly:40, AFTER advance@22 — detected here NEXT tick via LastHealth delta.
        if (a.Vitals.Health.Current < action.LastHealth)
            return ActionFailureReason.InterruptedByCombat;
        action.LastHealth = a.Vitals.Health.Current; // heal upward: just re-baseline
        return ActionFailureReason.None;
    }
}
```

Başarısızlık protokolü (tek yol, üç sebep de aynı kapıdan):

1. Advancer `LogManager.Failed(...)` döner (event + reason).
2. System `Reservations.Release(actorId)` + `Mind.ClearAction(Failed)` →
   `Mind.LastFailure = (reason, stamp)` (DOC 1).
3. **Replan bir sonraki tick** `living.decision@PerTick:18`'de: aktör boşta + aç →
   DecisionSystem yeni hedef seçer (`SourceDrained` sonrası aynı site'ı kısa süre dışlama
   politikası DOC 2'nin; buradan sadece reason beslenir). Hikâye tam burada doğar:
   "ekmeğe yürüyordum, Mehmet bitirdi, başka larder'a döndüm".

Yer değiştirme (witness nudge `CascadeSystems.cs:189-191` aktörü itebilir): başarısızlık
DEĞİLDİR. MoveToFood zaten bulunduğu yerden yeniden adımlar; TakeFood reach'i op içinde
yeniden doğrular — reach dışına itildiyse faz `MoveToFood`'a GERİLER (rezervasyon korunur,
`LogManager.Phase` "regressed" detayıyla loglar). Ucuz, kararlı, commitment bozulmaz.

---

## 8. LogManager — tek log kapısı

Kullanıcı direktifi: her eylem fazı TEK LogManager'dan. Domain-saf (Unity yok, IO yok);
`world.Events`'e yazan ince facade. **Kural: faz SINIRLARI loglanır, tick-içi ilerleme
loglanmaz** (NeedsStep özet dersi — MoveToFood'un her adımı loglansaydı yürüyen her aktör
tick başına bir event üretirdi).

```csharp
// Assets/Scripts/Simulation/Actions/LogManager.cs
// CONSTRAINT: the ONLY writer of action_* event lines (greppable grammar, single-sourced
// digest). Stamped with the cadence-boundary stamp (catchup contract) — never world.Time.
public static class LogManager
{
    public static void Started(...)   // action:eat phase:moving_to_food site:{id} tag:{tag}
    public static void Phase(...)     // action:eat phase:taking_food / eating [detail]
    public static ActionStepResult Completed(...) // action:eat done   → returns Completed
    public static ActionStepResult Failed(...)    // action:eat fail reason:{reason} phase:{phase}
}                                                 //   (sets action.FailureReason too)
```

- `Started` DecisionSystem tarafından da AYNI sınıftan çağrılır (DOC 2) — kapı tek.
- Yeni `WorldEventKind` değerleri (enum sonuna eklenir, `WorldEventKind.cs:32`'den sonra):
  `ActionStarted = 33, ActionPhaseChanged = 34, ActionCompleted = 35, ActionFailed = 36`.
- `meal_eaten` satırı `NeedChanged` kind'ıyla AYNEN kalır (§6) — RumorMill/Gate9 tüketicileri
  kırılmaz. Meal başına net +4 event: Started, 2×PhaseChanged, Completed (+fail dalında 1).

---

## 9. Emekli edilen kod yolları

| Yol | Dosya:satır | Akıbet |
|---|---|---|
| `EatOnArrivalStep` | `DefaultTickSystems.cs:254-264`, kayıt `:50` | SİLİNİR; PerTick:22 slotunu `ActionAdvancementStep` devralır |
| Hourly `TryEatCached` çağrısı | `NeedConsumptionSystem.cs:47-49` | SİLİNİR; `Tick` yalnız uyku/fatigue yarısı (`:53-59`) olarak kalır |
| `TickArrivals` + pile cache | `NeedConsumptionSystem.cs:67-126` | SİLİNİR (tek çağıran EatOnArrivalStep'ti) |
| `TryEatCached` / `TryEat` | `NeedConsumptionSystem.cs:128-210` | SİLİNİR; mutasyon bloğu (`:180-188`) `FoodOperations.CompleteConsume`'a birebir taşınır |
| `FindNearestFoodPile` / `FindFoodPile` | `NeedConsumptionSystem.cs:284-332` | Seçim matematiği DOC 2 DecisionSystem'e taşınır (birebir; tie-break sırası korunur) |
| ChooseTarget eat dalı + foodSpot akışı | `ScheduleSystem.cs:129-142`, foodSpots overloadları `:37-53`, `NearestSpot :158-169`, `Seat/SeatOffsets :171-186` | Eat skoru schedule'dan ÇIKAR (yeme kararı DOC 2'de); `Seat/SeatOffsets` DOC 2'nin seat atamasına taşınır; `ScheduleStep`'teki `FoodSpots` beslemesi (`DefaultTickSystems.cs:227-231`) silinir |
| `StepToward` (private) | `ScheduleSystem.cs:191-196` | `MovementService`'e birebir extract (§5) |

`ScheduleSystem.Advance` döngüsüne tek guard eklenir (`ScheduleSystem.cs:61` civarı):

```csharp
if (actor.Mind?.CurrentAction != null) continue; // action layer owns this actor's legs now
```

Kalanlar: schedule rest/work/idle yönlendirmesi ve pursuit çözümü AYNEN kalır (guard/enemy
davranışı bu dilimde action modeline geçmez). `FoodTags` (`:334-343`) ve `TryGetSiteCentre`
(`:213-226`) yaşar — DOC 2 ve FoodOperations tüketir. `FoodSpots/FoodSpot` (`:246-280`)
schedule beslemesi öldükten sonra çağıransız kalırsa silinir.

---

## 10. Registry yerleşimi (WorldTickRegistry + FieldOwnershipRegistry)

`DefaultTickSystems.Create` (`:41-64`) diff'i:

```csharp
  new ScheduleStep(schedule),          // artık action'lı aktörleri atlar
  new CompanionFollowStep(),
- new EatOnArrivalStep(),
+ new DecisionStep(...),               // DOC 2 — living.decision@PerTick:18
+ new ActionAdvancementStep(),         // living.action_advance@PerTick:22
```

`FieldOwnershipRegistry.cs` ledger diff'i (lint `GateContractLintTests` bunu ZORLAR):

```csharp
["Actor.Position"]     : + "living.action_advance@PerTick:22"   // MoveToFood adımı
["Actor.Needs"]        : - "living.eatOnArrival@PerTick:22"
                         + "living.action_advance@PerTick:22"   // ConsumeFood tamamlanışı
["World.Stockpiles"]   : - "living.eatOnArrival@PerTick:22"
                         + "living.action_advance@PerTick:22"   // TakeFood düşümü
["Actor.CarriedFood"]  : + "living.action_advance@PerTick:22"   // YENİ satır — tek yazar
["Actor.CurrentAction"]: + "living.decision@PerTick:18"         // yaratır (DOC 2)
                         + "living.action_advance@PerTick:22"   // ilerletir/bitirir
["World.Reservations"] : + "living.decision@PerTick:18"         // acquire (DOC 2)
                         + "living.action_advance@PerTick:22"   // release
```

---

## 11. Save/load — parallel arrays + mapper + reflection roundtrip

Mind state persistansının ana tasarımı DOC 1'de; EAT'in eklediği kolonlar:

- `ActorSaveData`'ya (aktör başına DTO, `WorldSaveData.cs` actors[] içinde):
  `carriedFoodTag` (string, null/"" = boş el), `actionKind` (int), `actionPhase` (int),
  `actionTicksInPhase` (int), `actionLastHealth` (int), `actionTargetSiteId` (ulong),
  `actionFoodTag` (string), `actionSeatX/actionSeatY` (int) — JsonUtility-uyumlu düz alanlar,
  mevcut pursuit paralel-dizi kalıbıyla aynı ruh (`WorldSaveData.cs:78-81`).
- `WorldSaveMapper.ActorDetail` iki yönde map'ler; `actionKind == 0` → `CurrentAction = null`.
- **Reflection golden roundtrip güvenlik ağıdır**: yeni alan mapper'da unutulursa
  `SaveLoadDigestRoundtripTests` kırmızı yanar — ayrı pin gerekmez (relative test).
- `WorldSaveMapper.CurrentSchemaVersion` +1; eski savelerde alanlar 0/null deserialize olur
  → aktör boşta yüklenir, decision bir tick sonra yeniden plan yapar (kayıpsız migration).

---

## 12. Determinizm ve chunking değişmezliği

Anayasa maddeleri ve bu tasarımın uyum kanıtı:

1. **Stateless + Stamp kuralı** (CAN SUYU): advancer/system nesnelerinde ilerleme alanı YOK;
   tüm eylem durumu WorldState içindeki aktörde. `CadenceChunkingInvarianceTests.cs:17-45`
   DEĞİŞMEDEN yeşil kalmak zorundadır — bu test re-baseline EDİLMEZ, relative'dir; kırmızıysa
   tasarım ihlal edilmiştir (sistem alanına state sızmış ya da stamp yanlış).
2. **Boundary stamp**: tüm LogManager satırları `context.Stamp` ile damgalanır
   (`NeedConsumptionSystem.cs:29-32` catchup dersi) — gün-gün ve çok-gün sıçrama aynı log.
3. **RNG yok**: hedef/seat seçimi decision'da deterministik (tie-break: stockpile sırası,
   ilk-kazanır — bugünkü `TryEatCached :149-151` sözleşmesi DOC 2'ye taşınır).
4. **Perf**: aksiyon hedefini TAŞIDIĞI için bugünkü "her aç aktör × pile × site" taraması
   tamamen ölür (TICKPERF `:71-77` yarasının kökten çözümü); advancement O(action'lı aktör).
   `CatchupPerfPinTests` yeşil kalmalı, muhtemelen iyileşir.

---

## 13. Golden'lar DEĞİŞECEK — re-baseline planı ve test yeniden yazımı

### Beklenen kayma (öngörülebilir, §4 zaman çizgisinden)

- Stok düşümü +1 tick, açlık/susuzluk/mood düşümü +4 tick, `meal_eaten` damgası +4 tick.
- Meal başına +4 `Action*` event'i; event LOG uzar, digest değişir.
- Açlık, 4 tick daha uzun tepe yapar (hourly ramp sınırı araya girerse eski tepeyi 1 kademe
  aşabilir) — Gate1 ufkunda davranışsal fark beklenmez.

### Re-baseline prosedürü (sıra ÖNEMLİ)

1. Kod iner; önce **relative testler**: `CadenceChunkingInvarianceTests` +
   `SaveLoadDigestRoundtripTests` değişmeden yeşil olana kadar baseline'a DOKUNMA.
2. `WorldTickDigestGoldenTests.Advance_OverTwoGameDays_MatchesCommittedBaselineDigest`
   (`:39-50`) kırmızı — beklenen. Seed 4242 iki günlük event diff'i GÖZLE doğrula:
   her `meal_eaten` öncesi tam zincir var mı (`ActionStarted → taking_food → eating →
   ActionCompleted → meal_eaten`), meal sayısı eski koşunun ~±%10'unda mı, açlıktan ölüm
   farkı sıfır mı. Sonra yeni digest sabiti commit edilir; commit mesajına eski/yeni digest
   yazılır (izlenebilirlik).
3. Proof/fallback harness: `--ember-proof-screenshots` protokolüyle koş; UI'ın "eating"
   etiketini `CurrentAction.PhaseName`'den VERBATIM gösterdiği ekran görüntüsüyle kanıtlanır
   (render katmanında doğrula — data-layer logu kanıt değildir).

### Yeniden yazılacak pinler → hikâye testleri

| Mevcut pin | Akıbet |
|---|---|
| `EatOnArrivalTests.TickArrivals_HungryCivilianAtTheLarder_EatsThisVeryTick` (`:15-38`) | SİLİNİR (sözleşme bilinçli değişti: arrival artık yemek DEĞİL, faz geçişi). Yerine `EatActionStoryTests.Hungry_civilian_walks_takes_and_consumes`: faz sırası pinlenir; pile TAM TakeFood tick'inde -1; hunger YALNIZ ConsumeFood bitişinde düşer; `CarriedFoodTag` ara durumda dolu |
| `EatOnArrivalTests.TickArrivals_NotHungryOrNotThere_NoMeal` (`:41-61`) | `EatActionStoryTests.Remote_eating_is_impossible` olarak yeniden doğar: uzaktaki aktör hiçbir fazda pile'ı düşüremez (teşhis kabul testi #1) |
| — (yeni) | `Pile_drained_mid_route_fails_and_replans`: rota ortasında pile boşalt → `ActionFailed reason:SourceDrained`, rezervasyon serbest, sonraki tick decision yeni hedef |
| — (yeni) | `Interrupted_consume_conserves_the_carried_unit`: consume ortasında predation vuruşu → fail, birim elde; toplam birim sayısı değişmez (kayıp/çoğalma yok) |
| `LivingWorldGateTests.Gate1` (`:39`) | KALIR; sadece ufuk toleransı gerekirse +1 saat (meals +4 tick kayar) |
| `LivingWorldGateTests.Gate4` (`:99`) | GÜÇLENDİRİLİR: kalabalık üyelerinin `CurrentAction`'ı Eat olmalı — "activity etiketi action'dan gelir" kabulü pinlenir |
| `LivingWorldGateTests.Gate8` (`:228`) | YENİDEN YAZILIR: seat artık yaratım anında commit (per-tick ordinal değil); eşzamanlı EatAction'ların `SeatCell`'leri birbirinden farklı olmalı |
| `GateContractLintTests` (`:1-83`) | §10 ledger diff'i işlenince kendiliğinden sürer — lint yeni yazarları zorlar |

---

## 14. Sonra genelleşecekler (bilinçli erteleme)

- **Sleep/Work/Harvest advancer'ları**: aynı `IActionAdvancer` kalıbı; ortak kesilme +
  faz-log şablonu 3. eylemde `ActionAdvancerBase`'e (Template Method) çıkarılır — 2 örnekle
  soyutlama erken.
- **MovementService arkasına pathfinding**: dormant `PathfindingSystem` tek satırlık
  entegrasyon noktası kazanır; CascadeSystems'ın inline adım kopyaları da oraya göçer.
- **CarriedFood → gerçek envanter slotu**: Haul dilimi count/stack ister; alan adı bilinçli
  dar tutuldu.
- **ActionQueue/Plan**: dilimde tek eylem yeter (MoveTo+Take+Consume TEK EatAction'ın
  fazları); çok-adımlı plan (Move→Craft→Haul) work diliminde gelir.
- **Projection genellemesi**: eating dışı aktiviteler hâlâ tahmin
  (`DomainSimulationAdapter.WorldProjection.cs:108-142`); her eylem tipi geldikçe tahmin
  dalı ölür, `PhaseName` kalır.

## 15. Satır bütçesi (LOW line count)

Yeni: ActorAction+EatAction+enum'lar ~70, IActionAdvancer+Registry+System ~70,
EatActionAdvancer ~100, FoodOperations ~70, InterruptionProbe ~25, LogManager ~35,
MovementService ~12, DefaultTickSystems adaptörü ~10, ActorRecord alanı ~8 → **~400 satır**.
Silinen: EatOnArrivalStep + TickArrivals/TryEatCached/TryEat/cache/finders ~230, ScheduleSystem
eat dalı + foodSpot akışı ~60 → **~290 satır**. Net ~+110 satıra gerçek faz makinesi,
kesilme, madde korunumu ve tek log kapısı alınır.

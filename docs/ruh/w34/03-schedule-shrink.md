# W34 / Doc 03 — The FINAL ScheduleSystem shrink: ambient drift as the only residue

> Diagnosis reference: `docs/RUH_TESHIS.md` §2.4 ("ScheduleSystem karar değil, her tick yeniden
> hesaplanan yönlendirme"), §6 (manager = metronom; hareket yalnızca aktif Move action), §8
> madde 2 ("`ScheduleSystem` hedef seçip hareket ettiren sistem olmaktan çıkar") ve §10
> ("Activity etiketi `CurrentAction` ile bire bir aynıdır").
>
> Scope: the LAST planned narrowing of `ScheduleSystem` + the endgame of `DescribeActivity`'s
> guess branches + the marathon LIVING census extension. This doc **lands after** W34 Doc 01
> (SLEEP slice) and Doc 02 (WORK slice) — it consumes their contracts (`ActorActionType.Sleep`,
> `ActorActionType.PerformWork`, their ActionVerbTable rows) and only READS them. Enum numbering,
> advancers, decision rules for sleep/work belong to docs 01/02, not here.
>
> Pattern contract: W32 EAT (`5049d445`) and W33 FARM (`61e340f3`) machinery is REUSED, never
> reinvented: `ActorActionState` (append-only enums), `ActionLifecycleSystem` single-writer
> decide+advance, `ActionAdvancer` + registry, `ActionLogManager`, `ReservationLedger`,
> `ActionVerbTable`, story tests under `Assets/Tests/EditMode/Actions/`. Constitution unchanged:
> determinism (pure functions of state + Stamp, no wall clock, no unseeded RNG), save compat
> (all-zero `ActorActionState` extends Idle), **chunking invariance is the referee**, low LOC,
> comments state constraints.

All code anchors verified against the working tree on 2026-07-25 (post-W33, pre-W34).

---

## 1. What is true today (read code, not memory)

`Assets/Scripts/Simulation/Living/ScheduleSystem.cs` (`living.schedule@PerTick:20`,
`DefaultTickSystems.cs:273`) currently steers **actionless** actors with a three-way utility
table plus two carve-outs:

| Branch | Anchor | Fate |
|---|---|---|
| W32 skip-guard: `ActionState.CurrentAction != None → continue` | `:56-58` | **STAYS** — verified sufficient in §4 |
| F18 lair pin: Enemy with `Home == DayAnchor` never routed | `:60-65` | STAYS until COMBAT slice |
| Guard pursuit resolution (`TryResolvePursuit`, prunes `World.GuardPursuits`) | `:68-69`, `:78-99` | STAYS until GUARD slice (declared writer: `FieldOwnershipRegistry.cs:52`) |
| Guard/Enemy `ClassicTarget` (post / curfew commute) | `:113-116`, `:131-137` | STAYS until GUARD/COMBAT slices |
| Civilian utility table: `rest = Fatigue + night bonus`, `work = 55`, `idle = 35` | `:24-29`, `:118-128` | **DIES HERE** |

After Doc 01 lands, tired civilians carry a Sleep chain (decision at `living.decision@PerTick:18`);
after Doc 02, job-holding civilians carry a work chain. The utility table's `rest` and `work`
scores then compete against actors who *never reach the table* — dead branches steering nobody
except the failure cases (§3 says that fallback is exactly what we want, minus the scores).

`FieldOwnershipRegistry.cs:18-26` declares six `Actor.Position` writers. This doc changes none
of the declarations; it narrows what `living.schedule@PerTick:20` writes *within* its slot
(comment updates only: "NARROWED (W34): ambient drift only").

---

## 2. The final form of `ScheduleSystem`

The whole civilian decision collapses to a clock-driven default posture:

```csharp
// Design note (replaces the H2/W32 history block):
// W34 FINAL SHRINK — this system is the world's IDLE ANIMATION, not a decider of deeds.
// Deeds (eat/farm/sleep/work) live in ActionLifecycleSystem; anything with a reservation,
// a duration, a failure reason, or a log line is an ACTION, never a schedule branch.
// CONSTRAINT: steers ONLY actors with ActionState.CurrentAction == None (single-writer:
// living.decision@18 claims legs BEFORE this runs at 20; action_advance@22 moves only
// non-None actors — the write sets are disjoint within every tick).
// CONSTRAINT: stateless + pure function of (actor fields, clock, pursuits) — chunking law.
public static GridPosition ChooseTarget(ActorRecord actor, GameTime time)
{
    // GUESS-era routing survives ONLY for roles that cannot carry actions yet:
    // GUESS(GUARD slice): post routing + pursuit steering become Patrol/Pursue actions.
    // GUESS(COMBAT slice): the curfew commute becomes real hostile behavior.
    if (actor.Role == ActorRole.Guard || actor.Role == ActorRole.Enemy)
        return ClassicTarget(actor, IsWorkHour(time));
    // Ambient drift: day → plaza/anchor stroll, night → home walk. Needs NO LONGER route
    // here — needs create intents in the action layer (W32/W34); the clock is the only input.
    return IsWorkHour(time) ? actor.DayAnchor : actor.Home;
}
```

Deleted with this change (net negative LOC):

- `WorkScore = 55`, `IdleScore = 35`, `NightRestBonus = 25` (`ScheduleSystem.cs:24-29`) — the
  H2 utility constants. Nothing else references them (verified: only `ScheduleSystemTests`).
- The civilian `rest/work/idle` scoring block (`:118-128`) including the
  `ScheduleState.TargetWorksitePosition` civilian branch — walking to work is Doc 02's
  `MoveToWorksite` action now. Civilians stop reading `ActorScheduleState` here entirely.
- The stale "guards eat off-shift"-era comment block is rewritten by the design note above.

Kept verbatim: `Advance` loop shape, skip-guard, lair pin, pursuit resolution + pruning,
`ClassicTarget` (guards still read `ScheduleState.TargetWorksitePosition` for their post),
`IsWorkHour` (shared clock predicate — Doc 02's decision gates on it too; one truth),
`MovementService.StepToward` one-tile stepping.

`WorkStartHour`/`WorkEndHour` stay public on `ScheduleSystem` — they are the colony's clock
truth, imported by the lifecycle decision (`ActionLifecycleSystem.cs:81`) and Doc 02.

---

## 3. Why ambient drift is acceptable residue (the doctrine)

RUH_TESHIS §6 says movement should happen "yalnızca aktif Move action üzerinden." We
deliberately stop short of that for the anchor/home stroll, and this section is the recorded
argument so the exception never silently widens.

**Ambient drift is not a deed.** Measured against the §5 soul chain, the stroll has:

- **no intent** — the actor wants nothing; there is no goal to fail;
- **no cost / no matter** — no item moves, no stock changes, nothing is consumed;
- **no contested resource** — a grid cell is not exclusive (actors co-locate freely); there is
  nothing to reserve, so a `ReservationLedger` row would be a lie;
- **no story on interruption** — a stroll abandoned mid-step loses nothing and nobody cares
  (contrast: an interrupted `HaulCrop` must conserve carried units);
- **no observable outcome** — logging it would be the per-tick event storm W32 explicitly
  refused (the "unlogged fallthrough" lesson in `ActionLifecycleSystem.TryDecideEat`).

An action without reservation, phases, failure, or log is pure ceremony: it would add ledger
noise, log pressure, and save surface while proving nothing. The drift is the world's **idle
animation** — the zero-commitment posture the decision layer preempts at will. Preemption is
structural, not polled: `living.decision@PerTick:18` runs BEFORE `living.schedule@PerTick:20`,
so the tick an actor gains an intent, its legs already belong to the action layer — it never
takes "one last ambient step."

It also degrades honestly: a pre-W32 save deserializes `ActorActionState` as all-zero = Idle,
and an Idle actor's defined behavior is exactly this drift. Legacy actors wake up as strollers,
then get recruited into real deeds by the next decision pass. Save compat and the residue rule
are the same fact.

**The promotion rule (the fence around the residue):** the moment a walk needs ANY of —
a reserved target, a duration, a failure reason, matter movement, or a log line — it MUST be
promoted to an action chain (append an `ActorActionType`, write an advancer, add a verb row).
New `ScheduleSystem` branches are forbidden, mirroring W32 DOC5 §4's "new guess branches are
forbidden." The two role carve-outs (`ClassicTarget`) are tagged `GUESS(GUARD slice)` /
`GUESS(COMBAT slice)` so the grep census (§5.3) tracks their planned death.

---

## 4. Skip-guard verification: the W32 guard already covers the shrink

Claim to verify: *ScheduleSystem never calls `MoveTo` for an actor whose ActionState is
non-idle.* The guard at `ScheduleSystem.cs:56-58` is:

```csharp
if (actor.ActionState.CurrentAction != ActorActionType.None) continue;
```

Three-part argument, valid for every W34 state:

1. **Running phase** — trivially `CurrentAction != None` → skipped.
2. **Terminal handover phases** — Succeeded/Failed states persist *across a tick boundary*
   (marked at `advance@22` of tick T, consumed at `advance@22` of tick T+1). During tick T+1's
   `schedule@20`, `CurrentAction` is still the terminal action (non-None) → skipped. The
   invariant constructor (`ActorActionState.cs`: "None action requires all action fields zero")
   makes a phase-without-action state unrepresentable, so the single field check is airtight.
3. **Intent-only states** (`CurrentIntent != None`, `CurrentAction == None`) — currently
   unrepresentable *between* systems: every `ForIntent(...)` is composed atomically with
   `.Start(...)` inside one decide call, and terminal consumption transitions to full `Idle`.
   If a future slice ever persists a bare intent (e.g. "wants a bed, none free — retry"), the
   guard intentionally does NOT cover it: an intent-only actor keeps ambient drift as its
   posture, which is correct (drift conflicts with nothing — the action layer only moves
   actors with a STARTED action; write sets stay disjoint). Documented here so nobody
   "fixes" the guard to `!IsIdle` and freezes bedless actors in place.

Ordering seals it: within a tick, `decision@18` → `schedule@20` → `advance@22`. Schedule sees
the post-decision state; advance only touches non-None actors. No tick exists in which both
systems write the same actor's `Position`. `Advance_ActorWithActiveAction_IsNotMoved`
(`ScheduleSystemTests.cs:105-121`) pins case 1; add one pin for case 2 (cheap, and terminal
states are the ones that actually persist across the schedule's view):

```csharp
[Test] // W34 §4: terminal (Succeeded) states persist across ticks and must also freeze the router.
public void Advance_ActorWithTerminalAction_IsNotMoved()
```

(Same fixture as the existing test; state = `...Start(...).Advanced().Succeeded()`.)

---

## 5. `DescribeActivity` endgame

`DomainSimulationAdapter.WorldProjection.cs` — the verb is already a verbatim projection of
`CurrentAction` via `ActionVerbTable` (W32 DOC5); guess branches survive only in
`DescribeScheduleWord` + `IsAsleepAtHome`. Ledger of every remaining guess and its death:

| Guess (today) | Anchor | Killed by | Replacement |
|---|---|---|---|
| `IsAsleepAtHome` (hour 22–06 + Chebyshev ≤1 to home → lying pose) | `WorldProjection.cs:100-108` | **W34 Doc 01** | `sleeping: actor.ActionState.CurrentAction == ActorActionType.Sleep` in `ProjectActor` |
| `"sleeping"` / `"heading home"` (night hours) | `:127` | **W34 Doc 01** | verbs born from the Sleep chain's actions (Doc 01's ActionVerbTable rows); an actionless night drifter shows **no label** |
| `"winding down"` (hour ≥20) | `:128` | **W34 Doc 01** | nothing — pre-sleep drift is unlabeled ambient |
| `"working"` (work hour + `!ScheduleState.IsIdle`) | `:134` | **W34 Doc 02** | `PerformWork` verb row (+ `MoveToWorksite` row) read verbatim |
| HAMMER pose icon (worker + 8–18 hour poll) | `NpcPoseIconView.cs:39,42` | **W34 Doc 02** | `SetActionKind("PerformWork")` push, same pattern as the W32 MUG fix; the `RuntimeFieldMirror.HourOfDay` poll dies with it |
| `"on watch"` (Guard role) | `:125` | GUARD slice (future) | Patrol/Watch actions |
| `"hunting"` (Enemy role) | `:126` | COMBAT slice (future) | hostile actions |
| `null` for idle civilians ("about town" playtest lesson) | `:134` | **NEVER** | permanent: ambient drift is not a deed (§3), so it honestly has no verb |

Post-W34 `DescribeScheduleWord` (terminal form until the guard/combat slices):

```csharp
// W34 Doc 03: the calendar-word fallback is now ROLE-ONLY. Civilian ambient drift shows no
// label — a verb appears only when it states a true CurrentAction (RUH_TESHIS §10). New guess
// branches remain forbidden: new verb = new action type + ActionVerbTable row.
private string DescribeScheduleWord(ActorRecord actor)
{
    if (actor.Role == ActorRole.Guard) return "on watch"; // GUESS(GUARD slice): retire with guard actions
    if (actor.Role == ActorRole.Enemy) return "hunting";  // GUESS(COMBAT slice): retire with combat actions
    return null; // ambient drift: deliberately wordless (W34 Doc 03 §3)
}
```

The `int hour` local and the `_world.Time` read disappear from the function — after W34 the
projection's ONLY clock consumer is gone, which is the cleanest possible statement of §2.9's
cure: *no verb in the game can be derived from the clock anymore.*

### 5.1 Acceptance grep

`grep -rn "GUESS(" Assets/Scripts/Presentation/` after W34 returns exactly the two role rows
above (WorldProjection.cs) and nothing in `NpcPoseIconView.cs`. This grep is the survivors'
census and the work-list for the GUARD/COMBAT slices — same convention as W32 DOC5 §4.

### 5.2 Label truth pins

`Assets/Tests/EditMode/Presentation/VisualLayer/ActivityLabelTruthTests.cs` gains the Doc 01/02
verb rows (their docs own those pins). This doc adds one negative pin: an actionless civilian
projects `Activity == null` at a night hour and at a work hour (the guess words are dead, not
relocated). Pure EditMode — `ActionVerbTable` and the state factory are public.

---

## 6. Marathon LIVING census extension: prove the slices live at scale

Memory lessons apply: data-layer logs are not proof, and a soak that proves nothing must not
PASS (the Potemkin rule already in `RunMarathon`). The census must show, from `ActorActionState`
alone, that sleep and work exist as *deeds* in a full-scale world.

### 6.1 Instantaneous counts in `ProofLivingCensus`

`DomainSimulationAdapter.WorldEncounter.cs:668-685` already walks `Actors.Records` for
`aliveActors`. Extend that same loop (no second pass, no allocation):

```csharp
int sleeping = 0, working = 0, acting = 0;
// inside the existing alive-actor loop:
var act = a.ActionState.CurrentAction;
if (act != ActorActionType.None) acting++;
if (act == ActorActionType.Sleep) sleeping++;            // Doc 01's enum value
else if (act == ActorActionType.PerformWork) working++;  // Doc 02's enum value
```

Returned string appends: `acting={acting} sleeping={sleeping} working={working}`. `acting`
is the free extra that catches "everyone is stuck in one giant chain" pathologies.

### 6.2 Peaks in the soak driver — an end-of-run snapshot proves nothing

A soak ending at noon legitimately shows `sleeping=0`; ending at 03:00 shows `working=0`. So
the driver tracks **peaks at the existing heartbeat** (`EmberProofScreenshotDriver.RunMarathon`,
the 60 s-realtime block at `:989-998`) — no per-tick cost, read-only, cannot touch determinism:

```csharp
// adapter (one small typed probe; parsing the census string would be a stringly API):
public (int sleeping, int working) ProofActionCounts()   // same loop, counts only

// driver locals: int sleepingPeak = 0, workingPeak = 0; long gameMinStart = <world clock at arm>;
// heartbeat block adds:
var (slp, wrk) = adapter.ProofActionCounts();
if (slp > sleepingPeak) sleepingPeak = slp;
if (wrk > workingPeak) workingPeak = wrk;
// heartbeat log line appends: $" sleeping={slp}(peak {sleepingPeak}) working={wrk}(peak {workingPeak})"
```

Final line: `[Marathon] LIVING: {census} sleepingPeak={n} workingPeak={n} gameHours={span}`.

### 6.3 Honesty rule (folded into PASS, gated on coverage)

Mirror of the existing `actions > 0` rule: compute `gameHours` from the world clock delta
(`_world.Time.TotalMinutes` at arm vs. end — game time, never wall clock). Then:

```csharp
// A soak that lived through a full day-night cycle in which NOBODY slept or NOBODY worked
// is a broken world wearing a green badge — the exact Potemkin pattern the V2 contract kills.
bool censusOk = gameHours < 24 || (sleepingPeak > 0 && workingPeak > 0);
bool pass = exceptions == 0 && flat && !aborted && actions > 0 && censusOk;
```

Under 24 game-hours the peaks are logged but advisory (a short smoke soak must not flake on
clock phase). At or above one full cycle they are load-bearing: the SLEEP and WORK slices must
be visible *as ActionState facts* in the very harness that certifies marathons. Render-layer
proof (screenshot of a "sleeping" label/pose born from `CurrentAction == Sleep`) belongs to
Doc 01's agentcheck extension; the census is the scale half of the evidence, not a replacement.

---

## 7. Pinned-test migrations

| Test | Anchor | Fate |
|---|---|---|
| `ChooseTarget_UtilityTable_NeedsDriveTheChoice` | `ScheduleSystemTests.cs:126-144` | REWRITE → `ChooseTarget_AmbientDrift_ClockIsTheOnlyInput`: day→anchor, night→home, and **needs are ignored** (set Fatigue 90 by day → still anchor; the assertion message documents that needs now live in the action layer) |
| `Advance_DuringWorkHours_StepsAssignedActorOneTileTowardWorksite` + `Advance_RepeatedDuringWorkHours_ConvergesToWorksiteWithoutOvershoot` | `:25-53` | DIE with Doc 02 (worksite walk = `MoveToWorksite` action; Doc 02's story tests own the journey). If Doc 02 has not landed when this doc's code does, land this doc AFTER — sequencing is a hard dependency, see header |
| `Advance_OutsideWorkHours_StepsAssignedActorTowardHome` | `:93-105` | KEEP, retitle intent: it now pins the ambient home walk (assigned-ness is irrelevant; drop the `ApplyScheduleState` line) |
| `Advance_DuringWorkHours_StepsIdleActorTowardDayAnchor`, `Advance_IdleActorWithoutAnchor_DoesNotMove` | `:67-91` | KEEP verbatim — they already pin the residue |
| `Advance_ActorWithActiveAction_IsNotMoved` | `:105-121` | KEEP + add the terminal-phase sibling (§4) |
| `Advance_PinnedEnemyLairGuard_HoldsPositionEvenWhenDisplaced` | `:146-160` | KEEP verbatim (COMBAT-slice carve-out) |
| `ActivityLabelTruthTests` | `Presentation/VisualLayer` | + null-label negative pin (§5.2); verb rows are Docs 01/02 |
| `CadenceChunkingInvarianceTests`, `EatChunkingPhaseTraceTests`, farm chunking | — | UNTOUCHED referee. The shrink makes `ChooseTarget` a pure function of (role, clock, home, anchor) — strictly fewer inputs than today, so invariance can only get easier. Any diff here means the shrink leaked state; revert, do not re-baseline |
| Golden saves / roundtrip | — | UNTOUCHED: this doc adds zero save fields; deleted constants are code-only |

---

## 8. Change surface (low-LOC summary)

| File | Change |
|---|---|
| `Simulation/Living/ScheduleSystem.cs` | utility table + 3 constants deleted, design note rewritten (~ −25 net) |
| `Presentation/Ember/Adapters/DomainSimulationAdapter.WorldProjection.cs` | `DescribeScheduleWord` → 3 lines; hour local dies (~ −8) *(after Docs 01/02 delete their branches)* |
| `Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs` | census counts + `ProofActionCounts` (~ +12) |
| `Presentation/Ember/Diagnostics/EmberProofScreenshotDriver.cs` | peaks, heartbeat append, `censusOk` (~ +12) |
| `Simulation/Composition/FieldOwnershipRegistry.cs` | comment update only ("NARROWED (W34): ambient drift only") |
| `Assets/Tests/EditMode/Living/ScheduleSystemTests.cs` | migrations per §7 |
| `Assets/Tests/EditMode/Presentation/VisualLayer/ActivityLabelTruthTests.cs` | + null-label pin |

No new files. No save-shape change. No new tick steps, no cadence/order changes.

---

## 9. Acceptance criteria

1. `ScheduleSystem` contains no need-score input: civilians route by clock + home/anchor only;
   grep for `Needs` in the file returns nothing.
2. No tick can double-move an actor: the §4 pins (Running AND terminal phase) are green, and
   `FieldOwnershipRegistry` declarations are unchanged (the ownership lint stays quiet).
3. `grep -rn "GUESS(" Assets/Scripts/Presentation/` returns exactly `on watch` (GUARD) and
   `hunting` (COMBAT) — every W34-scoped guess (sleep words, "winding down", "working",
   HAMMER hour poll, `IsAsleepAtHome`) is deleted, not thinned.
4. An actionless civilian projects `Activity == null` day and night (negative pin green).
5. A ≥24-game-hour marathon soak reports `sleepingPeak > 0 && workingPeak > 0` and folds
   that into VERDICT; a sub-24h soak logs the peaks as advisory.
6. Chunking invariance suites and golden saves pass without re-baselining.
7. Every surviving comment names its constraint (single-writer ordering, chunking law,
   promotion rule, wordless-drift doctrine).

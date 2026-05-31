# PRD — Data-Driven World-Tick Composition (E7-018, v1)

| Field | Value |
| --- | --- |
| Audit item | **E7-018** — make the world-tick composition data-driven (tick systems are currently composed in a fixed, hardcoded order). |
| Risk class | **DETERMINISM-CRITICAL.** The tick order *is* the simulation contract. Any reorder, drop, or behavioural change silently corrupts every save and every replay. |
| Engine coupling | **None.** All touched code lives in `EmberCrpg.Simulation` / `EmberCrpg.Domain` (`noEngineReferences: true`). The PRD must stay engine-free and pure-deterministic. |
| Strategy | **Safety-net first.** Phase 1 ships a same-seed world-state **digest golden test** and commits a baseline hash. Phase 2 refactors the hardcoded call list into an ordered, declarative registry that **reproduces the digest byte-for-byte**. The refactor is not allowed to land unless the baseline hash is unchanged. |
| Status | Proposed. No code written. No build run. |

---

## 0. Why this is risky, and why the digest comes first

`WorldTickComposer.Advance(WorldState, int)` is the single per-tick orchestrator for the live game (`DomainSimulationAdapter.AdvanceTick` calls `_tickComposer.Advance(_world, tickIndex)` at `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.cs:89`). It hand-codes the order in which deterministic systems mutate the world, gated into three cadence bands. The exact statement order — and the cadence gating — is load-bearing for determinism:

- **Per-tick:** time advance, then magic (cooldown / shield-buff decay).
- **Hourly band** (`TicksPerGameHour = 10`), per crossing, in this order: **(1)** assign pending jobs, **(2)** schedule step, **(3)** needs decay over every actor.
- **Daily band** (`TicksPerGameDay = 240`), per crossing, in this order: **(1)** caravans, **(2)** plant growth, **(3)** price recompute.

Two existing tests assert *aspects* of this but **none pins the whole world**:

- `Assets/Tests/EditMode/Composition/WorldLivesOverNTicksTests.cs` — asserts a handful of scalar outcomes (one plant stage advanced, one job claimed, one price rose) over 480 ticks, and a self-equality determinism check that compares a **6-field string snapshot** of the same run twice.
- `Assets/Tests/EditMode/Composition/WorldTickComposerReplayTests.cs` — asserts the **needs-event cadence timestamps** survive a cold save/load via `RebuildAccumulatorsFrom`.

Neither would catch a reorder that happens to leave those specific fields equal (e.g. swapping caravan and price order, or moving needs before schedule when no actor's path crosses a job tile in the seed). A whole-world digest closes that gap: it hashes **every mutated store** after N ticks, so *any* divergence in *any* system's output flips the hash.

**Therefore Phase 1 (digest) must be committed and green before a single line of Phase 2 is written.** The digest is the contract the refactor is measured against.

---

## 1. Files in scope

### Read / unchanged contract (do not modify)

| Path | Role |
| --- | --- |
| `Assets/Scripts/Simulation/Composition/WorldTickComposer.cs` | The hardcoded composer. Public surface to preserve: `Advance(WorldState, int)`, `ResetAnchor()`, `RebuildAccumulatorsFrom(GameTime)`, and the constants `MinutesPerTick`, `TicksPerGameDay`, `TicksPerGameHour`. All four constructors stay source-compatible. |
| `Assets/Scripts/Domain/World/WorldState.cs` | The state the digest walks. Stores mutated per tick: `Time`, `Actors`, `Events`, `Plants`, `Soils`, `Jobs`, `Worksites`, `Prices`, `Stockpiles`, `Caravans`, `PlayerSpellCooldowns`, `PlayerShieldBuffs`. |
| `Assets/Scripts/Simulation/Time/GameTimeAdvanceSystem.cs` | `Advance(GameTime, long)` — per-tick clock. |
| `Assets/Scripts/Simulation/Magic/MagicTickDriver.cs` | `AdvanceTicks(SpellCooldownState, ShieldBuffState, int)` — per-tick. |
| `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs` | `TryAssignNext(ActorStore, JobBoard, WorksiteStore, out result)` — hourly. |
| `Assets/Scripts/Simulation/Living/ScheduleSystem.cs` | `Advance(ActorStore, GameTime)` — hourly. |
| `Assets/Scripts/Simulation/Living/NeedsSystem.cs` | `TickActorNeeds(actor, WorldEventLog, GameTime, ticks)` — hourly. |
| `Assets/Scripts/Simulation/World/CaravanSystem.cs` | `Tick(caravans, findRoute, findStockpile, GameTime, events)` — daily. |
| `Assets/Scripts/Simulation/Process/PlantGrowthSystem.cs` | `AdvanceOneDay(species, plants, events, GameTime, season, isSnowing)` — daily. |
| `Assets/Scripts/Simulation/World/PriceUpdateSystem.cs` | `Recompute(prices, stockpile, tag, low, high, step, GameTime, events)` — daily. |

### Phase 1 — add (digest golden test)

| Path | Action | Purpose |
| --- | --- | --- |
| `Assets/Scripts/Simulation/Composition/WorldStateDigest.cs` | **add** | Pure, deterministic, engine-free static class that serialises the mutated world stores into a canonical byte stream and returns a stable hex hash. Lives in the simulation assembly (not test-only) so production replay/diagnostics can reuse it. |
| `Assets/Scripts/Simulation/Composition/WorldStateDigest.cs.meta` | **add** | Unity meta (fresh GUID). |
| `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs` | **add** | The golden test: build the fixed seed, run N ticks through the **current** `WorldTickComposer`, assert the digest equals a committed baseline constant. |
| `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs.meta` | **add** | Unity meta (fresh GUID). |

### Phase 2 — add (data-driven registry)

| Path | Action | Purpose |
| --- | --- | --- |
| `Assets/Scripts/Simulation/Composition/TickCadence.cs` | **add** | `enum TickCadence { PerTick = 0, Hourly = 1, Daily = 2 }`. Explicit integer values make the cadence ordering total and serialisation-stable. |
| `Assets/Scripts/Simulation/Composition/IWorldTickSystem.cs` | **add** | The declarative unit: `string Id`, `TickCadence Cadence`, `int Order`, `void Run(in TickContext ctx)`. |
| `Assets/Scripts/Simulation/Composition/TickContext.cs` | **add** | Readonly struct carrying everything a step needs without reaching into globals: the `WorldState world`, the cadence-boundary `GameTime Stamp`, and the integer `Delta` for per-tick steps. |
| `Assets/Scripts/Simulation/Composition/WorldTickRegistry.cs` | **add** | Owns the ordered list of `IWorldTickSystem`, sorts it by `(Cadence, Order, Id)`, rejects duplicate `Id`s, and exposes a frozen read-only view grouped by cadence. This is the single declarative source of tick order. |
| `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs` | **add** | The factory that declares the canonical registry: nine thin adapter steps wrapping the nine existing systems, with `Order` values that reproduce today's exact statement order. |
| `*.meta` for each of the five new files | **add** | Unity metas (fresh GUIDs). |
| `Assets/Scripts/Simulation/Composition/WorldTickComposer.cs` | **modify** | Replace the inline cadence blocks with a drive-loop over the registry. Public surface, constants, cadence math, and event-stamp arithmetic are **unchanged**. The four constructors keep working (they now build the default registry, or accept an injected one via one new internal overload). |
| `Assets/Tests/EditMode/Composition/WorldTickRegistryTests.cs` (+`.meta`) | **add** | Unit tests on the registry: stable sort, duplicate-Id rejection, exact declared order matches the documented canonical order. |

> **Assembly check:** every new production file lands under `Assets/Scripts/Simulation/Composition/`, inside `EmberCrpg.Simulation` (`noEngineReferences: true`). No `UnityEngine` references anywhere. The test files land in `EmberCrpg.Tests.EditMode`, which already references `EmberCrpg.Simulation` and `EmberCrpg.Domain`.

---

## 2. Phase 1 — same-seed world-state DIGEST golden test (ships FIRST)

### 2.1 Goal

Pin the **entire** post-tick world to a single committed hash so that, after the Phase 2 refactor, a byte-identical hash *proves* zero behavioural drift. This is the regression net; it has standalone value even if Phase 2 slipped.

### 2.2 The digest function — `WorldStateDigest.Compute(WorldState)`

Design constraints (all mandatory — these are what make the hash reproducible across machines and runs):

1. **Pure + engine-free.** No `UnityEngine`, no `DateTime.Now`, no environment, no floating-point formatting that varies by culture. Use `System.Security.Cryptography.SHA256` (already used in-repo at `Assets/Scripts/Simulation/Forge/PromptHash.cs`) over a canonical UTF-8 byte stream, lower-case hex output, mirroring `PromptHash.Sha256`'s formatting (`"x2"`, `InvariantCulture`).
2. **Deterministic ordering.** Never hash a dictionary/store in enumeration order. For every keyed collection, **materialise to a list and sort by the key** (e.g. component id, actor id, item tag) before writing. Stockpile tags sorted with `StringComparer.Ordinal`. This removes any dependence on insertion order or hashtable layout.
3. **Canonical field encoding.** Write a small, explicit set of fields per record in a fixed sequence with explicit separators and a per-section tag (e.g. `"PLANTS\n"`), so two different shapes can never collide. All numbers via `IFormatProvider = InvariantCulture`. Booleans as `0`/`1`. No locale, no reflection — hand-rolled, reviewable field lists.
4. **Cover exactly the per-tick mutable surface** (from §1), in this fixed section order:
   1. `Time.TotalMinutes`
   2. `Actors` — sorted by `ActorId`: id, position X/Y, `ScheduleState.IsIdle`, `ScheduleState.CurrentJobId`, and each need value (the fields `NeedsSystem` decays).
   3. `Plants` — sorted by `WorldComponentId`: id, `StageId.Value`, `DaysInStage`.
   4. `Soils` — sorted by id: id, fertility, moisture, plantId (growth can mutate soil-linked state).
   5. `Jobs` — sorted by `JobId`: id, `GetStatus(id).Code`, `GetClaimedBy(id).Value`.
   6. `Prices` — sorted by `(SiteId, tag)`: site, tag, price.
   7. `Stockpiles` — sorted by site; within each, entries sorted Ordinal by tag: tag, quantity.
   8. `Caravans` — sorted by caravan id: id, route/leg/progress scalars.
   9. `PlayerSpellCooldowns` / `PlayerShieldBuffs` — sorted by spell/buff key: key, remaining ticks.
   10. `Events` — count, plus for each event in **existing log order** (the log is append-only and order is itself part of the contract): `Tick.TotalMinutes`, `Kind`, and the stable `Detail`/reason string. Including events makes the digest sensitive to *cadence-stamp* drift too, which is the subtle class of bug §0 worries about.
5. **Versioned.** Prefix the stream with a literal `"WSDIGEST_v1\n"`. If the digest's own field set ever changes intentionally, bump to `v2` and re-baseline — so a *schema* change is never mistaken for a *behaviour* change.

Signature:

```
namespace EmberCrpg.Simulation.Composition
{
    public static class WorldStateDigest
    {
        public static string Compute(WorldState world);   // lower-case SHA-256 hex
    }
}
```

> The exact getter names above are the ones the current tests already use against these stores (`Plants.Get(id).StageId.Value`, `Plants.Get(id).DaysInStage`, `Jobs.GetStatus(id).Code`, `Jobs.GetClaimedBy(id).Value`, `Prices.GetPrice(site, tag)`, `stockpile.Entries` key/value, `actor.ScheduleState.IsIdle/.CurrentJobId`). Implementation reuses those accessors; it does **not** introduce new domain API.

### 2.3 The golden test — `WorldTickDigestGoldenTests`

- **Seed builder:** reuse the exact fixed world from `WorldLivesOverNTicksTests.BuildSeededWorld()` (worker, forge worksite + pending smith job, wheat seed + soil, understocked iron stockpile @ price 10, clock at 08:00). To avoid copy-drift, either (a) call a shared internal builder, or (b) duplicate it verbatim with a comment pointing at the canonical copy. **Prefer (a):** promote `BuildSeededWorld()` to an internal static helper both test classes call, so the seed can never silently diverge between the two tests.
- **Run:** `composer.Advance(world, 0)` to anchor, then `for tick in 1..N: composer.Advance(world, tick)`. Use **N = 480** (two game-days) — the same horizon as the acceptance test, guaranteeing at least 2 daily crossings and 48 hourly crossings so every band is exercised.
- **Assert:** `Assert.That(WorldStateDigest.Compute(world), Is.EqualTo(BaselineHash))`, where `BaselineHash` is a `const string` committed in the test file.
- **Second-run determinism guard (same test class):** build a fresh seed, run again, assert the two digests are equal to each other (catches non-determinism even before comparing to baseline).
- **Multi-N sanity (optional but recommended):** also assert digests at N=240 and N=480 are *different* from each other (proves the digest actually moves with simulation — a constant hash would be a false safety net).

### 2.4 How the baseline hash is produced and committed

The baseline is **not invented**; it is captured from the current, unmodified `WorldTickComposer`:

1. Land `WorldStateDigest.cs` and the test with a deliberately-wrong placeholder baseline.
2. Run the EditMode suite once (`tests-run` / fallback harness). The assertion fails and the failure message prints the **actual** computed hash.
3. Paste that actual hash into `BaselineHash`. Re-run — now green.
4. Commit Phase 1 (digest + baseline) **on its own**, before any Phase 2 work. This commit is the immutable reference point.

> The baseline is captured against today's behaviour, so it encodes the *current* order as truth. That is exactly the property Phase 2 must not break.

### 2.5 Phase 1 acceptance

- `WorldTickDigestGoldenTests` is green against the captured baseline.
- The whole existing EditMode suite still green (digest adds, changes nothing).
- Committed independently with the baseline hash, before Phase 2 begins.

---

## 3. Phase 2 — data-driven tick composition (lands ONLY if the digest is unchanged)

### 3.1 Goal

Replace the hardcoded call list in `WorldTickComposer.Advance` with an **ordered, declarative registry** of tick systems, while reproducing the **exact** current execution order, cadence gating, and event-stamp arithmetic — proven by an unchanged Phase 1 digest.

### 3.2 The declarative unit — `IWorldTickSystem`

```
public enum TickCadence { PerTick = 0, Hourly = 1, Daily = 2 }

public readonly struct TickContext
{
    public WorldState World { get; }
    public GameTime  Stamp { get; }   // cadence-boundary stamp (per-tick steps may ignore)
    public int       Delta { get; }   // ticks advanced this Advance call (for per-tick steps)
}

public interface IWorldTickSystem
{
    string      Id      { get; }   // stable, unique, e.g. "core.time"
    TickCadence Cadence { get; }
    int         Order   { get; }   // tie-break within a cadence; lower runs first
    void Run(in TickContext ctx);
}
```

Each existing system is wrapped in a **thin adapter** that does *only* what the corresponding inline block does today — no behavioural change, just relocation. Adapters live in `DefaultTickSystems.cs`.

### 3.3 Canonical order (must reproduce today's statement order exactly)

The registry declares these nine steps. `Order` values are spaced by 10 to leave room for future insertions without renumbering:

| Id | Cadence | Order | Wraps | Equivalent of today's line |
| --- | --- | --- | --- | --- |
| `core.time` | PerTick | 10 | `GameTimeAdvanceSystem.Advance` | `world.Time = _timeAdvance.Advance(world.Time, delta * MinutesPerTick)` |
| `core.magic` | PerTick | 20 | `MagicTickDriver.AdvanceTicks` | magic cooldown / shield decay (guarded by the same non-null check) |
| `econ.jobs` | Hourly | 10 | `JobAssignmentSystem` + event append | `AssignPendingJobs(world, stamp)` |
| `living.schedule` | Hourly | 20 | `ScheduleSystem.Advance` | `_schedule.Advance(world.Actors, stamp)` |
| `living.needs` | Hourly | 30 | `NeedsSystem.TickActorNeeds` over actors | the per-actor needs loop |
| `world.caravans` | Daily | 10 | `CaravanSystem.Tick` | `_caravans.Tick(...)` |
| `econ.plantgrowth` | Daily | 20 | `PlantGrowthSystem` over species | `AdvancePlantGrowth(world, stamp)` |
| `econ.prices` | Daily | 30 | `PriceUpdateSystem.Recompute` over stockpiles | `RecomputePrices(world, stamp)` |

> This table **is** the contract. The Order column is chosen so the sorted registry yields precisely the sequence in `WorldTickComposer.Advance` today. `WorldTickRegistryTests` asserts this exact `(cadence, order, id)` sequence as a literal expected list, so an accidental reorder fails a unit test *in addition to* the digest.

### 3.4 How the registry guarantees stable order

`WorldTickRegistry` is the single source of truth for ordering. Its guarantees:

1. **Deterministic sort key.** On construction it sorts the supplied steps by the **total** key `(Cadence ascending, Order ascending, Id Ordinal ascending)`. `Id` is the final tie-break so the order is total even if two steps share a cadence+order — there is never an "equal" pair left to a non-stable sort. (`List.Sort` is introsort and *not* stable, so we must not rely on stability; the explicit total key removes the need for it.)
2. **Duplicate-Id rejection.** Construction throws if two steps share an `Id`. Ids are the stable handles a future data-file would reference, so collisions are a hard error, not a silent last-wins.
3. **Frozen after build.** The sorted list is copied into a read-only array exposed grouped by cadence (`PerTick`, `Hourly`, `Daily` views). No mutation after construction — the composer cannot accidentally reorder at runtime.
4. **Cadence isolation.** The composer asks the registry for each band's frozen list and iterates it; cadence membership is data on the step, not a hardcoded `if`. Moving a step between bands is a one-line `Cadence`/`Order` edit in `DefaultTickSystems.cs` — and any such move shows up immediately as a digest diff.
5. **No hidden ordering inputs.** The registry never consults wall-clock, hash codes, reflection order, or `Dictionary` enumeration. Order depends solely on the declared `(Cadence, Order, Id)` triples — which is what makes it portable and replay-safe.

> **"Data-driven" scope decision (explicit):** the canonical registry is declared **in code** (`DefaultTickSystems.cs`) as an ordered list of records — *not* loaded from a JSON/ScriptableObject asset. Rationale: (a) loading order from an external file would itself be a determinism surface (parse order, culture, asset-import nondeterminism) that contradicts the engine-free, replay-safe mandate; (b) the audit's intent — *"composition is no longer a hardcoded call sequence buried in `Advance`"* — is satisfied by a declarative, reorderable, testable registry of typed steps. The architecture leaves a clean seam (`WorldTickRegistry(IEnumerable<IWorldTickSystem>)`) so a future PRD could source steps from data **if** a deterministic loader is specified then. This PRD deliberately stops at the in-code declarative registry to keep the determinism guarantee airtight. This is called out so reviewers do not read "data-driven" as "must parse a file".

### 3.5 What `WorldTickComposer.Advance` becomes

The cadence **math is untouched** — the same `_ticksSinceHourly` / `_ticksSinceDaily` accumulators, the same crossing count, the same boundary-stamp arithmetic (`stampMinutes = world.Time.TotalMinutes - _ticksSince... - (crossings - i) * period`). Only the *bodies* of the three loops change: instead of inline calls they iterate the registry's frozen per-band list and invoke `Run(in ctx)`.

Sketch (illustrative, not final code):

```
// per-tick band
foreach (var s in _registry.PerTick) s.Run(new TickContext(world, world.Time, delta));

// hourly band — unchanged crossing math
for (int i = 1; i <= hourlyCrossings; i++) {
    var stamp = /* identical stamp arithmetic as today */;
    foreach (var s in _registry.Hourly) s.Run(new TickContext(world, stamp, delta));
}

// daily band — unchanged crossing math
for (int i = 1; i <= dailyCrossings; i++) {
    var stamp = /* identical stamp arithmetic as today */;
    foreach (var s in _registry.Daily) s.Run(new TickContext(world, stamp, delta));
}
```

The per-step **null guards** that exist today (e.g. `world.Actors == null`, `world.Events == null`, magic state non-null) move *into* each adapter's `Run`, so the guard semantics are byte-identical — a step that is a no-op today stays a no-op.

**Constructor compatibility:** the four current constructors keep their signatures and now internally build the default registry via `DefaultTickSystems.Create(...)`, passing through any injected systems they already accept (time/needs/magic/caravan + the SOUL ctor's growth/jobs/price/schedule). One **new internal** constructor `WorldTickComposer(GameTimeAdvanceSystem, WorldTickRegistry)` (or an internal registry-accepting overload) enables registry-injection tests without widening the public surface. The parameterless ctor — the one `DomainSimulationAdapter` uses — behaves identically.

### 3.6 Acceptance (Phase 2)

1. **Digest unchanged — the gate.** `WorldTickDigestGoldenTests` passes against the **same baseline hash committed in Phase 1, byte-for-byte, with no edit to the baseline constant.** If the refactor required changing the baseline, it changed behaviour and is rejected.
2. `WorldLivesOverNTicksTests` (both tests) still green — acceptance scalars and self-equality intact.
3. `WorldTickComposerReplayTests` (both tests) still green — cold-load cadence and the DET-01 `RebuildAccumulatorsFrom` contract intact.
4. `WorldTickRegistryTests` green — stable total-order sort, duplicate-Id rejection, and the literal canonical-order assertion (§3.3) all hold.
5. Full EditMode suite green; no new warnings.
6. No public API removed/renamed on `WorldTickComposer`; `DomainSimulationAdapter` is **not** modified (it still calls `new WorldTickComposer()` + `Advance(world, tick)`).
7. No `UnityEngine` reference introduced in `EmberCrpg.Simulation`.

### 3.7 Out of scope

- Loading tick order from an external asset/JSON (see §3.4 decision).
- Adding, removing, or re-rating any simulation system (e.g. wiring faction-reputation decay). The step set is exactly today's nine.
- Touching `DomainSimulationAdapter`, save/load mapping, or any Presentation code.
- Changing cadence constants or the event-stamp arithmetic.

---

## 4. Determinism guarantee (the through-line)

1. **Capture before change.** Phase 1 freezes the current whole-world behaviour into one SHA-256 baseline, committed standalone.
2. **Total, input-free ordering.** Phase 2's order is a pure function of declared `(Cadence, Order, Id)` triples — no wall-clock, no hashtable order, no file parse, no float formatting. Same inputs → same order on every machine.
3. **Math preserved.** The cadence accumulators, crossing counts, and boundary-stamp arithmetic are copied verbatim; only call dispatch is relocated.
4. **Proven equal.** The refactor is admissible **iff** the Phase 1 digest is byte-identical with an untouched baseline constant — and the three existing determinism/replay tests plus the new registry-order test all stay green.

Net: the world that the registry-driven composer produces after N ticks is provably indistinguishable, bit-for-bit, from the world the hardcoded composer produces today.

---

## 5. Verification plan

- **Primary:** EditMode tests via the project's runner (`tests-run`) and the **fallback validation harness** (the headless EditMode runner this repo uses as the green-gate; same harness invoked after every SOUL/RA task in the project history). Run order: (1) confirm baseline-capture run prints and pins the hash; (2) post-refactor run must be green **without** editing `BaselineHash`.
- **Targeted classes:** `WorldTickDigestGoldenTests`, `WorldTickRegistryTests`, `WorldLivesOverNTicksTests`, `WorldTickComposerReplayTests`.
- **Meta hygiene:** every new `.cs` gets a fresh-GUID `.meta` (matching this repo's convention) so the fallback harness imports cleanly.
- **No build, no Play mode, no scene work** is required or permitted by this PRD beyond running the EditMode harness.
- **Rollback:** Phase 1 and Phase 2 are separate commits. If Phase 2's digest diverges and the cause can't be made byte-identical, revert the Phase 2 commit; Phase 1 (the digest net) stays and continues guarding `main`.

---

## 6. Commit sequencing

1. **Commit A (Phase 1):** `WorldStateDigest.cs` (+meta) + `WorldTickDigestGoldenTests.cs` (+meta) + promote shared seed builder. Baseline hash captured and pinned. Suite green.
2. **Commit B (Phase 2):** registry types + `DefaultTickSystems` + `WorldTickComposer` drive-loop refactor + `WorldTickRegistryTests`. Digest **unchanged**, all suites green.

Never squash A into B — the standalone Phase-1 commit is the audit artifact proving the net existed before the refactor.

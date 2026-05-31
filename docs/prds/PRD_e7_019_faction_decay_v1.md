# PRD: Faction Reputation Decay (E7-019) — Deterministic World-Tick Drift v1

**Project:** Ember CRPG (Unity 6 / URP)
**Audit item:** E7-019 — Faction politics (decay). Counter status `[~]` in `docs/REMEDIATION_V2_COUNTER.md` (line 86): *"Deterministic faction-reputation decay is a design-sensitive tick addition — staged with the digest work."*
**Layer:** Domain + Simulation (engine-free, deterministic). No Presentation, no Unity, no I/O.
**Author:** Claude session 2026-05-31 (E7 backend track)
**Date:** 2026-05-31
**Status:** Draft — implementation contract. **This document is a design + plan only; it ships no code.**

> **Premise.** `WorldTickComposer` already orchestrates the deterministic per-tick simulation and its own class summary closes with the open thread: *"Faction reputation decay remains unwired pending its own tick-contract review."* This PRD **is** that tick-contract review. It specifies a faction-reputation decay system that drifts every faction-pair standing toward a neutral baseline at a configurable rate, runs inside the existing tick cadence, uses integer-only math, and preserves same-seed reproducibility and save/load replay-equivalence.

---

## 1. Purpose

Faction standings today only move when something *explicitly* calls `FactionReputationSystem.ApplyDelta` (trade completed, crime observed, etc.). Between events, a -90 grudge or a +80 alliance is frozen forever. That contradicts the living-world model: with no fresh provocations, hard feelings and warm alliances should both **erode toward neutral** over game-time. This PRD adds that erosion as a deterministic tick system.

The decay is intentionally a *forgetting* force, not a politics simulator. It does not create new conflicts or alliances; it only relaxes existing standings toward a baseline when nothing else touches them. Active deltas always dominate decay (see §4.6 ordering).

This is a **product-visible increment** under the live remediation workflow: it adds a system that, when ticked, emits a new kind of `WorldEventLog` line (`FactionReputationChanged` with a decay reason code) that the Unity debug overlay already renders, and it extends behaviour the existing `FactionRelationSnapshot` HUD reader surfaces with zero read-side changes.

---

## 2. Scope

**In scope**

- A new deterministic Simulation system `FactionReputationDecaySystem` (engine-free, integer-only).
- A small immutable Domain config value object `FactionDecayConfig` (baseline, rate, cadence, dead-band).
- A per-faction-pair decay step that moves each `FactionReputation` toward a baseline (default `0`, i.e. `FactionReputation.Neutral`).
- Wiring into `WorldTickComposer` on the **daily** cadence band, after the existing daily systems, at a deterministic stamp.
- Event emission reusing `WorldEventKind.FactionReputationChanged` with a reserved decay reason code.
- Acceptance tests: same-seed reproducibility over N ticks, designed convergence, ordering vs. active deltas, save/load replay-equivalence.

**Out of scope (explicit non-goals)**

- New `WorldEventKind` enum members. Decay reuses `FactionReputationChanged` (rule 2 — no speculative additions). See §6.
- Save-schema changes. Reputation rows already round-trip (`factionReputations` array). See §7.
- Per-pair *authored* baselines/rates as data rows (e.g. two factions whose natural resting state is hostile). v1 ships a single global config; per-pair override tables are a v2 follow-up (§11).
- Faction *relationship generation* (new wars/alliances forming), caravan/AI politics, player-faction standing. Decay only relaxes existing pair rows.
- Any floating-point or `System.Random` usage anywhere in the path (hard determinism rule, §5).

---

## 3. Current system (as-built, surveyed 2026-05-31)

Files read for this PRD (all absolute under `C:/Users/msbel/projects/alcyone-ember-rpg/`):

| Concern | File |
|---|---|
| Reputation scalar (value object, **already has `Decay(int)`**) | `Assets/Scripts/Domain/World/FactionReputation.cs` |
| Reputation storage (symmetric pair dict + canonical `ReputationRows`) | `Assets/Scripts/Domain/World/FactionStore.cs` |
| Relation tiering (rep → allied/friendly/neutral/hostile/war) | `Assets/Scripts/Domain/World/FactionRelationKind.cs` |
| Faction handle / record | `Assets/Scripts/Domain/Core/FactionId.cs`, `Assets/Scripts/Domain/World/FactionRecord.cs` |
| Existing active-delta mutation path | `Assets/Scripts/Simulation/World/FactionReputationSystem.cs` |
| Deterministic tick orchestrator | `Assets/Scripts/Simulation/Composition/WorldTickComposer.cs` |
| World aggregate (holds `Factions`, `CopyFrom`, `EnsureInvariants`) | `Assets/Scripts/Domain/World/WorldState.cs` |
| Time (pure `long` minutes) | `Assets/Scripts/Domain/Core/GameTime.cs` |
| Event payload / kind / log | `Assets/Scripts/Domain/World/WorldEvent.cs`, `WorldEventKind.cs`, `WorldEventLog.cs` |
| World seeding (3 factions, 3 rep rows) | `Assets/Scripts/Simulation/World/WorldFactory.cs` |
| Save round-trip for reputation | `Assets/Scripts/Data/Save/SliceJson/WorldSaveMapper.World.cs`, `Assets/Scripts/Data/Save/WorldSaveData.cs` |
| HUD read-side (auto-reflects decay) | `Assets/Scripts/Presentation/Visual/FactionRelationSnapshot.cs` |
| Determinism acceptance harness (template to mirror) | `Assets/Tests/EditMode/Composition/WorldLivesOverNTicksTests.cs`, `WorldTickComposerReplayTests.cs` |

Key facts the design leans on:

1. **A decay primitive already exists.** `FactionReputation.Decay(int amount)` (FactionReputation.cs:38–47) moves toward `0`: positive values subtract toward `0` (floored at `0`), negative add toward `0` (ceiled at `0`), `0` is a fixed point. It is integer-only, throws on negative `amount`, and never overshoots the baseline. **The decay system is a scheduler around this method — it adds no new arithmetic to the value object.**
2. **Storage is symmetric and canonically ordered.** `FactionStore` keys reputation by an unordered `FactionPair` and exposes `ReputationRows` *sorted by `(Low.Value, High.Value)`* (FactionStore.cs:157–166). Iterating `ReputationRows` is the deterministic enumeration the decay loop will use. `GetReputation`/`WithReputation` are the read/write surface.
3. **The composer has three cadence bands.** per-tick (time, magic), hourly (`TicksPerGameHour = 10`), daily (`TicksPerGameDay = 240`). Each band computes the number of boundary crossings up front, then iterates `for i in 1..crossings` stamping each crossing at its **exact minute** (WorldTickComposer.cs:229–276). Save/load replay-equivalence is guaranteed by `RebuildAccumulatorsFrom(world.Time)` (cs:396–402), proven by `WorldTickComposerReplayTests`.
4. **The composer already declares this work pending** (WorldTickComposer.cs:39–40) — this PRD discharges that comment.
5. **Save already persists reputation.** `WorldSaveData.factionReputations` (WorldSaveData.cs:50) ⇄ `ReputationRows` via `ToFactionReputationData` / `ApplyFactionReputations` (WorldSaveMapper.World.cs:147–170). Decayed values are just smaller integers in the same rows — **no schema change**.

---

## 4. Decay model

### 4.1 Chosen model: stepwise linear drift toward a baseline, on a fixed cadence, with a dead-band

For each faction pair `(a, b)` with current reputation `r`:

```
toward 0 by a fixed integer step each decay cadence, never crossing the baseline,
and never moving a pair already inside a dead-band around the baseline.
```

This is **deliberately the simplest model that satisfies the design intent and the determinism rule**. It reuses `FactionReputation.Decay`, which already encodes "move toward 0, clamp at 0, never overshoot." Alternatives considered and rejected for v1 in §4.7.

### 4.2 Baseline

- Baseline is `FactionReputation.Neutral` (`0`) for v1. This matches the existing `Decay(int)` semantics and the symmetric Neutral band (`-25..+24`) in `FactionRelationKind`.
- The config carries the baseline as data so a v2 per-pair table can override it without touching the system. v1 always passes `0`.

### 4.3 Decay rate (per cadence step)

- `RatePerStep` is a **non-negative integer** number of reputation points removed *toward the baseline* per decay cadence step. Default: `1`.
- Magnitude only ever shrinks the distance to baseline. A pair at `+12` after one step is `+11`; at `-8` it becomes `-7`; at `0` it stays `0`.
- `RatePerStep = 0` disables decay entirely (system becomes a no-op) — useful as a kill-switch and for tests that must freeze standings.

### 4.4 Dead-band (anti-jitter / "close enough is neutral")

- `DeadBand` is a non-negative integer. Any pair whose absolute reputation is `<= DeadBand` is treated as already at rest and is **not** stepped (and emits no event). Default: `0` (no dead-band; only exact-`0` pairs are at rest).
- Rationale: a dead-band lets design declare "anything within ±N is effectively neutral, stop nudging it," which both prevents perpetual 1-point event spam near the baseline and gives a cheap convergence guarantee. With `DeadBand = 0` the behaviour is pure drift-to-exactly-zero.
- The dead-band check uses `Math.Abs(r.Value) <= DeadBand` on the integer value — no float distance.

### 4.5 Cadence (how often a decay step happens)

- Decay runs on the composer's **daily** band (`TicksPerGameDay = 240` ticks = 1 game-day), once per day-boundary crossing. Default `DaysPerDecayStep = 1` (decay every game-day).
- `DaysPerDecayStep` (positive integer, default `1`) lets design slow decay to "every K game-days" without a new cadence band: the system maintains an internal day-counter and only applies a step when `dayCounter % DaysPerDecayStep == 0`. The counter is derived deterministically from absolute game-time at the stamp (see §5.4) so it survives save/load without extra serialized state.
- **Why daily, not hourly/per-tick:** reputation is a slow social quantity; hourly decay would relax a -90 grudge to neutral in under four game-days at rate 1, which is too fast and floods the event log. Daily matches the cadence of the other "slow world" systems (crops, caravans, prices) already on that band.

### 4.6 Per-step algorithm (deterministic, integer-only)

For one decay step at deterministic stamp `now`:

```
for each row in factions.ReputationRows:            // canonical (Low.Value, High.Value) order
    r = row.Reputation
    if |r.Value| <= config.DeadBand:    continue     // at rest → skip, no event
    if config.RatePerStep == 0:         continue     // disabled
    next = r.Decay(config.RatePerStep)               // integer move toward 0, floored/ceiled at 0
    if next == r:                        continue     // no change → no event (defensive; should not happen if not in dead-band)
    factions.WithReputation(row.A, row.B, next)
    events.Append(FactionReputationChanged @ now, reason = "faction_decay a:.. b:.. from:.. to:.. reason:decay")
```

**Snapshot-before-mutate.** `ReputationRows` is a `LINQ` projection over the live dictionary; mutating the dict via `WithReputation` while enumerating that lazy sequence is undefined. The system **materializes the rows into an ordered list/array first** (`ReputationRows.ToList()`), then iterates the snapshot and writes back. This is mandatory for correctness and determinism. (Note: `WithReputation` replaces an existing key's value and does not add/remove keys during a step, but the snapshot rule is kept as a hard invariant so future edits cannot regress it.)

**Ordering vs. active deltas (the design-sensitive part).** Within a single `WorldTickComposer.Advance` call, decay runs **after** all event-driven systems for that span have run (decay is the *last* daily sub-step; active deltas come from trade/crime hooks that fire on their own cadence earlier in the same advance). The rule the tests pin: **a same-day active delta and a same-day decay step compose as `decay(apply(r, delta))`** — i.e. the explicit change lands first, then decay relaxes the result by one step. This keeps decay from ever *masking* a fresh provocation (the provocation is fully applied; decay only shaves one step off the new magnitude), and it is order-stable because both operations are deterministic integer functions of the snapshot.

### 4.7 Alternatives considered (rejected for v1)

- **Proportional / exponential decay** (`r -= r / K`): naturally eases and never overshoots, but integer division truncates asymmetrically (`-7 / 4 == -1` vs `7 / 4 == 1` is symmetric, but `-1/4 == 0` stalls small negatives differently than rounding would), making "converges to exactly baseline" awkward and the math harder to reason about in tests. Linear step + dead-band gives a clean, provable convergence in `ceil((|r0| - DeadBand) / RatePerStep)` steps. Deferred to v2 if designers want easing.
- **Float decay with rounding:** banned outright by §5.
- **Per-tick or hourly decay:** too fast for a social quantity and event-spammy (§4.5).
- **Decay as a delta through `FactionReputationSystem.ApplyDelta`:** rejected because `ApplyDelta` takes a *signed* delta and would need the system to compute sign/magnitude toward baseline per pair, re-implementing what `FactionReputation.Decay` already does correctly; and its reason string / no-op guards differ. The decay system writes through `WithReputation` directly and emits its own decay-reason event, mirroring `ApplyDelta`'s event shape. (`ApplyDelta` remains the path for *active* deltas.)

### 4.8 Worked example

Config: `baseline=0, RatePerStep=1, DeadBand=0, DaysPerDecayStep=1`. Seed rows from `WorldFactory`: forge↔harbor `+12`, forge↔watch `+4`, harbor↔watch `+8`.

| Game-day | forge↔harbor | forge↔watch | harbor↔watch |
|---|---|---|---|
| 0 (seed) | 12 | 4 | 8 |
| 1 | 11 | 3 | 7 |
| 4 | 8 | 0 ✓ at rest | 4 |
| 8 | 4 | 0 | 0 ✓ |
| 12 | 0 ✓ | 0 | 0 |

All three pairs converge to baseline and stop (no further events). With `DeadBand=2`, forge↔watch would stop at `2` on day 2 and harbor↔watch at `2` on day 6, etc.

---

## 5. Determinism contract (hard rules)

Domain + Simulation must stay engine-free and reproducible: **same seed + same tick sequence ⇒ byte-identical world**, on any machine, across save/load. The decay path obeys:

### 5.1 No floats, ever
All quantities (`RatePerStep`, `DeadBand`, `DaysPerDecayStep`, baseline, reputation) are `int`/`long`. `FactionReputation` is already integer-backed with saturating arithmetic (FactionReputation.cs:32–35 promotes to `long` before clamping to avoid overflow). No `float`/`double`/`decimal` appears in the system, config, events, or save mapping.

### 5.2 No `System.Random` / `UnityEngine.Random`
Decay is fully determined by current standings + config + game-time. No randomness, no jitter, no probability. (If designers later want *stochastic* forgetting, it must use the project's seeded deterministic PRNG threaded from world seed — explicitly a v2 item, not v1.)

### 5.3 Deterministic iteration order
The system iterates the **canonical** `ReputationRows` order (sorted `(Low.Value, High.Value)`), materialized to a list before mutation. It never iterates a raw `Dictionary` enumeration. Event-append order therefore equals row order every run. (This mirrors the Codex-audited fix in FactionStore.cs:149–156 that made `ReputationRows` order-stable precisely so snapshot consumers get a canonical layout.)

### 5.4 Save/load replay-equivalence (no new serialized state)
The only mutable state the system *could* carry is the `DaysPerDecayStep` counter. To stay replay-equivalent under a cold load (fresh process, in-memory counters at 0) **without** adding a save field, the counter is **derived from absolute game-time at the stamp**, not accumulated in a field:

```
gameDayIndex = stamp.TotalMinutes / GameTime.MinutesPerDay      // integer division, long
applyThisStep = (gameDayIndex % DaysPerDecayStep) == 0
```

`stamp` is the exact day-boundary `GameTime` the composer already computes for the daily band (WorldTickComposer.cs:265–268). Because it is reconstructed from `world.Time` (which *is* saved), a save at day 7 + reload reproduces the identical decay schedule as a continuous run — same proof shape as `WorldTickComposerReplayTests`. **The decay system is stateless** (holds only its immutable `FactionDecayConfig`), which sidesteps the entire "accumulator desync on load" class of bug the composer fought in DET-01.

### 5.5 Catch-up correctness
The composer's daily loop runs once **per crossed day-boundary** when `Advance` jumps multiple days (e.g. after a long fast-travel). Decay is invoked inside that loop at each boundary's stamp, so jumping 5 days applies 5 decay steps at 5 distinct stamps — identical to ticking day-by-day. The per-boundary stamp + game-time-derived counter make multi-day catch-up exactly equal to single-day stepping (acceptance test §8.4).

### 5.6 Bounds safety
`FactionReputation` clamps to `[-100, +100]` and `Decay` floors/ceils at `0`; the system cannot push a value out of range or past baseline. `Decay` throws on negative `amount`, so the config validates `RatePerStep >= 0` at construction and the system never passes a negative.

---

## 6. Where it hooks into `WorldTickComposer` (and the order)

### 6.1 Cadence band & position

Decay is a **new daily sub-step**, added as the **last** operation inside the existing daily crossing loop (`WorldTickComposer.cs:262–276`), after caravans, plant growth, and price recompute:

```
daily crossing i (stamp = exact day-boundary GameTime):
    1. CaravanSystem.Tick(...)            // existing
    2. AdvancePlantGrowth(world, stamp)   // existing
    3. RecomputePrices(world, stamp)      // existing
    4. DecayFactionReputation(world, stamp)   // NEW — last
```

`DecayFactionReputation(world, stamp)` is a private composer method (mirroring `AdvancePlantGrowth` / `RecomputePrices`) that:
- guards `world.Factions == null || world.Events == null` → return (graceful, like the other daily helpers);
- resolves "apply this step?" via §5.4;
- delegates to `_factionDecay.Apply(world.Factions, _decayConfig, stamp, world.Events)`.

### 6.2 Why daily / why last

- **Daily**: §4.5 — social quantity, matches the other slow systems, avoids event spam.
- **Last**: §4.6 — guarantees `decay(apply(r, delta))` composition for the day, so a fresh active delta is never masked. (Active deltas today are not yet driven from inside the composer; when trade/crime hooks are composer-driven they must run before this sub-step. v1 documents the contract; the test in §8.3 pins it by applying a delta to the same pair on the same day and asserting decay shaved exactly one step off the post-delta value.)

### 6.3 Construction / injection (back-compat preserving)

`WorldTickComposer` is constructed several ways (parameterless, back-compat 4-arg, canonical 8-arg, 1-arg calendar). To avoid breaking the ~existing call sites:

- Add two private fields: `readonly FactionReputationDecaySystem _factionDecay;` and `readonly FactionDecayConfig _decayConfig;`.
- Default them in the field initializer / parameterless path to `new FactionReputationDecaySystem()` and `FactionDecayConfig.Default` (the §4 defaults: baseline `0`, rate `1`, dead-band `0`, `DaysPerDecayStep` `1`).
- Optionally add **one** new canonical constructor overload that accepts a `FactionDecayConfig` (and/or the system) for tests/tuning; all existing constructors chain to it with the default config. No existing constructor signature changes. (This honors the project's pattern of additive constructor overloads with defaulted new systems, exactly as SOUL-01 did for plant/job/price/schedule.)

### 6.4 No change to anchor/accumulator logic

Because decay is stateless and time-derived (§5.4), `ResetAnchor()` and `RebuildAccumulatorsFrom(...)` need **no changes** — there is no decay accumulator to preserve or rebuild. This is a deliberate design choice to keep the save/load story trivial.

---

## 7. Data read / written + serialization impact

**Reads**

- `world.Factions.ReputationRows` (materialized snapshot) — the pairs and current values.
- `world.Time` (via the composer-computed daily `stamp`) — for the §5.4 day index.
- `FactionDecayConfig` — immutable, composer-held.

**Writes**

- `world.Factions.WithReputation(a, b, decayed)` for each pair that moves.
- `world.Events.Append(...)` one `FactionReputationChanged` event per moved pair.

**Serialization impact: NONE (no schema change).**

- Reputation already serializes via `WorldSaveData.factionReputations` ⇄ `FactionStore.ReputationRows` (`WorldSaveMapper.World.cs:147–170`). Decayed standings are just smaller integers in the same rows; a save written after decay restores to the decayed values automatically.
- The decay system holds **no serialized state** (§5.4). `WorldState.CopyFrom` / `EnsureInvariants` already cover `Factions`; nothing new to add to either. `WorldStateCopyFromTests` (the reflection guard) is unaffected.
- `FactionDecayConfig` is **engine config**, not save state — it is constructed at composer build time, never written to a save. Two saves made with different configs are still valid; the config is supplied by the runtime that loads them. (If per-pair authored baselines arrive in v2, they belong in `Assets/Manifests/` data rows + worldgen seeding, not in the save blob.)

**Event shape (reused kind, new reason code)**

- Kind: `WorldEventKind.FactionReputationChanged` (= 15). **No new enum member** (rule 2).
- Reason string: mirror `FactionReputationSystem.BuildReason` exactly but with reason code `decay`, e.g. `faction_reputation a:FactionId(1) b:FactionId(2) from:12 to:11 reason:decay`. The reserved code `decay` is what tests and the debug overlay filter on to distinguish drift from active deltas.
- Subject encoding: same convention as the active path — encode faction-A's value as the `SiteId` sentinel (`new SiteId(a.Value)`) so the `WorldEvent` "at least one of actorId/siteId non-empty" invariant holds without inventing a fake actor (`FactionReputationSystem.cs:38–47`).

**Read-side (HUD) impact: zero code.** `FactionRelationSnapshot.FromStore` reads `GetReputation` live, so decayed values and any tier transitions (e.g. `friendly → neutral`) surface automatically on the next snapshot. No Presentation change is in scope.

---

## 8. Acceptance tests

New EditMode tests under `Assets/Tests/EditMode/World/FactionReputationDecaySystemTests.cs` (unit) and additions/companions under `Assets/Tests/EditMode/Composition/` mirroring `WorldLivesOverNTicksTests` / `WorldTickComposerReplayTests` (integration). All are pure C#, NUnit, no Unity, no scene. Each builds a fixed seeded world so "same seed → same asserts" holds.

### 8.1 Unit — drift toward baseline & convergence
- A pair at `+12` with `rate=1, deadBand=0` becomes `+11` after one `Apply`, `0` after 12 applies, and **stays `0`** (idempotent at rest; no further events).
- A pair at `-8` ceils up to `0` over 8 applies; never overshoots to positive.
- A pair at `0` produces no change and no event.
- `rate=0` ⇒ no change, no event (kill-switch).
- `deadBand=3` ⇒ a pair at `+3`/`-3` is at rest immediately (no event); a pair at `+10` stops at `+3`.
- Event assertions: one `FactionReputationChanged` per moved pair, reason contains `reason:decay`, `from:`/`to:` reflect the integer step.
- Determinism: two independent `Apply` runs over the same seeded store produce identical `ReputationRows` and identical event reason sequences (string-join snapshot equality, like `WorldLivesOverNTicksTests.RunAndSnapshot`).

### 8.2 Integration — standings converge over N ticks through the real composer
- Build a world (via `WorldFactory().Create(seed)` or a minimal seeded `WorldState` with 2–3 pairs at known non-zero values).
- Tick the **real** `WorldTickComposer` over `K` game-days (e.g. `K = 15`, i.e. `15 * 240` ticks) day-by-day.
- Assert every seeded pair has moved strictly toward `0` and the high-magnitude pairs have reached `0` on the day predicted by `ceil((|r0| - deadBand)/rate)` (§4.8 table values).
- Assert at least one `FactionReputationChanged{reason:decay}` line exists in `world.Events` (product-visible-increment proof).

### 8.3 Integration — ordering vs. active delta (the design-sensitive pin)
- On a single game-day, apply an active delta to pair `(a,b)` via `FactionReputationSystem.ApplyDelta` (e.g. `+30` on a pair at `+10` → `+40`), then let the composer's daily decay run for that same day.
- Assert the end value is `+39` (= `decay(apply(10, +30)) = decay(40) = 39` at rate 1), proving decay composes **after** the active delta and never masks it.

### 8.4 Integration — multi-day catch-up equals day-by-day
- Run A: tick day-by-day for `K` days.
- Run B: from the same seed, a single `Advance` jump across `K` days.
- Assert both produce identical `ReputationRows` (decay applied exactly `K`/`DaysPerDecayStep` times in both), mirroring the cadence-boundary proof in `WorldTickComposer`.

### 8.5 Integration — save/load replay-equivalence (DET-01 shape)
- Continuous run to day `N` → snapshot reputation rows + decay event stamps.
- Cold-load run: tick to a mid-day save tick, serialize via the existing save mapper, deserialize into a fresh world + **fresh composer** (`RebuildAccumulatorsFrom(world.Time)`), tick to day `N`.
- Assert the cold-loaded reputation rows and decay-event stamps equal the continuous run — proves §5.4 (game-time-derived counter) survives save/load with no extra serialized state.

### 8.6 Determinism — whole-world snapshot stability
- Extend (or add a sibling to) `WorldLivesOverNTicksTests.Advance_IsDeterministic_SameSeedSameOutcome` so the string snapshot also includes a few decayed reputation values, guaranteeing the new system did not introduce a same-seed divergence.

---

## 9. Implementation plan (PR-sized steps)

Each step compiles green and keeps the fallback harness green. Steps 1–3 are pure Domain/Simulation; step 4 is the visible wire.

1. **Domain config value object** — `Assets/Scripts/Domain/World/FactionDecayConfig.cs`: immutable readonly struct with `Baseline` (int, default 0), `RatePerStep` (int ≥ 0, default 1), `DeadBand` (int ≥ 0, default 0), `DaysPerDecayStep` (int ≥ 1, default 1), a validating constructor, and a `static Default`. Engine-free, no Unity. Unit-tested for validation throws.
2. **Simulation system** — `Assets/Scripts/Simulation/World/FactionReputationDecaySystem.cs`: stateless `Apply(FactionStore, FactionDecayConfig, GameTime stamp, WorldEventLog)` implementing §4.6 (snapshot rows → per-pair `Decay` → write-back → event). Mirror `FactionReputationSystem`'s null-guards, reason-string builder (`reason:decay`), and `SiteId` subject encoding. Unit tests §8.1.
3. **Decay-only integration tests** — `Assets/Tests/EditMode/World/FactionReputationDecaySystemTests.cs` (§8.1) — full coverage of the system before it touches the composer.
4. **Compose into the tick (visible increment)** — edit `WorldTickComposer`: add the two fields (defaulted), one optional config-accepting constructor overload (all existing ctors chain with `FactionDecayConfig.Default`), the private `DecayFactionReputation(world, stamp)` daily sub-step (last in the daily loop), and the §5.4 day-index gate. Update the class-summary comment that currently says decay "remains unwired." Integration + replay tests §8.2–8.6.

Out-of-band housekeeping (per repo conventions): generate `.meta` files for the two new `.cs` files; run the fallback validation harness after step 4; update `docs/REMEDIATION_V2_COUNTER.md` E7-019 from `[~]` to done with a one-line proof pointer to the new tests.

---

## 10. Risks & determinism pitfalls (call-outs)

| Risk | Mitigation |
|---|---|
| **Mutating `ReputationRows` while enumerating** the lazy LINQ projection → undefined order / runtime error. | Materialize to an ordered list snapshot **before** any `WithReputation` write (§4.6, hard invariant; unit-tested by asserting stable order under multi-pair decay). |
| **Save/load decay-schedule desync** (the DET-01 class of bug). | System is **stateless**; the `DaysPerDecayStep` gate is derived from `world.Time` at the stamp, not from an in-memory accumulator (§5.4). Replay test §8.5 guards it. |
| **Decay masking a fresh active delta** (gameplay feel + determinism of compose order). | Decay is the **last** daily sub-step → `decay(apply(r, delta))`. Pinned by §8.3. |
| **Event-log spam** near baseline (perpetual ±1 nudges). | Dead-band skips at-rest pairs and exact convergence stops at `0`; once a pair is within `DeadBand` it emits nothing further (§4.4). |
| **Float creep** if a future "easing" model is bolted on. | §5.1 bans floats; §4.7 records the integer-only easing alternative as an explicit v2 with its own determinism review. |
| **Cadence too fast** relaxing standings unrealistically. | Daily band + `DaysPerDecayStep` knob; defaults tuned so a max grudge takes ~100 game-days at rate 1 (§4.5). |
| **Multi-day catch-up off-by-one** (long fast-travel). | Per-boundary stamp + time-derived counter make K-day jump == K single steps; pinned by §8.4. |
| **Breaking existing composer call sites** with a new ctor. | All new fields defaulted; existing constructors chain to a new overload with `FactionDecayConfig.Default`; no existing signature changes (§6.3). |
| **Overflow on extreme values.** | Out of reach in practice (rep is clamped to ±100), and `FactionReputation.Apply`/`Decay` already promote to `long`/saturate (FactionReputation.cs:32–35). |

---

## 11. Future work (v2+, out of scope here)

- **Per-pair / per-faction authored baselines & rates** as `Assets/Manifests/` data rows, seeded by worldgen — for factions whose natural resting state is *not* neutral (e.g. ancestral enemies that drift toward `-40`, not `0`). The v1 `FactionDecayConfig` already carries a `Baseline` field so the system needs no rewrite, only a per-pair lookup.
- **Easing / proportional decay** (integer rounding-aware) behind a config flag, with its own determinism proof (§4.7).
- **Stochastic forgetting** using the project's seeded deterministic PRNG threaded from world seed (never `System.Random`).
- **Tier-transition events** (a dedicated reason code or `ReasonTrace` when decay crosses a `FactionRelationKind` boundary, e.g. `friendly → neutral`) for richer DM/narrative hooks.

---

## 12. Acceptance gate (definition of done)

- `FactionDecayConfig` (Domain) + `FactionReputationDecaySystem` (Simulation) exist, engine-free, integer-only, no `Random`.
- Decay runs as the last daily sub-step in `WorldTickComposer`; the "remains unwired" comment is removed.
- All §8 tests green: unit drift/convergence, integration convergence over N ticks, ordering vs. active delta, multi-day catch-up, save/load replay-equivalence, whole-world same-seed snapshot.
- No save-schema change; `WorldStateCopyFromTests` still green.
- Fallback validation harness green; `docs/REMEDIATION_V2_COUNTER.md` E7-019 closed with a proof pointer.

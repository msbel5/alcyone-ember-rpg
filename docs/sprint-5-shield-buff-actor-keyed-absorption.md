# Sprint 5 Shield Buff Actor-Keyed Absorption

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-actor-keyed-absorption`
Base: `35362de` — Sprint 5 deterministic shield buff damage absorption seam merged on
`origin/main` (PR #35).

## Scope

The previous slice (PR #35) pinned the deterministic per-state damage
absorption seam: `ShieldBuffService.AbsorbDamage(state, incomingDamage)`
consumes magnitude across the active buffs of one `ShieldBuffState` bag
in ascending ordinal-string order of spell template id. That slice
explicitly listed the actor-keyed absorption sweep as a follow-up
("`AbsorbDamageForActor(registry, actorId, damage)`"). This slice adds
that follow-up.

This slice is the actor-keyed dispatcher only. It does not change the
per-state consume rule, does not change tick-down, does not touch
save/load, does not change application paths, and does not call into a
combat damage pipeline. It is a pure registry lookup that delegates to
the existing single-bag `AbsorbDamage` once the actor's bag has been
located.

Implemented:

- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs` — adds
  `AbsorbDamageForActor(ShieldBuffStateRegistry registry, string actorId,
  int incomingDamage)`:
  - null `registry` → `ArgumentNullException`.
  - null / whitespace `actorId` → `ArgumentException` (mirrors
    `ShieldBuffStateRegistry.GetOrCreate`).
  - negative `incomingDamage` → `ArgumentOutOfRangeException`.
  - zero `incomingDamage` → returns an empty trace
    (`Incoming/Absorbed/Remaining = 0`) and does **not** mutate the
    registry, in particular does not lazily create a bag for the
    actor.
  - actor not tracked in the registry → returns `RemainingDamage`
    equal to `IncomingDamage` and an empty trace, and does **not** add
    the actor to the registry. This mirrors how
    `AdvanceTicksForAllActors` is read-only over the registry.
  - actor tracked → delegates to the existing single-bag
    `AbsorbDamage(state, incomingDamage)` and returns its
    `ShieldBuffAbsorptionResult` unchanged. No new field, no new
    code path inside the per-buff consume loop.

- `Assets/Tests/EditMode/Magic/ShieldBuffServiceRegistryAbsorptionTests.cs`
  — 17 EditMode tests pinning:
  - argument guards: null registry, null actor id, whitespace actor
    id, negative damage.
  - zero damage on a tracked actor returns empty trace and preserves
    the bag's magnitude and remaining ticks.
  - zero damage on an untracked actor does not add the actor to the
    registry.
  - untracked actor with positive damage returns
    `RemainingDamage == IncomingDamage`, empty trace, and the
    registry remains empty.
  - tracked actor partial / exact / over consume mirrors the
    single-bag contract (magnitude reduction, ticks preserved on
    partial, buff cleared on magnitude exhaust even with ticks left,
    leftover damage returned on over-consume).
  - tracked actor multi-buff deterministic ordinal ordering:
    `aegis_pulse` is consumed before `ember_ward`.
  - target-actor isolation: only the named actor's bag is mutated;
    other tracked actors' magnitude and ticks are untouched.
  - repeated calls accumulate magnitude reduction without changing
    remaining ticks.
  - cross-seam: after `AdvanceTicksForAllActors` expires an actor's
    only buff, `AbsorbDamageForActor` returns full remaining and an
    empty trace.
  - tracked actor with an empty bag (created via `GetOrCreate` but
    no `SetActiveBuff` call) returns full remaining and an empty
    trace.
  - parity test: two parallel setups (one routed through the
    registry overload, one calling `AbsorbDamage` directly) produce
    the same result and the same end-state for magnitude and ticks.

## Why this slice matters

Future combat-pipeline integration will damage actors one at a time —
each enemy attack lands on a single target. The combat code should
not have to decide whether the target has a shield bag, fetch one out
of the registry, or branch on "tracked / untracked". This slice gives
the combat side a single entry point: pass the registry, the target
actor id, and the raw incoming damage; get back a
`ShieldBuffAbsorptionResult` that already encodes whether anything was
absorbed, what the remaining damage to apply to `Health` is, and which
buffs were touched or cleared. The single-bag `AbsorbDamage` remains
the source of truth for the per-buff consume rule; this slice only
adds the dispatch in front of it.

The registry-read-only invariant (do not lazily create a bag for an
unknown actor) keeps "untracked actor took damage" cheap and
side-effect-free, which is what the future combat loop wants for the
overwhelmingly common case where most damage events do not involve
shielded targets.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3 Layer 3: pure Simulation seam, no
  Unity types, no presentation coupling, no save coupling.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: timed magic
  effects resolve deterministically; this slice is the actor-keyed
  dispatcher in front of the existing per-state consume rule and
  reuses the established result object (`ShieldBuffAbsorptionResult`)
  without changing its contract.
- PRD Sprint 1 FR-06: deterministic save round-trip is preserved;
  this slice does not touch save state and does not introduce new
  serialized fields. The buff trace is per-call output only.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Latest measured result for this increment (2026-05-05):

- `git diff --check`: clean (exit 0).
- Fallback harness: `Passed: 342, Failed: 0, Skipped: 0,
  Total: 342, Duration: <see commit>`.

This is the prior 325-test baseline plus the 17 new
`ShieldBuffServiceRegistryAbsorptionTests` cases.

## Caveats

- Combat damage pipeline integration (routing real incoming damage
  through `AbsorbDamageForActor` before the remainder reaches `Health`
  on the target actor) is still a separate slice. This slice does not
  introduce a damage-source filter, magic-school resistance, or
  partial-pierce rule.
- A multi-actor bulk variant (e.g. `AbsorbDamageForActors(registry,
  IEnumerable<(actorId, damage)>)`) is intentionally NOT introduced
  here — combat applies damage one target at a time, and a bulk
  variant would invite a fan-out shape with no current consumer.
- Unity Editor validation is still blocked on this Pi because the
  Unity editor binary is not installed; the measured gate here is the
  pure C# fallback harness.

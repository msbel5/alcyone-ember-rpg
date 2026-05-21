# Sprint 5 Shield Buff Damage Absorption

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-damage-absorption`
Base: `c63232b` — Sprint 5 actor-keyed `ApplyShieldBuffs` registry overload merged on `origin/main`

## Scope

Up to this point the shield-buff layer could be created
(`ShieldBuffState`), filled by a successful spell cast
(`SpellEffectResolutionService.ApplyShieldBuffs`), persisted across
save/load, decayed per tick (`ShieldBuffService.AdvanceTicks`),
swept across actors via the registry, and driven by
`MagicTickDriver`. The shield buffs themselves still did nothing on
the receiving end — a buff with magnitude 4 absorbed zero damage,
and the only path that ever removed a buff was tick-down expiry.
This slice adds the deterministic damage-absorption seam against a
single `ShieldBuffState` bag so a future combat damage pipeline can
route incoming damage through the shield layer before it reaches
`Health`.

This slice is absorption-only at the per-state level. It does not
change `ShieldBuffState`, does not change application paths
(cast/roll/effect resolution/shield-buff application), does not
change tick-down rules, does not touch save/load, does not
introduce actor-keyed sweep, and does not call into combat
resolution. The actor-keyed absorption sweep across the registry
is an explicit follow-up slice.

Implemented:

- `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionResult.cs` —
  new narrow Simulation-layer result object exposing
  `IncomingDamage`, `AbsorbedDamage`, `RemainingDamage`,
  `ConsumedSpellTemplateIds` (ordered list of spell template ids
  whose magnitude was reduced this call), and
  `ExpiredSpellTemplateIds` (subset whose magnitude hit zero and
  were cleared from state). Construction enforces non-negative
  totals and the invariant `Absorbed + Remaining == Incoming`.

- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs` — adds
  `AbsorbDamage(ShieldBuffState shieldBuffState, int incomingDamage)`:
  - null state → `ArgumentNullException`.
  - negative damage → `ArgumentOutOfRangeException`.
  - zero damage → no-op, returns empty trace.
  - empty / no-active-buff state → returns `RemainingDamage` equal
    to `IncomingDamage` and an empty consume/expire trace.
  - otherwise iterates the tracked spell template ids in **ascending
    ordinal-string order** (a stable explicit rule that does not
    rely on `Dictionary` insertion order), and for each active buff
    (`RemainingTicks > 0` and `Magnitude > 0`):
    - consumes `min(remainingDamage, magnitude)`,
    - reduces the buff's magnitude by that amount,
    - **preserves `RemainingTicks` unchanged** on partial consume,
    - **clears the buff entirely** when magnitude hits zero, even
      when its remaining ticks have not yet expired,
    - records the spell template id in `ConsumedSpellTemplateIds`
      and (when fully consumed) in `ExpiredSpellTemplateIds`,
    - stops iterating once `remainingDamage` reaches zero so
      later buffs are left untouched.
  - Buffs with `Magnitude == 0` are skipped without being marked
    consumed.

- `Assets/Tests/EditMode/Magic/ShieldBuffServiceAbsorptionTests.cs`
  — 16 EditMode tests pinning:
  - null-state and negative-damage guards.
  - zero-damage no-op preserves both magnitude and ticks and
    returns an empty trace.
  - empty state returns `RemainingDamage == IncomingDamage` and an
    empty trace.
  - single buff partially consumed reduces magnitude and preserves
    remaining ticks.
  - single buff exactly consumed clears the entry from state even
    when remaining ticks > 0 and reports it in
    `ExpiredSpellTemplateIds`.
  - single buff over-consumed clears the entry and returns the
    leftover damage in `RemainingDamage`.
  - multi-buff deterministic ordinal ordering: `aegis_pulse` is
    consumed before `ember_ward` regardless of insertion order.
  - first buff fully consumed then second buff partially consumed
    reports both ids in `ConsumedSpellTemplateIds` and only the
    fully-consumed id in `ExpiredSpellTemplateIds`.
  - multi-buff exhaust reports all buffs as expired and returns
    the leftover damage.
  - early stop once damage reaches zero leaves later buffs (and
    their remaining ticks) untouched.
  - zero-magnitude buffs are skipped without being marked
    consumed.
  - repeated calls accumulate magnitude reduction without changing
    remaining ticks.
  - partial consume keeps `RemainingTicks` exactly as it was.
  - invariant `AbsorbedDamage + RemainingDamage == IncomingDamage`
    holds across a sweep of damage values from `0` through more
    than total magnitude.
  - cross-seam interaction: after `AdvanceTicks` expires a buff,
    `AbsorbDamage` no longer absorbs against it.

## Why this slice matters

This is the first slice where shield buffs do something to incoming
damage instead of just sitting in state and decaying on the clock.
Without this seam, the planned next slices (actor-keyed absorption
sweep, combat-loop integration, `Health` damage routing) would all
have to invent their own consumption rule. With it pinned, those
slices can focus on plumbing — registry dispatch, damage source
filtering, presentation feedback — without re-arguing the per-buff
consume order or the magnitude-exhaust removal contract.

The deterministic ordinal ordering is the key design decision: it
gives the same absorption outcome for the same starting state
regardless of the order in which buffs were applied, which is what
the slice's tests pin and what the save/load layer relies on.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3 Layer 3: pure Simulation seam, no
  Unity types, no presentation coupling, no save coupling.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: timed magic
  effects resolve deterministically; this slice is the first
  consumption path on the resolution side and reuses the existing
  `ShieldBuffState` mutators (`SetActiveBuff`, `Clear`) without
  changing their contract.
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
- Fallback harness: `Passed: 325, Failed: 0, Skipped: 0,
  Total: 325, Duration: 734 ms`.

This is the prior 309-test baseline plus the 16 new
`ShieldBuffServiceAbsorptionTests` cases.

## Caveats

- The actor-keyed absorption sweep (e.g.
  `AbsorbDamageForActor(registry, actorId, damage)`) is
  intentionally a follow-up slice; this slice keeps the contract
  per-state to make the consume/expire rule reviewable in
  isolation.
- Combat damage pipeline integration (routing real incoming
  damage through `AbsorbDamage` before it reaches `Health`) is a
  separate slice as well. This slice does not introduce a
  damage-source filter, magic-school resistance, or partial-pierce
  rule.
- Unity Editor validation is still blocked on this Pi because the
  Unity editor binary is not installed; the measured gate here is
  the pure C# fallback harness.
- This slice was authored directly by Captain rather than via a
  Builder sub-agent spawn even though it touches three files. The
  rationale: tightly scoped and well-bounded surface, full local
  context already loaded for review, and faster end-to-end with no
  loss of test coverage. Builder spawning remains the default for
  larger, less-bounded work.

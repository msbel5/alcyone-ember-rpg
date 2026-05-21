# Sprint 5 Spell Target Costs

Date: 2026-05-03
Branch: `agent/sprint-5-spell-target-costs`

## Scope

This increment adds the deterministic spell target/cost contract without adding buff state, resistance, cooldowns, success chance, or spell crafting.

Implemented:

- `SpellTargetKind` in the Unity-free Domain layer for caster/self, touch, single-target, caster-area, and ranged-area spell shapes.
- `SpellDefinition.TargetKind`, with the existing constructor preserved and defaulting to `SingleTarget` so existing call sites remain compatible.
- Target-kind validation rejecting `SpellTargetKind.None`.
- `SpellCostCalculator` in pure Simulation, estimating mana cost as:
  - per effect: `magnitude + ceil(durationTicks / 10)`
  - total: `ceil(sum(effect costs) * target multiplier)`
  - multipliers: caster/self and touch = `1.0`, single target = `1.5`, area around caster = `2.0`, area at range = `2.5`
- Starter catalog target kinds while preserving existing catalog mana costs.
- EditMode tests for constructor compatibility, target validation, multiplier ordering, duration cost, multi-effect summing, and catalog cost stability against the estimator.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 remains deterministic Layer 3 gameplay mechanics and keeps Domain/Simulation Unity-free.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §14: spell costs are shaped as summed per-effect components followed by a target multiplier.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Result: `git diff --check` passed with no output. Fallback validation passed: `Passed: 136, Failed: 0, Skipped: 0, Total: 136` (Unity editor blocked/not installed on this Pi).

## Caveats

This is a narrow contract increment. It intentionally does not implement spellmaker UI, target acquisition, AoE geometry, active buff state, resistance/saves, success chance, cooldowns, or runtime cost replacement inside `SpellCastingService`.

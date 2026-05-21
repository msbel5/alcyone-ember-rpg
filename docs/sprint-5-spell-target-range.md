# Sprint 5 Spell Target Range Enforcement

Date: 2026-05-04
Branch: `agent/sprint-5-spell-target-range`

## Scope

This increment adds deterministic range enforcement for `SingleTarget` spells inside the pure
`SpellTargetValidator`. The previous Sprint 5 target-routing increment deliberately deferred ranged
validation; this slice closes that gap without introducing line-of-sight, area geometry, success
chance, resistances, or cooldown state.

Implemented:

- `SpellDefinition.RangeInTiles` in the Unity-free Domain layer.
  - Existing constructors stay compatible and default to `0` (`unbounded at this layer`).
  - A new constructor overload persists explicit deterministic range for range-aware spells.
- `SpellTargetValidationError.TargetOutOfRange` for stable refusal reporting.
- `SpellTargetValidator` now enforces range for `SingleTarget` spells when `RangeInTiles > 0` using
  `GridPosition.ManhattanDistanceTo`.
- Starter catalog update: `Flame Bolt` now declares deterministic range `8` tiles.
- EditMode fallback coverage for:
  - constructor compatibility + range persistence
  - negative range rejection
  - catalog range contract
  - in-range single-target success
  - out-of-range single-target refusal
  - zero-range backward-compatible unbounded routing

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 remains Layer 3 deterministic gameplay mechanics; the
  new range contract stays in Domain/Simulation and introduces no `UnityEngine` dependency.
- `docs/EMBER_VISION_BIBLE.md` §8: Sprint 5 magic work is still incremental and honest about what
  is not implemented yet.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §14: target taxonomy and distance-shaped spell design
  justify explicit ranged validation before area resolution.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Result: `git diff --check` passed with no output. Fallback validation passed: `Passed: 157, Failed: 0, Skipped: 0, Total: 157` (Unity editor blocked/not installed on this Pi).

## Caveats

- Range uses Manhattan distance on the existing deterministic combat grid.
- Line-of-sight is still intentionally out of scope; a future increment should add obstruction-aware
  validation rather than overloading this range-only gate.
- `Touch` targeting remains stricter than range-based `SingleTarget`; it still requires exact
  orthogonal adjacency.

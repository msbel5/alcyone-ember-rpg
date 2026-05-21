# Sprint 5 Spell Success Roll

Date: 2026-05-04
Branch: `agent/sprint-5-spell-success-roll`
Base: Sprint 5 spell success chance increment on `origin/main`

## Scope

This increment adds the deterministic Tier 3 spell-cast roll seam described in
`docs/mechanics/ARCHITECTURE.md` §3.3. It pairs the existing Tier 2
`SpellSuccessChanceService` threshold with a seeded `IDeterministicRng` percentage roll and exposes
`roll`, `threshold`, and the full upstream chance breakdown for one cast attempt — without mutating
`SpellExecutionService`, mana, or any actor state yet.

Implemented:

- `SpellCastRollService` in pure `Simulation/Magic`, taking `ActorRecord caster`,
  `SpellDefinition spell`, and `IDeterministicRng rng`. It rejects null caster/spell/RNG with stable
  error codes, forwards upstream chance refusals, otherwise runs `rng.RollPercent()` against the
  computed chance threshold and returns a deterministic roll result.
- `SpellCastRollResult` carrying:
  - `Success` and stable `SpellCastRollError` outcome code
  - `Roll` (the d100 1..100 that came up) and `Threshold` (chance percent that needed to be met)
  - `Chance` — the full forwarded `SpellSuccessChanceResult` breakdown (base/primary/secondary
    bonuses, mana/effect/target/range penalties, clamped final chance)
  - `Message` for narration / DM transcript use
- `SpellCastRollError` with `None`, `InvalidCaster`, `InvalidSpell`, `InvalidRng`,
  `ChanceCalculationFailed`.
- Focused EditMode fallback tests in `SpellCastRollServiceTests` covering null inputs, incapacitated
  caster propagation, invalid-school propagation, breakdown forwarding equality with
  `SpellSuccessChanceService`, success and miss rolls against pinned XorShift seeds, same-seed
  determinism, different-seed divergence, and proof that the seam does not mutate caster mana.

## Why this slice matters

`docs/mechanics/ARCHITECTURE.md` §3.3 reserves Tier 3 seeded rolls (`RollResult { success,
rollValue, threshold, breakdown }`) as the only legitimate way the simulation moves from a
deterministic probability to an actual outcome. Sprint 5 already had the Tier 2 chance seam but no
Tier 3 spell roll. This slice fills that gap with a pure, no-side-effect service so future work can
plug in the roll wherever the spell pipeline (or DM) decides to commit an outcome — without having
to retrofit a seam later.

The existing `SpellExecutionService` is intentionally left untouched: wiring the roll into the
execution pipeline is a separate scope question (and would need design decisions about whether
fizzles still consume mana, whether reactions trigger, etc.).

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 is still Layer 3 deterministic gameplay mechanics.
- `docs/mechanics/ARCHITECTURE.md` §3.3: adds the missing `RollSpellCast`-style Tier 3 seam built on
  `IDeterministicRng` and the `RollResult { success, rollValue, threshold, breakdown }` shape.
- `docs/mechanics/ARCHITECTURE.md` §3.2: re-uses the existing `ComputeSpellSuccessChance` seam to
  produce the threshold rather than duplicating formula logic.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §14: follows the documented idea that casting failure
  is a real outcome that needs a stable roll seam, not just a mana check.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` passed with no output (exit 0).
- Fallback validation passed: `Passed: 184, Failed: 0, Skipped: 0, Total: 184` (+11 new tests
  vs. the prior `173` baseline; matches the 11 tests added in `SpellCastRollServiceTests`).

## Caveats

- `SpellExecutionService` is **not** wired to `SpellCastRollService` yet. The Tier 3 roll exists as
  a seam only; choosing where to commit the roll (and what fizzles cost) is deliberately deferred.
- `SpellCastRollService` does not consume mana, mutate actor state, or trigger reactions. Pure
  deterministic seam, safe to call from the DM, UI preview, or future pipeline glue.
- Critical success / critical failure thresholds are not modeled here. The roll is a single d100
  compared to the chance percent.
- Spell resistance, dispel resistance, target armor spell failure, and timed-effect dispel rolls
  remain later Sprint 5 increments.
- Seed derivation (`hash(worldSeed, gameTime, eventId)`) is the caller's responsibility — the
  service accepts any `IDeterministicRng` so callers can plug in `XorShiftRng` or a future seeded
  RNG factory without touching this seam.

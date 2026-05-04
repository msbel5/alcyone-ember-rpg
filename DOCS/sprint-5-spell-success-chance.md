# Sprint 5 Spell Success Chance

Date: 2026-05-04
Branch: `agent/sprint-5-spell-success-chance`
Base: `100b240` — Sprint 5 spell execution pipeline merged on `origin/main`

## Scope

This increment adds a deterministic cast-probability seam for Sprint 5 without changing the existing
spell execution gate yet. The new service answers a narrower question: **if a spell is otherwise
castable, what success percentage should the game or future DM/roll layer use?**

Implemented:

- `SpellSuccessChanceService` in pure `Simulation/Magic`.
- `SpellSuccessChanceResult` with explicit numeric breakdown fields:
  - base chance
  - primary/secondary mental attribute bonuses
  - mana cost penalty
  - effect complexity penalty
  - target-shape penalty
  - range penalty
  - final clamped percentage
- `SpellSuccessChanceError` for stable invalid-caster / invalid-spell refusals.
- School-aware attribute emphasis using the current six-stat Ember kernel honestly as a temporary
  baseline until per-school magic skills exist:
  - Destruction / Alteration / Conjuration favor `MND`
  - Restoration / Illusion / Mysticism favor `INS`
- Focused EditMode fallback tests covering failure paths, school emphasis, explicit breakdown values,
  harder area-at-range difficulty, and clamp behavior.

## Why this slice matters

`docs/mechanics/ARCHITECTURE.md` already reserves `ComputeSpellSuccessChance(caster, spell)` as a
Tier 2 deterministic probability call, but Sprint 5 only had mana checks + target/effect execution.
This slice fills that planning gap without pretending RNG, resistances, cooldowns, or active buff
state are finished.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 is still Layer 3 deterministic gameplay mechanics.
- `docs/mechanics/ARCHITECTURE.md` §3.2: adds the missing `ComputeSpellSuccessChance`-style seam.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §14: follows the documented idea that casting is more
  than mana affordability and should expose a success probability before rolls exist.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` passed with no output.
- Fallback validation passed: `Passed: 171, Failed: 0, Skipped: 0, Total: 171`.

## Caveats

- This service does **not** mutate mana, gate spell execution, or roll RNG yet; it is a deterministic
  percentage seam only.
- The formula is intentionally attribute-only until actor magic skills / school proficiencies land.
- Resistances, saves, cooldowns, armor spell failure, and active timed effects remain later Sprint 5
  increments.

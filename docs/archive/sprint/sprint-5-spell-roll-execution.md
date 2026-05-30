# Sprint 5 Spell Roll Execution

Date: 2026-05-04
Branch: `agent/sprint-5-spell-roll-execution`
Base: `e43e22a` — Sprint 5 spell success roll merged on `origin/main`
Merged to `main`: PR #21, merge commit `14c7b06`

## Scope

This increment wires the existing deterministic Tier 3 cast roll seam into spell execution without
forcing that behavior onto every caller yet. The existing non-roll execution path stays intact; a new
opt-in path accepts a seeded RNG and can now return an explicit **cast fizzled** outcome before mana
commit or effect mutation.

Implemented:

- `SpellExecutionService.TryExecuteWithRoll(...)` in pure `Simulation/Magic`.
- `SpellExecutionService` now performs cast prechecks, target routing, and effect-resolution preview
  first; only then does it run `SpellCastRollService` when the roll-aware path is used.
- `SpellExecutionError.CastFizzled` for the explicit post-precheck / pre-commit failure case.
- `SpellExecutionResult.CastRollResult` plus convenience fields (`Rolled`, `RollValue`,
  `RollThreshold`) so callers can inspect the deterministic roll evidence without parsing strings.
- Focused EditMode fallback tests covering:
  - roll-success path spends mana and applies damage
  - roll-fizzle path spends no mana and mutates no target state
  - precheck refusals happen before any roll result is produced
  - existing non-roll execution path still works unchanged

## Why this slice matters

`docs/sprint-5-spell-success-roll.md` deliberately stopped at the Tier 3 seam and left execution
unwired. That was honest, but it still left gameplay callers with a gap: they could calculate and
roll a cast chance, yet the end-to-end execution service had no native way to honour that result.
This increment closes that gap while keeping the policy narrow and testable.

## Chosen rule in this increment

A failed cast roll **does not** spend mana and **does not** mutate the routed target.

Reason: Sprint 5's execution pipeline already established atomicity around unsupported targets and
unresolvable effects. Keeping fizzles outside mana/effect commitment preserves that same narrow,
truthful rule until a later design pass explicitly chooses a different spend-on-fizzle economy.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 remains Layer 3 deterministic gameplay mechanics.
- `docs/mechanics/ARCHITECTURE.md` §3.2-§3.3: the existing chance and seeded-roll seams are now
  usable from one end-to-end execution path.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §14: casting still flows through mana affordability,
  success logic, and effect application in deterministic layers.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` passed with no output.
- Fallback validation passed: `Passed: 187, Failed: 0, Skipped: 0, Total: 187`.

## Release Evidence

- PR: `#21` — https://github.com/msbel5/alcyone-ember-rpg/pull/21
- Head commit: `06fbc48` — `feat(magic): wire cast roll into spell execution`
- Merge commit: `14c7b06`
- GitHub checks on the merged head commit:
  - `EditMode Tests` — `SUCCESS`
  - `PlayMode Tests + Screenshots` — `SUCCESS`
  - `Test Summary` — `SUCCESS`
  - `GitGuardian Security Checks` — `SUCCESS`
  - `Build Linux64 (headless)` — `SKIPPED`

## Caveats

- The non-roll `TryExecute(...)` path still exists intentionally for deterministic callers that want
  guaranteed execution after prechecks.
- A null/invalid RNG on the roll-aware path is still a rejected cast request, not a fizzle.
- Spend-on-fizzle economy, resistances, cooldowns, timed buffs, and AoE geometry remain future
  Sprint 5 increments.
- Local validation remains the pure .NET fallback harness, not a real local Unity Editor run.

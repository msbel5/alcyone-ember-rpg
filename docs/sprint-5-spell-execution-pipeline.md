# Sprint 5 Spell Execution Pipeline

Date: 2026-05-04
Branch: `agent/sprint-5-spell-execution-pipeline`
Base: `54cc9b2` — Sprint 5 single-target range enforcement merged on `origin/main`

## Scope

This increment adds the deterministic end-to-end spell execution pipeline for the currently supported
instantaneous spell subset. The key fix is atomicity: mana is no longer spent when target validation
or effect-resolution support would refuse the spell.

Implemented:

- `SpellCastingService.TryPrepareCast(...)` for non-mutating cast preflight.
- `SpellCastingService.CommitPreparedCast(...)` so mana spend is an explicit second step.
- `SpellExecutionService` in pure `Simulation/Magic` to orchestrate cast precheck, target routing,
  effect-resolution precheck, mana commit, and final resolution.
- `SpellExecutionError` and `SpellExecutionResult` so callers can see which stage refused the spell
  without parsing strings.
- `SpellEffectResolutionService.CanResolveInstantaneousEffects(...)` so unsupported/timed effects are
  rejected before mana commit.
- Focused EditMode fallback tests covering successful damage/heal casts plus atomic refusal for
  out-of-range, insufficient-mana, and timed-effect paths.

## Why this slice matters

Previous Sprint 5 slices added spell catalog, cost contracts, target validation, and range checks,
but a caller still had to stitch those services together manually. That left an easy failure mode:
spend mana first, discover an invalid target or unsupported effect second. This pipeline closes that
gap while staying fully deterministic and Unity-free.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 remains Layer 3 gameplay mechanics; the new pipeline is
  pure Domain/Simulation orchestration with no AI dependency.
- `docs/EMBER_VISION_BIBLE.md` §8: Sprint 5 keeps shipping narrow, testable magic increments instead
  of pretending the full spell stack is complete.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §14-§15: target validation and effect support are now
  composed into one atomic execution path before mana commitment.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` passed with no output.
- Fallback validation passed: `Passed: 163, Failed: 0, Skipped: 0, Total: 163`.

## Caveats

- Timed buffs like `Ember Ward` are still intentionally unsupported; this increment only prevents
  them from burning mana before the refusal is known.
- Unity Editor / PlayMode validation is still blocked on this Pi because the Unity editor binary is
  not installed here.
- Spell success chance, resistances, cooldowns, active buff state, and AoE geometry remain later
  Sprint 5 increments.

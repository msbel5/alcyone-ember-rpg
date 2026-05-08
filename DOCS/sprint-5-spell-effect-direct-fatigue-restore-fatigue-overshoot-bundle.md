# Sprint 5 — DirectFatigue + RestoreFatigue overshoot bundle pin

Date: 2026-05-08
Branch: `agent/sprint-5-spell-effect-direct-fatigue-restore-fatigue-overshoot-bundle`
Base: `867d326` — Sprint 5 DirectFatigue overshoot clamp-at-zero-fatigue
symmetry pin (PR #66) on `origin/main`.

Thalamus packet: `pkt_20260508020204_f3c4d0cab8a4`
Resolver key: `sha256:9f4c99fcf51704520fa60939eba723f141ae4c997b238ce789b4370cb15943ae`
Vector query present: yes (inline_vector populated, atoms.code/atoms.plan/atoms.memory namespaces)
Query path: vector

## Scope

Test-only EditMode pin. Adds an explicit
`ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigue_OvershootDrainClampsThenRestoreApplies`
regression test that mirrors the existing health-pool overshoot
bundle pin
(`ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_OvershootDamageClampsThenRestoreApplies`,
PR #63) on the fatigue pool.

The non-overshoot per-pair drain+restore aggregation contract is
already pinned for all three damage/restore couples:

- `ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealthAggregatesIndependently`
- `ResolveInstantaneousEffects_DirectMana_BundledWithRestoreManaAggregatesIndependently`
- `ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigueAggregatesIndependently`

…and the single-effect overshoot clamp is pinned for each pool. The
*bundled* overshoot path — drain that overshoots clamps at floor
*then* same-pool restore lifts the pool back up — is currently only
pinned for the health pool. This increment closes one of the two
remaining pool-symmetry gaps by adding the fatigue-pool variant.

## Why this increment

- The drain-then-restore overshoot path is the most likely place a
  future refactor of `SpellEffectResolutionService.ResolveInstantaneousEffects`
  could regress: a naive change could double-count the clamped
  overshoot magnitude into `TotalDirectFatigueDamage`, or apply the
  restore against the pre-clamp pool state and skip the floor.
- Health is already covered. Fatigue and mana are not. Pinning the
  fatigue variant first keeps the increment small and matches the
  "one small, truthful, tested" sprint factory rule.
- No production code change. No risk to the trading boundary, the
  catalog, the buff system, or PlayMode.

## Change set

- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Adds `ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigue_OvershootDrainClampsThenRestoreApplies`.
- `DOCS/sprint-5-spell-effect-direct-fatigue-restore-fatigue-overshoot-bundle.md` (this file).

## Out of scope

- No production code changes.
- No changes to existing tests.
- No catalog, buff, or timed-effect changes.
- No PlayMode changes.
- The DirectMana + RestoreMana overshoot bundle pin — left as the
  next increment so this run stays scoped to one pool.

## Test details

```
target  : Runner, ActorRole.Guard, health=16/16, mana=4/20, fatigue=4/12
spell   : new SpellDefinition("fatigue_overshoot_then_restore_test",
            [DirectFatigue 11, RestoreFatigue 5])
expect  : result.Success == true
          result.AppliedEffectCount == 2
          result.TotalDirectFatigueDamage == 4   (clamped at floor)
          result.TotalRestoredFatigue == 5
          target.Vitals.Fatigue.Current == 5     (0 + 5 after clamp)
```

Starting fatigue (4/12) makes the DirectFatigue 11 overshoot the
floor by 7. The resolver clamps the drain to the available 4 and
records `TotalDirectFatigueDamage = 4`. RestoreFatigue 5 then lifts
the pool from 0 to 5, well below the ceiling, so the pin is purely
about floor-clamp-on-drain + post-clamp-restore aggregation.

## Validation

- `git diff --check` clean.
- `./tools/validation/run-validation.sh --mode fallback`:
  Passed! - Failed: 0, Passed: 608, Skipped: 0, Total: 608.
  The new test name appears in
  `validation-output/fallback-test-results/fallback.trx`.
- Unity EditMode `SpellEffectResolutionServiceTests` runs in CI via
  the existing Unity Tests workflow.

## Next increment

- Add `ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_OvershootDrainClampsThenRestoreApplies`
  to close the last per-pool overshoot-bundle symmetry gap on the
  mana pool.

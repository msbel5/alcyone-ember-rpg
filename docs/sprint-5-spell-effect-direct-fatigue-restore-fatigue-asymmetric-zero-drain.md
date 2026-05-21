# Sprint 5 — DirectFatigue + RestoreFatigue asymmetric zero-drain bundle symmetry pin

Date: 2026-05-09
Branch: `agent/sprint-5-direct-fatigue-restore-fatigue-asymmetric-zero-drain`
Base: `9042608` — Sprint 5 DirectMana + RestoreMana asymmetric zero-drain
bundle symmetry pin (PR #76) on `origin/main`.

Thalamus packet: `pkt_20260509135641_f674a371c0c6`
Resolver key: `sha256:9a60a8fdb737576a2342c4cca72d40c6c92b9624cdf385f3d3c6a46ae3c6c6d0`
Vector query present: yes (atoms.code/atoms.plan/atoms.memory namespaces)

## Scope

Test-only EditMode pin. Adds an explicit
`ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigue_ZeroDrainLeavesRestoreApplied`
regression test that mirrors PR #76 (DirectMana + RestoreMana
asymmetric zero-drain) for the fatigue pool. The DirectMana and
DirectDamage pairs already have full asymmetric zero-magnitude
coverage on both halves; this pin closes the zero-drain half on
the fatigue family.

## Why this increment

- DirectDamage + RestoreHealth has asymmetric zero coverage on
  both halves (PRs #73, #74).
- DirectMana + RestoreMana has asymmetric zero coverage on both
  halves (PRs #75, #76).
- DirectFatigue + RestoreFatigue has only the symmetric zero
  bundle pinned (PR #72) plus the overshoot bundle (PR #68) and
  the independent-aggregation aggregate (existing test). The
  asymmetric zero halves were previously listed as the next
  increments in `DOCS/sprint-5-spell-effect-direct-mana-restore-mana-asymmetric-zero-drain.md`.
- Without this test, a future refactor of
  `SpellEffectResolutionService.ResolveInstantaneousEffects`
  could collapse a zero-magnitude `DirectFatigue` into a no-op
  that short-circuits the bundled `RestoreFatigue`. The existing
  aggregate tests would still pass because both sides are
  non-zero; only an asymmetric-zero pin catches that regression
  class on the fatigue pool.

## Change set

- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Adds `ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigue_ZeroDrainLeavesRestoreApplied`.
- `DOCS/sprint-5-spell-effect-direct-fatigue-restore-fatigue-asymmetric-zero-drain.md` (this file).

## Out of scope

- No production code changes.
- No changes to existing tests.
- The asymmetric zero-restore half on the
  `DirectFatigue + RestoreFatigue` pair is the next increment.

## Test details

```
target  : Runner, ActorRole.Guard, health=16, mana=4, fatigue=6
spell   : new SpellDefinition("direct_fatigue_restore_fatigue_zero_drain_bundle_test",
            [DirectFatigue 0, RestoreFatigue 4])
expect  : result.Success == true
          result.AppliedEffectCount == 2
          result.TotalDirectFatigueDamage == 0
          result.TotalRestoredFatigue == 4
          target.Vitals.Fatigue.Current == 10  (6 + 4, no clamp; fatigue max = 12)
```

The starting fatigue (6) plus the 4-magnitude restore (10) is
below `Fatigue.Max` (12 per `CreateActor`), so the restore does
not clamp. The asserts pin the independent-aggregation contract
on the asymmetric restore-side: the restore still applies and
accumulates into `TotalRestoredFatigue` even when the bundled
drain contributes nothing.

## Validation

- `git diff --check` clean.
- `./tools/validation/run-validation.sh --mode fallback` — pinned
  in commit body.

## Next increment

- Mirror this asymmetric-zero pair on the
  `DirectFatigue + RestoreFatigue` bundle (zero-restore side).

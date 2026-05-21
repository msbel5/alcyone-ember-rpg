# Sprint 5 — DirectMana + RestoreMana asymmetric zero-drain bundle symmetry pin

Date: 2026-05-09
Branch: `agent/sprint-5-direct-mana-restore-mana-asymmetric-zero-drain`
Base: `714a05f` — Sprint 5 DirectMana + RestoreMana asymmetric zero-restore
bundle symmetry pin (PR #75) on `origin/main`.

Thalamus packet: `pkt_20260509134627_e0870ea4b620`
Resolver key: `sha256:aac1d83bc2f7ba3991bce2fc906779386f8401d9933db7310343aec50a707118`
Vector query present: yes (atoms.code/atoms.plan/atoms.memory namespaces)

## Scope

Test-only EditMode pin. Adds an explicit
`ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_ZeroDrainLeavesRestoreApplied`
regression test that mirrors PR #74 (`DirectDamage + RestoreHealth`
zero-damage half on the health pool) for the mana pool. PR #75
already pinned the asymmetric zero-restore half on the mana pool.
This pin closes the symmetric gap on the restore side.

## Why this increment

- The DirectDamage + RestoreHealth pair has full asymmetric
  zero-magnitude coverage on both sides (PRs #73 and #74).
- The DirectMana + RestoreMana pair had the asymmetric
  zero-restore half pinned (PR #75) but not the asymmetric
  zero-drain half. Without this test, a future refactor of
  `SpellEffectResolutionService.ResolveInstantaneousEffects`
  could collapse a zero-magnitude `DirectMana` into a no-op that
  also short-circuits the bundled `RestoreMana` (or vice versa).
  The existing aggregate tests would still pass because both
  sides are non-zero; only an asymmetric-zero pin catches that
  regression class.

## Change set

- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Adds `ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_ZeroDrainLeavesRestoreApplied`.
- `DOCS/sprint-5-spell-effect-direct-mana-restore-mana-asymmetric-zero-drain.md` (this file).

## Out of scope

- No production code changes.
- No changes to existing tests.
- The same asymmetric zero-drain / zero-restore halves on the
  `DirectFatigue + RestoreFatigue` pair will be separate
  increments.

## Test details

```
target  : Acolyte, ActorRole.Player, health=16, mana=6, fatigue=12
spell   : new SpellDefinition("direct_mana_restore_mana_zero_drain_bundle_test",
            [DirectMana 0, RestoreMana 4])
expect  : result.Success == true
          result.AppliedEffectCount == 2
          result.TotalDirectManaDamage == 0
          result.TotalRestoredMana == 4
          target.Vitals.Mana.Current == 10   (6 + 4, no clamp; mana max = 20)
```

The starting mana (6) plus the 4-magnitude restore (10) is well
below `Mana.Max` (20 per `CreateActor`), so the restore does not
clamp. The asserts pin the independent-aggregation contract on
the asymmetric restore-side: the restore still applies and
accumulates into `TotalRestoredMana` even when the bundled drain
contributes nothing.

## Validation

- `git diff --check` clean.
- `./tools/validation/run-validation.sh --mode fallback` — pinned
  in commit body.

## Next increment

- Mirror this asymmetric-zero pair on the
  `DirectFatigue + RestoreFatigue` bundle (zero-drain side).

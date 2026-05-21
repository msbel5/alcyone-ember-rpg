# Sprint 5 — DirectMana + RestoreMana asymmetric zero-restore bundle symmetry pin

Date: 2026-05-09
Branch: `agent/sprint-5-direct-mana-restore-mana-asymmetric-zero-restore`
Base: `b189d7c` — Sprint 5 DirectDamage + RestoreHealth asymmetric zero-damage
bundle symmetry pin (PR #74) on `origin/main`.

Thalamus packet: `pkt_20260509131726_5825a14eda6d`
Resolver key: `sha256:0b66c468c9c283814977e527451f8938dbc799bbf992ce3a9e2220acf0a7bffb`
Vector query present: yes (atoms.code/atoms.plan/atoms.memory namespaces)

## Scope

Test-only EditMode pin. Adds an explicit
`ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_ZeroRestoreLeavesDrainApplied`
regression test that mirrors PR #73 for the mana pool. PR #73
pinned the asymmetric zero-restore half of the
`DirectDamage + RestoreHealth` bundle on the health pool;
PR #74 mirrored the zero-damage half on the same pool. The mana
pool already has the symmetric and the zero-magnitude bundle pins
(see `BundledWithRestoreManaAggregatesIndependently` and
`BundledWithRestoreMana_ZeroMagnitudeLeavesManaUnchanged`), but
the asymmetric zero-restore-side half was not pinned. This pin
closes that gap on the drain side.

## Why this increment

- The DirectDamage + RestoreHealth pair now has full asymmetric
  zero-magnitude coverage on both sides (PRs #73 and #74).
- The DirectMana + RestoreMana pair has symmetric and fully-zero
  bundle pins but no asymmetric zero-side pin yet.
- A future refactor of `SpellEffectResolutionService.ResolveInstantaneousEffects`
  could collapse a zero-magnitude `RestoreMana` into a no-op that
  also short-circuits the preceding `DirectMana` drain (or vice
  versa). The existing aggregate test would still pass because
  both sides are non-zero; the asymmetric pin is the only one
  that catches that specific class of regression.

## Change set

- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Adds `ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_ZeroRestoreLeavesDrainApplied`.
- `DOCS/sprint-5-spell-effect-direct-mana-restore-mana-asymmetric-zero-restore.md` (this file).

## Out of scope

- No production code changes.
- No changes to existing tests.
- The mirror zero-drain half on the mana pool (analogue of PR #74)
  and the same pair on the fatigue pool will be separate increments.

## Test details

```
target  : Acolyte, ActorRole.Player, health=16, mana=10, fatigue=12
spell   : new SpellDefinition("direct_mana_restore_mana_zero_restore_bundle_test",
            [DirectMana 4, RestoreMana 0])
expect  : result.Success == true
          result.AppliedEffectCount == 2
          result.TotalDirectManaDamage == 4
          result.TotalRestoredMana == 0
          target.Vitals.Mana.Current == 6   (10 - 4 + 0, no clamp)
```

The starting mana (10) leaves enough headroom that the drain does
not clamp at zero and the zero-magnitude restore does not need to
clamp at max. The asserts pin the independent-aggregation contract
on the asymmetric drain-side: the drain still applies and
accumulates into `TotalDirectManaDamage` even when the bundled
restore contributes nothing.

## Validation

- `git diff --check` clean.
- `./tools/validation/run-validation.sh --mode fallback` — pinned
  in commit body.

## Next increment

- Mirror this pin on the zero-drain side of the mana pair
  (DirectMana 0 + RestoreMana N).
- Then mirror both halves on the fatigue pool.

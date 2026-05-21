# Sprint 5 Spell Effect — DirectDamage + RestoreHealth Asymmetric Zero-Damage Bundle Symmetry

Date: 2026-05-09
Branch: `agent/sprint-5-direct-damage-restore-health-asymmetric-zero-damage`
Base: `c5911b1` — Sprint 5 DirectDamage + RestoreHealth asymmetric zero-restore bundle symmetry (PR #73) merged on `origin/main`

Thalamus packet: `pkt_20260509124637_dc743f34d106`
Resolver key: `sha256:eb601687f4ece09f6a94196d56b6433bf94b55d8a2ab0c7fe9de0fdf4793dbe4`
Confidence: 0.4 (Captain decomposition; mirror of PR #73, smallest possible follow-up)

## Scope

PR #73 pinned the `damage > 0, restore = 0` half of the asymmetric
DirectDamage + RestoreHealth bundle on the health pool. The "Next
increment" line of that doc explicitly calls out the mirror half —
`damage = 0, restore > 0` — as the next atom before extending the
asymmetric pattern to the mana and fatigue pools.

This increment closes that mirror. With both halves of the asymmetric
matrix pinned on the health pool, the resolver's contract for
zero-magnitude effects participating in a bundle (still walked, still
counted in `AppliedEffectCount`, contribute zero to their respective
totals) is symmetrically protected against silent regressions on the
damage side and the restore side independently.

The new test reuses the same actor shape as the zero-restore mirror
(target health 10 / max 16, mana 4) but flips the magnitudes:
`DirectDamage 0`, `RestoreHealth 4`. It asserts that the resolver still
walks both effect specs (`AppliedEffectCount == 2`), reports zero on
`TotalDamage` and 4 on `TotalHealing`, and lands the target at
`Health.Current == 14`. The mana and fatigue counterparts of the
asymmetric matrix are tracked as separate atoms for follow-up runs.

## Files changed

- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Adds `ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_ZeroDamageLeavesRestoreApplied`
    immediately after the existing `DirectDamage_BundledWithRestoreHealth_ZeroRestoreLeavesDamageApplied`
    pin, keeping the asymmetric pair ordered together inside the
    `DirectDamage` group of the suite.

No production code changed. No new spell effect kinds, no new result
fields, no new validators. Pure characterization pin.

## Validation

`./tools/validation/run-validation.sh --mode fallback`

- `unity_editor`: BLOCKED — Unity Editor not available on the Pi (expected).
- `fallback_harness`: see `validation-output/fallback-test-results/fallback.trx`.

## Next increment

Carry the asymmetric matrix to the mana pool: `DirectMana, m / 0` and
`DirectMana 0 / RestoreMana, m` mirror pins, then the same shape on the
fatigue pool. After per-pool asymmetric closes, the natural follow-up is
the `AllSix` aggregate's zero-magnitude variant that asserts every
running total is zero and every pool is untouched, mirroring the
per-pool zero pins in aggregate.

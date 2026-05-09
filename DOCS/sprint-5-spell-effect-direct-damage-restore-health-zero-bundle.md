# Sprint 5 Spell Effect — DirectDamage + RestoreHealth Zero-Magnitude Bundle Symmetry

Date: 2026-05-09
Branch: `agent/sprint-5-direct-damage-restore-health-zero-bundle`
Base: `a271e5e` — Sprint 5 DirectMana + RestoreMana zero-magnitude bundle symmetry (PR #70) merged on `origin/main`

Thalamus packet: `pkt_20260509063152_17bf897165dc`
Resolver key: `sha256:50728ff53a8efae00c09f9611c92d1cb3f7f9a3f7281556f6106e02f6c05b29a`
Vector query present: yes (inline_vector returned)
Query path: vector

## Scope

This increment pins the health-side mirror of the recently merged DirectMana
+ RestoreMana zero-magnitude bundle symmetry test (PR #70). Until now, the
zero-magnitude bundle invariant was covered for the mana pool (`DirectMana`
+ `RestoreMana`) but not for the health pool (`DirectDamage` +
`RestoreHealth`). Each individual zero-magnitude path was already pinned
(`DirectDamage_ZeroMagnitudeLeavesHealthUnchanged` at line 126,
`RestoreHealth_ZeroMagnitudeLeavesHealthUnchanged` at line 164), and the
non-zero bundle was pinned across two earlier sprints (the aggregate test
at PR #63 and the overshoot test at PR #67). The zero-magnitude bundle case
on the health pool itself was the missing symmetric pin.

The new test exercises the documented `magnitude >= 0` lower bound on
`SpellEffectSpec` against a bundle that contains both `DirectDamage` and
`RestoreHealth` with magnitude `0`. It asserts that the loop runs both
branches (so `AppliedEffectCount == 2`), reports zero on both running
totals (`TotalDamage == 0`, `TotalHealing == 0`), and leaves not just the
target's health pool but also the unrelated mana and fatigue pools
untouched. The mana and fatigue assertions guard against future
cross-pool leakage if the resolver ever shares a clamp helper across
effect kinds.

## Files changed

- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Adds `ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_ZeroMagnitudeLeavesHealthUnchanged`
    after the existing `DirectDamage_ZeroMagnitudeLeavesHealthUnchanged` and
    before the `RestoreHealth_HealsTargetUpToMax` group, mirroring the
    `DirectMana_BundledWithRestoreMana_ZeroMagnitudeLeavesManaUnchanged`
    template at line 507.

No production code changed. No new spell effect kinds, no new result
fields, no new validators. Pure characterization pin.

## Validation

`./tools/validation/run-validation.sh --mode fallback`

- `unity_editor`: BLOCKED — Unity Editor not available on the Pi (expected).
- `fallback_harness`: PASS — `ValidationFallbackHarness.dll` ran 611 tests
  (was 610 before this commit), 0 failed, 0 skipped, 943 ms.
- Result file: `validation-output/fallback-test-results/fallback.trx`.

## Next increment

The next missing symmetric pin in the magic instantaneous-effects matrix is
the fatigue-pool counterpart: `DirectFatigue + RestoreFatigue` zero-magnitude
bundle symmetry. After that, the per-pool zero-magnitude bundle matrix is
fully covered and the next slice of work moves to the multi-pool `AllSix`
bundle's degenerate cases (already partially pinned at line 794) or to
the next foundation step from the active sprint plan.

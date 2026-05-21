# Sprint 5 Spell Effect — DirectFatigue + RestoreFatigue Zero-Magnitude Bundle Symmetry

Date: 2026-05-09
Branch: `agent/sprint-5-direct-fatigue-restore-fatigue-zero-bundle`
Base: `07b839c` — Sprint 5 DirectDamage + RestoreHealth zero-magnitude bundle symmetry (PR #71) merged on `origin/main`

Thalamus packet: `pkt_20260509113651_f8e93d493f88`
Resolver key: `sha256:7a466bd7613bf01cf55f5463d87b17e7f1cf4fb3899fe11c8809cb1f8a5a3977`
Confidence: 0.35 (Captain decomposition; small shippable pin)

## Scope

This increment closes the per-pool zero-magnitude bundle symmetry matrix on
the fatigue side. The mana-pool counterpart was pinned at PR #70
(`DirectMana_BundledWithRestoreMana_ZeroMagnitudeLeavesManaUnchanged`) and
the health-pool counterpart was pinned at PR #71
(`DirectDamage_BundledWithRestoreHealth_ZeroMagnitudeLeavesHealthUnchanged`).
Each individual fatigue zero-magnitude path was already pinned
(`DirectFatigue_ZeroMagnitudeLeavesFatigueUnchanged` at line 684,
`RestoreFatigue_ZeroMagnitudeLeavesFatigueUnchanged` at line 277), and the
non-zero fatigue bundle was pinned across two earlier sprints (the aggregate
test at `DirectFatigue_BundledWithRestoreFatigueAggregatesIndependently` and
the overshoot test at PR #68). The zero-magnitude fatigue bundle was the
remaining missing symmetric pin.

The new test exercises the documented `magnitude >= 0` lower bound on
`SpellEffectSpec` against a bundle that contains both `DirectFatigue` and
`RestoreFatigue` with magnitude `0`. It asserts that the loop runs both
branches (`AppliedEffectCount == 2`), reports zero on both running fatigue
totals (`TotalDirectFatigueDamage == 0`, `TotalRestoredFatigue == 0`), and
leaves not just the target's fatigue pool but also the unrelated health and
mana pools untouched. The health and mana assertions guard against future
cross-pool leakage if the resolver ever shares a clamp helper across effect
kinds.

## Files changed

- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Adds `ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigue_ZeroMagnitudeLeavesFatigueUnchanged`
    after the existing `DirectFatigue_ZeroMagnitudeLeavesFatigueUnchanged` and
    before the `MultipleSupportedEffects_AppliesInDefinitionOrder` group,
    mirroring the `DirectMana_BundledWithRestoreMana_ZeroMagnitudeLeavesManaUnchanged`
    template at line 535.

No production code changed. No new spell effect kinds, no new result
fields, no new validators. Pure characterization pin.

## Validation

`./tools/validation/run-validation.sh --mode fallback`

- `unity_editor`: BLOCKED — Unity Editor not available on the Pi (expected).
- `fallback_harness`: PASS — `ValidationFallbackHarness.dll` ran 612 tests
  (was 611 before this commit), 0 failed, 0 skipped, 1 s.
- Result file: `validation-output/fallback-test-results/fallback.trx`.

## Next increment

The per-pool zero-magnitude bundle matrix is now fully covered (health,
mana, fatigue). The next slice of work moves to the multi-pool `AllSix`
bundle's degenerate cases (already partially pinned at line 825 of
`SpellEffectResolutionServiceTests.cs`) — specifically an `AllSix`
zero-magnitude variant that asserts every running total is zero and every
pool is untouched, mirroring the per-pool zero pins in aggregate. After
that, the next foundation step from the active sprint plan (shield buff
actor-keyed flow continuation) becomes the natural follow-up.

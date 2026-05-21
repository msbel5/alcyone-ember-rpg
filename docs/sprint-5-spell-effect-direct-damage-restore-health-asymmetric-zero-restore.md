# Sprint 5 Spell Effect — DirectDamage + RestoreHealth Asymmetric Zero-Restore Bundle Symmetry

Date: 2026-05-09
Branch: `agent/sprint-5-direct-damage-restore-health-asymmetric-zero-restore`
Base: `09cf7ea` — Sprint 5 DirectFatigue + RestoreFatigue zero-magnitude bundle symmetry (PR #72) merged on `origin/main`

Thalamus packet: `pkt_20260509120203_0698f8513a20`
Resolver key: `sha256:e8feaec23b514af588428ef37fbb815d08ab194bc0a66b6d9202eb3802987364`
Confidence: 0.35 (Captain decomposition; small shippable pin)

## Scope

The per-pool symmetric bundle matrix (both magnitudes equal, either both
non-zero or both zero) is fully pinned across the three pools (health, mana,
fatigue). The remaining symmetry gap is the asymmetric path: a bundle in
which one effect carries a non-zero magnitude and its sibling carries zero.
The resolver currently treats a zero-magnitude `RestoreHealth` as a still-
applied no-op (counted in `AppliedEffectCount`, contributing zero to
`TotalHealing`), and the paired `DirectDamage` should land its full
magnitude. Without an explicit pin, a future refactor that elides
zero-magnitude effects from `AppliedEffectCount` could silently break this
contract.

This increment pins the `damage > 0, restore = 0` half of the asymmetric
matrix on the health pool. It uses the same actor shape as the existing
`BundledWithRestoreHealthAggregatesIndependently` test (target health 10),
but with a 4-damage / 0-restore spec instead of 6 / 3. It asserts that the
resolver still walks both effect specs (`AppliedEffectCount == 2`), reports
the damage on `TotalDamage` and zero on `TotalHealing`, and lands the
target at `Health.Current == 6`. The mirrored `damage = 0, restore > 0`
half and the mana / fatigue counterparts are tracked as separate atoms
for follow-up runs.

## Files changed

- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Adds `ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_ZeroRestoreLeavesDamageApplied`
    immediately after the existing `DirectDamage_BundledWithRestoreHealth_ZeroMagnitudeLeavesHealthUnchanged`
    pin, keeping the per-bundle-shape ordering inside the
    `DirectDamage` group of the suite.

No production code changed. No new spell effect kinds, no new result
fields, no new validators. Pure characterization pin.

## Validation

`./tools/validation/run-validation.sh --mode fallback`

- `unity_editor`: BLOCKED — Unity Editor not available on the Pi (expected).
- `fallback_harness`: PASS — see `validation-output/fallback-test-results/fallback.trx`.

## Next increment

Mirror this pin for the `damage = 0, restore > 0` half of the same bundle,
then carry the asymmetric pattern across the mana and fatigue pools. After
the asymmetric per-pool matrix closes, the natural follow-up is the
`AllSix` aggregate's zero-magnitude variant that asserts every running
total is zero and every pool is untouched, mirroring the per-pool zero
pins in aggregate.

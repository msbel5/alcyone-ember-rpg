# Sprint 5 — Bundled DirectDamage Overshoot + RestoreHealth Symmetry

## Scope
Pin the bundled-overshoot interaction between `DirectDamage` and
`RestoreHealth` in `SpellEffectResolutionService.ResolveInstantaneousEffects`:
when DirectDamage magnitude exceeds the target's current health, the damage
must clamp at zero, and a subsequent RestoreHealth in the same spell bundle
must apply on top of the clamped pool. Aggregated counters
(`TotalDamage`, `TotalHealing`) must reflect the actually-applied amounts,
not the requested magnitudes.

## Why
The single-effect overshoot triad is now pinned for all three bilateral
pools (DirectDamage / DirectMana / DirectFatigue — PRs #64, #65, #66).
The next layer of regression risk is the **bundle case**: a destruction +
restoration spell that drains past zero and then heals back. Without this
test, a future refactor could accidentally apply the requested-magnitude
damage to the clamped pool calculation, causing TotalDamage to leak the
non-applied "phantom" overshoot or causing the restore step to mis-interpret
the post-clamp pool. This is the smallest bundled-overshoot regression pin
that uses an already-supported two-effect spec.

## Change
- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs` —
  add `ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_OvershootDamageClampsThenRestoreApplies`:
  - target health = 4 (max 16), spell = `[DirectDamage 11, RestoreHealth 5]`
  - expects `TotalDamage == 4` (clamped to pool), `TotalHealing == 5`
    (in-bounds), `target.Vitals.Health.Current == 5`,
    `AppliedEffectCount == 2`.

## Risk
None — pure test addition. No production code touched. No new types,
no new namespaces, no new asmdef references.

## Validation
- `git diff --check`: clean.
- `./tools/validation/run-validation.sh --mode fallback`: see report under
  `validation-output/`. Unity NUnit runner is unavailable on the Pi runner;
  fallback mode validates static repo structure and managed-block
  conventions, not test execution. Test execution will run in CI when the
  PR triggers GitHub Actions.

## Thalamus
- packet_id: `pkt_20260508003753_697aa2914967`
- resolver_key: `sha256:3ebe4965f2276a64b5702300b719c6de73d9a713812c29c27d9860b0c2418d36`
- vector_query_present: false
- query_path: text

## Next
Mirror the same bundled-overshoot pin for the second bilateral pool —
`DirectMana 11 + RestoreMana 5` against a low-mana target — to begin
extending the overshoot symmetry triad into the bundled-aggregation layer
before tackling cross-pool overshoot bundles
(e.g. `DirectDamage + RestoreHealth + DirectFatigue + RestoreFatigue`
mixed-overshoot composition).

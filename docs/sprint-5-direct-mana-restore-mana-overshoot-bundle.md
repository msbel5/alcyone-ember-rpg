# Sprint 5 — Bundled DirectMana Overshoot + RestoreMana Symmetry

Date: 2026-05-08
Branch: `agent/sprint-5-direct-mana-restore-mana-overshoot-bundle`
Base: `1865f07` — Sprint 5 DirectFatigue + RestoreFatigue overshoot bundle pin (PR #68) on `origin/main`.

Thalamus packet: `pkt_20260508030143_0057cfce9020`
Resolver key: `sha256:9aff5c2569b0d3663fbb71ccb5d49a617e976273c4730dd9582e15b62cb942a0`
Vector query present: yes (inline_vector returned)
Query path: vector

## Scope
Pin the bundled-overshoot interaction between `DirectMana` and
`RestoreMana` in
`SpellEffectResolutionService.ResolveInstantaneousEffects`: when
`DirectMana` magnitude exceeds the target's current mana, the drain
must clamp at zero, and a subsequent `RestoreMana` in the same spell
bundle must apply on top of the clamped pool. Aggregated counters
(`TotalDirectManaDamage`, `TotalRestoredMana`) must reflect the
actually-applied amounts, not the requested magnitudes.

## Why
The bilateral overshoot-bundle triad now closes:
- DirectDamage + RestoreHealth (PR #67)
- DirectFatigue + RestoreFatigue (PR #68)
- **DirectMana + RestoreMana — this PR**

Without a bundled overshoot pin for the mana pool, a future refactor
of `ApplyDirectMana` could accidentally apply requested-magnitude drain
to the post-restore pool calculation, leaking phantom drain into
`TotalDirectManaDamage` or letting `RestoreMana` mis-interpret the
post-clamp pool. This is the smallest bundled-overshoot regression pin
that closes the symmetry across all three bilateral pools.

## Change
- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs` —
  add `ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_OvershootDrainClampsThenRestoreApplies`:
  - target mana = 4 (max 16), spell = `[DirectMana 11, RestoreMana 5]`
  - expects `TotalDirectManaDamage == 4` (clamped to pool),
    `TotalRestoredMana == 5` (in-bounds),
    `target.Vitals.Mana.Current == 5`,
    `AppliedEffectCount == 2`.

## Validation
`./tools/validation/run-validation.sh --mode fallback`
Result: `Passed! - Failed: 0, Passed: 609, Skipped: 0, Total: 609`
(`fallback_exit_code=0`, harness `PASS fallback_harness`).

## Next increment
Closes the bilateral pool overshoot-bundle triad. Natural follow-up
candidates:
- A three-effect overshoot bundle (e.g. `[DirectDamage overshoot,
  RestoreHealth, DirectMana overshoot]`) that pins independent
  per-pool clamping in a heterogeneous spell.
- Symmetric ZeroMagnitude bundle pin: `[DirectMana 0, RestoreMana 0]`
  must yield no aggregate counter movement and no vital change.

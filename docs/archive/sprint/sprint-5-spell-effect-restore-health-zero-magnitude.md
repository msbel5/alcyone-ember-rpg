# Sprint 5 — RestoreHealth zero-magnitude no-op pin

## Scope
Adds one EditMode regression test that closes the last symmetry gap in
zero-magnitude no-op coverage across the instantaneous direct/restore
effect kinds. Pre-existing tests pin zero-magnitude behaviour for
`DirectDamage` (PR #59), `RestoreMana`, `DirectMana`, and `DirectFatigue`.
`RestoreHealth` and `RestoreFatigue` were the remaining direct-style
instantaneous kinds without an explicit zero-magnitude pin; this
increment closes the `RestoreHealth` gap. `RestoreFatigue` is left for
a follow-up increment to keep this PR small and single-purpose.

## Why this increment
- The all-six bundle test (PR #58) pins compositional correctness, but
  does not exercise a zero-magnitude path for `RestoreHealth`.
- Future refactors of
  `SpellEffectResolutionService.ResolveInstantaneousEffects` must keep
  the documented contract: zero-magnitude restore-health applies the
  effect (`AppliedEffectCount = 1`) and reports `TotalHealing = 0`
  while leaving the target's health pool untouched.
- Symmetry with the existing zero-magnitude pins for the other
  instantaneous direct/restore kinds keeps the regression net even
  across the full kind set.

## Change set
- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Added
    `ResolveInstantaneousEffects_RestoreHealth_ZeroMagnitudeLeavesHealthUnchanged`.
- `docs/sprint-5-spell-effect-restore-health-zero-magnitude.md`
  (this file).

## Out of scope
- No production code changes.
- No changes to existing tests.
- No buff/timed-effect changes.
- `RestoreFatigue` zero-magnitude pin is intentionally deferred.

## Validation
- `git diff --check` clean.
- `./tools/validation/run-validation.sh --mode fallback`.
- Unity EditMode `SpellEffectResolutionServiceTests` runs in CI via the
  existing Unity Tests workflow.

## Thalamus packet
- packet: `pkt_20260507091715_8dd2dbd170b5`
- resolver: `sha256:2f7f451f4ca2c391c11624244add01b731eb0424424d1427e0a7f7999bf8bbe7`
- query path: vector (inline_vector populated, atoms.code namespace)

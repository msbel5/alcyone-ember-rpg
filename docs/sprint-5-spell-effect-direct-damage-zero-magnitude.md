# Sprint 5 — DirectDamage zero-magnitude no-op pin

## Scope
Adds one EditMode regression test to round out the symmetry of zero-magnitude
no-op coverage across the instantaneous direct kinds. Pre-existing tests
already pin zero-magnitude behaviour for `RestoreMana`, `DirectMana`, and
`DirectFatigue`. `DirectDamage` was the only direct kind without an explicit
zero-magnitude pin; this test closes that gap.

## Why this increment
- All six instantaneous spell effect kinds are wired and aggregated.
- The all-six bundle test (PR #58) pins compositional correctness, but does
  not exercise a zero-magnitude path for `DirectDamage`.
- Future refactors of `SpellEffectResolutionService.ResolveInstantaneousEffects`
  must keep the documented contract: zero-magnitude direct damage applies the
  effect (`AppliedEffectCount = 1`) and reports `TotalDamage = 0` while leaving
  the target's health pool untouched.

## Change set
- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Added `ResolveInstantaneousEffects_DirectDamage_ZeroMagnitudeLeavesHealthUnchanged`.
- `DOCS/sprint-5-spell-effect-direct-damage-zero-magnitude.md` (this file).

## Out of scope
- No production code changes.
- No changes to existing tests.
- No buff/timed-effect changes.

## Validation
- `git diff --check` clean.
- `./tools/validation/run-validation.sh --mode fallback`.
- Unity EditMode `SpellEffectResolutionServiceTests` runs in CI via the
  existing Unity Tests workflow.

## Thalamus packet
- packet: `pkt_20260507081646_b9c62354916e`
- resolver: `sha256:d75f6479782359c4df6403a52eb67685f8ace4fca2c8230baa36c07dc018ead8`
- query path: vector (inline_vector populated, atoms.code namespace)

# Sprint 5 — RestoreFatigue zero-magnitude no-op pin

## Scope
Adds one EditMode regression test to round out zero-magnitude no-op
coverage across the instantaneous direct/restore kinds. Pre-existing
tests already pin zero-magnitude behaviour for `DirectDamage` (PR #59),
`RestoreHealth` (PR #60), `RestoreMana`, `DirectMana`, and `DirectFatigue`.
`RestoreFatigue` was the last instantaneous restore kind without an
explicit zero-magnitude pin; this test closes that gap.

## Why this increment
- All six instantaneous spell effect kinds are wired and aggregated.
- The all-six bundle test (PR #58) pins compositional correctness, but
  does not exercise a zero-magnitude path for `RestoreFatigue` in
  isolation.
- Future refactors of `SpellEffectResolutionService.ResolveInstantaneousEffects`
  must keep the documented contract: zero-magnitude restore-fatigue
  still applies the effect (`AppliedEffectCount = 1`) and reports
  `TotalRestoredFatigue = 0` while leaving the target's fatigue pool
  untouched.

## Change set
- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Added `ResolveInstantaneousEffects_RestoreFatigue_ZeroMagnitudeLeavesFatigueUnchanged`.
- `docs/sprint-5-spell-effect-restore-fatigue-zero-magnitude.md` (this file).

## Out of scope
- No production code changes.
- No changes to existing tests.
- No buff/timed-effect or catalog changes.

## Validation
- `git diff --check` clean.
- `./tools/validation/run-validation.sh --mode fallback`.
- Unity EditMode `SpellEffectResolutionServiceTests` runs in CI via the
  existing Unity Tests workflow.

## Thalamus packet
- packet: `pkt_20260507101717_78d445a92e6e`
- resolver: `sha256:3eb0059de47dfa9421d0abfdea1facbe2f5df03f7aee32598f85a9299e0f08b6`
- query path: vector (inline_vector populated, 1024 dims, atoms.code namespace)

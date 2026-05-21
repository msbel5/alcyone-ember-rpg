# Sprint 5 — Bundled DirectMana + RestoreMana Zero-Magnitude Symmetry

Date: 2026-05-09
Branch: `agent/sprint-5-direct-mana-restore-mana-zero-bundle`
Base: `147d84d` — Sprint 5 DirectMana + RestoreMana overshoot bundle pin (PR #69) on `origin/main`.

Thalamus packet: `pkt_20260509043152_5e830ef4a26d`
Resolver key: `sha256:b13752a947162ad1a92df2bffa3207795e542e01f7d02ad4abed3e08863d5484`
Vector query present: yes (inline_vector returned)
Query path: vector

## Scope
Pin the symmetric zero-magnitude bundle case for `DirectMana` +
`RestoreMana` in
`SpellEffectResolutionService.ResolveInstantaneousEffects`: when both
effects in a spell bundle carry magnitude `0`, both must still be
counted as applied (`AppliedEffectCount == 2`), aggregate counters
(`TotalDirectManaDamage`, `TotalRestoredMana`) must remain at `0`, and
no vital pool (mana, health, fatigue) may move.

## Why
The previous sprint summary explicitly named this as a natural
follow-up after PR #69 closed the bilateral overshoot-bundle triad.
Without a zero-magnitude bundle pin for the mana pool, a future
refactor of bundle aggregation could:
- silently skip `0`-magnitude entries and miscount `AppliedEffectCount`;
- accumulate phantom counter movement on a no-op spell;
- mutate an unrelated pool when the bundle order is reshuffled.

This is the smallest regression pin that locks the no-op contract for
a heterogeneous mana bundle.

## Change
- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs` —
  add
  `ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_ZeroMagnitudeLeavesManaUnchanged`:
  - target mana = 8 (max 16), health 16, fatigue 12,
    spell = `[DirectMana 0, RestoreMana 0]`
  - expects `Success == true`, `AppliedEffectCount == 2`,
    `TotalDirectManaDamage == 0`, `TotalRestoredMana == 0`,
    `Mana.Current == 8`, `Health.Current == 16`,
    `Fatigue.Current == 12`.

## Validation
`./tools/validation/run-validation.sh --mode fallback`
Result: `Passed! - Failed: 0, Passed: 610, Skipped: 0, Total: 610`
(`fallback_exit_code=0`, harness `PASS fallback_harness`).

## Next increment
Mana zero-magnitude bundle symmetry is now pinned. Natural follow-up
candidates:
- Symmetric ZeroMagnitude bundle pin for the fatigue pool:
  `[DirectFatigue 0, RestoreFatigue 0]` — closes the bilateral
  zero-magnitude triad alongside the existing health/mana pins.
- Heterogeneous all-zero bundle pin: a single spell carrying every
  supported instantaneous effect at magnitude `0`, asserting all six
  aggregate counters remain `0` and `AppliedEffectCount == 6`.

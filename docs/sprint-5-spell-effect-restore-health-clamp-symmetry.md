# Sprint 5 — RestoreHealth ClampsAtMaxHealth symmetry pin

Date: 2026-05-07
Branch: `agent/sprint-5-spell-effect-restore-health-clamp-symmetry`
Base: `a6c0e1c` — Sprint 5 RestoreFatigue zero-magnitude pin (PR #61)
on `origin/main`.

Thalamus packet: `pkt_20260507111737_a8b8a6af7201`
Resolver key: `sha256:e47e656fcdef72e7e6232b9fc7931862e77c3c0504644c273f977da780c3547e`
Vector query present: yes (inline_vector populated, atoms.code namespace)
Query path: vector

## Scope

Test-only EditMode pin. Adds an explicit
`ResolveInstantaneousEffects_RestoreHealth_ClampsAtMaxHealth`
regression test that mirrors the existing symmetric clamp pins for
the other two restore verbs:

- `ResolveInstantaneousEffects_RestoreFatigue_ClampsAtMaxFatigue`
- `ResolveInstantaneousEffects_RestoreMana_ClampsAtMaxMana`

Both already use a hand-built `SpellDefinition` whose magnitude
deliberately overshoots the target's pool ceiling, then assert the
restore counter equals only the headroom and the pool ends exactly
at maximum. RestoreHealth previously had only
`HealsTargetUpToMax`, which uses the `MendingTouch` catalog spell
whose magnitude is implementation-defined. The new test makes the
clamp contract explicit on the health pool with the same symmetric
shape as the fatigue/mana variants, so refactors of
`SpellEffectResolutionService.ResolveInstantaneousEffects` cannot
accidentally weaken health-pool clamping while leaving the other
two intact.

## Why this increment

- All six instantaneous spell verbs are wired and aggregated.
- Clamp behaviour is pinned for fatigue (PR ancestors) and mana
  (PR ancestors), but the matching health-pool pin is implicit via
  a catalog-spell test whose magnitude is not visually symmetric
  with the other two.
- The magic resolver is the layer most likely to be touched as the
  buff/timed-effect code expands; tightening the regression net on
  the simplest path keeps future refactors honest.

## Change set

- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Adds `ResolveInstantaneousEffects_RestoreHealth_ClampsAtMaxHealth`.
- `DOCS/sprint-5-spell-effect-restore-health-clamp-symmetry.md` (this file).

## Out of scope

- No production code changes.
- No changes to existing tests.
- No catalog, buff, or timed-effect changes.
- No PlayMode changes.

## Test details

```
target  : Guard, ActorRole.Guard, health=14/16, mana=4/20, fatigue=12/12
spell   : new SpellDefinition("restore_health_clamp_test", magnitude=5, RestoreHealth)
expect  : result.Success == true
          result.TotalHealing == 2
          target.Vitals.Health.Current == 16
```

The magnitude (5) overshoots the available headroom (16 - 14 = 2),
so `VitalStat.Restore` must clamp the pool at 16 and the resolver
must aggregate only the realised delta of 2 into `TotalHealing`.

## Validation

- `git diff --check` clean.
- `./tools/validation/run-validation.sh --mode fallback`.
- Unity EditMode `SpellEffectResolutionServiceTests` runs in CI via
  the existing Unity Tests workflow.

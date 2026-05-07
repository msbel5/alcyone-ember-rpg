# Sprint 5 — DirectDamage + RestoreHealth bundle symmetry pin

Date: 2026-05-07
Branch: `agent/sprint-5-spell-effect-direct-damage-restore-health-bundle-symmetry`
Base: `c9e3f9b` — Sprint 5 RestoreHealth clamp-at-max symmetry pin (PR #62)
on `origin/main`.

Thalamus packet: `pkt_20260507121654_936be3c8fe02`
Resolver key: `sha256:017337b57826c969ba1be3e8511422d88bd14d4c26b5cab5b4089163204ce2eb`
Vector query present: yes (inline_vector populated, atoms.code/atoms.plan/atoms.memory namespaces)
Query path: vector

## Scope

Test-only EditMode pin. Adds an explicit
`ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealthAggregatesIndependently`
regression test that mirrors the existing symmetric drain+restore
bundle pins for the other two damage/restore pairs:

- `ResolveInstantaneousEffects_DirectMana_BundledWithRestoreManaAggregatesIndependently`
- `ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigueAggregatesIndependently`

Both already build a hand-rolled `SpellDefinition` containing a
direct-drain effect followed by a same-pool restore effect, then
assert each side aggregates into its own counter and the pool ends
at the algebraic delta. The DirectDamage/RestoreHealth pair was the
only damage/restore couple without a dedicated bundled-aggregation
pin; the existing `MultipleSupportedEffects_AppliesInDefinitionOrder`
test mixes three kinds across two pools and was not symmetric with
the per-pair pins. The new test makes the per-pair contract
explicit on the health pool, so future refactors of
`SpellEffectResolutionService.ResolveInstantaneousEffects` cannot
accidentally collapse `TotalDamage` into `TotalHealing` (or vice
versa) while leaving the fatigue/mana pairs intact.

## Why this increment

- All six instantaneous spell verbs are wired and aggregated and
  every per-verb clamp + zero-magnitude pin is now in place.
- Per-pair drain+restore symmetry is pinned for fatigue and mana
  but not for health.
- The magic resolver is the layer most likely to be touched as
  buff/timed-effect code expands; tightening the regression net on
  the simplest aggregation path keeps future refactors honest.

## Change set

- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Adds `ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealthAggregatesIndependently`.
- `DOCS/sprint-5-spell-effect-direct-damage-restore-health-bundle-symmetry.md` (this file).

## Out of scope

- No production code changes.
- No changes to existing tests.
- No catalog, buff, or timed-effect changes.
- No PlayMode changes.

## Test details

```
target  : Acolyte, ActorRole.Player, health=10/16, mana=4/20, fatigue=12/12
spell   : new SpellDefinition("damage_then_restore_test",
            [DirectDamage 6, RestoreHealth 3])
expect  : result.Success == true
          result.AppliedEffectCount == 2
          result.TotalDamage == 6
          result.TotalHealing == 3
          target.Vitals.Health.Current == 7   (10 - 6 + 3, no clamp)
```

The starting pool (10/16) leaves enough headroom that neither
ceiling nor floor clamping fires, so the asserts pin the
independent-aggregation contract directly: each side accumulates
in its own counter and the resolver applies the pair in
definition order.

## Validation

- `git diff --check` clean.
- `./tools/validation/run-validation.sh --mode fallback`.
- Unity EditMode `SpellEffectResolutionServiceTests` runs in CI via
  the existing Unity Tests workflow.

# Sprint 5 — DirectDamage ClampsAtZeroHealth symmetry pin

Date: 2026-05-07
Branch: `agent/sprint-5-direct-damage-clamp-symmetry`
Base: `5653089` — Sprint 5 DirectDamage + RestoreHealth bundle symmetry
pin (PR #63) on `origin/main`.

Thalamus packet: `pkt_20260507131848_f6582c724c88`
Resolver key: `sha256:a12469968e2a321258d667b50c8cb063431fcafdb07ebba9e8627dfc0ff14d9f`
Vector query present: yes (inline_vector populated, atoms.code/atoms.plan/atoms.memory namespaces)
Query path: vector

## Scope

Test-only EditMode pin. Adds an explicit
`ResolveInstantaneousEffects_DirectDamage_OvershootMagnitudeClampsAtZeroHealth`
regression test that mirrors the existing hand-rolled clamp pins for
the other two direct-drain verbs:

- `ResolveInstantaneousEffects_DirectMana_ClampsAtZeroMana`
- `ResolveInstantaneousEffects_DirectFatigue_ClampsAtZeroFatigue`

Both already build a hand-rolled `SpellDefinition` whose magnitude
deliberately overshoots the target's pool floor, then assert the
drain counter equals only the available pool and the pool ends
exactly at zero. The DirectDamage equivalent — the existing
`ResolveInstantaneousEffects_DirectDamage_ClampsAtZeroHealth` — uses
the `FlameBolt` catalog spell whose magnitude is implementation-
defined; if a future content pass tunes `FlameBolt` below the pool
target's starting health the clamp branch is no longer exercised by
that test. The new hand-rolled pin makes the health-pool clamp
contract explicit and content-independent, mirroring the symmetric
shape of the mana/fatigue variants. The previous symmetry pass
filled the same gap on the restore side
(`RestoreHealth_ClampsAtMaxHealth`, PR #62); this run completes the
matching pair on the drain side.

## Why this increment

- All six instantaneous spell verbs are wired and aggregated and
  every per-verb zero-magnitude / bundled-aggregation pin is now in
  place.
- Per-verb clamp behaviour is pinned hand-rolled for the mana and
  fatigue drain pairs, but the matching health-pool drain pin is
  catalog-magnitude dependent.
- The magic resolver is the layer most likely to be touched as the
  buff/timed-effect code expands; tightening the regression net on
  the simplest clamp path keeps future refactors honest.

## Change set

- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
  - Adds `ResolveInstantaneousEffects_DirectDamage_OvershootMagnitudeClampsAtZeroHealth`.
- `docs/sprint-5-spell-effect-direct-damage-clamp-symmetry.md` (this file).

## Out of scope

- No production code changes.
- No changes to existing tests.
- No catalog, buff, or timed-effect changes.
- No PlayMode changes.

## Test details

```
target  : Ash Rat, ActorRole.Enemy, health=3, mana=4
spell   : new SpellDefinition("direct_damage_clamp_test",
            magnitude=9, DirectDamage)
expect  : result.Success == true
          result.TotalDamage == 3
          target.Vitals.Health.Current == 0
```

The magnitude (9) overshoots the available pool (3), so
`VitalStat.Damage` must clamp the pool at 0 and the resolver must
aggregate only the realised drain of 3 into `TotalDamage`. The
shape mirrors the mana/fatigue clamp pins exactly: hand-rolled
spell, drain magnitude > current pool, expect drain counter ==
starting pool and pool floor == 0.

## Validation

- `git diff --check` clean.
- `./tools/validation/run-validation.sh --mode fallback`.
- Unity EditMode `SpellEffectResolutionServiceTests` runs in CI via
  the existing Unity Tests workflow.

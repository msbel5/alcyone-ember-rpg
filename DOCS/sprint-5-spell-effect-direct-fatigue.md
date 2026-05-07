# Sprint 5 Spell Effect — DirectFatigue

Date: 2026-05-07
Branch: `agent/sprint-5-spell-effect-direct-fatigue`
Base: `407aa56` — Sprint 5 DirectMana instantaneous spell effect kind (PR #56)
merged on `origin/main`.

Thalamus packet: `pkt_20260507061413_f8c45777109f`
Resolver key: `sha256:9656f505ec4e64cf44e7cb58e2640c217e06a5fc463c73918ce094c16f0b5d2f`
Vector query present: yes (inline_vector returned)
Query path: vector

## Scope

This increment adds `DirectFatigue` as a new instantaneous spell effect
kind and wires it through `SpellEffectResolutionService.ResolveInstantaneousEffects`.
It is the symmetric drain counterpart to the `RestoreFatigue` effect —
`RestoreFatigue` heals the target's fatigue pool, `DirectFatigue` damages
it. Until now nothing could deterministically drain an enemy's stamina;
this closes that gap with a target-side fatigue-burn verb that mirrors
the `DirectMana` shape introduced in PR #56.

This is a strict per-effect mutation over the target's `Vitals.Fatigue`
pool via `VitalStat.Damage(amount)`; it does not touch health, mana, the
caster, or trace contracts, and adds no Unity dependency.

Implemented:

- `SpellEffectKind.DirectFatigue = 7` — new enum member appended after
  `DirectMana`. Stable numeric value, additive change.
- `SpellEffectResolutionResult.TotalDirectFatigueDamage` — new int counter
  on the result object. New 9-arg `Ok` factory plus an 8-arg `Ok` overload
  that defaults `totalDirectFatigueDamage` to zero so all existing callers
  compile unchanged. `Fail` initializes the new counter to zero.
- `SpellEffectResolutionService.ResolveInstantaneousEffects` — adds a
  sixth branch in the per-effect loop. On a `DirectFatigue` effect it
  captures `target.Vitals.Fatigue.Current` before the mutation, applies
  `target.Vitals.WithFatigue(target.Vitals.Fatigue.Damage(magnitude))`,
  and adds the pre-minus-post delta to `totalDirectFatigueDamage`. Result
  includes the new total via the 9-arg `Ok` factory.
- `SpellEffectResolutionService.IsSupported` — accepts `DirectFatigue` so
  the validator no longer rejects it as an unsupported effect.
- Validator failure message updated to reflect the six supported kinds.
- Service-level design comment updated to document the new effect path
  and to clarify that `DirectFatigue` here only drains the target's
  fatigue pool with no caster-side feedback.

## Tests added

In `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`:

- `ResolveInstantaneousEffects_DirectFatigue_DrainsTargetFatigue` — one
  `DirectFatigue, magnitude 5` effect against fatigue=12 target leaves
  fatigue=7 and `TotalDirectFatigueDamage=5`; health/mana untouched.
- `ResolveInstantaneousEffects_DirectFatigue_ClampsAtZeroFatigue` —
  magnitude 9 against fatigue=3 leaves fatigue=0 and
  `TotalDirectFatigueDamage=3` (clamped).
- `ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigueAggregatesIndependently`
  — `DirectFatigue 6` then `RestoreFatigue 3` against fatigue=10 yields
  fatigue=7 with separate counters: `TotalDirectFatigueDamage=6`,
  `TotalRestoredFatigue=3`.
- `ResolveInstantaneousEffects_DirectFatigue_ZeroMagnitudeLeavesFatigueUnchanged`
  — magnitude 0 leaves fatigue untouched and counter at 0.

Existing `DirectMana`, `RestoreMana`, `RestoreFatigue`, `RestoreHealth`,
`DirectDamage`, `ShieldBuff`-rejection, and order-preservation tests
still pass unchanged.

## Validation

Local fallback validation:

- `tools/validation/run-validation.sh --mode fallback`
- Result: see PR description for the actual run output.

This is the pure-.NET fallback corpus, not a real local Unity EditMode
run. Unity headless still requires the GitHub PR check matrix.

## Out of scope

- No new spell catalog entry binds `DirectFatigue` yet — this is a
  verb-only increment. A follow-up sprint can add a Stamina Burn template
  plus DM trace once a wider spell catalog overhaul lands.
- No caster-side feedback (lifesteal-style fatigue refund) — the
  increment is target-only by design and matches `DirectMana`'s asymmetry.
- No save/load mapper changes; `DirectFatigue` only mutates an existing
  pool that is already round-tripped by the actor save mapper.

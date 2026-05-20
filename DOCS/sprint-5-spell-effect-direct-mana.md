# Sprint 5 Spell Effect — DirectMana

Date: 2026-05-06
Branch: `agent/sprint-5-spell-effect-direct-mana`
Base: `521882d` — Sprint 5 RestoreMana instantaneous spell effect kind (PR #55)
merged on `origin/main`.

Thalamus packet: `pkt_20260506191708_79ff093fe579`
Resolver key: `sha256:536048fcd51e4bc67d6411422c0fddd15aca0783be4b7d3a26785199a2a7fc0c`
Vector query present: yes (inline_vector returned)
Query path: vector

## Scope

This increment adds `DirectMana` as a new instantaneous spell effect kind and
wires it through `SpellEffectResolutionService.ResolveInstantaneousEffects`.
It is the symmetric drain counterpart to the `RestoreMana` effect added in
PR #55 — `RestoreMana` heals the target's mana pool, `DirectMana` damages
it. Until now the only mana mutation paths were the cast cost paid by
`SpellCastingService` against the caster, and `RestoreMana` against the
target; nothing could mana-burn an enemy. This closes that gap with a
deterministic, target-side drain verb.

This is a strict per-effect mutation over the target's `Vitals.Mana` pool
via `VitalStat.Damage(amount)`; it does not touch health, fatigue, the
caster, or trace contracts, and adds no Unity dependency.

Implemented:

- `SpellEffectCode.DirectMana = 6` — new enum member appended after
  `RestoreMana`. Stable numeric value, additive change.
- `SpellEffectResolutionResult.TotalDirectManaDamage` — new int counter on
  the result object. New 8-arg `Ok` factory plus a 7-arg `Ok` overload that
  defaults `totalDirectManaDamage` to zero so all existing callers compile
  unchanged. `Fail` initializes the new counter to zero.
- `SpellEffectResolutionService.ResolveInstantaneousEffects` — adds a fifth
  branch in the per-effect loop. On a `DirectMana` effect it captures
  `target.Vitals.Mana.Current` before the mutation, applies
  `target.Vitals.WithMana(target.Vitals.Mana.Damage(magnitude))`, and adds
  the pre-minus-post delta to `totalDirectManaDamage`. Result includes the
  new total via the 8-arg `Ok` factory.
- `SpellEffectResolutionService.IsSupported` — accepts `DirectMana` so the
  validator no longer rejects it as an unsupported effect.
- Validator failure message updated to reflect the five supported kinds.
- Service-level design comment updated to document the new effect path and
  to clarify that `DirectMana` here only drains the target's mana pool.

## Tests added

In `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`:

- `ResolveInstantaneousEffects_DirectMana_DrainsTargetMana` — one
  `DirectMana, magnitude 5` effect against mana=14 target leaves
  mana=9 and `TotalDirectManaDamage=5`; health/fatigue untouched.
- `ResolveInstantaneousEffects_DirectMana_ClampsAtZeroMana` — magnitude 9
  against mana=3 leaves mana=0 and `TotalDirectManaDamage=3` (clamped).
- `ResolveInstantaneousEffects_DirectMana_BundledWithRestoreManaAggregatesIndependently`
  — `DirectMana 6` then `RestoreMana 3` against mana=10 yields mana=7 with
  separate counters: `TotalDirectManaDamage=6`, `TotalRestoredMana=3`.
- `ResolveInstantaneousEffects_DirectMana_ZeroMagnitudeLeavesManaUnchanged`
  — magnitude 0 leaves mana untouched and counter at 0.

Existing `RestoreMana`, `RestoreFatigue`, `RestoreHealth`, `DirectDamage`,
`ShieldBuff`-rejection, and order-preservation tests still pass unchanged.

## Validation

Local fallback validation:

- `tools/validation/run-validation.sh --mode fallback`
- Result: `Passed: 593, Failed: 0, Skipped: 0` in 910 ms
- TRX: `validation-output/fallback-test-results/fallback.trx`

This is the pure-.NET fallback corpus, not a real local Unity EditMode
run. Unity headless still requires the GitHub PR check matrix.

## Out of scope

- No new spell catalog entry binds `DirectMana` yet — this is a verb-only
  increment. A follow-up sprint can add a Mana Burn template plus DM
  trace once a wider spell catalog overhaul lands.
- No caster-side feedback (lifesteal-style mana refund) — the increment
  is target-only by design and matches `RestoreMana`'s asymmetry.
- No save/load mapper changes; `DirectMana` only mutates an existing
  pool that is already round-tripped by the actor save mapper.

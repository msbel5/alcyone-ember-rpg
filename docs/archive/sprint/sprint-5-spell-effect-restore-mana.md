# Sprint 5 Spell Effect — RestoreMana

Date: 2026-05-06
Branch: `agent/sprint-5-spell-effect-restore-mana`
Base: `d76a78c` — Sprint 5 stacked-filter map-level From overload for shield-buff
absorption totals (PR #54) merged on `origin/main`

Thalamus packet: `pkt_20260506171740_48fe4354c897`
Resolver key: `sha256:2c295e9adb017d74c67d6a531254aa4c63ef67424fcbff64a47c289b150f3cb6`
Vector query present: yes (inline_vector returned)
Query path: vector

## Scope

This increment adds `RestoreMana` as a new instantaneous spell effect kind and
wires it through `SpellEffectResolutionService.ResolveInstantaneousEffects`,
mirroring the existing `RestoreHealth` and `RestoreFatigue` paths. The
existing magic foundation already tracks a separate `Vitals.Mana` pool with
`Damage` / `Restore` semantics; the previous instantaneous effect set
(`DirectDamage`, `RestoreHealth`, `RestoreFatigue`) only mutated health and
fatigue. Until now, no spell effect could refill a target's mana — only the
caster's mana was ever touched, and only as the cast cost paid by
`SpellCastingService`. This increment closes that gap with a deterministic,
target-side restoration verb.

This is a strict per-effect mutation over the target's `Vitals.Mana` pool;
it does not refund cast cost, does not touch the caster, does not change
trace contracts, and does not introduce any Unity dependency.

Implemented:

- `SpellEffectCode.RestoreMana = 5` — new enum member appended after
  `ShieldBuff`. Stable numeric value, additive change.
- `SpellEffectResolutionResult.TotalRestoredMana` — new int counter on the
  result object. New 7-arg `Ok` factory plus a 6-arg `Ok` overload that
  defaults `totalRestoredMana` to zero so all existing callers compile
  unchanged. `Fail` initializes the new counter to zero.
- `SpellEffectResolutionService.ResolveInstantaneousEffects` — adds a fourth
  branch in the per-effect loop. On a `RestoreMana` effect it captures
  `target.Vitals.Mana.Current` before the mutation, applies
  `target.Vitals.WithMana(target.Vitals.Mana.Restore(magnitude))`, and adds
  the post-mutation delta to `totalRestoredMana`. Result includes the new
  total via the 7-arg `Ok` factory.
- `SpellEffectResolutionService.IsSupported` — accepts `RestoreMana` so the
  validator no longer rejects it as an unsupported effect.
- Validator failure message updated to reflect the four supported kinds.
- Service-level design comment updated to document the new effect path and
  to clarify that `RestoreMana` here only restores the target's mana pool
  and does not refund the caster's cast cost.

Behavior:

- `RestoreMana` effect with magnitude `m` against a target whose mana is
  below max raises `target.Vitals.Mana.Current` by `min(m, Max - Current)`.
  The reported `TotalRestoredMana` equals the actual delta applied.
- Restoration clamps at `Vitals.Mana.Max`. A target already at full mana
  reports `TotalRestoredMana = 0` and is left untouched.
- A zero-magnitude `RestoreMana` effect is permitted (matches the existing
  `RestoreFatigue` convention from `SpellEffectSpec`'s `magnitude >= 0`
  guard), reports `TotalRestoredMana = 0`, and leaves the mana pool
  unchanged.
- Bundled spells that combine `RestoreHealth`, `RestoreFatigue`, and
  `RestoreMana` aggregate per-kind: each per-effect mutation lands in its
  own counter and on its own vital pool. Cross-pool deltas never bleed
  across counters.
- Existing failure paths (null cast, failed cast, null/dead target,
  non-instantaneous effect, unsupported effect such as instantaneous
  `ShieldBuff`) continue to short-circuit before any mana mutation. A
  rejection leaves `TotalRestoredMana` at zero and the target's mana pool
  untouched.
- The caster's mana is unaffected by `RestoreMana`. The caster's cast cost
  was already paid up front by `SpellCastingService.TryCast`; this resolver
  does not refund it.

Out of scope, deliberately deferred:

- A starter catalog spell that uses `RestoreMana`. `SliceSpellCatalog` is
  not modified. The effect kind exists, is wired into the resolver, and is
  test-covered through ad-hoc `SpellDefinition` instances.
- Self-targeting variants. Same single-target shape as `RestoreHealth` /
  `RestoreFatigue`.
- Mana-restore-over-time. Timed mana effects would need a separate
  registry slot like `ShieldBuffState`; not in this slice.
- Mana-cost refund opcodes. `RestoreMana` is target-side only.

## Tests

`Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs` adds:

- `ResolveInstantaneousEffects_RestoreMana_RestoresTargetMana` — single
  effect raises target mana from 6 to 13 with a magnitude-7 effect and a
  20-cap pool; confirms `TotalRestoredMana = 7`, all other counters zero,
  health and fatigue untouched.
- `ResolveInstantaneousEffects_RestoreMana_ClampsAtMaxMana` — magnitude-8
  effect against a 17/20 mana target reports `TotalRestoredMana = 3` and
  leaves mana at 20.
- `ResolveInstantaneousEffects_RestoreMana_BundledWithOtherEffectsAggregatesPerKind`
  — three-effect spell (`RestoreMana 6`, `RestoreFatigue 2`,
  `RestoreHealth 3`) lands each delta in its own counter and pool with
  no cross-bleed.
- `ResolveInstantaneousEffects_RestoreMana_ZeroMagnitudeLeavesManaUnchanged`
  — magnitude-0 `RestoreMana` is accepted and leaves both the counter and
  the target mana pool at zero / unchanged.

## Validation

Local fallback harness:

```
./tools/validation/run-validation.sh --mode fallback
```

Result: `Passed! - Failed: 0, Passed: 589, Skipped: 0, Total: 589`.
The previous baseline on `main` reported 585 passing tests; this increment
adds the four `RestoreMana` tests above. `git diff --check` is clean.

## Files

- `Assets/Scripts/Domain/Magic/SpellEffectCode.cs`
- `Assets/Scripts/Simulation/Magic/SpellEffectResolutionResult.cs`
- `Assets/Scripts/Simulation/Magic/SpellEffectResolutionService.cs`
- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`
- `docs/sprint-5-spell-effect-restore-mana.md`

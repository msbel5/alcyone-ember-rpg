# Sprint 5 — DirectFatigue Overshoot Clamp Symmetry

## Scope
Pin the symmetric counterpart to the DirectMana overshoot clamp test for the
`DirectFatigue` instantaneous spell effect kind: when magnitude exceeds the
target's current fatigue pool, the effect must drain only what is available
and clamp the pool at zero, while `TotalDirectFatigueDamage` reflects the
actual amount consumed (not the requested magnitude).

## Why
The DirectMana overshoot semantics are pinned in
`sprint-5-spell-effect-direct-mana-overshoot-clamp-symmetry.md` (PR #65). The
fatigue family already has `ClampsAtZeroFatigue` for the standard partial
overshoot (magnitude 9 vs pool 3 → clamps to 3) but no test for an
overshoot that vastly exceeds the pool (magnitude 11 vs pool 2 → clamps to 2).
Adding the parallel test guarantees that future refactors of the fatigue
resolution path keep the same drain-only-what-is-there contract that mana
already enforces, and prevents accidental drift between the two pools.

## Change
- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs` —
  add `ResolveInstantaneousEffects_DirectFatigue_OvershootMagnitudeClampsAtZeroFatigue`
  mirroring the existing DirectMana overshoot test:
  - target fatigue = 2, magnitude = 11
  - expects `TotalDirectFatigueDamage == 2` and
    `target.Vitals.Fatigue.Current == 0`.

## Risk
None — pure test addition. No production code touched.

## Validation
- `git diff --check` clean.
- `./tools/validation/run-validation.sh --mode fallback` (Unity unavailable on
  Pi runner; fallback mode validates static structure).

## Thalamus
- packet_id: `pkt_20260507151634_7a9a8579ad11`
- resolver_key: `sha256:e8df71f501772f6bd3e17a4acadde7f342603b206b6c2e4deb02b068d6f28c2e`
- vector_query_present: true
- query_path: vector

## Next
Mirror the same overshoot pin for the third bilateral pool — `DirectDamage`
overshoot with very-low health (target health = 1, magnitude = 11) — to
complete the three-pool symmetry triad before moving to bundled
direct/restore overshoot interactions.

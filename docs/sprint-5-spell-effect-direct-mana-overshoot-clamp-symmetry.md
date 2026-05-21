# Sprint 5 — Spell Effect: DirectMana overshoot clamp-at-zero-mana symmetry pin

## Increment

Adds an explicit hand-rolled EditMode regression test
`SpellEffectResolutionServiceTests.ResolveInstantaneousEffects_DirectMana_OvershootMagnitudeClampsAtZeroMana`
mirroring the symmetric overshoot pin already in place for
`DirectDamage_OvershootMagnitudeClampsAtZeroHealth` (#64).

The previously-existing `DirectMana_ClampsAtZeroMana` test already
exercises the same clamp boundary using a 9-magnitude drain on a
3-mana target, but it does not pin the contract under an explicit
"OvershootMagnitude" naming. This pin adds a distinct hand-rolled
scenario (11-magnitude drain on a 2-mana target) so the mana-pool
clamp contract for `DirectMana` is now anchored under both naming
conventions, matching the DirectDamage symmetry exactly.

## Contract pinned

When `ResolveInstantaneousEffects` applies a `DirectMana` effect whose
magnitude exceeds the target's current mana pool:
- `Success` is `True`.
- `AppliedEffectCount` is `1`.
- `TotalDirectManaDamage` equals the target's prior mana pool (clamped
  drain), not the raw effect magnitude.
- `target.Vitals.Mana.Current` is exactly `0` afterwards.

## Why

The Sprint 5 magic foundation deliberately keeps health, fatigue, and
mana drain/restore effects deterministic and pool-bounded. Symmetric
overshoot pins make it impossible for a future refactor to silently
allow `TotalDirectManaDamage` to leak past the target's available
mana, even when the catalog does not yet expose a DirectMana spell.

## Scope

- Tests only — no production code, catalog, or buff changes.
- Single file modified: `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`.

## Thalamus

- Packet: `pkt_20260507141716_5b431cce73d9`
- Resolver: `sha256:64c61b9bbeff87a1a4247e77f7f7bc42b9848f968bf0501d382941ee12fd4f97`
- Vector query present: true
- Query path: vector
- Inline vector: 1024 dims, namespace `atoms.code`, model `qwen3-embedding-0.6b-q4_0`

## Validation

`./tools/validation/run-validation.sh --mode fallback`

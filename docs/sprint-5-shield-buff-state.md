# Sprint 5 Shield Buff State

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-state`
Base: `6f31cbb` — Sprint 5 spell catalog cooldowns merged on `origin/main`

## Scope

The `SliceSpellCatalog` already declares `EmberWardCooldownTicks = 30` to match the
`ShieldBuff` duration of the Ember Ward spell, but there is no place to actually
record an active shield buff against an actor. The `SpellEffectResolutionService`
still rejects non-instantaneous effects with `NonInstantaneousEffect`, and that
will remain true until the resolution chain learns about timed buffs.

This slice introduces the deterministic state container — `ShieldBuffState` —
that timed buff resolution will write into. It is the foundation slice: state
shape, validation, and lookups only. It does not change resolution semantics,
does not tick anything down, and does not yet wire into save/load.

Implemented:

- `Assets/Scripts/Domain/Magic/ShieldBuffState.cs` — pure-Domain mutable bag,
  parallel in shape to `SpellCooldownState` but with two values per entry:
  - `RemainingTicks`
  - `Magnitude` (the absorbed-damage value declared by the spell)
  - API: `GetRemainingTicks`, `GetMagnitude`, `IsActive`,
    `GetTrackedSpellTemplateIds`, `SetActiveBuff(id, remainingTicks, magnitude)`,
    `Clear(id)`. Setting `remainingTicks = 0` removes the entry to keep state
    canonical.
  - Validation: blank id, negative ticks, and negative magnitude all throw, so
    bad inputs cannot silently corrupt buff state.
- `Assets/Tests/EditMode/Magic/ShieldBuffStateTests.cs` — 11 EditMode tests
  pinning untracked-spell defaults, set/replace, zero-tick removal, blank-id /
  negative-input rejection, `Clear` behavior (including no-op for unknown /
  null / whitespace ids), and the tracked-id list contents.

## Why this slice matters

EmberWard is the only catalog spell with a timed effect, but until now there was
no domain-side place to record "this actor currently has a shield buff for N
ticks that absorbs M damage." Adding that container — small, validated, and
fully isolated from Unity — gives the next slice (apply-on-cast and tick-down
resolution) something concrete to write into without simultaneously inventing
the storage shape and the resolution rules. The same shape will later be the
serialization unit for the save mapper, mirroring how `SpellCooldownState`
graduated from foundation → execution rejection → persistence in earlier
Sprint 5 slices.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: foundation stays in the Layer 3 deterministic
  gameplay band; `ShieldBuffState` has no `UnityEngine` reference.
- `docs/EMBER_VISION_BIBLE.md` §8: another narrow, testable Sprint 5 magic
  increment, not a balance pass.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: Magic effects/opcodes —
  shield buff is the timed verb the foundation must eventually carry.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` produced no output.
- Fallback validation passed: `Passed: 220, Failed: 0, Skipped: 0, Total: 220`.
  Previous baseline on `origin/main` was `209/209`; this slice added 11 new
  tests in `ShieldBuffStateTests`.

## Release Evidence

- Branch: `agent/sprint-5-shield-buff-state`
- Local fallback baseline before slice: `209 / 209`
- Local fallback baseline after slice: `220 / 220`
- See PR for commit hashes and CI status when opened.

## Caveats

- Foundation-only. `SpellEffectResolutionService` still rejects shield-buff
  effects with `NonInstantaneousEffect`. Application, tick-down, and damage
  absorption are intentionally out of scope for this slice.
- No save/load integration yet. A follow-up mapper slice (parallel to
  `SpellCooldownSaveMapper`) will carry buff state across save/load.
- No actor-keyed wrapper. This slice tracks state per spell template id; how
  it is attached to an `ActorRecord` is a later decision.
- Local validation remains the pure .NET fallback harness, not a real local
  Unity Editor / EditMode run.

## Thalamus Provenance

- `thalamus_packet_id`: `pkt_20260505021708_3a72e26c4db0`
- `thalamus_resolver_key`: `sha256:9315ea1f32390915a3aee598a3c8d871188a4d721e93f19c1cf9953710965258`
- Vector query was present (1024-dim, namespace `atoms.code`,
  `qwen3-embedding-0.6b-q4_0`).

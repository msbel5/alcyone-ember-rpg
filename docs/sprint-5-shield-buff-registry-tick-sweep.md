# Sprint 5 Shield Buff Registry Tick Sweep

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-registry-tick-sweep`
Base: `813fe8e` — Sprint 5 ShieldBuffStateRegistry merged on `origin/main`

## Scope

`ShieldBuffStateRegistry` already lazily owns one `ShieldBuffState` per
stable actor id, and `ShieldBuffService.AdvanceTicks(ShieldBuffState,
int)` already decays a single bag in place. The wider simulation tick
loop still has to enumerate the registry itself to advance every
actor's bag in the right order. This slice introduces the deterministic
actor-keyed tick-down sweep on the existing service so a future combat
or world tick loop can advance the entire shield-buff surface across
all tracked actors through one call.

This slice is glue-only. It does not change `ShieldBuffState`, does not
change the application paths (cast/roll/effect resolution/shield-buff
application), does not introduce new decay rules, does not alter
save/load, and does not absorb any damage. Per-actor bags continue to
be created lazily through `ShieldBuffStateRegistry.GetOrCreate`; the
sweep itself never creates new actor entries.

Implemented:

- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs` — adds
  `AdvanceTicksForAllActors(ShieldBuffStateRegistry registry, int
  elapsedTicks)`:
  - null registry → `ArgumentNullException`.
  - negative `elapsedTicks` → `ArgumentOutOfRangeException`.
  - `elapsedTicks == 0` → no-op (mirrors single-bag `AdvanceTicks`).
  - otherwise enumerates `registry.GetTrackedActorIds()` and forwards
    each non-null bag to the existing single-bag `AdvanceTicks`. The
    sweep uses `GetOrNull` so it never materializes new actor state.
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceRegistrySweepTests.cs`
  — 9 EditMode tests pinning:
  - null registry guard.
  - negative elapsed throws.
  - zero elapsed is a no-op for a registered actor's bag.
  - empty registry is a safe no-op.
  - multi-actor decay reduces each actor's remaining ticks
    independently and preserves each magnitude.
  - per-actor expiry only removes the entries that hit zero on that
    actor, leaving other actors' active buffs untouched.
  - per-actor parity vs stand-alone `AdvanceTicks` on equivalent input
    state across multiple actors and overlapping spell ids.
  - repeated sweep calls accumulate decay per actor.
  - actor with an empty bag stays empty after a sweep.

## Why this slice matters

Up to this point any caller advancing shield buffs across multiple
actors had to enumerate the registry itself and remember which bag
each actor owned. With the sweep in place, callers can hand the
service the registry directly and still get exactly the same per-bag
decay semantics that single-bag `AdvanceTicks` already pins. This
keeps the planned next slices (actor-keyed application wiring,
damage absorption, and combat-loop integration) focused on their own
behavior rather than on per-tick plumbing.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3 Layer 3: pure Simulation seam, no
  Unity types, no presentation coupling.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: timed magic effects
  decay deterministically; the sweep preserves that contract by
  delegating to the existing single-bag path without modification.
- PRD Sprint 1 FR-06: deterministic save round-trip is preserved; the
  sweep does not touch save state and does not change which bags a
  registry tracks.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` produced no output.
- Fallback validation passed:
  `Passed: 289, Failed: 0, Skipped: 0, Total: 289`.

## Next dependent slices

- Actor-keyed application wiring: thread an `actorId` through
  `ShieldBuffApplicationService` so successful casts route into
  `ShieldBuffStateRegistry.GetOrCreate(actorId)` instead of a single
  shared `ShieldBuffState`.
- Save-mapper extension so the JSON save layer round-trips the full
  per-actor registry, not just a single shared bag.
- Damage absorption that consumes the defender's own shield-buff
  magnitude on incoming damage, ahead of HP reduction.
- Combat-loop integration that calls
  `ShieldBuffService.AdvanceTicksForAllActors` once per encounter
  tick alongside cooldown decay (eventual extension of
  `MagicTickDriver` to take a registry instead of a single bag).

## Thalamus

- packet_id: `pkt_20260505081719_5e6aab4e8595`
- resolver_key:
  `sha256:0687f0b82b301bdc6706fa53f6c81e5fdd0c07ccea2ff4bb383a36a138a816a8`
- inline_vector: present (1024-dim, namespace `atoms.code`,
  model `qwen3-embedding-0.6b-q4_0`)
- query_path: vector
- escalation_reason: complex planning/design task requires Captain
  decomposition (handled directly by Captain — small <100 LOC slice
  in pure Simulation mirroring an existing single-bag pattern, no
  Builder spawn).

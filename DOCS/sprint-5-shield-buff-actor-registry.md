# Sprint 5 Shield Buff Actor Registry

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-actor-registry`
Base: `a616270` — Sprint 5 MagicTickDriver merged on `origin/main`

## Scope

`ShieldBuffState` already tracks active timed shield buffs by stable
spell template id, and `MagicTickDriver` advances cooldown + shield-buff
state through one entry point. Today both still operate on a single
shared `ShieldBuffState` instance, which is fine for single-caster
exercises but does not survive multiple actors carrying independent
buff bags. This slice introduces the deterministic per-actor seam
that the planned actor-keyed shield wiring needs.

This slice is foundation-only. It does not apply, tick, save, or
absorb damage. `ShieldBuffApplicationService`, `ShieldBuffService`,
`MagicTickDriver`, and the JSON save layer continue to consume a
single `ShieldBuffState` directly until the follow-up slices wire
them through the registry.

Implemented:

- `Assets/Scripts/Domain/Magic/ShieldBuffStateRegistry.cs` — new
  pure-Domain registry that lazily owns one `ShieldBuffState` per
  stable actor id:
  - `HasState(actorId)` returns false for null/empty/whitespace ids
    and for actors with no state.
  - `GetOrCreate(actorId)` throws `ArgumentException` for
    null/empty/whitespace ids and otherwise returns the existing bag
    or creates and stores a fresh `ShieldBuffState`.
  - `GetOrNull(actorId)` is a non-mutating lookup, returning null for
    null/empty/whitespace ids and for untracked actors.
  - `GetTrackedActorIds()` enumerates the actors with state in
    deterministic dictionary order.
  - `Remove(actorId)` drops a single actor's bag and is a no-op for
    null/empty/whitespace ids and untracked actors.
- `Assets/Tests/EditMode/Magic/ShieldBuffStateRegistryTests.cs` —
  18 EditMode tests pinning:
  - `HasState` for untracked actors and for null/empty/whitespace
    ids.
  - `GetOrCreate` argument validation, fresh-state shape, idempotency
    on the same id, and distinct instances per actor.
  - state preservation across `GetOrCreate` round-trips and isolation
    between distinct actors.
  - `GetOrNull` returning null for untracked actors and for invalid
    ids, returning the stored instance when present, and never
    creating new state.
  - `GetTrackedActorIds` empty-by-default and full enumeration after
    multiple `GetOrCreate` calls.
  - `Remove` dropping state, being a safe no-op for unknown or
    invalid ids, leaving other actors intact, and allowing a fresh
    `GetOrCreate` to start over.

## Why this slice matters

Up to this point the magic stack assumes a single shield-buff bag
shared by whoever is casting in the current scene. With the registry
in place, the next slice can wire `ShieldBuffApplicationService` and
`ShieldBuffService` through actor ids without touching `ShieldBuffState`
itself, and the future damage-absorption slice can resolve incoming
damage against the defender's own bag. Keeping the registry separate
from `ShieldBuffState` preserves the per-spell-id contract that the
save mapper and tick-down service already rely on.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3 Layer 3: pure Domain/Simulation
  separation. The registry stays in `Domain.Magic` with no Unity or
  Simulation dependency.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: timed magic effects
  remain per-actor; the registry keys those effects to stable actor
  ids without changing how each bag decays.
- PRD Sprint 1 FR-06: deterministic save round-trip is preserved;
  the registry does not touch save state in this slice — the existing
  save mapper continues to operate on a single `ShieldBuffState`.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --cached --check` produced no output.
- Fallback validation passed:
  `Passed: 280, Failed: 0, Skipped: 0, Total: 280`.

## Next dependent slices

- Actor-keyed shield-buff wiring: thread an `actorId` through
  `ShieldBuffApplicationService` and `ShieldBuffService` and have
  them resolve the right bag via `ShieldBuffStateRegistry.GetOrCreate`.
- Save-mapper extension so the JSON save layer round-trips the full
  per-actor registry, not just a single shared bag.
- Damage absorption that consumes the defender's own shield-buff
  magnitude on incoming damage.

## Thalamus

- packet_id: `pkt_20260505071637_475966ca61dc`
- resolver_key:
  `sha256:619116d4c9032b0d82ae0afbf7e3300a51b002311ab1ba3f7a1991a01dca5675`
- inline_vector: present (1024-dim, namespace `atoms.code`,
  model `qwen3-embedding-0.6b-q4_0`)
- query_path: vector
- escalation_reason: complex planning/design task requires Captain
  decomposition (handled directly by Captain — small <100 LOC slice
  in pure Domain mirroring an existing dictionary pattern, no
  Builder spawn).

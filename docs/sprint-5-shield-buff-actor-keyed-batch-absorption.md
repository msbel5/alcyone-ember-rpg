# Sprint 5 Shield Buff Actor-Keyed Batch Absorption

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-actor-keyed-batch-absorption`
Base: `ce79436` — Sprint 5 actor-keyed shield buff damage absorption registry overload (PR #36) merged on `origin/main`

## Scope

This increment adds the deterministic batch dispatcher for the actor-keyed shield-buff damage
absorption seam introduced by PR #36. A future combat damage-resolution pass can absorb shield
buffs across multiple damaged actors in a single deterministic call without enumerating
`ShieldBuffStateRegistry` itself. This mirrors the `AdvanceTicks` → `AdvanceTicksForAllActors`
pattern already established by the tick-down side of the registry.

Implemented:

- `ShieldBuffService.AbsorbDamageForActors(ShieldBuffStateRegistry registry,
  IReadOnlyDictionary<string,int> incomingDamageByActorId)` returning
  `IReadOnlyDictionary<string, ShieldBuffAbsorptionResult>`.
- Dispatcher delegates to the existing single-actor `AbsorbDamageForActor` for every input
  entry, preserving the per-buff consume order, magnitude-exhaust expiry, and trace contract
  per actor.

Behavior:

- `null` registry &rarr; `ArgumentNullException`.
- `null` incoming map &rarr; `ArgumentNullException`.
- `null`/whitespace/empty actor key in the input map &rarr; `ArgumentException`.
- negative damage value in the input map &rarr; `ArgumentOutOfRangeException`.
- empty input map &rarr; empty result map; the registry is not enumerated and not mutated.
- untracked actor in the input map &rarr; result with `RemainingDamage == IncomingDamage`,
  empty consumed/expired traces, and the actor is NOT lazily added to the registry. This
  mirrors `AdvanceTicksForAllActors`'s registry-read-only invariant.
- result keys mirror the input keys exactly, so callers can reason about the result map
  purely from their own input rather than registry contents.
- registry actors that are not present in the input map are not touched — their buff bags
  retain their magnitude and remaining ticks unchanged.

Pure Simulation: no Unity types, no save coupling, no tick mutation, no combat-pipeline call.

## Why this slice matters

PR #36 established the per-damaged-actor absorption seam. Combat damage resolution rarely
hits exactly one actor per call — area effects, multi-target spells, cleave hits, and
chain damage all need an absorption pass across a set of damaged actors. The batch
dispatcher closes that gap with a narrow, fully deterministic, registry-read-only
contract that callers can drive from their own per-tick damage map.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 remains Layer 3 gameplay mechanics; the new
  dispatcher is pure Domain/Simulation orchestration with no AI dependency.
- `docs/EMBER_VISION_BIBLE.md` §8: Sprint 5 keeps shipping narrow, testable magic
  increments instead of pretending the full combat-absorption stack is wired.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: shield-buff absorption now composes
  with the existing actor-keyed registry without changing per-buff resolution rules.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` passed with no output.
- Fallback validation passed: `Passed: 358, Failed: 0, Skipped: 0, Total: 358`
  (342 prior + 16 new EditMode cases for the batch dispatcher).

## Caveats

- The dispatcher does not introduce a registry-wide enumeration: it strictly walks the
  input map. Callers that want to absorb damage against every tracked actor must pass
  the keys themselves. This is intentional — combat damage is per-incoming-event, not
  a uniform registry sweep.
- Combat-pipeline integration remains a later increment; this slice only exposes the
  seam.
- Unity Editor / PlayMode validation is still blocked on this Pi because the Unity
  editor binary is not installed here.
- Save/load, application, tick-down, resistances, saving throws, and cooldown
  integration are unchanged by this slice.

## Thalamus Provenance

- packet_id: `pkt_20260505141625_9627aff26e7b`
- resolver_key: `sha256:111a271c41f09e8d9a2ed69107a458cc2b92dca47f0c0d37d3c83c04b002d180`
- query_path: vector (1024-dim inline_vector returned by thalamus_route)
- vector_query_present: true

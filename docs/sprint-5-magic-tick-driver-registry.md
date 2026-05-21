# Sprint 5 Magic Tick Driver Registry Overload

Date: 2026-05-05
Branch: `agent/sprint-5-magic-tick-driver-registry`
Base: `44052ca` — Sprint 5 ShieldBuffService.AdvanceTicksForAllActors merged on `origin/main`

## Scope

`MagicTickDriver` already advances both the shared cooldown bag and a
single `ShieldBuffState` in one deterministic call through its
existing `AdvanceTicks(SpellCooldownState, ShieldBuffState, int)`
overload. After the actor-keyed registry sweep landed on
`ShieldBuffService.AdvanceTicksForAllActors(ShieldBuffStateRegistry,
int)`, the wider simulation tick loop still has to call cooldown decay
and the per-actor sweep separately. This slice adds the registry-aware
driver overload `AdvanceTicks(SpellCooldownState,
ShieldBuffStateRegistry, int)` so a future combat/world tick can
advance both surfaces — shared cooldown and every actor's per-actor
shield-buff bag — through a single driver call.

This slice is orchestration-only. It does not change `ShieldBuffState`,
does not change `ShieldBuffStateRegistry`, does not change
`SpellCooldownService.AdvanceTicks`, does not change
`ShieldBuffService.AdvanceTicksForAllActors`, does not introduce new
decay rules, does not alter application paths, does not touch
save/load, and does not absorb any damage. Per-actor bags continue to
be created lazily through `ShieldBuffStateRegistry.GetOrCreate`; the
new overload never materializes new actor entries.

Implemented:

- `Assets/Scripts/Simulation/Magic/MagicTickDriver.cs` — adds
  `AdvanceTicks(SpellCooldownState, ShieldBuffStateRegistry, int)`:
  - null cooldown state → `ArgumentNullException`.
  - null registry → `ArgumentNullException`.
  - negative `elapsedTicks` → `ArgumentOutOfRangeException`.
  - `elapsedTicks == 0` → no-op (mirrors single-bag overload).
  - otherwise calls `_spellCooldownService.AdvanceTicks(cooldownState,
    elapsedTicks)` and `_shieldBuffService.AdvanceTicksForAllActors(
    shieldBuffStateRegistry, elapsedTicks)` in that order.
- `Assets/Tests/EditMode/Magic/MagicTickDriverRegistryTests.cs` —
  10 EditMode tests pinning:
  - null cooldown state guard.
  - null registry guard (with explicit `(ShieldBuffStateRegistry)null`
    cast to disambiguate from the single-bag overload).
  - negative elapsed throws.
  - zero elapsed is a no-op for cooldown and a registered actor's bag.
  - empty registry still advances cooldown (and stays empty).
  - multi-actor decay reduces each actor's remaining ticks
    independently and preserves each magnitude.
  - per-actor expiry only removes the entries that hit zero on that
    actor, leaving other actors' active buffs untouched.
  - repeated overload calls accumulate decay across cooldown and every
    actor's bag.
  - parity vs the existing single-bag overload run per actor across
    multiple actors and overlapping spell ids.
  - per-side parity vs `SpellCooldownService.AdvanceTicks` and
    `ShieldBuffService.AdvanceTicksForAllActors` independently.
- `Assets/Tests/EditMode/Magic/MagicTickDriverTests.cs` — disambiguates
  the existing `AdvanceTicks_NullShieldBuffState_Throws` test by
  casting `null` to `(ShieldBuffState)null`. Same observable behavior;
  the cast is required because the new registry overload would
  otherwise make the `null` literal call ambiguous between the two
  overloads.

## Why this slice matters

Up to this point any caller advancing the magic surface across actors
had to call `MagicTickDriver.AdvanceTicks` for the cooldown side and
`ShieldBuffService.AdvanceTicksForAllActors` for the registry side
itself. With the registry-aware driver overload in place, callers can
hand the driver the cooldown state and the registry directly and still
get exactly the same per-side decay semantics that the underlying
service calls already pin. This keeps the planned next slices
(actor-keyed shield-buff application wiring, save-mapper extension to
round-trip the per-actor registry, damage absorption against the
defender's own bag, and combat-loop integration) focused on their own
behavior rather than on per-tick plumbing.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3 Layer 3: pure Simulation seam, no
  Unity types, no presentation coupling.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: timed magic effects
  decay deterministically; the new overload preserves that contract by
  delegating to existing services without modification and ordering
  cooldown decay before the shield-buff sweep, identical to the
  existing single-bag overload's order.
- PRD Sprint 1 FR-06: deterministic save round-trip is preserved; the
  new overload does not touch save state and does not change which
  bags a registry tracks.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` produced no output.
- Fallback validation passed:
  `Passed: 299, Failed: 0, Skipped: 0, Total: 299` (was 289 on
  `origin/main`; +10 from `MagicTickDriverRegistryTests`).

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
  `MagicTickDriver.AdvanceTicks(SpellCooldownState,
  ShieldBuffStateRegistry, int)` once per encounter tick.

## Thalamus

- packet_id: `pkt_20260505091715_e4beeb6648a4`
- resolver_key:
  `sha256:4bb485a6e1344cf34b7f6aa7bdd5daf6149b1299e0c500f8267922e9d3e68772`
- inline_vector: present (1024-dim, namespace `atoms.code`,
  model `qwen3-embedding-0.6b-q4_0`)
- query_path: vector
- escalation_reason: complex planning/design task requires Captain
  decomposition (handled directly by Captain — small ~80 LOC slice in
  pure Simulation mirroring existing single-bag and registry-sweep
  patterns; no Builder spawn).

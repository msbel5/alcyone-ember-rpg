# Sprint 5 Shield Buff Actor-Keyed Application

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-actor-keyed-application`
Base: `44052ca` — Sprint 5 ShieldBuffStateRegistry tick sweep merged on `origin/main`

## Scope

`SpellEffectResolutionService.ApplyShieldBuffs(SpellCastResult,
ShieldBuffState)` already records timed shield-buff effects from a
successful cast into a single shared `ShieldBuffState` bag, and
`ShieldBuffStateRegistry` already lazily owns one bag per stable actor
id. This slice introduces an actor-keyed application overload that
takes the registry plus an `actorId` and routes the writes into
`ShieldBuffStateRegistry.GetOrCreate(actorId)` — the per-actor bag
owned by that actor — by delegating to the existing single-bag
overload.

This slice is glue-only. It does not change `ShieldBuffState`, does
not change `ShieldBuffStateRegistry`, does not introduce new
application semantics (replacement still pins via the existing
single-bag path), does not change tick-down, save/load, or damage
absorption. The single-bag overload is the source of truth for the
write rules; the registry overload is pure delegation after a cast
guard plus argument validation.

Implemented:

- `Assets/Scripts/Simulation/Magic/SpellEffectResolutionService.cs` —
  adds `ApplyShieldBuffs(SpellCastResult, ShieldBuffStateRegistry,
  string actorId)`:
  - null/failed cast or missing `Spell` → `InvalidCast` rejection
    that mirrors the existing single-bag overload (returns
    `Spell == null`, registry untouched).
  - null registry → `ArgumentNullException`.
  - whitespace `actorId` → `ArgumentException` (matches the registry
    contract for `GetOrCreate`).
  - otherwise resolves the bag through
    `ShieldBuffStateRegistry.GetOrCreate(actorId)` and forwards to the
    existing single-bag overload, so the result shape and contracts
    are identical to direct single-bag calls.
- `Assets/Tests/EditMode/Magic/ShieldBuffActorKeyedApplicationServiceTests.cs`
  — 10 EditMode tests pinning:
  - Ember Ward writes into the caster's own actor bag with the
    expected magnitude/duration.
  - other actors' bags are not touched (multi-actor isolation).
  - re-cast on the same actor replaces that actor's existing entry.
  - mixed-effect spell only writes the timed shield-buff effect.
  - Flame Bolt cast produces no buff writes; lazy bag creation still
    occurs once the cast guard passes (no special-case empty bag
    branch).
  - null cast is rejected and the registry stays empty.
  - failed cast (e.g., `InsufficientMana`) is rejected and the
    registry stays empty.
  - null registry throws `ArgumentNullException`.
  - whitespace actor id throws `ArgumentException` and the registry
    stays empty.
  - parity with the existing single-bag overload on the same input
    state (same `Success`, `Error`, applied counts, magnitude, and
    duration; same per-spell remaining ticks and magnitude on the
    actor bag vs a stand-alone `ShieldBuffState`).

## Why this slice matters

Up to this point shield-buff application required the caller to hold
or fabricate a single `ShieldBuffState` bag, which is fine for the
caster-self vertical slice but does not scale to a world with multiple
casters. With the registry overload in place, callers (eventual combat
loop, scripted encounters, and save-load reconstruction) hand the
service the registry plus the casting actor's id and still get exactly
the same per-bag write semantics that the single-bag overload already
pins. The next planned slices — save-mapper extension for the full
per-actor registry, damage absorption, and combat-loop integration —
can now consume per-actor buff state without inventing their own
routing.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3 Layer 3: pure Simulation seam, no
  Unity types, no presentation coupling.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: timed magic effects
  are written deterministically; the registry overload preserves that
  contract by delegating to the existing single-bag path.
- PRD Sprint 1 FR-06: deterministic save round-trip is preserved; the
  overload does not touch save state.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` produced no output.
- Fallback validation passed:
  `Passed: 299, Failed: 0, Skipped: 0, Total: 299` (was 289 before
  this slice; +10 new EditMode tests).

## Next dependent slices

- Save-mapper extension so the JSON save layer round-trips the full
  per-actor registry, not just a single shared bag.
- Damage absorption that consumes the defender's own shield-buff
  magnitude on incoming damage, ahead of HP reduction.
- Combat-loop integration that calls the registry overload of
  `ApplyShieldBuffs` for the casting actor and the registry overload
  of `ShieldBuffService.AdvanceTicksForAllActors` per encounter tick
  (eventual extension of `MagicTickDriver` to take a registry instead
  of a single bag).

## Thalamus

- packet_id: `pkt_20260505101718_673a9782e719`
- resolver_key:
  `sha256:e9e5595d07730b9932555bcd6b54d304507d2be8cae3efb2d9c804643eaf0f66`
- inline_vector: present (1024-dim, namespace `atoms.code`,
  model `qwen3-embedding-0.6b-q4_0`)
- query_path: vector
- escalation_reason: complex planning/design task requires Captain
  decomposition (handled directly by Captain — small additive slice
  in pure Simulation mirroring an existing single-bag pattern, no
  Builder spawn).

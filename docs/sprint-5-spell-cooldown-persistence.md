# Sprint 5 Spell Cooldown Persistence

Date: 2026-05-05
Branch: `agent/sprint-5-spell-cooldown-persistence`
Base: `f59e458` — Sprint 5 cooldown foundation merged on `origin/main`

## Scope

The cooldown foundation slice (PR #23) explicitly flagged that cooldown state was external to the
saveable world graph and that a later increment had to decide where it lived. This slice closes that
gap with the smallest honest move: keep cooldown state as a deterministic data bag, place it on the
slice world snapshot, and round-trip it through the existing JSON save layer. Behavior of casts,
fizzles, and tick-down is unchanged.

Implemented:

- `SpellCooldownState` moved from `EmberCrpg.Simulation.Magic` to `EmberCrpg.Domain.Magic`:
  - same public surface (`GetRemainingTicks`, `GetTrackedSpellTemplateIds`, `SetRemainingTicks`)
  - it is pure data with no service behavior, so it lives next to `SpellDefinition` in Domain
  - allows the slice world graph and Data save layer to carry cooldown state without crossing the
    `Domain → Simulation` asmdef boundary
  - existing Simulation services (`SpellCastingService`, `SpellCooldownService`,
    `SpellExecutionService`) keep working with one added `using EmberCrpg.Domain.Magic;` where
    needed; tests already imported both namespaces
- `SliceWorldState.PlayerSpellCooldowns` now exists as a default-empty `SpellCooldownState`
- `SliceSaveData.playerSpellCooldowns` plus a small `SpellCooldownSaveData` /
  `SpellCooldownEntrySaveData` DTO pair, JsonUtility-friendly, no behavior
- `SpellCooldownSaveMapper` round-trips state and DTO with deterministic, ordinal-sorted entries,
  null tolerance on both sides, and zero-tick filtering on rebuild so the bag stays canonical
- `SliceSaveMapper` wires the mapper into `ToData`/`ToWorld` next to `NpcMemory`
- focused fallback tests:
  - `SpellCooldownSaveMapperTests` cover null state, empty state, ordering, null/empty/zero-tick
    entry filtering, full round-trip preservation, and zero-only round-trip emptiness
  - `JsonSliceSaveServiceTests` get an end-to-end check that two active cooldowns survive a full
    JSON save/load cycle, plus a fresh-world test confirming the default state is empty

## Why this slice matters

Sprint 5 cooldowns previously had a deterministic seam but no continuity across save/load. That was
acceptable for the foundation, but Sprint 5 only earns the "deterministic gameplay layer" label when
its state survives a session boundary. This slice gives cooldowns a stable home (the slice world
graph), a stable serialization shape (ordinal-sorted DTO entries), and stable test coverage so future
balance changes will not silently invalidate persistence.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 stays in the Layer 3 deterministic gameplay band.
- `docs/EMBER_VISION_BIBLE.md` §8: this is another narrow, testable magic increment, not a "full
  spell save system" claim.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §14: spell casting remains a pure simulation concern;
  the cooldown bag now sits in pure Domain so persistence does not pull Simulation into Data.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` passed with no output.
- Fallback validation passed: `Passed: 207, Failed: 0, Skipped: 0, Total: 207` (previous Sprint 5
  cooldown foundation baseline was `198`; this slice added `9` new fallback tests).

## Release Evidence

- Branch: `agent/sprint-5-spell-cooldown-persistence`
- Local fallback baseline before slice: `198 / 198`
- Local fallback baseline after slice: `207 / 207`
- See PR for commit hashes and CI status when opened.

## Caveats

- Only `PlayerSpellCooldowns` is wired today. Enemy/NPC cooldown bags can join the same DTO when
  Sprint 5 needs them.
- Save shape is intentionally minimal: `{ spellTemplateId, remainingTicks }`. Per-cooldown metadata
  (start tick, last cast tick, telemetry) is a later increment if combat history needs it.
- Local validation remains the pure .NET fallback harness, not a real local Unity Editor / PlayMode
  run. CI on the PR is the canonical Unity check.
- Timed buffs like `Ember Ward`, resistances, saving throws, AoE geometry, and balance tuning of
  cooldown ticks per spell are still later Sprint 5 work.

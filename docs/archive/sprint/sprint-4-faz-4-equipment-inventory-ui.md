# Sprint 4 Faz 4 — Equipment and Inventory UI

_Date:_ 2026-04-30
_Branch:_ `agent/sprint-4-faz4-equipment-inventory-ui`
_Base:_ `a63e499` — Sprint 4 Faz 3 procedural dungeon

## Scope delivered

Faz 4 adds a tight equipment vertical slice on top of the existing inventory, pickup, trade, guard, door, dungeon, and combat systems.

Delivered:
- pure Domain equipment slot/state model with stable item identity (`EquipmentSlot`, `EquipmentState`)
- equipment-capable `InventoryItem` fields for slot, accuracy bonus, and damage bonus
- inventory stacking rule refinement: consumable/trade items still stack by template, equipment items remain distinct item instances
- deterministic pure Simulation `EquipmentService` with success/refusal result codes
- one player weapon item, **Ash Training Blade**, seeded into the starting inventory
- equipped weapon effects on the approved Sprint 1 encounter loop through additive accuracy/damage combat stats
- JSON save/load mapping for equipment item definitions and equipped slot -> item id references
- player-facing inventory/equipment text support:
  - `I` inspect inventory/equipment
  - `Z` equip the first inventory weapon
  - `X` unequip weapon
  - HUD shows current weapon and bonuses

## Architecture

Pure Domain additions:
- `Assets/Scripts/Domain/Inventory/EquipmentSlot.cs`
- `Assets/Scripts/Domain/Inventory/EquipmentState.cs`
- `InventoryItem` now carries optional equipment metadata while preserving the existing non-equipment constructor.
- `InventoryState` now exposes identity lookups and keeps equipment instances from stacking.

Pure Simulation additions:
- `EquipmentService` validates equip/unequip constraints and produces deterministic `EquipmentActionError` codes.
- `EquipmentCombatStats` is the small mechanics seam currently used by `EncounterTurnService`.
- `CombatMathService` accepts optional accuracy/damage bonuses without changing existing call sites.

Save/load additions:
- `SliceSaveData` stores `playerEquipment` separately from inventory items.
- `ItemSaveData` stores equipment slot and combat bonuses.
- `SliceSaveMapper` round-trips equipped weapon identity by `ItemId`.

Presentation additions:
- `InventoryEquipmentFormatter` formats inspect/HUD output without Unity mutation.
- `SliceGameSession` exposes inspect/equip/unequip commands and passes equipped combat bonuses into encounter turns.
- `SliceGameController` binds `I`, `Z`, and `X` to those commands.

## Validation evidence

Local fallback harness:

```text
tools/validation/run-validation.sh --mode fallback
Passed!  - Failed: 0, Passed: 96, Skipped: 0, Total: 96
PASS fallback_harness
```

Additional static checks run for this phase:
- `git diff --check`
- static scan of `Assets/Scripts/Domain` and `Assets/Scripts/Simulation` for `UnityEngine` references
- presentation file-size sanity check for new/modified presentation files

New/expanded tests cover:
- equip success by stable item id
- non-equipment equip refusal
- occupied weapon-slot refusal
- weapon unequip clearing the slot
- equipped weapon increasing encounter strike damage
- JSON save/load round-trip for equipped weapon identity
- inventory/equipment formatter output for empty and equipped weapon states

## Caveats / manual gaps

- Local validation is still the pure .NET fallback harness, not a real local Unity Editor/EditMode/PlayMode run.
- The UI is intentionally simple OnGUI/debug-HUD text, not a final RPG inventory screen.
- Only one equipment slot exists in this phase: `Weapon`.
- There is no drop path yet, so equipped items are not removed from inventory during gameplay. A future drop/equipment pass should block dropping equipped items or auto-unequip deterministically.
- Manual Unity play validation is still needed for input feel, OnGUI layout, and the visible combat loop.

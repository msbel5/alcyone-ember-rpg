# Sprint 4 Faz 3 — Multi-Room Procedural Dungeon

_Date:_ 2026-04-30
_Branch:_ `agent/sprint-4-faz3-procedural-dungeon`
_Base:_ `9fffa35` — Sprint 4 Faz 2 RTWP combat foundation

## Scope delivered

Faz 3 adds a deterministic, saveable multi-room dungeon substrate while keeping the existing one-room presentation slice intact.

Delivered:
- pure Domain layout records for generated rooms, doors, spawn points, room state, and door state
- pure Simulation generator that creates **5-10 connected rooms** from one seed
- deterministic graph generation with no orphan rooms: every new room is attached through a door edge when created
- deterministic room-local spawn anchors for player, guard, talker, merchant, enemy, and pickup archetypes
- Sprint 2-style door integration: the first generated door starts closed and requires guard clearance; existing `DoorInteractionService` opens/closes its generated door state when the south door is toggled
- pure dungeon traversal service that moves `CurrentRoomId` through open generated doors and marks entered rooms visited
- save/load DTO and mapper coverage for generated layout, actor room ids, mutable room state, and mutable door state

## Architecture

New pure Domain files live under `Assets/Scripts/Domain/World`:
- `GeneratedDungeonLayout` — seed, start room id, rooms, doors, and spawn points
- `DungeonRoom` — graph coordinate, dimensions, template id, and connected door ids
- `DungeonDoor` — room-to-room edge, threshold cells, initial open flag, and guard-clearance requirement
- `DungeonSpawnPoint` / `DungeonSpawnKind` — room-local archetype anchors
- `DungeonRoomState` / `DungeonDoorState` — mutable saveable state separate from deterministic layout

New pure Simulation files live under `Assets/Scripts/Simulation/World`:
- `MultiRoomDungeonGenerator` — deterministic graph generator using the existing `XorShiftRng`
- `DungeonTraversalService` — generated-door traversal and visited-room marking

`SliceWorldFactory` now creates both the legacy `ProceduralRoom` and a generated dungeon. The legacy room remains for current Unity presentation compatibility; the new generated dungeon is the Sprint 4 substrate for future multi-room rendering/traversal presentation.

## Validation evidence

Local fallback harness:

```text
tools/validation/run-validation.sh --mode fallback
Passed!  - Failed: 0, Passed: 89, Skipped: 0, Total: 89
PASS fallback_harness
```

Additional static checks run for this phase:
- `git diff --check`
- static scan of `Assets/Scripts/Domain` and `Assets/Scripts/Simulation` for `UnityEngine` references

New/expanded tests:
- generated dungeon creates 5-10 rooms
- generated dungeon graph is fully connected and has no orphan rooms
- same seed repeats topology, doors, and spawn anchors
- talker/merchant/enemy spawn anchors are inside walkable cells
- closed guarded generated door blocks traversal until Sprint 2 door rules open it
- JSON save/load round-trips generated layout, room ids, room state, and door state

## Caveats / manual gaps

- Local validation is still the pure .NET fallback harness, not a real local Unity Editor/EditMode/PlayMode run.
- The existing Unity presentation still renders the legacy one-room slice. Faz 3 adds the deterministic multi-room substrate and save mapping; multi-room 3D rendering, room streaming, camera feel, and manual traversal video remain future work.
- Actor records still store local grid positions only; room-local actor ids are tracked on `SliceWorldState` as `PlayerRoomId`, `TalkerRoomId`, `MerchantRoomId`, `GuardRoomId`, `EnemyRoomId`, and `PickupRoomId` until a fuller entity-location model lands.
- The first generated door mirrors the current Sprint 2 south-door clearance flow. Additional generated doors start open for traversal proof; richer lock/key/door variety should be a later content pass.

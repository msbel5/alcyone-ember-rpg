using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;

// Design note:
// DungeonSaveMapper translates generated dungeon layout/state to JsonUtility-friendly DTO arrays.
// Inputs: pure generated layout plus mutable door/room state lists.
// Outputs: deterministic, round-trippable save DTOs without Unity dependencies.
// Bible reference: PRD Sprint 1 FR-06, Sprint 4 Faz 3 layout and room-state persistence.
namespace EmberCrpg.Data.Save
{
    /// <summary>Pure mapping helpers for generated dungeon save data.</summary>
    public static class DungeonSaveMapper
    {
        public static DungeonRoomSaveData[] ToRoomData(GeneratedDungeonLayout dungeon)
        {
            return (dungeon?.Rooms ?? new System.Collections.Generic.List<DungeonRoom>()).Select(room => new DungeonRoomSaveData
            {
                id = room.Id,
                gridX = room.GridX,
                gridY = room.GridY,
                width = room.Width,
                height = room.Height,
                templateId = room.TemplateId,
                doorIds = room.DoorIds.ToArray(),
            }).ToArray();
        }

        public static DungeonDoorSaveData[] ToDoorData(GeneratedDungeonLayout dungeon)
        {
            return (dungeon?.Doors ?? new System.Collections.Generic.List<DungeonDoor>()).Select(door => new DungeonDoorSaveData
            {
                id = door.Id,
                fromRoomId = door.FromRoomId,
                toRoomId = door.ToRoomId,
                fromX = door.FromCell.X,
                fromY = door.FromCell.Y,
                toX = door.ToCell.X,
                toY = door.ToCell.Y,
                startsOpen = door.StartsOpen,
                requiresGuardClearance = door.RequiresGuardClearance,
            }).ToArray();
        }

        public static DungeonSpawnSaveData[] ToSpawnData(GeneratedDungeonLayout dungeon)
        {
            return (dungeon?.SpawnPoints ?? new System.Collections.Generic.List<DungeonSpawnPoint>()).Select(spawn => new DungeonSpawnSaveData
            {
                roomId = spawn.RoomId,
                kind = (int)spawn.Kind,
                positionX = spawn.Position.X,
                positionY = spawn.Position.Y,
            }).ToArray();
        }

        public static GeneratedDungeonLayout ToLayout(int seed, int startRoomId, DungeonRoomSaveData[] rooms, DungeonDoorSaveData[] doors, DungeonSpawnSaveData[] spawns)
        {
            var layout = new GeneratedDungeonLayout { Seed = seed, StartRoomId = startRoomId };
            layout.Rooms = (rooms ?? new DungeonRoomSaveData[0]).Select(ToRoom).ToList();
            layout.Doors = (doors ?? new DungeonDoorSaveData[0]).Select(ToDoor).ToList();
            layout.SpawnPoints = (spawns ?? new DungeonSpawnSaveData[0]).Select(ToSpawn).ToList();
            return layout;
        }

        public static DungeonRoomStateSaveData[] ToRoomStateData(System.Collections.Generic.IEnumerable<DungeonRoomState> states)
        {
            return (states ?? new DungeonRoomState[0]).Select(state => new DungeonRoomStateSaveData
            {
                roomId = state.RoomId,
                visited = state.Visited,
                cleared = state.Cleared,
            }).ToArray();
        }

        public static DungeonDoorStateSaveData[] ToDoorStateData(System.Collections.Generic.IEnumerable<DungeonDoorState> states)
        {
            return (states ?? new DungeonDoorState[0]).Select(state => new DungeonDoorStateSaveData
            {
                doorId = state.DoorId,
                open = state.Open,
            }).ToArray();
        }

        public static System.Collections.Generic.List<DungeonRoomState> ToRoomStates(DungeonRoomStateSaveData[] data)
        {
            return (data ?? new DungeonRoomStateSaveData[0]).Select(state => new DungeonRoomState(state.roomId, state.visited, state.cleared)).ToList();
        }

        public static System.Collections.Generic.List<DungeonDoorState> ToDoorStates(DungeonDoorStateSaveData[] data)
        {
            return (data ?? new DungeonDoorStateSaveData[0]).Select(state => new DungeonDoorState(state.doorId, state.open)).ToList();
        }

        private static DungeonRoom ToRoom(DungeonRoomSaveData data)
        {
            return new DungeonRoom
            {
                Id = data.id,
                GridX = data.gridX,
                GridY = data.gridY,
                Width = data.width,
                Height = data.height,
                TemplateId = data.templateId,
                DoorIds = (data.doorIds ?? new int[0]).ToList(),
            };
        }

        private static DungeonDoor ToDoor(DungeonDoorSaveData data)
        {
            return new DungeonDoor
            {
                Id = data.id,
                FromRoomId = data.fromRoomId,
                ToRoomId = data.toRoomId,
                FromCell = new GridPosition(data.fromX, data.fromY),
                ToCell = new GridPosition(data.toX, data.toY),
                StartsOpen = data.startsOpen,
                RequiresGuardClearance = data.requiresGuardClearance,
            };
        }

        private static DungeonSpawnPoint ToSpawn(DungeonSpawnSaveData data)
        {
            return new DungeonSpawnPoint(data.roomId, (DungeonSpawnKind)data.kind, new GridPosition(data.positionX, data.positionY));
        }
    }
}

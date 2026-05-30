using System.Collections.Generic;
using System.Linq;

// Design note:
// GeneratedDungeonLayout groups deterministic multi-room topology and archetype spawn points.
// Inputs: room graph, doors, and spawn metadata produced from a seed.
// Outputs: pure generated layout for simulation, tests, and JSON save mapping.
// Bible reference: MASTER_MECHANICS_BIBLE.md deterministic world lock-in, Sprint 4 Phase 3.
namespace EmberCrpg.Domain.World
{
    /// <summary>Pure deterministic dungeon graph snapshot.</summary>
    public sealed class GeneratedDungeonLayout
    {
        public int Seed;
        public int StartRoomId;
        public List<DungeonRoom> Rooms = new List<DungeonRoom>();
        public List<DungeonDoor> Doors = new List<DungeonDoor>();
        public List<DungeonSpawnPoint> SpawnPoints = new List<DungeonSpawnPoint>();

        public DungeonRoom FindRoom(int roomId)
        {
            return Rooms.First(room => room.Id == roomId);
        }

        public DungeonSpawnPoint FindSpawn(DungeonSpawnKind kind)
        {
            return SpawnPoints.First(spawn => spawn.Kind == kind);
        }
    }
}

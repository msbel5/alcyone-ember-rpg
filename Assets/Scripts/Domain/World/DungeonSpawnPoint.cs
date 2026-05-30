using EmberCrpg.Domain.Actors;

// Design note:
// DungeonSpawnPoint is a pure room-local placement anchor inside the generated dungeon graph.
// Inputs: room id, spawn kind, and grid position local to that room.
// Outputs: deterministic actor/pickup placement metadata for simulation and saves.
// Bible reference: PRD Sprint 1 FR-03/FR-04, Sprint 4 Phase 3 multi-room placement.
namespace EmberCrpg.Domain.World
{
    /// <summary>Room-local spawn anchor for a generated dungeon archetype.</summary>
    public sealed class DungeonSpawnPoint
    {
        public int RoomId;
        public DungeonSpawnKind Kind;
        public GridPosition Position;

        public DungeonSpawnPoint()
        {
        }

        public DungeonSpawnPoint(int roomId, DungeonSpawnKind kind, GridPosition position)
        {
            RoomId = roomId;
            Kind = kind;
            Position = position;
        }
    }
}

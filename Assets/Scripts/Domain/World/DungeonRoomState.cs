// Design note:
// DungeonRoomState stores mutable per-room progress separate from deterministic layout metadata.
// Inputs: room id plus visited/cleared flags changed by simulation.
// Outputs: saveable room state for generated dungeon round-trips.
// Bible reference: PRD Sprint 1 FR-06, Sprint 4 Phase 3 generated room state persistence.
namespace EmberCrpg.Domain.World
{
    /// <summary>Mutable room-progress flags for a generated dungeon room.</summary>
    public sealed class DungeonRoomState
    {
        public int RoomId;
        public bool Visited;
        public bool Cleared;

        public DungeonRoomState()
        {
        }

        public DungeonRoomState(int roomId, bool visited, bool cleared)
        {
            RoomId = roomId;
            Visited = visited;
            Cleared = cleared;
        }
    }
}

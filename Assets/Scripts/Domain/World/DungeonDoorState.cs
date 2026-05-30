// Design note:
// DungeonDoorState stores Sprint 2-style open/closed door state for generated dungeon edges.
// Inputs: door id and mutable open flag.
// Outputs: saveable transition state independent from deterministic door metadata.
// Bible reference: PRD Sprint 2 door persistence, Sprint 4 Phase 3 transition persistence.
namespace EmberCrpg.Domain.World
{
    /// <summary>Mutable open/closed state for a generated dungeon door.</summary>
    public sealed class DungeonDoorState
    {
        public int DoorId;
        public bool Open;

        public DungeonDoorState()
        {
        }

        public DungeonDoorState(int doorId, bool open)
        {
            DoorId = doorId;
            Open = open;
        }
    }
}

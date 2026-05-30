using System;
using EmberCrpg.Domain.Actors;

// Design note:
// DungeonDoor is the deterministic connection between two generated rooms.
// Inputs: adjacent room ids, threshold cells, and Sprint 2-style initial door rules.
// Outputs: graph edge metadata whose open/closed state is stored separately.
// Bible reference: PRD Sprint 2 door system, Sprint 4 Phase 3 door/transition rules.
namespace EmberCrpg.Domain.World
{
    /// <summary>Pure room graph edge with local threshold cells on both sides.</summary>
    public sealed class DungeonDoor
    {
        public int Id;
        public int FromRoomId;
        public int ToRoomId;
        public GridPosition FromCell;
        public GridPosition ToCell;
        public bool StartsOpen;
        public bool RequiresGuardClearance;

        public int OtherRoom(int roomId)
        {
            if (roomId == FromRoomId)
                return ToRoomId;
            if (roomId == ToRoomId)
                return FromRoomId;
            throw new ArgumentException("Room is not connected by this door.", nameof(roomId));
        }
    }
}

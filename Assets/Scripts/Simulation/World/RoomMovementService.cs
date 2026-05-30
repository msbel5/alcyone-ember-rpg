using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;

// Design note:
// RoomMovementService clamps discrete movement to the slice room and its guarded door threshold.
// Inputs: room snapshot or world snapshot, origin cell, and requested delta.
// Outputs: next valid grid cell or the unchanged blocked cell.
// Bible reference: ARCHITECTURE.md movement before combat, PRD Sprint 1 FR-03, Sprint 2 FR-02.
namespace EmberCrpg.Simulation.World
{
    /// <summary>Pure grid movement helper for room-bound movement tests.</summary>
    public sealed class RoomMovementService
    {
        public GridPosition Move(ProceduralRoom room, GridPosition origin, int deltaX, int deltaY)
        {
            var candidate = origin.Translate(deltaX, deltaY);
            return room.IsWalkable(candidate) ? candidate : origin;
        }

        public GridPosition Move(WorldState world, GridPosition origin, int deltaX, int deltaY)
        {
            var candidate = origin.Translate(deltaX, deltaY);
            if (candidate.Equals(world.Room.DoorCell))
                return world.DoorOpen ? candidate : origin;
            return world.Room.IsWalkable(candidate) ? candidate : origin;
        }
    }
}

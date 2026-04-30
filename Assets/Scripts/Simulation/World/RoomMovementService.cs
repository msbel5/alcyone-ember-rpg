using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;

// Design note:
// RoomMovementService clamps discrete movement to Sprint 1's generated room interior.
// Inputs: room snapshot, origin cell, and requested delta.
// Outputs: next valid grid cell or the unchanged blocked cell.
// Bible reference: ARCHITECTURE.md movement before combat, PRD FR-01/FR-03.
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
    }
}

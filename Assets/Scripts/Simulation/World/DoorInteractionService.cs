using System.Linq;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Actors;

// Design note:
// DoorInteractionService owns the deterministic Sprint 2 south-door toggle rules.
// Inputs: world state with player position, door state, and guard clearance state.
// Outputs: open/closed door transitions or grounded refusal text without Unity dependencies.
// Bible reference: MASTER_MECHANICS_BIBLE.md §41, PRD Sprint 2 FR-02.
namespace EmberCrpg.Simulation.World
{
    /// <summary>Rules-based doorway toggle for the slice threshold.</summary>
    public sealed class DoorInteractionService
    {
        public string Toggle(SliceWorldState world)
        {
            if (world.Actors.FirstByRole(ActorRole.Player).Position.ManhattanDistanceTo(world.Room.DoorCell) > 1)
                return "Stand at the south threshold before working the door.";
            if (!world.DoorOpen && !world.GuardDoorAccessGranted)
                return "The sealed south door refuses to move without Sentinel Rook's clearance.";

            world.DoorOpen = !world.DoorOpen;
            var guardedDoor = world.Dungeon?.Doors.FirstOrDefault(door => door.RequiresGuardClearance);
            if (guardedDoor != null)
            {
                var guardedState = world.DungeonDoorStates.FirstOrDefault(state => state.DoorId == guardedDoor.Id);
                if (guardedState != null)
                    guardedState.Open = world.DoorOpen;
            }
            return world.DoorOpen
                ? "The south door grinds open and the threshold is now passable."
                : "You pull the south door shut and bar the threshold again.";
        }
    }
}

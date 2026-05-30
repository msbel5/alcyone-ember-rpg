using System.Linq;
using EmberCrpg.Domain.World;

// Design note:
// DungeonTraversalService moves between generated rooms through saveable Sprint 2-style doors.
// Inputs: world current room and selected dungeon door id.
// Outputs: deterministic current-room/visited-state changes or a grounded refusal message.
// Bible reference: PRD Sprint 2 door rules, Sprint 4 Phase 2 traversal contracts, Phase 3 graph layout.
namespace EmberCrpg.Simulation.World
{
    /// <summary>Pure generated-dungeon transition helper.</summary>
    public sealed class DungeonTraversalService
    {
        public string Traverse(SliceWorldState world, int doorId)
        {
            var door = world.Dungeon.Doors.FirstOrDefault(candidate => candidate.Id == doorId);
            if (door == null || (door.FromRoomId != world.CurrentRoomId && door.ToRoomId != world.CurrentRoomId))
                return "No matching doorway connects to the current room.";

            var state = world.DungeonDoorStates.FirstOrDefault(candidate => candidate.DoorId == doorId);
            if (state != null && !state.Open)
                return "The door is closed.";

            world.CurrentRoomId = door.OtherRoom(world.CurrentRoomId);
            var roomState = world.DungeonRoomStates.FirstOrDefault(candidate => candidate.RoomId == world.CurrentRoomId);
            if (roomState != null)
                roomState.Visited = true;
            return $"You pass into room {world.CurrentRoomId}.";
        }
    }
}

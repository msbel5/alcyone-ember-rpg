using EmberCrpg.Domain.Actors;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin deterministic movement inside the room and through the guarded door threshold.
// They cover grid movement rules only, not Unity character control.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies walkable-room movement clamping and door rules.</summary>
    public sealed class RoomMovementServiceTests
    {
        [Test]
        public void Move_InsideRoom_AdvancesToCandidate()
        {
            var room = new ProceduralRoomGenerator().Generate(1337);
            var moved = new RoomMovementService().Move(room, new GridPosition(2, 2), 1, 0);
            Assert.That(moved, Is.EqualTo(new GridPosition(3, 2)));
        }

        [Test]
        public void Move_IntoWall_StaysInPlace()
        {
            var room = new ProceduralRoomGenerator().Generate(1337);
            var moved = new RoomMovementService().Move(room, new GridPosition(1, 1), -1, 0);
            Assert.That(moved, Is.EqualTo(new GridPosition(1, 1)));
        }

        [Test]
        public void Move_ClosedDoorCell_StaysInPlace()
        {
            var world = new SliceWorldFactory().Create(1337);
            var moved = new RoomMovementService().Move(world, new GridPosition(world.Room.DoorCell.X, 1), 0, -1);
            Assert.That(moved, Is.EqualTo(new GridPosition(world.Room.DoorCell.X, 1)));
        }

        [Test]
        public void Move_OpenDoorCell_AllowsThresholdStep()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.DoorOpen = true;
            var moved = new RoomMovementService().Move(world, new GridPosition(world.Room.DoorCell.X, 1), 0, -1);
            Assert.That(moved, Is.EqualTo(world.Room.DoorCell));
        }
    }
}

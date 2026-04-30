using EmberCrpg.Domain.Actors;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin deterministic movement inside the one-room slice.
// They cover only grid movement rules, not Unity character control.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies walkable-room movement clamping.</summary>
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
    }
}

using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin the deterministic Sprint 2 door-toggle rules.
// They cover proximity, clearance, and state transitions only.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies guard-gated door toggling.</summary>
    public sealed class DoorInteractionServiceTests
    {
        [Test]
        public void Toggle_WithoutClearance_RefusesToOpen()
        {
            var world = CreateDoorReadyWorld();
            var reply = new DoorInteractionService().Toggle(world);
            Assert.That(reply, Does.Contain("clearance"));
            Assert.That(world.DoorOpen, Is.False);
        }

        [Test]
        public void Toggle_WithClearance_OpensThenClosesDoor()
        {
            var world = CreateDoorReadyWorld();
            world.GuardDoorAccessGranted = true;
            var service = new DoorInteractionService();
            service.Toggle(world);
            service.Toggle(world);
            Assert.That(world.DoorOpen, Is.False);
        }

        private static SliceWorldState CreateDoorReadyWorld()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.Player.MoveTo(new GridPosition(world.Room.DoorCell.X, 1));
            return world;
        }
    }
}

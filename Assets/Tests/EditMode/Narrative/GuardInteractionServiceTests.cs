using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.World;
using NUnit.Framework;
using EmberCrpg.Domain.Actors;

// Design note:
// These tests pin Sentinel Rook's deterministic Sprint 2 checkpoint behavior.
// They cover warning escalation and clearance grants only.
namespace EmberCrpg.Tests.EditMode.Narrative
{
    /// <summary>Verifies guard-specific warnings and gate-writ clearance.</summary>
    public sealed class GuardInteractionServiceTests
    {
        [Test]
        public void Interact_WithoutGateWrit_EscalatesWarnings()
        {
            var world = CreateGuardReadyWorld();
            var service = new GuardInteractionService();
            service.Interact(world);
            service.Interact(world);
            Assert.That(world.GuardWarningCount, Is.EqualTo(2));
        }

        [Test]
        public void Interact_WithGateWrit_GrantsDoorClearance()
        {
            var world = CreateGuardReadyWorld();
            world.PlayerInventory.TryAdd(SliceItemCatalog.CreateGateWrit());

            var reply = new GuardInteractionService().Interact(world);

            Assert.That(reply, Does.Contain("grants clearance"));
            Assert.That(world.GuardDoorAccessGranted, Is.True);
        }

        private static SliceWorldState CreateGuardReadyWorld()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.Actors.FirstByRole(ActorRole.Player).MoveTo(world.Actors.FirstByRole(ActorRole.Guard).Position.Translate(0, -1));
            return world;
        }
    }
}

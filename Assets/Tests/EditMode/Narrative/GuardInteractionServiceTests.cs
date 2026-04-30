using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin Sentinel Rook's deterministic Sprint 2 checkpoint behavior.
// They cover warning escalation, clearance grants, and Sprint 3 guard memory writes.
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
        public void Interact_WithGateWrit_GrantsDoorClearanceAndRecordsMemory()
        {
            var world = CreateGuardReadyWorld();
            ActorMemory memory;
            world.PlayerInventory.TryAdd(SliceItemCatalog.CreateGateWrit(world.ItemIds));

            var reply = new GuardInteractionService().Interact(world);

            Assert.That(reply, Does.Contain("grants clearance"));
            Assert.That(world.GuardDoorAccessGranted, Is.True);
            Assert.That(world.NpcMemories.TryGet(world.Guard.Id, out memory), Is.True);
            Assert.That(memory.Events[memory.Events.Count - 1].Type, Is.EqualTo(ActorMemoryEventType.DoorClearanceGranted));
        }

        private static SliceWorldState CreateGuardReadyWorld()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.Player.MoveTo(world.Guard.Position.Translate(0, -1));
            return world;
        }
    }
}

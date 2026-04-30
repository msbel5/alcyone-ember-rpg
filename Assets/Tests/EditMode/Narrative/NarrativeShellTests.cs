using EmberCrpg.Domain.Memory;
using EmberCrpg.Simulation.DM;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin the deterministic Ask About, Ask DM, Think, and DM-query shells.
// They intentionally avoid live AI dependencies.
namespace EmberCrpg.Tests.EditMode.Narrative
{
    /// <summary>Verifies grounded narrative and memory-facing query behavior.</summary>
    public sealed class NarrativeShellTests
    {
        [Test]
        public void AskAbout_FirstQuestion_UsesTopicAnswerAndRecordsMemory()
        {
            var world = new SliceWorldFactory().Create(1337);
            var reply = new AskAboutService().Ask(world, "embers");
            ActorMemory memory;

            Assert.That(reply, Does.Contain("embers").IgnoreCase);
            Assert.That(world.NpcMemories.TryGet(world.Talker.Id, out memory), Is.True);
            Assert.That(memory.DialogueSeen, Does.Contain("embers"));
            Assert.That(memory.Events[0].Type, Is.EqualTo(ActorMemoryEventType.DialogueTopic));
        }

        [Test]
        public void AskDm_ReturnsGroundedWorldSummary()
        {
            var world = new SliceWorldFactory().Create(1337);
            var reply = new AskDmService().Ask(world, "What should I do?");
            Assert.That(reply, Does.Contain("room seed 1337"));
            Assert.That(reply, Does.Contain("objective="));
        }

        [Test]
        public void AskDm_DoorQuestion_FocusesGuardMemory()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.Player.MoveTo(world.Guard.Position.Translate(0, -1));
            new GuardInteractionService().Interact(world);

            var reply = new AskDmService().Ask(world, "Can I pass the south door?");

            Assert.That(reply, Does.Contain("focus=Sentinel Rook"));
            Assert.That(reply, Does.Contain("warning #1"));
        }

        [Test]
        public void DmQueryService_TradeQuestion_PrefersMerchantMemory()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.Player.MoveTo(world.Merchant.Position.Translate(0, 1));
            world.PlayerInventory.TryAdd(world.Pickups[0].Item);
            world.Pickups[0].Collect();
            new MerchantTradeService().TradeGateWrit(world);

            var focus = new SliceDmQueryService().GetRelevantNpcMemory(world, "What does Quartermaster Ivo remember about our trade?");

            Assert.That(focus.NpcName, Is.EqualTo("Quartermaster Ivo"));
            Assert.That(focus.RecentEvents[0], Does.Contain("gate writ"));
        }

        [Test]
        public void Think_PrefersNearbyPickupWhenInventoryHasSpace()
        {
            var world = new SliceWorldFactory().Create(1337);
            var reply = new ThinkService().Think(world);
            Assert.That(reply, Does.Contain("Ember Shard"));
        }
    }
}

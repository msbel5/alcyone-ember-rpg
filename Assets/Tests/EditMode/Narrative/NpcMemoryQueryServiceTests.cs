using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin Sprint 3 Phase 5's thin deterministic DM query layer over saved NPC memory.
// They prove live interaction text is selected from persisted facts, not random flavor or transient counters.
namespace EmberCrpg.Tests.EditMode.Narrative
{
    /// <summary>Verifies memory-backed query contexts and wired player-facing responses.</summary>
    public sealed class NpcMemoryQueryServiceTests
    {
        [Test]
        public void AskAbout_ThirdRepeatedTopic_UsesWellWornMemoryState()
        {
            var world = new SliceWorldFactory().Create(1337);
            var service = new AskAboutService();

            service.Ask(world, "embers");
            service.Ask(world, "embers");
            var thirdReply = service.Ask(world, "embers");

            Assert.That(thirdReply, Does.Contain("familiar answer"));
            Assert.That(thirdReply, Does.Contain("3 tellings"));
            Assert.That(world.NpcMemory.GetOrCreate(world.Talker.Id).CountEvents(ActorMemoryEventTypes.DialogueTopic), Is.EqualTo(3));
        }

        [Test]
        public void GuardInteract_WithPersistedPassageRequests_UsesClosedStanceEvenWhenCounterIsReset()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.Player.MoveTo(world.Guard.Position.Translate(0, -1));
            world.GuardWarningCount = 0;
            var memory = world.NpcMemory.GetOrCreate(world.Guard.Id);
            memory.RecordEvent(CreateGuardPassageRequest(world, 0));
            memory.RecordEvent(CreateGuardPassageRequest(world, 1));

            var reply = new GuardInteractionService().Interact(world);

            Assert.That(reply, Does.Contain("after 3 requests"));
            Assert.That(reply, Does.Contain("closed"));
            Assert.That(world.GuardWarningCount, Is.EqualTo(3));
        }

        [Test]
        public void GuardInteract_WithPersistedClearance_RemembersAccessWithoutTransientFlag()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.Player.MoveTo(world.Guard.Position.Translate(0, -1));
            world.GuardDoorAccessGranted = false;
            world.NpcMemory.GetOrCreate(world.Guard.Id).RecordEvent(new InteractionEvent(
                world.Time,
                ActorMemoryEventTypes.ClearanceGranted,
                world.Player.Id,
                "south_door",
                SliceItemCatalog.GateWritTemplateId,
                1,
                world.Guard.Position));

            var reply = new GuardInteractionService().Interact(world);

            Assert.That(reply, Does.Contain("clearance still stands"));
        }

        [Test]
        public void MerchantTrade_WithPriorTransaction_UsesRecognizedCustomerFlavor()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.Player.MoveTo(world.Merchant.Position.Translate(0, 1));
            world.PlayerInventory.TryAdd(SliceItemCatalog.CreateEmberShard());
            var memory = world.NpcMemory.GetOrCreate(world.Merchant.Id);
            memory.RecordEvent(new InteractionEvent(
                world.Time,
                ActorMemoryEventTypes.TradedWith,
                world.Player.Id,
                "gate_writ_trade",
                SliceItemCatalog.GateWritTemplateId,
                1,
                world.Merchant.Position));
            memory.RecordTransaction(new TransactionRecord(
                world.Time,
                "IssueGateWrit",
                SliceItemCatalog.GateWritTemplateId,
                1,
                0));

            var reply = new MerchantTradeService().TradeGateWrit(world);

            Assert.That(reply, Does.Contain("recognizes"));
            Assert.That(reply, Does.Contain("another Ember Shard"));
        }

        [Test]
        public void QueryService_DerivesContextsFromNpcMemoryStore()
        {
            var world = new SliceWorldFactory().Create(1337);
            var guardMemory = world.NpcMemory.GetOrCreate(world.Guard.Id);
            guardMemory.RecordEvent(CreateGuardPassageRequest(world, 0));
            var merchantMemory = world.NpcMemory.GetOrCreate(world.Merchant.Id);
            merchantMemory.RecordTransaction(new TransactionRecord(world.Time, "IssueGateWrit", SliceItemCatalog.GateWritTemplateId, 1, 0));

            var query = new NpcMemoryQueryService();

            Assert.That(query.GetDialogueContext(world.NpcMemory, world.Talker.Id, "embers").State, Is.EqualTo(DialogueMemoryState.NewTopic));
            Assert.That(query.GetGuardContext(world.NpcMemory, world.Guard.Id, "south_door").Stance, Is.EqualTo(GuardStance.FinalWarning));
            Assert.That(query.GetMerchantContext(world.NpcMemory, world.Merchant.Id).Familiarity, Is.EqualTo(MerchantFamiliarity.Recognized));
        }

        private static InteractionEvent CreateGuardPassageRequest(EmberCrpg.Domain.World.SliceWorldState world, int amount)
        {
            return new InteractionEvent(
                world.Time,
                ActorMemoryEventTypes.PassageRequested,
                world.Player.Id,
                "south_door",
                string.Empty,
                amount,
                world.Guard.Position);
        }
    }
}

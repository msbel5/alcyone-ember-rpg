#if UNITY_EDITOR
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.Quest;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation
{
    public sealed class PlayableLoopCraftQuestTests
    {
        [Test]
        public void ForgeQuest_StartsFromNpc_CraftsThenCompletesOnDelivery()
        {
            var world = new WorldFactory().Create(roomSeed: 17);
            var smith = new NpcSeedRecord(
                new NpcId(7UL),
                new SettlementId(1UL),
                new FactionId(1UL),
                "Ada the Smith",
                980,
                NpcRole.Blacksmith);
            world.NpcSeeds.Add(smith);
            var adapter = new DomainSimulationAdapter(world);
            var source = adapter.GetDialogSource("Ada the Smith");

            Assert.That(world.Quests.Contains(QuestCatalog.ForgeIronIngotId), Is.False);
            Assert.That(world.PlayerInventory.Contains("iron_ore"), Is.False);
            Assert.That(source.GetTopics(), Does.Contain(QuestInteractionService.ForgeIronIngotTopicId));

            source.SelectTopic(QuestInteractionService.ForgeIronIngotTopicId);

            Assert.That(world.Quests.Contains(QuestCatalog.ForgeIronIngotId), Is.True);
            Assert.That(world.PlayerInventory.Contains("iron_ore"), Is.True);
            Assert.That(world.PlayerInventory.Contains("fuel"), Is.True);

            var result = ((ICraftingCommandSink)adapter).ExecuteCraft("1001");
            var chapters = ((IJournalSource)adapter).GetChapters();

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(chapters.Count, Is.EqualTo(1));
            Assert.That(chapters[0].Entries[0].Status, Is.EqualTo(JournalEntryStatus.Active));
            Assert.That(chapters[0].Entries[0].Body, Does.Contain("Return"));

            source.SelectTopic(QuestInteractionService.ForgeIronIngotTopicId);
            chapters = ((IJournalSource)adapter).GetChapters();

            Assert.That(chapters[0].Entries[0].Status, Is.EqualTo(JournalEntryStatus.Completed));
            Assert.That(chapters[0].Entries[0].Body, Does.Contain("delivered"));
            Assert.That(world.PlayerInventory.Contains("iron_ingot"), Is.False);
            Assert.That(((IQuestGuidanceSource)adapter).ReadQuestGuidance().HasTarget, Is.False);
        }

        [Test]
        public void ForgeQuest_PreexistingIngot_DoesNotCompleteWithoutPostQuestCraft()
        {
            var world = new WorldFactory().Create(roomSeed: 21);
            var smith = new NpcSeedRecord(
                new NpcId(11UL),
                new SettlementId(1UL),
                new FactionId(1UL),
                "Toma the Smith",
                984,
                NpcRole.Blacksmith);
            world.NpcSeeds.Add(smith);
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(900UL), "iron_ingot", "Iron Ingot", 1));
            var adapter = new DomainSimulationAdapter(world);
            var source = adapter.GetDialogSource("Toma the Smith");

            source.SelectTopic(QuestInteractionService.ForgeIronIngotTopicId);
            source.SelectTopic(QuestInteractionService.ForgeIronIngotTopicId);

            var chapters = ((IJournalSource)adapter).GetChapters();
            Assert.That(chapters[0].Entries[0].Status, Is.EqualTo(JournalEntryStatus.Active));
            Assert.That(Count(world.PlayerInventory, "iron_ingot"), Is.EqualTo(1));

            Assert.That(((ICraftingCommandSink)adapter).ExecuteCraft("1001").Success, Is.True);
            source.SelectTopic(QuestInteractionService.ForgeIronIngotTopicId);

            Assert.That(Count(world.PlayerInventory, "iron_ingot"), Is.EqualTo(1));
            Assert.That(((IJournalSource)adapter).GetChapters()[0].Entries[0].Status, Is.EqualTo(JournalEntryStatus.Completed));
        }

        // F9 ("zindanı bulamadım"): the delve pointer must exist on a FRESH seeded world and must stay
        // visible while the forge quest is still active — v0.2 hid it behind quest completion.
        [Test]
        public void DelveGuidance_AvailableOnFreshWorld_AndIndependentOfQuestState()
        {
            var world = new WorldFactory().Create(roomSeed: 17);
            var adapter = new DomainSimulationAdapter(world);
            adapter.SeedWorld("grim", "survival", "crossroads", 7u);

            var fresh = ((IQuestGuidanceSource)adapter).ReadDelveGuidance();
            Assert.That(fresh.HasTarget, Is.True, "fresh world must already point at a delve");
            Assert.That(fresh.Title, Is.EqualTo("Delve Lead"));
            Assert.That(fresh.TargetName, Is.Not.Empty);

            world.Quests.Add(QuestCatalog.ForgeIronIngotId, new QuestState(1, world.Time));
            var during = ((IQuestGuidanceSource)adapter).ReadDelveGuidance();
            Assert.That(during.HasTarget, Is.True, "active forge quest must not hide the delve pointer");
            Assert.That(during.TargetName, Is.EqualTo(fresh.TargetName));
        }

        // F9 root-cause guard: the EXACT world the proof harness (and a default New Game) seeds — answer
        // tuple (grim, wanderer, crossroads), derived seed — must contain a Dungeon settlement. Dungeon
        // kind only rolls from small Mountain/Ash/Swamp placements, so a temperate planet shipped ZERO
        // delves until the EnsureAtLeastOneDungeon worldgen invariant; this pins that invariant.
        [Test]
        public void DelveGuidance_DefaultNewGameWorld_AlwaysHasADungeon()
        {
            var world = new WorldFactory().Create(roomSeed: 17);
            var adapter = new DomainSimulationAdapter(world);
            adapter.SeedWorld("grim", "wanderer", "crossroads", null);

            var row = ((IQuestGuidanceSource)adapter).ReadDelveGuidance();
            Assert.That(row.HasTarget, Is.True, "every generated world must contain at least one delve");
        }

        private static int Count(EmberCrpg.Domain.Inventory.InventoryState inventory, string templateId)
        {
            var total = 0;
            foreach (var item in inventory.Items)
                if (item.TemplateId == templateId)
                    total += item.Quantity;
            return total;
        }

        [Test]
        public void ForgeQuest_IsOfferedBySmithJobWorkerEvenWhenNpcRoleIsGeneric()
        {
            var world = new WorldFactory().Create(roomSeed: 18);
            var npcId = new NpcId(8UL);
            var actorId = new ActorId(10_000UL + npcId.Value);
            world.NpcSeeds.Add(new NpcSeedRecord(
                npcId,
                new SettlementId(1UL),
                new FactionId(1UL),
                "Mara the Worker",
                981,
                NpcRole.Farmer));
            world.Actors.Add(new ActorRecord(
                actorId,
                "Mara the Worker",
                ActorRole.Talker,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(new VitalStat(30, 30), new VitalStat(30, 30), new VitalStat(20, 20)),
                new GridPosition(1, 1),
                accuracy: 35,
                dodge: 30,
                armor: 4,
                baseDamage: 4,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) }));
            var adapter = new DomainSimulationAdapter(world);

            var source = adapter.GetDialogSource(actorId);

            Assert.That(source.GetTopics(), Does.Contain(QuestInteractionService.ForgeIronIngotTopicId));
        }

        [Test]
        public void QuestGuidance_PointsToForgeQuestGiverBeforeJournalExists()
        {
            var world = new WorldFactory().Create(roomSeed: 19);
            var npcId = new NpcId(9UL);
            var actorId = new ActorId(10_000UL + npcId.Value);
            world.NpcSeeds.Add(new NpcSeedRecord(
                npcId,
                new SettlementId(1UL),
                new FactionId(1UL),
                "Bera the Smith",
                982,
                NpcRole.Farmer));
            world.Actors.Add(new ActorRecord(
                actorId,
                "Bera the Smith",
                ActorRole.Talker,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(new VitalStat(30, 30), new VitalStat(30, 30), new VitalStat(20, 20)),
                new GridPosition(3, 1),
                accuracy: 35,
                dodge: 30,
                armor: 4,
                baseDamage: 4,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) }));
            var adapter = new DomainSimulationAdapter(world);

            var guidance = ((IQuestGuidanceSource)adapter).ReadQuestGuidance();

            Assert.That(world.Quests.Contains(QuestCatalog.ForgeIronIngotId), Is.False);
            Assert.That(guidance.HasTarget, Is.True);
            Assert.That(guidance.TargetName, Is.EqualTo("Bera the Smith"));
            Assert.That(guidance.Title, Is.EqualTo("Quest Lead"));
            Assert.That(guidance.Line, Does.Contain("forge work"));
        }

        [Test]
        public void QuestGuidance_TracksLivePlayerLocalPosition()
        {
            var world = new WorldFactory().Create(roomSeed: 20);
            var npcId = new NpcId(10UL);
            var actorId = new ActorId(10_000UL + npcId.Value);
            var targetPosition = new GridPosition(6, 4);
            world.NpcSeeds.Add(new NpcSeedRecord(
                npcId,
                new SettlementId(1UL),
                new FactionId(1UL),
                "Corin the Smith",
                983,
                NpcRole.Farmer));
            world.Actors.Add(new ActorRecord(
                actorId,
                "Corin the Smith",
                ActorRole.Talker,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(new VitalStat(30, 30), new VitalStat(30, 30), new VitalStat(20, 20)),
                targetPosition,
                accuracy: 35,
                dodge: 30,
                armor: 4,
                baseDamage: 4,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) }));
            var adapter = new DomainSimulationAdapter(world);
            var origin = adapter.BillboardOriginCell();
            var tracker = (IQuestGuidanceTracker)adapter;

            tracker.UpdateQuestGuidancePlayerLocalPosition(new GridPosition(1 - origin.X, 4 - origin.Y));
            var far = ((IQuestGuidanceSource)adapter).ReadQuestGuidance();

            tracker.UpdateQuestGuidancePlayerLocalPosition(new GridPosition(6 - origin.X, 4 - origin.Y));
            var near = ((IQuestGuidanceSource)adapter).ReadQuestGuidance();

            Assert.That(far.HasTarget, Is.True);
            Assert.That(far.DistanceTiles, Is.EqualTo(5));
            Assert.That(far.Direction, Is.EqualTo("east"));
            Assert.That(near.HasTarget, Is.True);
            Assert.That(near.DistanceTiles, Is.EqualTo(0));
            Assert.That(near.Direction, Is.EqualTo("nearby"));
        }
    }
}
#endif

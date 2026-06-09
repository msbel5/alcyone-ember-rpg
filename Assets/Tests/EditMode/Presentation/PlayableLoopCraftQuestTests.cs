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
    }
}
#endif

using System.Linq;
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Quest;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Quest
{
    public sealed class QuestSystemTests
    {
        private static readonly SiteId ForgeSite = new SiteId(77UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);
        private static readonly ActorId Player = new ActorId(1UL);
        private static readonly ActorId Worker = new ActorId(2UL);
        private static readonly JobId SmeltJob = new JobId(701UL);

        [Test]
        public void Tick_WithoutIronIngot_LeavesQuestIncomplete()
        {
            var world = CreateQuestWorld();
            var questState = SeedForgeQuest(world);

            new QuestSystem().Tick(world);

            Assert.That(questState.IsComplete, Is.False);
            Assert.That(CountEvents(world, WorldEventKind.QuestStarted), Is.EqualTo(1));
            Assert.That(CountEvents(world, WorldEventKind.QuestCompleted), Is.EqualTo(0));
        }

        [Test]
        public void Tick_WithPreexistingIronIngot_DoesNotMarkForgeObjective()
        {
            var world = CreateQuestWorld();
            var questState = SeedForgeQuest(world);
            var system = new QuestSystem();

            system.Tick(world);
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(501UL), "iron_ingot", "Iron Ingot", 1));
            system.Tick(world);
            system.Tick(world);

            Assert.That(questState.IsComplete, Is.False);
            Assert.That(questState.IsTaskTriggered(0), Is.False);
            Assert.That(CountEvents(world, WorldEventKind.QuestTaskTriggered), Is.EqualTo(0));
            Assert.That(CountEvents(world, WorldEventKind.QuestCompleted), Is.EqualTo(0));
        }

        [Test]
        public void Tick_WithPostQuestCraftEvent_MarksForgeObjectiveWithoutCompletingQuest()
        {
            var world = CreateQuestWorld();
            var questState = SeedForgeQuest(world);
            var system = new QuestSystem();

            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(501UL), "iron_ingot", "Iron Ingot", 1));
            world.Events.Append(new WorldEvent(
                world.Time,
                WorldEventKind.RecipeCompleted,
                Player,
                ForgeSite,
                "recipe_completed:1001"));
            system.Tick(world);

            Assert.That(questState.IsComplete, Is.False);
            Assert.That(questState.IsTaskTriggered(0), Is.True);
            Assert.That(CountEvents(world, WorldEventKind.QuestTaskTriggered), Is.EqualTo(1));
            Assert.That(CountEvents(world, WorldEventKind.QuestCompleted), Is.EqualTo(0));
        }

        [Test]
        public void WorldTickComposer_SmeltLoop_MarksForgeObjectiveDeterministically()
        {
            var first = RunSmeltQuest();
            var second = RunSmeltQuest();

            Assert.That(first.QuestState.IsComplete, Is.False);
            Assert.That(first.QuestState.IsTaskTriggered(0), Is.True);
            Assert.That(first.IronIngotCount, Is.EqualTo(1));
            Assert.That(first.QuestCompletedCount, Is.EqualTo(0));
            Assert.That(first.TaskTriggeredTickMinutes, Is.GreaterThan(0));
            Assert.That(first.TaskTriggeredTickMinutes, Is.EqualTo(second.TaskTriggeredTickMinutes));
        }

        private static (QuestState QuestState, int IronIngotCount, int QuestCompletedCount, long TaskTriggeredTickMinutes) RunSmeltQuest()
        {
            var world = CreateSmeltQuestWorld();
            var questState = SeedForgeQuest(world);
            var composer = new WorldTickComposer();

            composer.Advance(world, 0);
            for (var tick = 1; tick <= WorldTickComposer.TicksPerGameDay; tick++)
            {
                composer.Advance(world, tick);
                if (questState.IsTaskTriggered(0))
                    break;
            }

            return (
                questState,
                Quantity(world.PlayerInventory, "iron_ingot"),
                CountEvents(world, WorldEventKind.QuestCompleted),
                FirstEventTick(world, WorldEventKind.QuestTaskTriggered));
        }

        private static WorldState CreateQuestWorld()
        {
            var world = new WorldState
            {
                Time = new GameTime(8 * GameTime.MinutesPerHour),
                PlayerInventory = new InventoryState(8),
            };

            world.Sites.Add(new SiteRecord(ForgeSite, SiteKind.Settlement, "Forge", new GridPosition(0, 0), new GridPosition(2, 2)));
            world.Actors.Add(CreateActor(Player, ActorRole.Player, new GridPosition(0, 0)));
            return world;
        }

        private static WorldState CreateSmeltQuestWorld()
        {
            var world = CreateQuestWorld();
            world.Actors.Add(CreateSmith());
            world.Worksites.Add(new WorksiteRecord(ForgeSite, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            world.Jobs.Add(new JobRequest(
                SmeltJob,
                EmberCrpg.Data.Recipes.ProductionRecipeRegistry.SmeltIronIngotId,
                ForgeSite,
                FurnacePosition,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(1),
                quantity: 1,
                requesterId: Worker));
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(601UL), "iron_ore", "Iron Ore", 2));
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(602UL), "fuel", "Fuel", 1));
            return world;
        }

        private static QuestState SeedForgeQuest(WorldState world)
        {
            var quest = QuestCatalog.ForgeIronIngot();
            var questState = new QuestState(quest.Tasks.Count, world.Time);
            world.Quests.Add(quest.Id, questState);
            return questState;
        }

        private static ActorRecord CreateSmith()
        {
            return CreateActor(
                Worker,
                ActorRole.Talker,
                FurnacePosition.Translate(0, 1),
                new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) });
        }

        private static ActorRecord CreateActor(ActorId actorId, ActorRole role, GridPosition position, ActorJobPreference[] preferences = null)
        {
            return new ActorRecord(
                actorId,
                role == ActorRole.Player ? "Player" : "Smith Ada",
                role,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(new VitalStat(30, 30), new VitalStat(30, 30), new VitalStat(20, 20)),
                position,
                accuracy: 40,
                dodge: 30,
                armor: 4,
                baseDamage: 4,
                jobPreferences: preferences);
        }

        private static int CountEvents(WorldState world, WorldEventKind kind)
        {
            return world.Events.Events.Count(evt => evt.Kind == kind);
        }

        private static long FirstEventTick(WorldState world, WorldEventKind kind)
        {
            return world.Events.Events.First(evt => evt.Kind == kind).Tick.TotalMinutes;
        }

        private static int Quantity(InventoryState inventory, string templateId)
        {
            return inventory.Items
                .Where(item => item.TemplateId == templateId)
                .Sum(item => item.Quantity);
        }
    }
}

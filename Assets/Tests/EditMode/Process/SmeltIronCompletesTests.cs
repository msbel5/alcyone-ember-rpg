using System.Linq;
using EmberCrpg.Data.Recipes;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    public sealed class SmeltIronCompletesTests
    {
        private static readonly SiteId Site = new SiteId(77UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);
        private static readonly ActorId Worker = new ActorId(1UL);
        private static readonly JobId Job = new JobId(701UL);

        [Test]
        public void Advance_OverOneDeterministicDay_CompletesSmeltAndProducesIronIngot()
        {
            var world = BuildSeededWorld();
            var composer = new WorldTickComposer();

            composer.Advance(world, 0);
            for (var tick = 1; tick <= WorldTickComposer.TicksPerGameDay; tick++)
                composer.Advance(world, tick);

            Assert.That(Quantity(world.PlayerInventory, "iron_ingot"), Is.EqualTo(1));
            Assert.That(world.Jobs.Contains(Job), Is.False);
            Assert.That(world.Actors.Get(Worker).ScheduleState, Is.EqualTo(ActorScheduleState.Idle));
            Assert.That(world.Events.Events.Any(evt => evt.Kind == WorldEventKind.JobAssigned && evt.Reason == "job_assigned:701"), Is.True);
            Assert.That(world.Events.Events.Any(evt => evt.Kind == WorldEventKind.RecipeCompleted && evt.Reason == "recipe_completed:1001"), Is.True);
            Assert.That(
                world.Events.Events.Any(evt =>
                    evt.Kind == WorldEventKind.JobCompleted
                    && evt.Reason == "job_completed:701"
                    && evt.ReasonTrace.Causes.Contains("recipe:1001")),
                Is.True);
        }

        private static WorldState BuildSeededWorld()
        {
            var world = new WorldState
            {
                Time = new GameTime(8 * GameTime.MinutesPerHour),
                PlayerInventory = new InventoryState(8),
            };

            world.Actors.Add(CreateSmith());
            world.Worksites.Add(new WorksiteRecord(Site, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            world.Jobs.Add(new JobRequest(
                Job,
                ProductionRecipeRegistry.SmeltIronIngotId,
                Site,
                FurnacePosition,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(1),
                quantity: 1,
                requesterId: Worker));
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(501UL), "iron_ore", "Iron Ore", 2));
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(502UL), "fuel", "Fuel", 1));
            return world;
        }

        private static ActorRecord CreateSmith()
        {
            return new ActorRecord(
                Worker,
                "Smith Ada",
                ActorRole.Talker,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(
                    new VitalStat(30, 30),
                    new VitalStat(30, 30),
                    new VitalStat(20, 20)),
                FurnacePosition.Translate(0, 1),
                accuracy: 40,
                dodge: 30,
                armor: 4,
                baseDamage: 4,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) });
        }

        private static int Quantity(InventoryState inventory, string templateId)
        {
            return inventory.Items
                .Where(item => string.Equals(item.TemplateId, templateId, System.StringComparison.Ordinal))
                .Sum(item => item.Quantity);
        }
    }
}

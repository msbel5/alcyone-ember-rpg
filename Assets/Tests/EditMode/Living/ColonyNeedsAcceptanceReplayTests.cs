using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    public sealed class ColonyNeedsAcceptanceReplayTests
    {
        private static readonly SiteId Site = new SiteId(40UL);
        private static readonly GridPosition WorksitePos = new GridPosition(2, 3);

        [Test]
        public void AcceptanceReplay_ThreeDaysUnfed_RefusesThenRecoversAfterMeal()
        {
            var actors = new ActorStore();
            var actor = CreateActorWithPreference(new ActorNeeds(), jobKind: JobKind.Smith);
            actors.Add(actor);

            var board = new JobBoard();
            var job = new JobRequest(new JobId(200UL), new RecipeId(300UL), Site, WorksitePos, WorksiteKind.Furnace, JobKind.Smith, JobPriority.Active(1), quantity: 1, requesterId: new ActorId(99UL));
            board.Add(job);

            var worksites = new WorksiteStore();
            worksites.Add(new WorksiteRecord(Site, WorksitePos, WorksiteKind.Furnace, isActive: true));

            var log = new WorldEventLog();
            var needsSystem = new NeedsSystem();

            // Simulate three in-game days of missed meals
            needsSystem.TickActorNeeds(actor, log, new GameTime(0), ticks: 3);

            var assigner = new JobAssignmentSystem();
            var now = default(GameTime);

            // Actor should refuse due to low mood or hunger
            Assert.That(assigner.TryAssignNext(actors, board, worksites, log, now, out var result), Is.False);
            Assert.That(log.Events.Any(e => e.Kind == WorldEventKind.JobRefused), Is.True);
            Assert.That(board.IsClaimed(job.Id), Is.False);

            // Provide one meal in inventory and perform recovery
            var inventory = MealInventory(quantity: 1);
            var recovered = new NeedRecoverySystem().EatMeal(actor, inventory, MealRecipe(), log, new GameTime(1000));

            Assert.That(recovered, Is.True);
            Assert.That(actor.Needs.Hunger.Value, Is.LessThan(80));

            // After recovery, assignment should succeed
            var assigned = assigner.TryAssignNext(actors, board, worksites, log, now, out result);
            Assert.That(assigned, Is.True);
            Assert.That(board.IsClaimed(job.Id), Is.True);
        }

        [Test]
        public void AcceptanceReplay_HungerBoundary_RefusesAtThreshold()
        {
            var actors = new ActorStore();
            var actor = CreateActorWithPreference(new ActorNeeds(new NeedValue(80), NeedValue.Comfortable, NeedValue.Comfortable), jobKind: JobKind.Smith);
            actors.Add(actor);

            var board = new JobBoard();
            var job = new JobRequest(new JobId(201UL), new RecipeId(301UL), Site, WorksitePos, WorksiteKind.Furnace, JobKind.Smith, JobPriority.Active(1), quantity: 1, requesterId: new ActorId(99UL));
            board.Add(job);

            var worksites = new WorksiteStore();
            worksites.Add(new WorksiteRecord(Site, WorksitePos, WorksiteKind.Furnace, isActive: true));

            var log = new WorldEventLog();
            var assigner = new JobAssignmentSystem();

            Assert.That(assigner.TryAssignNext(actors, board, worksites, log, default(GameTime), out var r), Is.False);
            Assert.That(log.Events.Any(e => e.Kind == WorldEventKind.JobRefused), Is.True);
        }

        [Test]
        public void AcceptanceReplay_HungerOnly_TriggersRefusal()
        {
            var actors = new ActorStore();
            var actor = CreateActorWithPreference(new ActorNeeds(new NeedValue(90), NeedValue.Comfortable, NeedValue.Comfortable), jobKind: JobKind.Smith);
            actors.Add(actor);

            var board = new JobBoard();
            var job = new JobRequest(new JobId(202UL), new RecipeId(302UL), Site, WorksitePos, WorksiteKind.Furnace, JobKind.Smith, JobPriority.Active(1), quantity: 1, requesterId: new ActorId(99UL));
            board.Add(job);

            var worksites = new WorksiteStore();
            worksites.Add(new WorksiteRecord(Site, WorksitePos, WorksiteKind.Furnace, isActive: true));

            var log = new WorldEventLog();
            var assigner = new JobAssignmentSystem();

            Assert.That(assigner.TryAssignNext(actors, board, worksites, log, default(GameTime), out var r), Is.False);
            Assert.That(log.Events.Any(e => e.Kind == WorldEventKind.JobRefused), Is.True);
        }

        private static ActorRecord CreateActorWithPreference(ActorNeeds needs, JobKind jobKind)
        {
            var pref = new ActorJobPreference(jobKind, JobPriority.Active(1));
            return new ActorRecord(
                new ActorId((ulong)System.DateTime.UtcNow.Ticks % 10000UL),
                "Acceptance",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                10,
                5,
                1,
                3,
                jobPreferences: new[] { pref },
                needs: needs,
                mood: ActorMood.Neutral);
        }

        private static NeedRecoveryRecipe MealRecipe()
        {
            return new NeedRecoveryRecipe("meal-basic", NeedRecoverySystem.EatMealAction, NeedKind.Hunger, 50, "simple_meal");
        }

        private static InventoryState MealInventory(int quantity)
        {
            var inventory = new InventoryState(4);
            inventory.TryAdd(new InventoryItem(new ItemId(900UL), "simple_meal", "Simple Meal", quantity));
            return inventory;
        }
    }
}

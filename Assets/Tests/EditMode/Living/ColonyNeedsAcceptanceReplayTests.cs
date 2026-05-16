using NUnit.Framework;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Process;

namespace EmberCrpg.Tests.EditMode.Living
{
    public sealed class ColonyNeedsAcceptanceReplayTests
    {
        private static readonly SiteId Site = new SiteId(30UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);

        [Test]
        public void ThreeDaysWithoutFood_RefusesWork_ThenMeal_AllowsWork()
        {
            var actors = new ActorStore();
            var actor = new ActorRecord(
                new ActorId(99UL),
                "Pawn",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10,10), new VitalStat(10,10), new VitalStat(10,10)),
                new GridPosition(0,0),
                accuracy: 5,
                dodge: 1,
                armor: 0,
                baseDamage: 1,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) });

            actors.Add(actor);

            var board = new JobBoard();
            var job = new JobRequest(new JobId(11UL), new RecipeId(211UL), Site, FurnacePosition, WorksiteKind.Furnace, JobKind.Smith, JobPriority.Active(1), quantity: 1, requesterId: new ActorId(0UL));
            board.Add(job);

            var worksites = new WorksiteStore();
            worksites.Add(new WorksiteRecord(Site, FurnacePosition, WorksiteKind.Furnace, isActive: true));

            var eventLog = new WorldEventLog();
            var needsSystem = new NeedsSystem();
            var assigner = new JobAssignmentSystem();
            var now = default(GameTime);

            // three ticks lowers mood under refusal threshold
            needsSystem.TickActorNeeds(actor, eventLog, now, ticks: 3);
            Assert.That(actor.Mood.IsLow, Is.True);

            // refusing actor should not claim the job and should emit JobRefused
            Assert.That(assigner.TryAssignNext(actors, board, worksites, eventLog, now, out var result1), Is.False);
            Assert.That(eventLog.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(eventLog.Events[0].Kind, Is.EqualTo(WorldEventKind.JobRefused));
            Assert.That(board.IsClaimed(job.Id), Is.False);

            // Give the actor one food item and an eat recipe
            var inventory = new InventoryState(4);
            var apple = new InventoryItem(new EmberCrpg.Domain.Core.ItemId(500UL), "food_apple", "Apple", 1);
            Assert.That(inventory.TryAdd(apple), Is.True);

            var eatRecipe = new NeedRecoveryRecipe("eat_apple", NeedRecoverySystem.EatMealAction, NeedKind.Hunger, 50, "food_apple");
            var recovery = new NeedRecoverySystem();

            var ate = recovery.EatMeal(actor, inventory, eatRecipe, eventLog, now);
            Assert.That(ate, Is.True);

            // after eating the actor should be able to claim the job
            Assert.That(assigner.TryAssignNext(actors, board, worksites, eventLog, now, out var result2), Is.True);
            Assert.That(board.IsClaimed(job.Id), Is.True);
        }
    }
}

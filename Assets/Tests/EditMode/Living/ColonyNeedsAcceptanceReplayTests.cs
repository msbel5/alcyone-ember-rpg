using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

// Design note:
// These tests replay Sprint 4's player-visible colony-needs acceptance path:
// missed meals block work, a concrete meal recovery unblocks work, and the
// hunger refusal boundary stays explicit.
namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>Verifies colony-needs refusal and recovery acceptance behavior.</summary>
    public sealed class ColonyNeedsAcceptanceReplayTests
    {
        private static readonly SiteId Site = new SiteId(40UL);
        private static readonly GridPosition WorksitePos = new GridPosition(2, 3);
        private static readonly ActorId Requester = new ActorId(99UL);

        [Test]
        public void AcceptanceReplay_ThreeDaysUnfed_RefusesThenRecoversAfterMeal()
        {
            var actors = new ActorStore();
            var actor = CreateActorWithPreference(
                1UL,
                new ActorNeeds(),
                JobKind.Smith);
            actors.Add(actor);

            var board = new JobBoard();
            var job = MakeSmithingJob(200UL, 300UL);
            board.Add(job);

            var worksites = ActiveFurnaceStore();
            var log = new WorldEventLog();
            var needsSystem = new NeedsSystem();

            needsSystem.TickActorNeeds(actor, log, new GameTime(0), ticks: 3);

            var assigner = new JobAssignmentSystem();
            var now = default(GameTime);

            Assert.That(assigner.TryAssignNext(actors, board, worksites, log, now, out var result), Is.False);
            AssertJobRefused(log, actor.Id, job.Id);
            Assert.That(board.IsClaimed(job.Id), Is.False);

            var inventory = MealInventory(quantity: 1);
            var recovered = new NeedRecoverySystem().EatMeal(actor, inventory, MealRecipe(), log, new GameTime(1000));

            Assert.That(recovered, Is.True);
            Assert.That(actor.Needs.Hunger.Value, Is.LessThan(80));

            // Codex audit (eighth pass A-P1): Thirst now decays with ticks
            // too, so after three days unfed the actor is also dehydrated
            // and tired. EatMeal only restores Hunger — to exercise the
            // post-recovery assignment branch, simulate the actor having
            // also drunk and rested (recovery actions outside this atom's
            // scope) so mood crosses back above ActorMood.LowMoodThreshold.
            actor.ApplyNeeds(new ActorNeeds(
                actor.Needs.Hunger,
                NeedValue.Comfortable,
                NeedValue.Comfortable));
            new NeedsSystem().RecomputeMood(actor);

            // After recovery, assignment should succeed
            var assigned = assigner.TryAssignNext(actors, board, worksites, log, now, out result);
            Assert.That(assigned, Is.True);
            Assert.That(board.IsClaimed(job.Id), Is.True);
        }

        [Test]
        public void AcceptanceReplay_HungerBoundary_RefusesAtThreshold()
        {
            var actors = new ActorStore();
            var actor = CreateActorWithPreference(
                2UL,
                new ActorNeeds(new NeedValue(80), NeedValue.Comfortable, NeedValue.Comfortable),
                JobKind.Smith);
            actors.Add(actor);

            var board = new JobBoard();
            var job = MakeSmithingJob(201UL, 301UL);
            board.Add(job);

            var log = new WorldEventLog();
            var assigner = new JobAssignmentSystem();

            Assert.That(
                assigner.TryAssignNext(actors, board, ActiveFurnaceStore(), log, default(GameTime), out _),
                Is.False);
            AssertJobRefused(log, actor.Id, job.Id);
            Assert.That(board.IsClaimed(job.Id), Is.False);
        }

        [Test]
        public void AcceptanceReplay_HungerOnly_TriggersRefusal()
        {
            var actors = new ActorStore();
            var actor = CreateActorWithPreference(
                3UL,
                new ActorNeeds(new NeedValue(90), NeedValue.Comfortable, NeedValue.Comfortable),
                JobKind.Smith);
            actors.Add(actor);

            var board = new JobBoard();
            var job = MakeSmithingJob(202UL, 302UL);
            board.Add(job);

            var log = new WorldEventLog();
            var assigner = new JobAssignmentSystem();

            Assert.That(
                assigner.TryAssignNext(actors, board, ActiveFurnaceStore(), log, default(GameTime), out _),
                Is.False);
            AssertJobRefused(log, actor.Id, job.Id);
            Assert.That(board.IsClaimed(job.Id), Is.False);
        }

        private static JobRequest MakeSmithingJob(ulong jobId, ulong recipeId)
        {
            return new JobRequest(
                new JobId(jobId),
                new RecipeId(recipeId),
                Site,
                WorksitePos,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(1),
                quantity: 1,
                requesterId: Requester);
        }

        private static WorksiteStore ActiveFurnaceStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(Site, WorksitePos, WorksiteKind.Furnace, isActive: true));
            return store;
        }

        private static ActorRecord CreateActorWithPreference(ulong actorId, ActorNeeds needs, JobKind jobKind)
        {
            var pref = new ActorJobPreference(jobKind, JobPriority.Active(1));
            return new ActorRecord(
                new ActorId(actorId),
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
            return new NeedRecoveryRecipe(
                "meal-basic",
                NeedRecoverySystem.EatMealAction,
                NeedKind.Hunger,
                50,
                "simple_meal");
        }

        private static InventoryState MealInventory(int quantity)
        {
            var inventory = new InventoryState(4);
            inventory.TryAdd(new InventoryItem(new ItemId(900UL), "simple_meal", "Simple Meal", quantity));
            return inventory;
        }

        private static void AssertJobRefused(WorldEventLog log, ActorId actorId, JobId jobId)
        {
            var refusal = log.Events.Single(e => e.Kind == WorldEventKind.JobRefused);

            Assert.That(refusal.ActorId, Is.EqualTo(actorId));
            Assert.That(refusal.SiteId, Is.EqualTo(Site));
            Assert.That(refusal.Reason, Is.EqualTo($"job_refused:{jobId.Value}"));
            Assert.That(refusal.ReasonTrace.Causes, Is.EqualTo(new[]
            {
                $"job:{jobId.Value}",
                $"actor:{actorId.Value}",
                "reason:hunger_or_low_mood",
            }));

            // PR#132 bot review fix: this replay shares the same eventLog
            // across TickActorNeeds and TryAssignNext. TickActorNeeds appends
            // NeedChanged BEFORE refusal, so a correct replay must observe
            // the NeedChanged-then-JobRefused order. Pinning the index here
            // catches a regression where assignment runs before the need tick.
            var events = log.Events.ToList();
            var needsTickIndex = events.FindIndex(e => e.Kind == WorldEventKind.NeedChanged);
            var refusalIndex = events.IndexOf(refusal);
            if (needsTickIndex >= 0)
                Assert.That(refusalIndex, Is.GreaterThan(needsTickIndex),
                    "JobRefused must be appended AFTER NeedChanged in the shared event log.");
        }
    }
}

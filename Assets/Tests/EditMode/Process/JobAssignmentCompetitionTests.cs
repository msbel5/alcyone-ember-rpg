using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

// Design note:
// These tests pin the Phase 3 competition-proof bundle: a second concrete recipe
// lane exists, job assignment still prefers the higher-priority smithing actor,
// and baking waits when the required bakery worksite is absent.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies deterministic competition between smithing and baking job lanes.</summary>
    public sealed class JobAssignmentCompetitionTests
    {
        private static readonly SiteId Site = new SiteId(31UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);
        private static readonly GridPosition BakeryPosition = new GridPosition(8, 2);
        private static readonly ActorId Requester = new ActorId(99UL);

        [Test]
        public void HigherPrioritySmithWinsFurnace()
        {
            var actors = new ActorStore();
            var lowerPriorityBaker = CreateActor(1UL, "Baker", new ActorJobPreference(JobKind.Baker, JobPriority.Active(2)));
            var higherPrioritySmith = CreateActor(2UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(lowerPriorityBaker);
            actors.Add(higherPrioritySmith);
            var breadRecipe = RecipeFixtureCatalog.BakeBread(new RecipeId(301UL));
            var smeltRecipe = RecipeFixtureCatalog.SmeltIronIngot(new RecipeId(302UL));
            var board = new JobBoard();
            var breadJob = MakeRequest(10UL, breadRecipe.Id, BakeryPosition, WorksiteKind.Bakery, JobKind.Baker);
            var smeltJob = MakeRequest(11UL, smeltRecipe.Id, FurnacePosition, WorksiteKind.Furnace, JobKind.Smith);
            board.Add(breadJob);
            board.Add(smeltJob);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, ActiveFurnaceAndBakeryStore(), out var result), Is.True);

            Assert.That(result.ActorId, Is.EqualTo(higherPrioritySmith.Id));
            Assert.That(result.JobId, Is.EqualTo(smeltJob.Id));
            Assert.That(result.WorksitePosition, Is.EqualTo(FurnacePosition));
            Assert.That(board.GetClaimedBy(smeltJob.Id), Is.EqualTo(higherPrioritySmith.Id));
            Assert.That(board.IsClaimed(breadJob.Id), Is.False);
            Assert.That(lowerPriorityBaker.ScheduleState.IsIdle, Is.True);
            Assert.That(higherPrioritySmith.ScheduleState, Is.EqualTo(ActorScheduleState.Assigned(smeltJob.Id, Site, FurnacePosition)));
        }

        [Test]
        public void BreadJobWaitsWithoutBakery()
        {
            var actors = new ActorStore();
            var baker = CreateActor(1UL, "Baker", new ActorJobPreference(JobKind.Baker, JobPriority.Active(1)));
            actors.Add(baker);
            var breadRecipe = RecipeFixtureCatalog.BakeBread(new RecipeId(301UL));
            var board = new JobBoard();
            var breadJob = MakeRequest(10UL, breadRecipe.Id, BakeryPosition, WorksiteKind.Bakery, JobKind.Baker);
            board.Add(breadJob);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, ActiveFurnaceStore(), out var result), Is.False);

            Assert.That(result, Is.EqualTo(default(JobAssignmentResult)));
            Assert.That(board.IsClaimed(breadJob.Id), Is.False);
            Assert.That(baker.ScheduleState.IsIdle, Is.True);
        }

        private static JobRequest MakeRequest(
            ulong jobId,
            RecipeId recipeId,
            GridPosition worksitePosition,
            WorksiteKind worksiteKind,
            JobKind kind)
        {
            return new JobRequest(
                new JobId(jobId),
                recipeId,
                Site,
                worksitePosition,
                worksiteKind,
                kind,
                JobPriority.Active(1),
                quantity: 1,
                requesterId: Requester);
        }

        private static WorksiteStore ActiveFurnaceAndBakeryStore()
        {
            var store = ActiveFurnaceStore();
            store.Add(new WorksiteRecord(Site, BakeryPosition, WorksiteKind.Bakery, isActive: true));
            return store;
        }

        private static WorksiteStore ActiveFurnaceStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(Site, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            return store;
        }

        private static ActorRecord CreateActor(ulong id, string name, ActorJobPreference preference)
        {
            return new ActorRecord(
                new ActorId(id),
                name,
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 5,
                armor: 1,
                baseDamage: 3,
                jobPreferences: new[] { preference });
        }
    }
}

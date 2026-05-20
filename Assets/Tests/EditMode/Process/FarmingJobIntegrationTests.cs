using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies Faz 5 farming jobs enter the existing assignment/path target lane.</summary>
    public sealed class FarmingJobIntegrationTests
    {
        private static readonly SiteId FarmSite = new SiteId(5UL);
        private static readonly GridPosition FieldPosition = new GridPosition(3, 4);
        private static readonly ActorId Requester = new ActorId(99UL);

        [Test]
        public void PlantingJob_AssignsFarmerToFieldWorksite()
        {
            var farmer = CreateFarmer();
            var actors = new ActorStore();
            actors.Add(farmer);
            var board = new JobBoard();
            var job = FarmingJobRequestFactory.CreatePlantingJob(new JobId(100), FarmSite, FieldPosition, Requester, JobPriority.Active(1));
            board.Add(job);

            Assert.That(new JobAssignmentSystem().TryAssignNext(actors, board, ActiveFieldStore(), out var result), Is.True);

            Assert.That(result, Is.EqualTo(new JobAssignmentResult(farmer.Id, job.Id, FarmSite, FieldPosition)));
            Assert.That(board.GetClaimedBy(job.Id), Is.EqualTo(farmer.Id));
            Assert.That(farmer.ScheduleState, Is.EqualTo(ActorScheduleState.Assigned(job.Id, FarmSite, FieldPosition)));
        }

        [Test]
        public void HarvestJob_WaitsWithoutActiveFieldWorksite()
        {
            var actors = new ActorStore();
            actors.Add(CreateFarmer());
            var board = new JobBoard();
            var job = FarmingJobRequestFactory.CreateHarvestJob(new JobId(101), FarmSite, FieldPosition, Requester, JobPriority.Active(1));
            board.Add(job);

            Assert.That(new JobAssignmentSystem().TryAssignNext(actors, board, new WorksiteStore(), out var result), Is.False);

            Assert.That(result, Is.EqualTo(default(JobAssignmentResult)));
            Assert.That(board.IsClaimed(job.Id), Is.False);
        }

        [Test]
        public void Factory_CreatesDistinctPlantAndHarvestRequests()
        {
            var plant = FarmingJobRequestFactory.CreatePlantingJob(new JobId(100), FarmSite, FieldPosition, Requester, JobPriority.Active(2), quantity: 3);
            var harvest = FarmingJobRequestFactory.CreateHarvestJob(new JobId(101), FarmSite, FieldPosition, Requester, JobPriority.Active(1));

            Assert.That(plant.RecipeId, Is.EqualTo(FarmingJobRequestFactory.PlantCropRecipeId));
            Assert.That(harvest.RecipeId, Is.EqualTo(FarmingJobRequestFactory.HarvestCropRecipeId));
            Assert.That(plant.WorksiteKind, Is.EqualTo(WorksiteKind.Field));
            Assert.That(harvest.Kind, Is.EqualTo(JobKind.Farmer));
            Assert.That(plant.Quantity, Is.EqualTo(3));
        }

        private static ActorRecord CreateFarmer()
        {
            return new ActorRecord(
                new ActorId(1UL),
                "Farmer",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                accuracy: 5,
                dodge: 1,
                armor: 0,
                baseDamage: 1,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Farmer, JobPriority.Active(1)) });
        }

        private static WorksiteStore ActiveFieldStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(FarmSite, FieldPosition, WorksiteKind.Field, isActive: true));
            return store;
        }
    }
}

using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Save;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// Pins the Phase 3 job-save-proof rail: JsonSliceSaveService carries JobBoard state like the
// existing Worksite rail, while actor records carry their schedule target through
// ActorSaveData. W34 WORK migration (docs/ruh/w34/02 §5.2): the in-flight order rides
// WorldState.WorkOrders (jobId-bound Domain row) instead of the retired Simulation park list —
// the same save now proves claim + schedule + bench progress reload as ONE consistent truth,
// which is exactly the double-consumption wound's closing condition.
namespace EmberCrpg.Tests.EditMode.Save
{
    public sealed class JobAssignmentRoundTripTests
    {
        private static readonly JobId Job = new JobId(701UL);
        private static readonly RecipeId Recipe = new RecipeId(1001UL);
        private static readonly SiteId FurnaceSite = new SiteId(77UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);

        [Test]
        public void JsonDto_RoundTripsClaimedJobBoardActorScheduleAndWorkOrderRow()
        {
            var world = new WorldFactory().Create(303);
            var actor = world.Actors.FirstByRole(ActorRole.Player);
            actor.ApplyJobPreferences(new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) });
            actor.ApplyScheduleState(ActorScheduleState.Assigned(Job, FurnaceSite, FurnacePosition));

            var board = new JobBoard();
            var request = new JobRequest(
                Job,
                Recipe,
                FurnaceSite,
                FurnacePosition,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(2),
                quantity: 2,
                requesterId: actor.Id);
            board.Add(request);
            Assert.That(board.TryClaim(Job, actor.Id, out _), Is.True);

            var service = new JsonSliceSaveService()
            {
                Jobs = board,
                Worksites = CreateActiveFurnaceStore(),
            };
            // W34: the claimed job's bench progress lives on the WORLD now — one execution done,
            // the second mid-stroke. The load below must resume, never re-fund (§5.2 invariant).
            world.WorkOrders.Add(new WorkOrderRecord
            {
                JobId = Job.Value,
                RecipeId = Recipe.Value,
                SiteId = FurnaceSite.Value,
                PositionX = FurnacePosition.X,
                PositionY = FurnacePosition.Y,
                StartedByActorId = actor.Id.Value,
                ProgressTicks = 1,
                CompletedExecutions = 1,
            });

            var json = service.SaveToJson(world);
            Assert.That(json, Does.Contain("jobs"));
            Assert.That(json, Does.Contain("currentJobId"));

            var loaded = service.LoadFromJson(json);
            var loadedActor = loaded.Actors.Get(actor.Id);
            var loadedJob = service.Jobs.Requests.Single();

            Assert.That(loadedActor.JobPreferences.Single().Kind, Is.EqualTo(JobKind.Smith));
            Assert.That(loadedActor.JobPreferences.Single().Priority, Is.EqualTo(JobPriority.Active(1)));
            Assert.That(loadedActor.ScheduleState.CurrentJobId, Is.EqualTo(Job));
            Assert.That(loadedActor.ScheduleState.TargetSiteId, Is.EqualTo(FurnaceSite));
            Assert.That(loadedActor.ScheduleState.TargetWorksitePosition, Is.EqualTo(FurnacePosition));

            Assert.That(loadedJob.Id, Is.EqualTo(Job));
            Assert.That(loadedJob.RecipeId, Is.EqualTo(Recipe));
            Assert.That(loadedJob.Quantity, Is.EqualTo(2));
            Assert.That(service.Jobs.GetClaimedBy(Job), Is.EqualTo(actor.Id));
            Assert.That(service.Worksites.Get(FurnaceSite, FurnacePosition).IsActive, Is.True);

            Assert.That(loaded.WorkOrders.TryGetByJob(Job.Value, out var row), Is.True,
                "the order row rebinds to the claim by jobId — the save wound's structural close");
            Assert.That(row.ProgressTicks, Is.EqualTo(1), "ProgressTicks > 0 means funded: no re-consumption on load");
            Assert.That(row.CompletedExecutions, Is.EqualTo(1));
        }

        private static WorksiteStore CreateActiveFurnaceStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(FurnaceSite, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            return store;
        }
    }
}

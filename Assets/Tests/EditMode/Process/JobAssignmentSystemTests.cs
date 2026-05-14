using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

// Design note:
// These tests pin Faz 3's first assignment-system pass: actor preference rows
// claim pending jobs and write ActorScheduleState. Recipe starts, EventLog rows,
// save/load, and completion are intentionally later atoms.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies deterministic actor-to-job claiming.</summary>
    public sealed class JobAssignmentSystemTests
    {
        private static readonly SiteId Site = new SiteId(30UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);
        private static readonly ActorId Requester = new ActorId(99UL);

        [Test]
        public void TryAssignNext_AssignsTwoSmithsDeterministically()
        {
            var actors = new ActorStore();
            var first = CreateActor(1UL, "Ada", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            var second = CreateActor(2UL, "Borin", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(first);
            actors.Add(second);
            var board = new JobBoard();
            var firstJob = MakeRequest(10UL, priority: 1);
            var secondJob = MakeRequest(11UL, priority: 1);
            board.Add(firstJob);
            board.Add(secondJob);
            var worksites = ActiveFurnaceStore();
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, worksites, out var firstResult), Is.True);
            Assert.That(system.TryAssignNext(actors, board, worksites, out var secondResult), Is.True);

            Assert.That(firstResult, Is.EqualTo(new JobAssignmentResult(first.Id, firstJob.Id, Site, FurnacePosition)));
            Assert.That(secondResult, Is.EqualTo(new JobAssignmentResult(second.Id, secondJob.Id, Site, FurnacePosition)));
            Assert.That(board.GetClaimedBy(firstJob.Id), Is.EqualTo(first.Id));
            Assert.That(board.GetClaimedBy(secondJob.Id), Is.EqualTo(second.Id));
            Assert.That(first.ScheduleState, Is.EqualTo(ActorScheduleState.Assigned(firstJob.Id, Site, FurnacePosition)));
            Assert.That(second.ScheduleState, Is.EqualTo(ActorScheduleState.Assigned(secondJob.Id, Site, FurnacePosition)));
        }

        [Test]
        public void TryAssignNext_UsesActorPriorityBeforeActorOrder()
        {
            var actors = new ActorStore();
            var lowPreference = CreateActor(1UL, "Low", new ActorJobPreference(JobKind.Smith, JobPriority.Active(3)));
            var highPreference = CreateActor(2UL, "High", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(lowPreference);
            actors.Add(highPreference);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1);
            board.Add(job);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, ActiveFurnaceStore(), out var result), Is.True);

            Assert.That(result.ActorId, Is.EqualTo(highPreference.Id));
            Assert.That(board.GetClaimedBy(job.Id), Is.EqualTo(highPreference.Id));
            Assert.That(lowPreference.ScheduleState.IsIdle, Is.True);
        }

        [Test]
        public void TryAssignNext_IgnoresDisabledOrUnavailableActors()
        {
            var actors = new ActorStore();
            var disabled = CreateActor(1UL, "Disabled", ActorJobPreference.Disabled(JobKind.Smith));
            var noPreference = CreateActor(2UL, "No Preference");
            actors.Add(disabled);
            actors.Add(noPreference);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1);
            board.Add(job);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, ActiveFurnaceStore(), out var result), Is.False);

            Assert.That(result, Is.EqualTo(default(JobAssignmentResult)));
            Assert.That(board.IsClaimed(job.Id), Is.False);
            Assert.That(disabled.ScheduleState.IsIdle, Is.True);
            Assert.That(noPreference.ScheduleState.IsIdle, Is.True);
        }

        [Test]
        public void CanActorWorkJob_RequiresAliveIdlePreferenceAndActiveWorksite()
        {
            var system = new JobAssignmentSystem();
            var job = MakeRequest(10UL, priority: 1);
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));

            Assert.That(system.CanActorWorkJob(actor, job, ActiveFurnaceStore()), Is.True);

            var busy = CreateActor(2UL, "Busy", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            busy.ApplyScheduleState(ActorScheduleState.Assigned(new JobId(77UL), Site, FurnacePosition));
            Assert.That(system.CanActorWorkJob(busy, job, ActiveFurnaceStore()), Is.False);

            var dead = CreateActor(3UL, "Dead", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)), alive: false);
            Assert.That(system.CanActorWorkJob(dead, job, ActiveFurnaceStore()), Is.False);
            Assert.That(system.CanActorWorkJob(actor, job, InactiveFurnaceStore()), Is.False);
        }

        private static JobRequest MakeRequest(ulong jobId, int priority)
        {
            return new JobRequest(
                new JobId(jobId),
                new RecipeId(200UL + jobId),
                Site,
                FurnacePosition,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(priority),
                quantity: 1,
                requesterId: Requester);
        }

        private static WorksiteStore ActiveFurnaceStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(Site, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            return store;
        }

        private static WorksiteStore InactiveFurnaceStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(Site, FurnacePosition, WorksiteKind.Furnace, isActive: false));
            return store;
        }

        private static ActorRecord CreateActor(ulong id, string name, ActorJobPreference preference = default, bool alive = true)
        {
            ActorJobPreference[] preferences = preference.Kind == JobKind.None ? null : new[] { preference };
            return new ActorRecord(
                new ActorId(id),
                name,
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(alive ? 12 : 0, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 5,
                armor: 1,
                baseDamage: 3,
                jobPreferences: preferences);
        }
    }
}

using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    public sealed class JobNeedsRefusalTests
    {
        private static readonly SiteId Site = new SiteId(30UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);
        private static readonly ActorId Requester = new ActorId(99UL);

        [Test]
        public void TryAssignNext_RejectsHungryLowMoodActorAndEmitsJobRefused()
        {
            var actors = new ActorStore();

            var needs = new ActorNeeds(new NeedValue(90), new NeedValue(0), new NeedValue(0));
            var mood = new ActorMood(20); // low mood
            var pref = new ActorJobPreference(JobKind.Smith, JobPriority.Active(1));
            var actor = new ActorRecord(
                new ActorId(1UL),
                "Hungry",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 5,
                armor: 1,
                baseDamage: 3,
                jobPreferences: new[] { pref },
                scheduleState: default,
                needs: needs,
                mood: mood);

            actors.Add(actor);

            var board = new JobBoard();
            var job = new JobRequest(new JobId(10UL), new RecipeId(210UL), Site, FurnacePosition, WorksiteKind.Furnace, JobKind.Smith, JobPriority.Active(1), quantity: 1, requesterId: new ActorId(99UL));
            board.Add(job);

            var worksites = new WorksiteStore();
            worksites.Add(new WorksiteRecord(Site, FurnacePosition, WorksiteKind.Furnace, isActive: true));

            var eventLog = new WorldEventLog();
            var system = new JobAssignmentSystem();
            var now = default(GameTime);

            Assert.That(system.TryAssignNext(actors, board, worksites, eventLog, now, out var result), Is.False);
            Assert.That(eventLog.Count, Is.EqualTo(1));
            Assert.That(eventLog.Events[0].Kind, Is.EqualTo(WorldEventKind.JobRefused));
            Assert.That(board.IsClaimed(job.Id), Is.False);
        }

        [Test]
        public void TryAssignNext_WithEventLog_RefusesAtHungerBoundary()
        {
            var actor = CreateActor(2UL, new ActorNeeds(new NeedValue(80), NeedValue.Comfortable, NeedValue.Comfortable), ActorMood.Neutral);
            var actors = StoreWith(actor);
            var board = BoardWith(out var job);
            var eventLog = new WorldEventLog();
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, ActiveFurnaceStore(), eventLog, default(GameTime), out var result), Is.False);

            Assert.That(result, Is.EqualTo(default(JobAssignmentResult)));
            Assert.That(board.IsClaimed(job.Id), Is.False);
            AssertRefusal(eventLog, actor.Id, job.Id);
        }

        [Test]
        public void TryAssignNext_WithEventLog_LowMoodOnlyEmitsJobRefused()
        {
            var actor = CreateActor(3UL, new ActorNeeds(), new ActorMood(20));
            var actors = StoreWith(actor);
            var board = BoardWith(out var job);
            var eventLog = new WorldEventLog();
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, ActiveFurnaceStore(), eventLog, default(GameTime), out _), Is.False);

            Assert.That(board.IsClaimed(job.Id), Is.False);
            AssertRefusal(eventLog, actor.Id, job.Id);
        }

        [Test]
        public void TryAssignNext_WithoutEventLog_RefusesWithoutClaimingJob()
        {
            var actor = CreateActor(4UL, new ActorNeeds(new NeedValue(90), NeedValue.Comfortable, NeedValue.Comfortable), ActorMood.Neutral);
            var actors = StoreWith(actor);
            var board = BoardWith(out var job);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, ActiveFurnaceStore(), out var result), Is.False);

            Assert.That(result, Is.EqualTo(default(JobAssignmentResult)));
            Assert.That(board.IsClaimed(job.Id), Is.False);
            Assert.That(actor.ScheduleState.IsIdle, Is.True);
        }

        [Test]
        public void TryAssignNext_WithEventLog_DoesNotLogRefusalWhenWorksiteUnavailable()
        {
            var actor = CreateActor(5UL, new ActorNeeds(new NeedValue(90), NeedValue.Comfortable, NeedValue.Comfortable), ActorMood.Neutral);
            var actors = StoreWith(actor);
            var board = BoardWith(out var job);
            var eventLog = new WorldEventLog();
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, InactiveFurnaceStore(), eventLog, default(GameTime), out _), Is.False);

            Assert.That(eventLog.Count, Is.EqualTo(0));
            Assert.That(board.IsClaimed(job.Id), Is.False);
        }

        private static ActorStore StoreWith(ActorRecord actor)
        {
            var actors = new ActorStore();
            actors.Add(actor);
            return actors;
        }

        private static JobBoard BoardWith(out JobRequest job)
        {
            var board = new JobBoard();
            job = new JobRequest(
                new JobId(10UL),
                new RecipeId(210UL),
                Site,
                FurnacePosition,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(1),
                quantity: 1,
                requesterId: Requester);
            board.Add(job);
            return board;
        }

        private static WorksiteStore ActiveFurnaceStore()
        {
            var worksites = new WorksiteStore();
            worksites.Add(new WorksiteRecord(Site, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            return worksites;
        }

        private static WorksiteStore InactiveFurnaceStore()
        {
            var worksites = new WorksiteStore();
            worksites.Add(new WorksiteRecord(Site, FurnacePosition, WorksiteKind.Furnace, isActive: false));
            return worksites;
        }

        private static ActorRecord CreateActor(ulong actorId, ActorNeeds needs, ActorMood mood)
        {
            return new ActorRecord(
                new ActorId(actorId),
                "Hungry",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 5,
                armor: 1,
                baseDamage: 3,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) },
                scheduleState: default,
                needs: needs,
                mood: mood);
        }

        private static void AssertRefusal(WorldEventLog eventLog, ActorId actorId, JobId jobId)
        {
            Assert.That(eventLog.Count, Is.EqualTo(1));
            Assert.That(eventLog.Events[0].Kind, Is.EqualTo(WorldEventKind.JobRefused));
            Assert.That(eventLog.Events[0].ActorId, Is.EqualTo(actorId));
            Assert.That(eventLog.Events[0].Reason, Is.EqualTo($"job_refused:{jobId.Value}"));
            Assert.That(eventLog.Events[0].ReasonTrace.Causes, Is.EqualTo(new[]
            {
                $"job:{jobId.Value}",
                $"actor:{actorId.Value}",
                "reason:hunger_or_low_mood",
            }));
        }
    }
}

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
    }
}

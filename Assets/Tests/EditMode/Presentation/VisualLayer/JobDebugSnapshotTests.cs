using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Visual;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation.VisualLayer
{
    /// <summary>Pins deterministic snapshot rows for the Unity job debug surface.</summary>
    public sealed class JobDebugSnapshotTests
    {
        private static readonly SiteId Site = new SiteId(3UL);
        private static readonly GridPosition Worksite = new GridPosition(5, 5);
        private static readonly ActorId Requester = new ActorId(6UL);
        private static readonly ActorId Smith = new ActorId(10UL);

        private static JobRequest MakeJob()
        {
            return new JobRequest(
                new JobId(1UL),
                new RecipeId(100UL),
                Site,
                Worksite,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(1),
                quantity: 1,
                requesterId: Requester);
        }

        private static ActorRecord MakeSmith(string name = "Smith")
        {
            return new ActorRecord(
                Smith, name, ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(0, 0),
                accuracy: 10, dodge: 5, armor: 1, baseDamage: 3);
        }

        [Test]
        public void EmptyBoard_ProducesEmptySnapshot()
        {
            var snapshot = JobDebugSnapshot.FromStores(new ActorStore(), new JobBoard(), new WorksiteStore());
            Assert.That(snapshot.Rows, Is.Empty);
        }

        [Test]
        public void PendingJob_HasPendingStatusAndEmptyActor()
        {
            var board = new JobBoard();
            board.Add(MakeJob());

            var snapshot = JobDebugSnapshot.FromStores(new ActorStore(), board, new WorksiteStore());

            Assert.That(snapshot.Rows.Count, Is.EqualTo(1));
            var row = snapshot.Rows[0];
            Assert.That(row.StatusCode, Is.EqualTo("pending"));
            Assert.That(row.ActorId.IsEmpty, Is.True);
            Assert.That(row.QueueIndex, Is.EqualTo(-1));
            Assert.That(row.JobKindCode, Is.EqualTo("Smith"));
        }

        [Test]
        public void ClaimedJob_SurfacesActorNameAndAssignedStatus()
        {
            var actors = new ActorStore();
            var board = new JobBoard();
            actors.Add(MakeSmith("Yorick"));
            board.Add(MakeJob());
            board.TryClaim(new JobId(1UL), Smith, out _);

            var snapshot = JobDebugSnapshot.FromStores(actors, board, new WorksiteStore());

            var row = snapshot.Rows[0];
            Assert.That(row.StatusCode, Is.EqualTo("assigned"));
            Assert.That(row.ActorName, Is.EqualTo("Yorick"));
            Assert.That(row.QueueIndex, Is.EqualTo(0));
        }
    }
}

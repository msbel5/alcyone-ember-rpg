using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>
    /// Pins deterministic queue ordering for multiple actors targeting the same worksite.
    /// Closes CO-04 row in docs/sprint-phase-4-atom-map.md Debt ledger.
    /// </summary>
    public sealed class JobAssignmentQueueIndexTests
    {
        private static readonly SiteId Site = new SiteId(3UL);
        private static readonly GridPosition Furnace = new GridPosition(5, 5);
        private static readonly GridPosition Bakery = new GridPosition(8, 8);
        private static readonly ActorId Requester = new ActorId(6UL);
        private static readonly ActorId SmithA = new ActorId(10UL);
        private static readonly ActorId SmithB = new ActorId(11UL);
        private static readonly ActorId SmithC = new ActorId(12UL);
        private static readonly ActorId Baker = new ActorId(20UL);

        private static JobRequest MakeFurnaceJob(ulong jobId)
        {
            return new JobRequest(
                new JobId(jobId),
                new RecipeId(100UL),
                Site,
                Furnace,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(1),
                quantity: 1,
                requesterId: Requester);
        }

        private static JobRequest MakeBakeryJob(ulong jobId)
        {
            return new JobRequest(
                new JobId(jobId),
                new RecipeId(200UL),
                Site,
                Bakery,
                WorksiteKind.Bakery,
                JobKind.Baker,
                JobPriority.Active(1),
                quantity: 1,
                requesterId: Requester);
        }

        [Test]
        public void UnclaimedJob_HasNoQueueIndex()
        {
            var board = new JobBoard();
            var job = MakeFurnaceJob(1UL);
            board.Add(job);

            Assert.That(board.GetQueueIndex(job.Id), Is.EqualTo(-1));
        }

        [Test]
        public void TwoActorsClaimingSameWorksite_ReceiveDistinctQueueIndices_InClaimOrder()
        {
            var board = new JobBoard();
            var first = MakeFurnaceJob(1UL);
            var second = MakeFurnaceJob(2UL);
            board.Add(first);
            board.Add(second);

            Assert.That(board.TryClaim(first.Id, SmithA, out _), Is.True);
            Assert.That(board.TryClaim(second.Id, SmithB, out _), Is.True);

            Assert.That(board.GetQueueIndex(first.Id), Is.EqualTo(0));
            Assert.That(board.GetQueueIndex(second.Id), Is.EqualTo(1));
        }

        [Test]
        public void ThreeActorsClaimingSameWorksite_KeepDeterministicClaimOrder()
        {
            var board = new JobBoard();
            var first = MakeFurnaceJob(1UL);
            var second = MakeFurnaceJob(2UL);
            var third = MakeFurnaceJob(3UL);
            board.Add(first);
            board.Add(second);
            board.Add(third);

            board.TryClaim(third.Id, SmithC, out _);
            board.TryClaim(first.Id, SmithA, out _);
            board.TryClaim(second.Id, SmithB, out _);

            Assert.That(board.GetQueueIndex(third.Id), Is.EqualTo(0));
            Assert.That(board.GetQueueIndex(first.Id), Is.EqualTo(1));
            Assert.That(board.GetQueueIndex(second.Id), Is.EqualTo(2));
        }

        [Test]
        public void QueueIndex_IsPerWorksite_NotGlobal()
        {
            var board = new JobBoard();
            var furnaceJob = MakeFurnaceJob(1UL);
            var bakeryJob = MakeBakeryJob(2UL);
            board.Add(furnaceJob);
            board.Add(bakeryJob);

            board.TryClaim(furnaceJob.Id, SmithA, out _);
            board.TryClaim(bakeryJob.Id, Baker, out _);

            Assert.That(board.GetQueueIndex(furnaceJob.Id), Is.EqualTo(0));
            Assert.That(board.GetQueueIndex(bakeryJob.Id), Is.EqualTo(0));
        }

        [Test]
        public void EmptyJobId_ReturnsMinusOne()
        {
            var board = new JobBoard();

            Assert.That(board.GetQueueIndex(default), Is.EqualTo(-1));
        }
    }
}

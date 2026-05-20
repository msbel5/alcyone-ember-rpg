using System;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin Faz 3's pure JobBoard state before actor matching or recipe
// ticking exists: deterministic add/peek/claim/terminal removal behaviour only.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies deterministic pending job queue semantics.</summary>
    public sealed class JobBoardTests
    {
        private static readonly SiteId Site = new SiteId(3UL);
        private static readonly GridPosition Worksite = new GridPosition(4, 5);
        private static readonly ActorId Requester = new ActorId(6UL);
        private static readonly ActorId FirstActor = new ActorId(10UL);
        private static readonly ActorId SecondActor = new ActorId(11UL);

        private static JobRequest MakeRequest(ulong jobId, int priority = 1, ulong recipeId = 100UL)
        {
            return new JobRequest(
                new JobId(jobId),
                new RecipeId(recipeId),
                Site,
                Worksite,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(priority),
                quantity: 1,
                requesterId: Requester);
        }

        /// <summary>Add stores the request and preserves insertion-order enumeration.</summary>
        [Test]
        public void Add_StoresRequestsInInsertionOrder()
        {
            var board = new JobBoard();
            var first = MakeRequest(1UL);
            var second = MakeRequest(2UL);

            board.Add(first);
            board.Add(second);

            Assert.That(board.Count, Is.EqualTo(2));
            Assert.That(board.Contains(first.Id), Is.True);
            Assert.That(board.Requests.ToArray(), Is.EqualTo(new[] { first, second }));
        }

        /// <summary>Add rejects null rows and duplicate job ids.</summary>
        [Test]
        public void Add_RejectsNullOrDuplicateRequest()
        {
            var board = new JobBoard();
            var first = MakeRequest(1UL);
            board.Add(first);

            Assert.Throws<ArgumentNullException>(() => board.Add(null));
            Assert.Throws<InvalidOperationException>(() => board.Add(MakeRequest(1UL, recipeId: 200UL)));
        }

        /// <summary>TryGet exposes requests and returns false for empty or missing ids.</summary>
        [Test]
        public void TryGet_ReturnsRequestWhenPresent()
        {
            var board = new JobBoard();
            var first = MakeRequest(1UL);
            board.Add(first);

            Assert.That(board.TryGet(first.Id, out var actual), Is.True);
            Assert.That(actual, Is.SameAs(first));
            Assert.That(board.TryGet(default, out var empty), Is.False);
            Assert.That(empty, Is.Null);
            Assert.That(board.TryGet(new JobId(99UL), out var missing), Is.False);
            Assert.That(missing, Is.Null);
        }

        /// <summary>Peek chooses lower active priority first, then insertion order for ties.</summary>
        [Test]
        public void TryPeekNext_OrdersByPriorityThenInsertionOrder()
        {
            var board = new JobBoard();
            var low = MakeRequest(1UL, priority: 3);
            var highFirst = MakeRequest(2UL, priority: 1);
            var highSecond = MakeRequest(3UL, priority: 1);
            board.Add(low);
            board.Add(highFirst);
            board.Add(highSecond);

            var found = board.TryPeekNext(out var next);

            Assert.That(found, Is.True);
            Assert.That(next, Is.SameAs(highFirst));
        }

        /// <summary>Claim marks exactly one job for one actor and hides it from future peeks.</summary>
        [Test]
        public void TryClaim_ClaimsJobAndSkipsClaimedRows()
        {
            var board = new JobBoard();
            var first = MakeRequest(1UL);
            var second = MakeRequest(2UL, priority: 2);
            board.Add(first);
            board.Add(second);

            var claimed = board.TryClaim(first.Id, FirstActor, out var actual);

            Assert.That(claimed, Is.True);
            Assert.That(actual, Is.SameAs(first));
            // CO-05 migration: use the JobStatus value object instead of the binary IsClaimed flag.
            Assert.That(board.GetStatus(first.Id), Is.EqualTo(JobStatus.Assigned));
            Assert.That(board.IsClaimed(first.Id), Is.True);
            Assert.That(board.GetClaimedBy(first.Id), Is.EqualTo(FirstActor));
            Assert.That(board.TryPeekNext(out var next), Is.True);
            Assert.That(next, Is.SameAs(second));
        }

        /// <summary>Invalid, missing, already claimed, and duplicate actor claims are rejected.</summary>
        [Test]
        public void TryClaim_RejectsDuplicateOrInvalidClaims()
        {
            var board = new JobBoard();
            var first = MakeRequest(1UL);
            var second = MakeRequest(2UL);
            board.Add(first);
            board.Add(second);

            Assert.That(board.TryClaim(default, FirstActor, out _), Is.False);
            Assert.That(board.TryClaim(first.Id, default, out _), Is.False);
            Assert.That(board.TryClaim(new JobId(99UL), FirstActor, out _), Is.False);
            Assert.That(board.TryClaim(first.Id, FirstActor, out _), Is.True);
            Assert.That(board.TryClaim(first.Id, SecondActor, out _), Is.False);
            Assert.That(board.TryClaim(second.Id, FirstActor, out _), Is.False);
        }

        /// <summary>Complete and Cancel remove terminal jobs and keep remaining order stable.</summary>
        [Test]
        public void CompleteAndCancel_RemoveTerminalJobs()
        {
            var board = new JobBoard();
            var first = MakeRequest(1UL);
            var second = MakeRequest(2UL);
            var third = MakeRequest(3UL);
            board.Add(first);
            board.Add(second);
            board.Add(third);

            Assert.That(board.Complete(second.Id), Is.True);
            Assert.That(board.Cancel(new JobId(99UL)), Is.False);
            Assert.That(board.Cancel(first.Id), Is.True);

            Assert.That(board.Count, Is.EqualTo(1));
            Assert.That(board.Requests.ToArray(), Is.EqualTo(new[] { third }));
        }

        /// <summary>Clear drops requests and claims together.</summary>
        [Test]
        public void Clear_DropsAllJobs()
        {
            var board = new JobBoard();
            board.Add(MakeRequest(1UL));
            board.Add(MakeRequest(2UL));

            board.Clear();

            Assert.That(board.Count, Is.EqualTo(0));
            Assert.That(board.TryPeekNext(out var next), Is.False);
            Assert.That(next, Is.Null);
        }
    }
}

using EmberCrpg.Domain.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>
    /// Pins the eight stable lifecycle codes plus the blocked factory contract for <see cref="JobStatus"/>.
    /// Closes CO-05 row in docs/sprint-faz-4-atom-map.md Debt ledger.
    /// </summary>
    public sealed class JobStatusTests
    {
        [Test]
        public void Pending_Code_IsStable()
        {
            Assert.That(JobStatus.Pending.Code, Is.EqualTo("pending"));
            Assert.That(JobStatus.Pending.IsTerminal, Is.False);
        }

        [Test]
        public void Assigned_Code_IsStable()
        {
            Assert.That(JobStatus.Assigned.Code, Is.EqualTo("assigned"));
            Assert.That(JobStatus.Assigned.IsTerminal, Is.False);
        }

        [Test]
        public void Traveling_Code_IsStable()
        {
            Assert.That(JobStatus.Traveling.Code, Is.EqualTo("traveling"));
            Assert.That(JobStatus.Traveling.IsTerminal, Is.False);
        }

        [Test]
        public void Queued_Code_IsStable()
        {
            Assert.That(JobStatus.Queued.Code, Is.EqualTo("queued"));
            Assert.That(JobStatus.Queued.IsTerminal, Is.False);
        }

        [Test]
        public void Active_Code_IsStable()
        {
            Assert.That(JobStatus.Active.Code, Is.EqualTo("active"));
            Assert.That(JobStatus.Active.IsTerminal, Is.False);
        }

        [Test]
        public void Completed_Code_IsStable_AndTerminal()
        {
            Assert.That(JobStatus.Completed.Code, Is.EqualTo("completed"));
            Assert.That(JobStatus.Completed.IsTerminal, Is.True);
        }

        [Test]
        public void Canceled_Code_IsStable_AndTerminal()
        {
            Assert.That(JobStatus.Canceled.Code, Is.EqualTo("canceled"));
            Assert.That(JobStatus.Canceled.IsTerminal, Is.True);
        }

        [Test]
        public void Blocked_PrefixesReason_WithBlockedColon()
        {
            var hungry = JobStatus.Blocked("too_hungry_to_work");
            Assert.That(hungry.Code, Is.EqualTo("blocked:too_hungry_to_work"));
            Assert.That(hungry.IsTerminal, Is.False);
        }

        [Test]
        public void Blocked_TrimsAndLowersReason_ForStability()
        {
            var noisy = JobStatus.Blocked("  Too_Hungry  ");
            Assert.That(noisy.Code, Is.EqualTo("blocked:too_hungry"));
        }

        [Test]
        public void Blocked_WithEmptyReason_FallsBackToBlockedSentinel()
        {
            var generic = JobStatus.Blocked(string.Empty);
            Assert.That(generic.Code, Is.EqualTo("blocked"));
        }

        [Test]
        public void Equality_IsByCode()
        {
            Assert.That(JobStatus.Pending, Is.EqualTo(JobStatus.Pending));
            Assert.That(JobStatus.Pending, Is.Not.EqualTo(JobStatus.Active));
            Assert.That(JobStatus.Pending == JobStatus.Pending, Is.True);
            Assert.That(JobStatus.Pending != JobStatus.Active, Is.True);
            Assert.That(JobStatus.Pending.GetHashCode(), Is.EqualTo(JobStatus.Pending.GetHashCode()));
        }

        [Test]
        public void ToString_ReturnsStableCode()
        {
            Assert.That(JobStatus.Active.ToString(), Is.EqualTo("active"));
            Assert.That(JobStatus.Blocked("hunger").ToString(), Is.EqualTo("blocked:hunger"));
        }
    }
}

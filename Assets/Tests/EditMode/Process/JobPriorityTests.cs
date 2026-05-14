using System;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin the first LIVING/PROCESS job-preference primitive. The type
// only validates and orders priorities; matching actors to JobBoard entries is a
// later JobAssignmentSystem atom.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>
    /// Verifies the deterministic priority semantics used by future job assignment.
    /// </summary>
    public sealed class JobPriorityTests
    {
        /// <summary>Active priorities expose their raw positive value.</summary>
        [Test]
        public void Active_StoresValue()
        {
            var priority = JobPriority.Active(2);

            Assert.That(priority.Value, Is.EqualTo(2));
            Assert.That(priority.IsActive, Is.True);
        }

        /// <summary>The default priority is the explicit disabled sentinel.</summary>
        [Test]
        public void Default_IsDisabled()
        {
            Assert.That(default(JobPriority).IsActive, Is.False);
            Assert.That(JobPriority.Disabled, Is.EqualTo(default(JobPriority)));
        }

        /// <summary>Active priorities must be positive.</summary>
        [Test]
        public void Active_RejectsZeroOrNegativeValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => JobPriority.Active(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => JobPriority.Active(-1));
        }

        /// <summary>Two priorities with the same raw value compare equal.</summary>
        [Test]
        public void SameValue_IsEqual()
        {
            var left = JobPriority.Active(1);
            var right = JobPriority.Active(1);

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        /// <summary>Lower active priority numbers sort before higher numbers.</summary>
        [Test]
        public void CompareTo_LowerActiveNumberWins()
        {
            var highPriority = JobPriority.Active(1);
            var lowPriority = JobPriority.Active(3);

            Assert.That(highPriority.CompareTo(lowPriority), Is.LessThan(0));
            Assert.That(lowPriority.CompareTo(highPriority), Is.GreaterThan(0));
        }

        /// <summary>Any active priority sorts before the disabled sentinel.</summary>
        [Test]
        public void CompareTo_ActiveSortsBeforeDisabled()
        {
            var active = JobPriority.Active(5);

            Assert.That(active.CompareTo(JobPriority.Disabled), Is.LessThan(0));
            Assert.That(JobPriority.Disabled.CompareTo(active), Is.GreaterThan(0));
        }

        /// <summary>Debug labels distinguish disabled and active priority values.</summary>
        [Test]
        public void ToString_ReturnsDebugLabel()
        {
            Assert.That(JobPriority.Disabled.ToString(), Is.EqualTo("JobPriority.Disabled"));
            Assert.That(JobPriority.Active(4).ToString(), Is.EqualTo("JobPriority(4)"));
        }
    }
}

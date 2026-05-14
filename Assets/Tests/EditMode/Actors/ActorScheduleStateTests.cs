using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin only the actor's current job target state. Pathing, job
// selection, recipe ticking, save/load, and EventLog output are later atoms.
namespace EmberCrpg.Tests.EditMode.Actors
{
    /// <summary>Verifies idle and assigned actor schedule snapshots.</summary>
    public sealed class ActorScheduleStateTests
    {
        [Test]
        public void Default_IsIdle()
        {
            Assert.That(default(ActorScheduleState).IsIdle, Is.True);
            Assert.That(ActorScheduleState.Idle, Is.EqualTo(default(ActorScheduleState)));
        }

        [Test]
        public void Assigned_StoresCurrentJobAndWorksiteTarget()
        {
            var state = ActorScheduleState.Assigned(
                new JobId(5UL),
                new SiteId(8UL),
                new GridPosition(2, 3));

            Assert.That(state.IsIdle, Is.False);
            Assert.That(state.CurrentJobId, Is.EqualTo(new JobId(5UL)));
            Assert.That(state.TargetSiteId, Is.EqualTo(new SiteId(8UL)));
            Assert.That(state.TargetWorksitePosition, Is.EqualTo(new GridPosition(2, 3)));
        }

        [Test]
        public void Assigned_RejectsEmptyJobOrSite()
        {
            Assert.Throws<ArgumentException>(() => ActorScheduleState.Assigned(default, new SiteId(1UL), new GridPosition(0, 0)));
            Assert.Throws<ArgumentException>(() => ActorScheduleState.Assigned(new JobId(1UL), default, new GridPosition(0, 0)));
        }

        [Test]
        public void SameAssignmentFields_AreEqual()
        {
            var left = ActorScheduleState.Assigned(new JobId(2UL), new SiteId(3UL), new GridPosition(4, 5));
            var right = ActorScheduleState.Assigned(new JobId(2UL), new SiteId(3UL), new GridPosition(4, 5));

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void ToString_ReturnsDebugLabel()
        {
            Assert.That(ActorScheduleState.Idle.ToString(), Is.EqualTo("ActorScheduleState.Idle"));
            Assert.That(
                ActorScheduleState.Assigned(new JobId(2UL), new SiteId(3UL), new GridPosition(4, 5)).ToString(),
                Is.EqualTo("ActorScheduleState(JobId(2), SiteId(3), (4,5))"));
        }
    }
}

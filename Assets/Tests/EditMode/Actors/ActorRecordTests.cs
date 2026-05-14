using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin the mutable pure-Domain actor record used across movement, combat, and saves.
// They cover position updates, vital replacement, and remembered topics only.
namespace EmberCrpg.Tests.EditMode.Actors
{
    /// <summary>Verifies the Sprint 1 actor-record shell.</summary>
    public sealed class ActorRecordTests
    {
        [Test]
        public void MoveTo_UpdatesGridPosition()
        {
            var actor = CreateActor();
            actor.MoveTo(new GridPosition(4, 5));
            Assert.That(actor.Position, Is.EqualTo(new GridPosition(4, 5)));
        }

        [Test]
        public void ApplyVitals_ReplacesHealthSnapshot()
        {
            var actor = CreateActor();
            actor.ApplyVitals(actor.Vitals.WithHealth(actor.Vitals.Health.Damage(3)));
            Assert.That(actor.Vitals.Health.Current, Is.EqualTo(9));
        }

        [Test]
        public void RecordTopic_StoresOnlyUniqueTopicIds()
        {
            var actor = CreateActor();
            actor.RecordTopic("embers");
            actor.RecordTopic("embers");
            Assert.That(actor.AskedTopicIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void ApplyJobPreferences_ReplacesRowsWithoutChangingIdentity()
        {
            var actor = CreateActor();
            var preference = new ActorJobPreference(JobKind.Smith, JobPriority.Active(1));

            actor.ApplyJobPreferences(new[] { preference });

            Assert.That(actor.Id, Is.EqualTo(new ActorId(7)));
            Assert.That(actor.JobPreferences, Is.EqualTo(new[] { preference }));
            actor.ApplyJobPreferences(null);
            Assert.That(actor.JobPreferences, Is.Empty);
        }

        [Test]
        public void ApplyJobPreferences_RejectsDuplicateKinds()
        {
            var actor = CreateActor();
            var first = new ActorJobPreference(JobKind.Smith, JobPriority.Active(1));
            var second = ActorJobPreference.Disabled(JobKind.Smith);

            Assert.Throws<System.InvalidOperationException>(() => actor.ApplyJobPreferences(new[] { first, second }));
        }

        [Test]
        public void ApplyScheduleState_ReplacesCurrentJobState()
        {
            var actor = CreateActor();
            var assigned = ActorScheduleState.Assigned(new JobId(5UL), new SiteId(3UL), new GridPosition(9, 2));

            actor.ApplyScheduleState(assigned);

            Assert.That(actor.ScheduleState, Is.EqualTo(assigned));
            Assert.That(actor.Id, Is.EqualTo(new ActorId(7)));
            actor.ApplyScheduleState(ActorScheduleState.Idle);
            Assert.That(actor.ScheduleState.IsIdle, Is.True);
        }

        private static ActorRecord CreateActor()
        {
            return new ActorRecord(
                new ActorId(7),
                "Test",
                ActorRole.Talker,
                new EmberStatBlock(10, 11, 12, 13, 14, 15),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(1, 1),
                10,
                5,
                1,
                3,
                new[] { "embers" });
        }
    }
}

using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
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

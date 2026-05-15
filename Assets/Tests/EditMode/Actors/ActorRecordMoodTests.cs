using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using NUnit.Framework;

// Design note:
// These tests pin ActorRecord as the store-compatible carrier for mood. They
// avoid mood derivation, ticking, save/load, and job refusal behaviour.
namespace EmberCrpg.Tests.EditMode.Actors
{
    /// <summary>Verifies ActorRecord mood replacement.</summary>
    public sealed class ActorRecordMoodTests
    {
        [Test]
        public void Constructor_DefaultsToNeutralMood()
        {
            var actor = CreateActor();

            Assert.That(actor.Mood, Is.EqualTo(ActorMood.Neutral));
        }

        [Test]
        public void Constructor_CanSeedMood()
        {
            var actor = CreateActor(new ActorMood(22));

            Assert.That(actor.Mood, Is.EqualTo(new ActorMood(22)));
        }

        [Test]
        public void ApplyMood_ReplacesMoodWithoutChangingIdentity()
        {
            var actor = CreateActor();

            actor.ApplyMood(new ActorMood(18));

            Assert.That(actor.Mood, Is.EqualTo(new ActorMood(18)));
            Assert.That(actor.Id, Is.EqualTo(new ActorId(12)));
            Assert.That(actor.Name, Is.EqualTo("Mood Carrier"));
        }

        private static ActorRecord CreateActor(ActorMood mood = default)
        {
            return new ActorRecord(
                new ActorId(12),
                "Mood Carrier",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                10,
                5,
                1,
                3,
                mood: mood);
        }
    }
}

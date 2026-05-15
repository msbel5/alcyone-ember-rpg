using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using NUnit.Framework;

// Design note:
// These tests pin ActorRecord as the store-compatible carrier for needs. They
// avoid ticking, recovery, save/load, and refusal behaviour.
namespace EmberCrpg.Tests.EditMode.Actors
{
    /// <summary>Verifies ActorRecord needs replacement.</summary>
    public sealed class ActorRecordNeedsTests
    {
        [Test]
        public void Constructor_DefaultsToComfortableNeeds()
        {
            var actor = CreateActor();

            Assert.That(actor.Needs, Is.EqualTo(ActorNeeds.Comfortable));
        }

        [Test]
        public void Constructor_CanSeedNeeds()
        {
            var needs = new ActorNeeds(new NeedValue(10), new NeedValue(20), new NeedValue(30));
            var actor = CreateActor(needs);

            Assert.That(actor.Needs, Is.EqualTo(needs));
        }

        [Test]
        public void ApplyNeeds_ReplacesNeedsWithoutChangingIdentity()
        {
            var actor = CreateActor();
            var needs = new ActorNeeds(new NeedValue(40), new NeedValue(50), new NeedValue(60));

            actor.ApplyNeeds(needs);

            Assert.That(actor.Needs, Is.EqualTo(needs));
            Assert.That(actor.Id, Is.EqualTo(new ActorId(9)));
            Assert.That(actor.Name, Is.EqualTo("Needs Test"));
        }

        private static ActorRecord CreateActor(ActorNeeds needs = default)
        {
            return new ActorRecord(
                new ActorId(9),
                "Needs Test",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                10,
                5,
                1,
                3,
                needs: needs);
        }
    }
}

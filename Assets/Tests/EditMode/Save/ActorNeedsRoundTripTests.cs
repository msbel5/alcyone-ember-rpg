using NUnit.Framework;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Data.Save;

namespace EmberCrpg.Tests.EditMode.Save
{
    public sealed class ActorNeedsRoundTripTests
    {
        private static readonly SiteId Site = new SiteId(10UL);

        [Test]
        public void ActorNeeds_RoundTrip_PreservesNeedsAndMood()
        {
            var needs = new ActorNeeds(new NeedValue(70), new NeedValue(10), new NeedValue(5));
            var mood = new ActorMood(20);

            var actor = new ActorRecord(
                new ActorId(7UL),
                "Tester",
                ActorRole.Guard,
                new EmberStatBlock(10,10,10,10,10,10),
                new ActorVitals(new VitalStat(10,10), new VitalStat(0,0), new VitalStat(0,0)),
                new GridPosition(0,0),
                accuracy: 5,
                dodge: 1,
                armor: 0,
                baseDamage: 1,
                jobPreferences: null,
                scheduleState: default,
                needs: needs,
                mood: mood);

            var save = ActorSaveMapper.ToSave(actor);
            var restored = ActorSaveMapper.FromSave(save);

            Assert.That(restored.Needs.Hunger.Value, Is.EqualTo(actor.Needs.Hunger.Value));
            Assert.That(restored.Needs.Fatigue.Value, Is.EqualTo(actor.Needs.Fatigue.Value));
            Assert.That(restored.Needs.Thirst.Value, Is.EqualTo(actor.Needs.Thirst.Value));
            Assert.That(restored.Mood.Value, Is.EqualTo(actor.Mood.Value));
        }
    }
}

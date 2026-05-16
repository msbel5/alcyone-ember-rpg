using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Data.Save;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    public sealed class ActorNeedsRoundTripTests
    {
        [Test]
        public void ActorNeedsAndMood_RoundTrip_SaveThenRestore_PreservesValues()
        {
            var original = new ActorRecord(
                new ActorId(42UL),
                "Tester",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(2, 3),
                accuracy: 10,
                dodge: 5,
                armor: 1,
                baseDamage: 3,
                jobPreferences: null,
                scheduleState: default,
                needs: new ActorNeeds(new NeedValue(70), new NeedValue(20), new NeedValue(10)),
                mood: new ActorMood(30));

            var save = ActorSaveMapper.ToSave(original);
            var restored = ActorSaveMapper.FromSave(save);

            Assert.That(restored.Needs.Hunger.Value, Is.EqualTo(original.Needs.Hunger.Value));
            Assert.That(restored.Needs.Fatigue.Value, Is.EqualTo(original.Needs.Fatigue.Value));
            Assert.That(restored.Needs.Thirst.Value, Is.EqualTo(original.Needs.Thirst.Value));
            Assert.That(restored.Mood.Value, Is.EqualTo(original.Mood.Value));
        }
    }
}

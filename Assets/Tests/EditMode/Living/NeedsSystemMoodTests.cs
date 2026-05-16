using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

// Design note:
// These tests pin mood recomputation after deterministic need ticks. They do
// not decide work refusal; they only prove the refusal threshold can be crossed
// by ignored needs.
namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>Verifies need ticks can drive derived mood downward.</summary>
    public sealed class NeedsSystemMoodTests
    {
        [Test]
        public void RecomputeMood_AppliesDerivedMoodToActor()
        {
            var actor = CreateActor(new ActorNeeds(new NeedValue(80), new NeedValue(40), NeedValue.Comfortable));

            var mood = new NeedsSystem().RecomputeMood(actor);

            Assert.That(mood.Value, Is.EqualTo(10));
            Assert.That(actor.Mood, Is.EqualTo(mood));
        }

        [Test]
        public void ThreeUnfedNeedTicks_LowerMoodUnderRefusalThreshold()
        {
            var actor = CreateActor();
            var log = new WorldEventLog();
            var system = new NeedsSystem();

            system.TickActorNeeds(actor, log, new GameTime(0), ticks: 3);

            Assert.That(actor.Needs.Hunger.Value, Is.EqualTo(60));
            Assert.That(actor.Needs.Fatigue.Value, Is.EqualTo(45));
            Assert.That(actor.Mood.IsLow, Is.True);
            Assert.That(actor.Mood.Value, Is.LessThanOrEqualTo(ActorMood.LowMoodThreshold));
        }

        private static ActorRecord CreateActor(ActorNeeds needs = default)
        {
            return new ActorRecord(
                new ActorId(21),
                "Needs Tick Mood",
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

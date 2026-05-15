using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

// Design note:
// These tests pin the pure mood derivation step. They avoid ticking, recovery,
// job refusal, save/load, and EventLog output.
namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>Verifies deterministic need-to-mood derivation.</summary>
    public sealed class NeedMoodEvaluatorTests
    {
        [Test]
        public void NeutralNeeds_PreserveNeutralMood()
        {
            var mood = new NeedMoodEvaluator().Evaluate(ActorNeeds.Comfortable);

            Assert.That(mood, Is.EqualTo(ActorMood.Neutral));
        }

        [Test]
        public void HungerAndFatigue_LowerMoodDeterministically()
        {
            var needs = ActorNeeds.Comfortable
                .With(NeedKind.Hunger, new NeedValue(80))
                .With(NeedKind.Fatigue, new NeedValue(40));

            var mood = new NeedMoodEvaluator().Evaluate(needs);

            Assert.That(mood.Value, Is.EqualTo(20));
            Assert.That(mood.IsLow, Is.True);
        }

        [Test]
        public void MemoryPressure_LowersMoodWithoutMutatingNeeds()
        {
            var needs = new ActorNeeds(new NeedValue(10), new NeedValue(10), new NeedValue(10));
            var mood = new NeedMoodEvaluator().Evaluate(needs, new NeedValue(50));

            Assert.That(mood.Value, Is.EqualTo(30));
            Assert.That(needs, Is.EqualTo(new ActorNeeds(new NeedValue(10), new NeedValue(10), new NeedValue(10))));
        }

        [Test]
        public void ActorOverload_ReadsActorNeedsAndRejectsNullActor()
        {
            var actor = CreateActor(new ActorNeeds(new NeedValue(40), NeedValue.Comfortable, NeedValue.Comfortable));
            var evaluator = new NeedMoodEvaluator();

            Assert.That(evaluator.Evaluate(actor).Value, Is.EqualTo(40));
            Assert.Throws<ArgumentNullException>(() => evaluator.Evaluate((ActorRecord)null));
        }

        private static ActorRecord CreateActor(ActorNeeds needs)
        {
            return new ActorRecord(
                new ActorId(11),
                "Mood Test",
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

using System;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

// Design note:
// These tests pin sleep as deterministic fatigue recovery without inventory
// mutation. Job refusal and save/load replay remain later Faz 4 atoms.
namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>Verifies deterministic sleep recovery.</summary>
    public sealed class NeedRecoverySystemSleepTests
    {
        [Test]
        public void Sleep_LowersFatigueRecomputesMoodAndLogsReasonTrace()
        {
            var actor = CreateActor(new ActorNeeds(new NeedValue(10), new NeedValue(80), NeedValue.Comfortable));
            var log = new WorldEventLog();
            var recipe = new NeedRecoveryRecipe("sleep-basic", NeedRecoverySystem.SleepAction, NeedKind.Fatigue, 60);

            var recovered = new NeedRecoverySystem().Sleep(actor, recipe, log, new GameTime(480));

            Assert.That(recovered, Is.True);
            Assert.That(actor.Needs.Fatigue.Value, Is.EqualTo(20));
            Assert.That(actor.Mood.Value, Is.EqualTo(40));

            var evt = log.Events.Single();
            Assert.That(evt.Kind, Is.EqualTo(WorldEventKind.NeedChanged));
            Assert.That(evt.ActorId, Is.EqualTo(actor.Id));
            Assert.That(evt.ReasonTrace.Causes, Is.EqualTo(new[]
            {
                "need_recovery",
                "action:sleep",
                "recipe:sleep-basic",
                $"actor:{actor.Id.Value}",
                "rest:sleep",
                "fatigue:80->20",
                "mood:40",
            }));
        }

        [Test]
        public void Sleep_WhenActorIsRestedDoesNotAppendEvent()
        {
            var actor = CreateActor(new ActorNeeds(new NeedValue(10), NeedValue.Comfortable, NeedValue.Comfortable));
            var log = new WorldEventLog();
            var recipe = new NeedRecoveryRecipe("sleep-basic", NeedRecoverySystem.SleepAction, NeedKind.Fatigue, 60);

            var recovered = new NeedRecoverySystem().Sleep(actor, recipe, log, new GameTime(480));

            Assert.That(recovered, Is.False);
            Assert.That(actor.Needs.Fatigue, Is.EqualTo(NeedValue.Comfortable));
            Assert.That(actor.Mood, Is.EqualTo(ActorMood.Neutral));
            Assert.That(log.IsEmpty, Is.True);
        }

        [Test]
        public void Sleep_RejectsMealRecipeAndNullInputs()
        {
            var system = new NeedRecoverySystem();
            var actor = CreateActor(new ActorNeeds(NeedValue.Comfortable, new NeedValue(80), NeedValue.Comfortable));
            var log = new WorldEventLog();
            var sleep = new NeedRecoveryRecipe("sleep-basic", NeedRecoverySystem.SleepAction, NeedKind.Fatigue, 60);
            var meal = new NeedRecoveryRecipe("meal-basic", NeedRecoverySystem.EatMealAction, NeedKind.Hunger, 50, "simple_meal");

            Assert.Throws<ArgumentNullException>(() => system.Sleep(null, sleep, log, new GameTime(0)));
            Assert.Throws<ArgumentNullException>(() => system.Sleep(actor, null, log, new GameTime(0)));
            Assert.Throws<ArgumentNullException>(() => system.Sleep(actor, sleep, null, new GameTime(0)));
            Assert.Throws<ArgumentException>(() => system.Sleep(actor, meal, log, new GameTime(0)));
        }

        private static ActorRecord CreateActor(ActorNeeds needs)
        {
            return new ActorRecord(
                new ActorId(32),
                "Sleep Recovery",
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

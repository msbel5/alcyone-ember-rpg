using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W34 DOC4 S5 (capstone): a smith's 24 hours are a chain of bodied actions in cause
    /// order — work day, walk home, sleep, wake, work again. The takeaway pin from
    /// RUH_TESHIS: "aktörlerde kimlik var, fakat devam eden irade yok". The day is
    /// LIVED, not narrated.
    /// </summary>
    public sealed class SleepWorkStoryChainTests
    {
        [Test]
        public void TwoDayHorizon_ContainsAWorkThenSleepThenWork_Cycle()
        {
            // The composite world: a smith's home cell inside a village that also happens
            // to host a smelt job. Quantity 10 with stock for only three: day 1 mints three
            // ingots then hits SourceDrained; on day 2 the smith STILL walks to the bench
            // and tries to fund — the Work intent survives across days, so laterWork exists.
            var world = WorkSliceWorld.Build(ore: 6, fuel: 3);
            var smith = SmithAtHome(7, homeX: 2, homeY: 2);
            world.Actors.Add(smith);
            WorkSliceWorld.PostSmeltJob(world, quantity: 10);
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            ActorRecord A() => world.Actors.Get(new ActorId(7));

            // Sample the actor's intent at every tick across two days — the transitions
            // are the day's narrative (Work -> Rest -> Work).
            var actions = new List<ActorActionType>(2 * 1440);
            var intents = new List<ActorIntent>(2 * 1440);
            var fatigueByTick = new Dictionary<int, int>();
            for (var t = 1; t <= 2 * 1440; t++)
            {
                composer.Advance(world, t);
                actions.Add(A().ActionState.CurrentAction);
                intents.Add(A().ActionState.CurrentIntent);
                fatigueByTick[t] = A().Needs.Fatigue.Value;
            }

            // Both banners must have shown up: the day WORKED and the night SLEPT.
            Assert.That(intents.Any(i => i == ActorIntent.Work), Is.True,
                "vacuous guard: the smith must actually work during the horizon");
            Assert.That(intents.Any(i => i == ActorIntent.Rest), Is.True,
                "vacuous guard: the smith must actually sleep during the horizon");

            // Cause order: first Work index (day 1) < first Rest index (evening) < later Work
            // index (day 2). The intents drop off in that order — the day is lived.
            int firstWork = intents.FindIndex(i => i == ActorIntent.Work);
            int firstRest = intents.FindIndex(firstWork, i => i == ActorIntent.Rest);
            int laterWork = intents.FindIndex(firstRest, i => i == ActorIntent.Work);
            Assert.That(firstWork, Is.GreaterThanOrEqualTo(0));
            Assert.That(firstRest, Is.GreaterThan(firstWork),
                "sleep must come AFTER the workday, not before it — the calendar was lived");
            Assert.That(laterWork, Is.GreaterThan(firstRest),
                "day 2's work must come AFTER the night's sleep — the cycle closed");

            // Sleep chain is MoveToBed -> Sleep (§5.3): the walk home is bodied.
            var restActions = actions
                .Select((a, i) => (a, i))
                .Where(x => intents[x.i] == ActorIntent.Rest)
                .Select(x => x.a).Distinct().ToArray();
            Assert.That(restActions, Does.Contain(ActorActionType.MoveToBed),
                "no bed without a bodied walk home");
            Assert.That(restActions, Does.Contain(ActorActionType.Sleep));

            // Fatigue sawtooth: the morning fatigue AFTER the sleep is lower than the
            // fatigue AT bedtime — the night actually rested.
            int bedtimeTick = intents.FindIndex(i => i == ActorIntent.Rest) + 1; // +1: 1-based
            int wakeTick = 1440 + 8 * 60; // 08:00 on day 2 — well after dawn (06:00)
            Assert.That(fatigueByTick[wakeTick], Is.LessThan(fatigueByTick[bedtimeTick]),
                "the night rested the smith — fatigue at wake < fatigue at bedtime");

            // The circle closes: production happened. The smith minted ingots across the two
            // workdays — the calendar was PRODUCTIVE, not theatre.
            Assert.That(WorkSliceWorld.Pile(world).Get(WorkSliceWorld.IngotTag),
                Is.GreaterThanOrEqualTo(1),
                "at least one ingot was minted — the two days produced");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.RecipeCompleted),
                Is.True, "the productive stroke logged a RecipeCompleted");
        }

        /// <summary>A smith whose Home cell is inside the site — fed, rested, Smith-preferring,
        /// so the day's opening decision hesitates between meal (rested-out) and work (yes).</summary>
        private static ActorRecord SmithAtHome(ulong id, int homeX, int homeY)
        {
            return new ActorRecord(
                new ActorId(id), "Smith" + id, ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(homeX, homeY), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) },
                home: new GridPosition(homeX, homeY));
        }
    }
}

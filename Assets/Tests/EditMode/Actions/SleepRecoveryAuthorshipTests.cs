using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W34 DOC4 S1: sleepwalking is DEAD. Fatigue may only fall inside a Running Sleep tick,
    /// and only while the actor stands on its home cell (BedReachCells==1). NeedsSystem is
    /// the world's ONLY other fatigue writer and it only INCREASES — so every observed drop
    /// pins the invariant: Sleep-Running at Home. The vacuous guard makes sure a sleep
    /// episode actually happened; a horizon with no Sleep would silently prove nothing.
    /// </summary>
    public sealed class SleepRecoveryAuthorshipTests
    {
        [Test]
        public void FatigueDrops_OnlyDuringRunningSleep_AtHomeCell()
        {
            var world = SleepSliceWorld.Build();
            world.Actors.Add(SleepSliceWorld.Tired(7, SleepSliceWorld.FarField.X, SleepSliceWorld.FarField.Y));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            ActorRecord A() => world.Actors.Get(new ActorId(7));

            // Two-day horizon guarantees at least one full night bracket: decision at 22:00,
            // walk home, sleep to dawn — the WHOLE bedded night must fit inside the sample.
            var trace = new List<(int tick, int fatigue, ActorActionType action,
                ActionPhase phase, GridPosition pos)>();
            trace.Add((0, A().Needs.Fatigue.Value, A().ActionState.CurrentAction,
                A().ActionState.Phase, A().Position));
            for (var t = 1; t <= 2 * 1440; t++)
            {
                composer.Advance(world, t);
                trace.Add((t, A().Needs.Fatigue.Value, A().ActionState.CurrentAction,
                    A().ActionState.Phase, A().Position));
            }

            Assert.That(trace.Any(s => s.action == ActorActionType.Sleep), Is.True,
                "vacuous guard: two days must contain at least one Sleep episode");

            // Every drop tick's carrier: Sleep + Running + on Home cell. NeedsSystem never
            // subtracts (Hourly:30 Increase only), so any decrease is Sleep's authorship.
            for (var i = 1; i < trace.Count; i++)
            {
                var prev = trace[i - 1];
                var cur = trace[i];
                if (cur.fatigue >= prev.fatigue) continue;
                Assert.That(cur.action, Is.EqualTo(ActorActionType.Sleep),
                    $"tick {cur.tick}: fatigue fell during {cur.action} — sleepwalking survived");
                Assert.That(cur.phase, Is.EqualTo(ActionPhase.Running),
                    $"tick {cur.tick}: fatigue fell in a terminal Sleep phase — handover ticks do not recover");
                Assert.That(cur.pos, Is.EqualTo(A().Home),
                    $"tick {cur.tick}: fatigue fell {cur.pos} away from Home {A().Home}");
            }

            // The MoveToBed walk NEVER pays recovery: fatigue must be flat-or-rising while
            // the actor is Running MoveToBed (the "yürüyen adam uyumaz" pin).
            for (var i = 1; i < trace.Count; i++)
            {
                var prev = trace[i - 1];
                var cur = trace[i];
                if (cur.action != ActorActionType.MoveToBed
                    || cur.phase != ActionPhase.Running) continue;
                Assert.That(cur.fatigue, Is.GreaterThanOrEqualTo(prev.fatigue),
                    $"tick {cur.tick}: MoveToBed leaked recovery — the fiat blanket lives");
            }

            // Sustainability: fatigue is lower at daybreak of day 2 than at bedtime of day 1
            // (the Gate1 truth the S-series inherits: a bodied night beats the daily ramp).
            var day1Bedtime = trace.First(s => s.tick >= 22 * 60 && s.tick < 24 * 60).fatigue;
            var day2Sunrise = trace.First(s => s.tick >= 1440 + 6 * 60 && s.tick < 1440 + 8 * 60).fatigue;
            Assert.That(day2Sunrise, Is.LessThan(day1Bedtime),
                "the night actually rested — recovery > overnight ramp");
        }

        [Test]
        public void NeedsSystem_IsTheOnlyOtherFatigueWriter_AndItNeverDecreases()
        {
            // A dry-run of NeedsSystem shows the invariant is safe: the ONE other authored
            // path over fatigue increases it. If a rate ever went negative here, the whole
            // "fatigue drop -> Sleep" argument in the primary test would silently rot.
            var needs = new NeedsSystem();
            var seed = new ActorNeeds(new NeedValue(20), new NeedValue(30), new NeedValue(40));
            var next = needs.TickNeeds(seed, ticks: 1);
            Assert.That(next.Fatigue.Value, Is.GreaterThanOrEqualTo(seed.Fatigue.Value),
                "NeedsSystem must NEVER lower fatigue — the recovery monopoly belongs to SleepAdvancer");
        }
    }
}

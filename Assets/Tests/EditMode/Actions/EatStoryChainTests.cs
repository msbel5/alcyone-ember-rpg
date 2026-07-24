using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Diagnostics;
using EmberCrpg.Simulation.Living.Actions;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W32 DOC6 T8 (capstone): the §10 chain is complete. One actor's ONE episode logs
    /// reservation -> arrival -> transfer -> meal in CAUSE order, all under one episode
    /// identity — events are the chain's causal links now, not after-the-fact commentary.
    /// The sink half proves every phase transition also flowed through the single
    /// ActionLogManager seam (the EmberLog.Sink capture pattern, as TickPerf uses).
    /// </summary>
    public sealed class EatStoryChainTests
    {
        [Test]
        public void OneEpisode_LogsTheFullChain_InCauseOrder_UnderOneIdentity()
        {
            var world = EatSliceWorld.Build();
            world.Actors.Add(EatSliceWorld.Hungry(7, 20, 20)); // T4's world: a far hungry actor
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            var sinkLines = new List<string>();
            var priorSink = EmberLog.Sink;
            var priorEnabled = ActionLogDebugSink.Enabled;
            EmberLog.Sink = line => { if (line.StartsWith("[Action] ")) sinkLines.Add(line); };
            ActionLogDebugSink.Enabled = true;
            try
            {
                for (var tick = 1; tick <= 200
                    && !world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionCompleted); tick++)
                    composer.Advance(world, tick);
            }
            finally
            {
                EmberLog.Sink = priorSink;
                ActionLogDebugSink.Enabled = priorEnabled;
            }

            // The ring is the ordered chain (append order == cause order — never post-hoc).
            var chain = new List<ActionLogEntry>();
            for (var i = 0; i < world.ActionLog.Count; i++)
                if (world.ActionLog.At(i).ActorId == 7UL) chain.Add(world.ActionLog.At(i));
            Assert.That(chain.All(e => e.Intent == ActorIntent.Eat), Is.True,
                "every link carries the SAME episode intent stamp");

            int reserved = chain.FindIndex(e =>
                e.Reason == ActionLogReason.ReservationAcquired && e.ToAction == ActorActionType.MoveToFood);
            int arrived = chain.FindIndex(e =>
                e.Reason == ActionLogReason.Arrived && e.ToPhase == ActionPhase.Succeeded);
            int transferred = chain.FindIndex(e =>
                e.ToAction == ActorActionType.TakeFood && e.ToPhase == ActionPhase.Succeeded);
            int consumed = chain.FindIndex(e =>
                e.Reason == ActionLogReason.Completed && e.ToAction == ActorActionType.ConsumeFood
                && e.ToPhase == ActionPhase.Succeeded);
            Assert.That(reserved, Is.GreaterThanOrEqualTo(0), "missing link: ResourceReserved");
            Assert.That(arrived, Is.GreaterThan(reserved), "Arrived must FOLLOW the reservation");
            Assert.That(transferred, Is.GreaterThan(arrived), "ItemTransferred must FOLLOW arrival");
            Assert.That(consumed, Is.GreaterThan(transferred), "MealConsumed must FOLLOW the transfer");

            // Terminal events: the story surfaces read a structured outcome + the verbatim meal line.
            var meal = world.Events.Events.Single(e => e.Kind == WorldEventKind.ActionCompleted);
            Assert.That(meal.ActorId.Value, Is.EqualTo(7UL));
            Assert.That(meal.Reason, Does.Contain("eat:consume completed"));
            Assert.That(world.Events.Events.Any(e =>
                e.Reason != null && e.Reason.StartsWith("meal_eaten")
                && e.Tick.TotalMinutes == meal.Tick.TotalMinutes), Is.True,
                "the meal_eaten line lands on the SAME tick — one commit, two readers");
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(9), "the transfer was physical");

            // LogManager proof: every phase transition of the episode also flowed out the seam.
            Assert.That(sinkLines.Count(l => l.Contains("actor=7") && l.Contains("ph=")),
                Is.GreaterThanOrEqualTo(3),
                "the MoveToFood/TakeFood/ConsumeFood transitions must ALL pass the single log gate");
        }
    }
}

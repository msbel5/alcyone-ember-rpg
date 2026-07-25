using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W34 DOC2 W1 (capstone): the circle closes — a posted smelt job becomes a CLAIM, the
    /// claim becomes a WALK, the walk becomes bench labour, the labour becomes an ingot in the
    /// SITE pile. Inputs leave the pile ONLY at the bench (claim tick leaves it byte-equal),
    /// the job completes ONLY on the commit stroke, and the on-screen verb is the action
    /// itself, verbatim ("working" == CurrentAction — RUH_TESHIS §10's last theatre dies).
    /// </summary>
    public sealed class WorkStoryChainTests
    {
        [Test]
        public void PostedSmeltJob_BecomesABodiedChain_AndTheIngotLandsInTheSitePile()
        {
            var world = WorkSliceWorld.Build(ore: 2, fuel: 1);
            var smith = WorkSliceWorld.Smith(7, 9, 9);
            world.Actors.Add(smith);
            WorkSliceWorld.PostSmeltJob(world);
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            var pile = WorkSliceWorld.Pile(world);
            int tick = 0;
            long fundedAt = 0, claimedAt = 0;
            var commuteDistances = new List<long>();
            bool sawWorkingVerb = false;
            while (pile.Get(WorkSliceWorld.IngotTag) == 0 && tick < WorldTickComposer.TicksPerGameDay)
            {
                composer.Advance(world, ++tick);
                if (claimedAt == 0 && world.Jobs.IsClaimed(WorkSliceWorld.Job))
                {
                    claimedAt = world.Time.TotalMinutes;
                    // The claim step starts NOTHING: no order row, no consumption (§8).
                    Assert.That(pile.Get(WorkSliceWorld.OreTag), Is.EqualTo(2), "claim tick leaves the ore untouched");
                    Assert.That(pile.Get(WorkSliceWorld.FuelTag), Is.EqualTo(1), "claim tick leaves the fuel untouched");
                    Assert.That(world.WorkOrders.Rows, Is.Empty, "no order row is born at claim");
                }
                if (smith.ActionState.CurrentAction == ActorActionType.MoveToWorksite)
                    commuteDistances.Add(System.Math.Max(
                        System.Math.Abs(smith.Position.X - WorkSliceWorld.Bench.X),
                        System.Math.Abs(smith.Position.Y - WorkSliceWorld.Bench.Y)));
                if (smith.ActionState.CurrentAction == ActorActionType.PerformWork)
                {
                    sawWorkingVerb |= EmberCrpg.Presentation.Ember.Adapters.ActionVerbTable
                        .Verb(smith.ActionState.CurrentAction) == "working";
                    // The bench is furniture: labour happens from ADJACENCY, never remotely.
                    Assert.That(System.Math.Max(
                        System.Math.Abs(smith.Position.X - WorkSliceWorld.Bench.X),
                        System.Math.Abs(smith.Position.Y - WorkSliceWorld.Bench.Y)),
                        Is.LessThanOrEqualTo(1), "PerformWork requires a body at the bench");
                }
                if (fundedAt == 0 && pile.Get(WorkSliceWorld.OreTag) == 0)
                {
                    fundedAt = world.Time.TotalMinutes;
                    Assert.That(smith.ActionState.CurrentAction, Is.EqualTo(ActorActionType.PerformWork),
                        "inputs drop ONLY at the bench — the funding tick is a PerformWork step");
                }
            }

            Assert.That(claimedAt, Is.GreaterThan(0), "the claim happened");
            Assert.That(fundedAt, Is.GreaterThan(claimedAt), "funding waited for the ARRIVAL, not the claim");
            // The commute was step-by-step: each MoveToWorksite tick closed exactly one cell.
            Assert.That(commuteDistances.Count, Is.GreaterThanOrEqualTo(2), "the walk was observable");
            for (var i = 1; i < commuteDistances.Count; i++)
                Assert.That(commuteDistances[i], Is.EqualTo(commuteDistances[i - 1] - 1),
                    "the smith walked one cell per tick — no teleport");
            Assert.That(sawWorkingVerb, Is.True, "the on-screen verb IS the action, verbatim");

            // The commit: outputs in the SITE pile, matter fully converted.
            Assert.That(pile.Get(WorkSliceWorld.IngotTag), Is.EqualTo(1), "the ingot landed in the SITE pile");
            Assert.That(pile.Get(WorkSliceWorld.OreTag), Is.EqualTo(0));
            Assert.That(pile.Get(WorkSliceWorld.FuelTag), Is.EqualTo(0));
            Assert.That(world.Jobs.Contains(WorkSliceWorld.Job), Is.False, "the job closed WITH the chain");
            Assert.That(world.WorkOrders.Rows, Is.Empty, "the finished piece left the bench");
            Assert.That(smith.ScheduleState.IsIdle, Is.True, "the smith is free again");

            // Event grammar: RecipeCompleted carries the REAL boundary stamp (the fossil
            // GameTime(ProgressTicks) died on this strip) and JobCompleted shares its tick.
            var recipeDone = world.Events.Events.Single(e => e.Kind == WorldEventKind.RecipeCompleted);
            var jobDone = world.Events.Events.Single(e => e.Kind == WorldEventKind.JobCompleted);
            Assert.That(recipeDone.Reason, Does.Contain("recipe_completed:1001"));
            Assert.That(recipeDone.Tick.TotalMinutes, Is.EqualTo(fundedAt + 1),
                "the commit stamp is world time (fund stroke + 1), not the order's own counter");
            Assert.That(jobDone.Tick.TotalMinutes, Is.EqualTo(recipeDone.Tick.TotalMinutes),
                "quantity 1: the job completes on the same commit stroke");
        }
    }
}

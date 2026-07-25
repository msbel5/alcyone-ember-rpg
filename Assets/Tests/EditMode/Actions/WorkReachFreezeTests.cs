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
    /// W34 DOC4 S3: recipe progress is reach-coupled — the moment the worker leaves the
    /// bench, the WorkOrder counter FREEZES; the moment they return, it resumes exactly
    /// one stroke at a time (no catch-up, no reset). Matter conservation over the freeze
    /// is the second half: inputs already funded stay accounted (either in the order or
    /// back in the pile via refund), never in limbo.
    /// </summary>
    public sealed class WorkReachFreezeTests
    {
        // Sum of iron in play: pile ore (each ingot = 2 ore = 1 ingot), pile ingots (+2), and
        // any half-executed row's ingredient debt (the recipe input reservation). The recipe
        // is smelt(2 ore + 1 fuel -> 1 ingot); we track ore & ingot separately for clarity.
        private static (int ore, int ingot, int fuel) MatterOf(WorldState world)
        {
            var pile = WorkSliceWorld.Pile(world);
            return (pile.Get(WorkSliceWorld.OreTag),
                    pile.Get(WorkSliceWorld.IngotTag),
                    pile.Get(WorkSliceWorld.FuelTag));
        }

        [Test]
        public void PushedOffTheBench_TheOrderFreezes_AndReturnResumesOneStrokeAtATime()
        {
            // Stock for three executions; ask for three so the row LIVES between commits.
            var world = WorkSliceWorld.Build(ore: 6, fuel: 3);
            var smith = WorkSliceWorld.Smith(7, 5, 5); // Chebyshev 1 to bench (4,5)
            world.Actors.Add(smith);
            WorkSliceWorld.PostSmeltJob(world, quantity: 3);
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            // Advance until the FIRST ingot is minted — the row is now "live between
            // executions": CompletedExecutions=1, ProgressTicks=0, waiting for the next
            // funded stroke. This is the clean freeze anchor (no half-funded stroke to
            // worry about — the freeze proof is exclusively "no NEW work happens").
            int tick = 0;
            while (WorkSliceWorld.Pile(world).Get(WorkSliceWorld.IngotTag) == 0 && tick < 3 * 60)
                composer.Advance(world, ++tick);
            Assert.That(WorkSliceWorld.Pile(world).Get(WorkSliceWorld.IngotTag), Is.EqualTo(1),
                "the horizon must reach the first commit before the freeze — otherwise the pin is vacuous");
            Assert.That(world.WorkOrders.TryGetByJob(WorkSliceWorld.Job.Value, out var row), Is.True);
            Assert.That(row.CompletedExecutions, Is.EqualTo(1),
                "one execution done; two ahead — the row lives on the bench");
            var completedBefore = row.CompletedExecutions;
            var ingotsBefore = WorkSliceWorld.Pile(world).Get(WorkSliceWorld.IngotTag);
            var oreBefore = WorkSliceWorld.Pile(world).Get(WorkSliceWorld.OreTag);
            var fuelBefore = WorkSliceWorld.Pile(world).Get(WorkSliceWorld.FuelTag);

            // The single hold gate: pursuit fails EVERY attempted step — the smith
            // cannot reach the bench, so no PerformWork tick ever fires (S3's freeze).
            WorkSliceWorld.Interrupt(world, 7UL, minutes: 12 * 60);

            for (var end = tick + 4 * 60; tick < end;) // four hours of stalled bench
            {
                composer.Advance(world, ++tick);
                Assert.That(world.WorkOrders.TryGetByJob(WorkSliceWorld.Job.Value, out var stalled),
                    Is.True, "the row lives across the freeze — pause is not abandon");
                Assert.That(stalled.CompletedExecutions, Is.EqualTo(completedBefore),
                    "no NEW execution completed while the smith was held away");
                Assert.That(WorkSliceWorld.Pile(world).Get(WorkSliceWorld.IngotTag),
                    Is.EqualTo(ingotsBefore),
                    "no new ingot appeared — the counter did not tick without a body");
                Assert.That(WorkSliceWorld.Pile(world).Get(WorkSliceWorld.OreTag),
                    Is.EqualTo(oreBefore),
                    "no new ore was consumed — funding never fired without a body");
                Assert.That(WorkSliceWorld.Pile(world).Get(WorkSliceWorld.FuelTag),
                    Is.EqualTo(fuelBefore),
                    "no new fuel was consumed — funding never fired without a body");
            }

            // Release the gate; the smith walks back and the row RESUMES exactly one commit
            // at a time — never catch-up, never reset (chunking-referee guarantee).
            WorkSliceWorld.ClearInterrupts(world);
            for (var end = tick + 6 * 60;
                 tick < end && WorkSliceWorld.Pile(world).Get(WorkSliceWorld.IngotTag) < 2;)
                composer.Advance(world, ++tick);
            Assert.That(WorkSliceWorld.Pile(world).Get(WorkSliceWorld.IngotTag),
                Is.EqualTo(2),
                "the second ingot committed AFTER return — the row picked up where it was");
        }

        [Test]
        public void HeldAway_MatterIsConserved_TheOrderRefundsOnJobDrop()
        {
            // A cousin of §6.3 refund: if the job cancels while the row still has
            // funded progress (ProgressTicks>0), the inputs come BACK to the site pile.
            var world = WorkSliceWorld.Build(ore: 2, fuel: 1);
            var smith = WorkSliceWorld.Smith(7, 4, 4);
            world.Actors.Add(smith);
            WorkSliceWorld.PostSmeltJob(world);
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            // Advance until the ore has left the pile (funding fired at the bench):
            int tick = 0;
            while (WorkSliceWorld.Pile(world).Get(WorkSliceWorld.OreTag) == 2 && tick < 4 * 60)
                composer.Advance(world, ++tick);
            Assert.That(WorkSliceWorld.Pile(world).Get(WorkSliceWorld.OreTag), Is.LessThan(2),
                "the horizon must fund at least once — the refund test needs a debt to refund");
            var (oreMid, ingotMid, fuelMid) = MatterOf(world);
            Assert.That(oreMid + ingotMid * 2, Is.LessThanOrEqualTo(2),
                "matter is conserved so far: ore + ore-equivalent ingots <= start stock");

            // The city cancels the order (SweepOrphanWorkOrders class): remove the job while
            // the row is mid-execution. The refund arm returns the inputs to the site pile.
            world.Jobs.Cancel(WorkSliceWorld.Job);
            composer.Advance(world, ++tick); // the sweep fires at the top of decide

            Assert.That(world.WorkOrders.Rows.Any(r => r != null && r.JobId == WorkSliceWorld.Job.Value),
                Is.False, "the orphaned row dropped");
            var (oreEnd, ingotEnd, fuelEnd) = MatterOf(world);
            // Sanity: nothing minted from the abandoned execution.
            Assert.That(ingotEnd, Is.EqualTo(ingotMid), "no ingot was minted from an abandoned execution");
            // Conservation: if a debt existed (ore or fuel drained), the refund put it back.
            Assert.That(oreEnd, Is.EqualTo(2), "refund returned the ore to the pile");
            Assert.That(fuelEnd, Is.EqualTo(1), "refund returned the fuel to the pile");
        }
    }
}

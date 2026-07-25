using System.Linq;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W34 DOC2 W2 + W6: the slice's execution proof and its patience proof. W2 — a claimed
    /// smith HELD AWAY from the bench produces NOTHING: no order row, no consumption, no
    /// ingot, however many hours pass. (In the old world the external clock minted the ingot
    /// in two hours while he slept — free-running ticks. This test is that theatre's death
    /// certificate.) W6 — an ore-less site never freezes and never ghost-cancels: the job
    /// waits CLAIMED; the restock, not a timer, starts the chain.
    /// </summary>
    public sealed class WorkRemoteLabourDeathTests
    {
        [Test]
        public void HeldAwayFromTheBench_HoursPass_NothingIsProduced()
        {
            var world = WorkSliceWorld.Build(ore: 2, fuel: 1);
            var smith = WorkSliceWorld.Smith(7, 9, 9);
            world.Actors.Add(smith);
            WorkSliceWorld.PostSmeltJob(world);
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            int tick = 0;
            while (!world.Jobs.IsClaimed(WorkSliceWorld.Job) && tick < 3 * 60)
                composer.Advance(world, ++tick);
            Assert.That(world.Jobs.IsClaimed(WorkSliceWorld.Job), Is.True, "the claim machine is intact");

            // The hold: a standing pursuit fails every advancement step at its probe — the
            // smith never reaches the bench (the W32 interruption gate as a test fixture).
            WorkSliceWorld.Interrupt(world, 7UL, minutes: 12 * 60);
            var pile = WorkSliceWorld.Pile(world);
            for (var end = tick + 6 * 60; tick < end;)
            {
                composer.Advance(world, ++tick);
                Assert.That(pile.Get(WorkSliceWorld.OreTag), Is.EqualTo(2), "no bench, no consumption");
                Assert.That(world.WorkOrders.Rows, Is.Empty, "no bench, no order row");
            }
            Assert.That(pile.Get(WorkSliceWorld.IngotTag), Is.EqualTo(0),
                "hours passed and NO ingot appeared — remote labour is dead");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.RecipeCompleted), Is.False);
            Assert.That(world.Jobs.IsClaimed(WorkSliceWorld.Job), Is.True,
                "the claim survives the stall — the job waits for a body, it does not decay");
        }

        [Test]
        public void OrelessSite_WaitsClaimed_ThenTheRestockStartsTheChain()
        {
            var world = WorkSliceWorld.Build(ore: 0, fuel: 0);
            var smith = WorkSliceWorld.Smith(7, 6, 6);
            world.Actors.Add(smith);
            WorkSliceWorld.PostSmeltJob(world);
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            int tick = 0;
            for (var end = 4 * 60; tick < end;)
                composer.Advance(world, ++tick);

            Assert.That(world.Jobs.IsClaimed(WorkSliceWorld.Job), Is.True,
                "the dry site holds the claim — no freeze, no abandon");
            Assert.That(world.Jobs.Contains(WorkSliceWorld.Job), Is.True);
            Assert.That(world.Events.Events.Any(e => e.Reason != null && e.Reason.Contains("job_dropped")),
                Is.False, "a REGISTERED recipe id can never ghost-cancel (the net is for unknown ids only)");
            Assert.That(world.WorkOrders.Rows, Is.Empty, "no funding, no order row");

            // The caravan arrives (stand-in): the pile funds one execution — the chain wakes.
            var pile = WorkSliceWorld.Pile(world);
            pile.Add(WorkSliceWorld.OreTag, 2);
            pile.Add(WorkSliceWorld.FuelTag, 1);
            for (var end = tick + 60; tick < end && pile.Get(WorkSliceWorld.IngotTag) == 0;)
                composer.Advance(world, ++tick);

            Assert.That(pile.Get(WorkSliceWorld.IngotTag), Is.EqualTo(1), "restock, walk, work, ingot");
            Assert.That(world.Jobs.Contains(WorkSliceWorld.Job), Is.False, "the job closed with the chain");
        }

        [Test]
        public void BatchDrain_IsAPause_NeverAnException_AndTheRestockResumesTheBatch()
        {
            // Quantity 2 funded for ONE execution: the old strip THREW "Cannot start next
            // execution" here (a counter cannot wait); the bodied strip pauses SourceDrained.
            var world = WorkSliceWorld.Build(ore: 2, fuel: 1);
            var smith = WorkSliceWorld.Smith(7, 6, 6);
            world.Actors.Add(smith);
            WorkSliceWorld.PostSmeltJob(world, quantity: 2);
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            var pile = WorkSliceWorld.Pile(world);
            int tick = 0;
            while (pile.Get(WorkSliceWorld.IngotTag) == 0 && tick < 4 * 60)
                composer.Advance(world, ++tick);
            Assert.That(pile.Get(WorkSliceWorld.IngotTag), Is.EqualTo(1), "the funded execution committed");

            for (var end = tick + 2 * 60; tick < end;)
                composer.Advance(world, ++tick); // hours of drain: no throw, no decay
            Assert.That(world.WorkOrders.TryGetByJob(WorkSliceWorld.Job.Value, out var row), Is.True,
                "the half-done batch WAITS on the bench");
            Assert.That(row.CompletedExecutions, Is.EqualTo(1), "the batch counter survived the drain");
            Assert.That(world.Jobs.IsClaimed(WorkSliceWorld.Job), Is.True);

            pile.Add(WorkSliceWorld.OreTag, 2);
            pile.Add(WorkSliceWorld.FuelTag, 1);
            while (pile.Get(WorkSliceWorld.IngotTag) < 2 && tick < 10 * 60)
                composer.Advance(world, ++tick);
            Assert.That(pile.Get(WorkSliceWorld.IngotTag), Is.EqualTo(2), "the batch finished after restock");
            Assert.That(world.Jobs.Contains(WorkSliceWorld.Job), Is.False);
            Assert.That(world.WorkOrders.Rows, Is.Empty);
        }
    }
}

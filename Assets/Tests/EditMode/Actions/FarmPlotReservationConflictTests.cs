using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W33 DOC4 F3: one plot, ONE active claim — W32 T2 brought to the soil. Two farmers want
    /// the same free cell; the winner is deterministic (store insertion order), the loser KNOWS
    /// (no plan, replan next tick), and an interrupted winner returns the cell to the world.
    /// </summary>
    public sealed class FarmPlotReservationConflictTests
    {
        [Test]
        public void TwoFarmers_OneFreeCell_DeterministicWinner_FreedOnInterrupt()
        {
            var world = FarmSliceWorld.Build(seedStock: 4, soilCells: 1); // ONE free cell
            world.Actors.Add(FarmSliceWorld.Farmer(1, 4, 4));             // store order: FIRST
            world.Actors.Add(FarmSliceWorld.Farmer(2, 4, 5));
            FarmSliceWorld.PostPlantingJob(world, 1);
            FarmSliceWorld.PostPlantingJob(world, 2); // both hold claimed jobs for the same field
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            ActorRecord ActorOf(ulong id) => world.Actors.Get(new ActorId(id));

            // Jobs claim on the hourly band (t=60); the next decision band resolves the plot.
            int tick = 0;
            while (FarmSliceWorld.PlotClaims(world) == 0 && tick < 200)
                composer.Advance(world, ++tick);

            Assert.That(FarmSliceWorld.PlotClaims(world), Is.EqualTo(1), "one cell → ONE claim");
            Assert.That(FarmSliceWorld.PlotClaim(world).ActorId, Is.EqualTo(1UL),
                "the winner is store insertion order — ORDER breaks the tie, never a seed (W32 T2)");
            Assert.That(ActorOf(2).ActionState.CurrentAction, Is.Not.EqualTo(ActorActionType.MoveToPlot),
                "the loser binds to NO plan; replan is next tick's decision");
            // Direct double-claim: refused, and the trace holds exactly ONE ReservationAcquired.
            Assert.That(world.Reservations.TryReserve(FarmSliceWorld.Site.Value,
                FarmSliceWorld.PlotClaim(world).ItemTag, 2UL, 9_999L, 1, out _), Is.False,
                "a claimed plot refuses a second claim outright");
            Assert.That(System.Text.RegularExpressions.Regex.Matches(
                ActionTrace.Of(world), "ReservationAcquired").Count, Is.EqualTo(1),
                "no second ReservationAcquired anywhere in the trace");

            // Release proof (W32 T5 pattern): interrupt the winner — the cell belongs to the
            // world again and the runner-up can claim it RIGHT NOW.
            FarmSliceWorld.Interrupt(world, 1UL);
            composer.Advance(world, ++tick);
            Assert.That(FarmSliceWorld.PlotClaims(world), Is.Zero, "the interrupt freed the cell");
            Assert.That(world.Reservations.TryReserve(FarmSliceWorld.Site.Value,
                FarmSliceWorld.PlotKeyPrefix + FarmSliceWorld.SoilId(0).Value, 2UL,
                9_999L, 1, out _), Is.True, "the cell is reservable by the loser again");
        }

        [Test]
        public void TwoHarvesters_OneRipePlant_OneClaim_YieldNeverExceedsOneHarvest()
        {
            var world = FarmSliceWorld.Build(seedStock: 0, soilCells: 1);
            FarmSliceWorld.PlantRipe(world);
            world.Actors.Add(FarmSliceWorld.Farmer(1, 3, 3)); // store order: FIRST (and nearer)
            world.Actors.Add(FarmSliceWorld.Farmer(2, 4, 3));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            composer.Advance(world, 1); // decision band: both want the one ripe plot
            Assert.That(FarmSliceWorld.PlotClaims(world), Is.EqualTo(1), "one ripe plot → ONE claim");
            Assert.That(FarmSliceWorld.PlotClaim(world).ActorId, Is.EqualTo(1UL));

            int tick = 1;
            while (world.Plants.Count > 0 && tick < 300) composer.Advance(world, ++tick);
            for (var settle = 0; settle < 30; settle++) composer.Advance(world, ++tick);

            Assert.That(world.Events.Events.Count(e => e.Kind == WorldEventKind.PlantHarvested),
                Is.EqualTo(1), "the plant lives PlantHarvested exactly once");
            Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.CropTag),
                Is.EqualTo(FarmSliceWorld.HarvestYield),
                "total yield never exceeds ONE harvest's yield — no double-dip through the race");
        }
    }
}

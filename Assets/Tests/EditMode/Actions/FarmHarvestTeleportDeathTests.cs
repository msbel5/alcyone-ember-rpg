using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.World;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W33 DOC4 F2: the teleport harvest is DEAD. The retired world.harvest@Daily:25 wrote
    /// +2 to the pile and rewound the plant to seed the moment a villager stood near — counters
    /// teleporting matter (RUH_TESHIS §2.8). Now: no completed HarvestCrop action, no yield;
    /// stock only ever rises through a bodied HaulCrop deposit (or a conservation refund),
    /// never through proximity magic.
    /// </summary>
    public sealed class FarmHarvestTeleportDeathTests
    {
        [Test]
        public void NoHarvester_NoYield_TheRipePlantWaits()
        {
            var world = FarmSliceWorld.Build(seedStock: 0, soilCells: 1);
            FarmSliceWorld.PlantRipe(world);
            // NO civilians on purpose: the old code waited for a nearby hand but wrote +2 the
            // tick it found one — with nobody to act, three daily boundaries must change NOTHING.
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            for (var tick = 1; tick <= 3 * WorldTickComposer.TicksPerGameDay; tick++)
                composer.Advance(world, tick);

            Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.CropTag), Is.Zero,
                "nobody lived a harvest ACTION, so the stock may not move (the old world minted +6)");
            Assert.That(world.Plants.Rows.Single().Value.StageId.Value, Is.EqualTo("ripe"),
                "the plant waits ripe — ghost hands can no longer rewind it to seed");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.PlantHarvested), Is.False);
        }

        [Test]
        public void CrowdedWorld_EveryStockIncrease_IsAuthoredByABodiedDeposit()
        {
            // Authorship sweep over the REAL cast, five days. The field pile (site 5) may only
            // rise through (a) a farm:haul completion — the deposit commit — or (b) a matter-
            // conservation refund riding an ActionFailed (W32 T5 / W33 fail-sweep). Caravan
            // grain lands on the site-1 pile, so it cannot alibi a field-pile jump.
            var world = new WorldFactory().Create(roomSeed: 4242);
            WorldFactory.SeedVillagers(world);
            world.EnsureInvariants();
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            var fieldPile = world.Stockpiles.First(p => p != null && p.SiteId.Value == 5UL);
            var centre = SiteCentre(world, 5UL);

            int prev = fieldPile.Get(FarmSliceWorld.CropTag);
            for (var tick = 1; tick <= 5 * WorldTickComposer.TicksPerGameDay; tick++)
            {
                composer.Advance(world, tick);
                int cur = fieldPile.Get(FarmSliceWorld.CropTag);
                long now = world.Time.TotalMinutes;
                if (cur > prev)
                {
                    var deposit = world.Events.Events.FirstOrDefault(e =>
                        e.Kind == WorldEventKind.ActionCompleted && e.Tick.TotalMinutes == now
                        && e.Reason != null && e.Reason.Contains("farm:haul completed"));
                    bool refund = world.Events.Events.Any(e =>
                        e.Kind == WorldEventKind.ActionFailed && e.Tick.TotalMinutes == now);
                    Assert.That(deposit != null || refund, Is.True,
                        $"t={now}: the field pile rose {prev}->{cur} with neither a HaulCrop " +
                        "completion nor a conservation refund — a teleport survives somewhere");
                    if (deposit != null)
                    {
                        // Physicality: the depositor stands within eat-reach of the site centre
                        // at the commit tick — the unit arrived on legs, not by counter magic.
                        var hauler = world.Actors.Get(deposit.ActorId);
                        long dist = System.Math.Max(
                            System.Math.Abs(hauler.Position.X - centre.X),
                            System.Math.Abs(hauler.Position.Y - centre.Y));
                        Assert.That(dist, Is.LessThanOrEqualTo(2), "the hauler delivered IN PERSON");
                    }
                }
                // PlantHarvested itself moves NO stock: the yield goes to HANDS. Any same-tick
                // pile rise must therefore carry its own deposit author (checked above).
                prev = cur;
            }
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.PlantHarvested), Is.True,
                "vacuous guard: five days must contain at least one real harvest");
        }

        private static EmberCrpg.Domain.Actors.GridPosition SiteCentre(WorldState world, ulong siteId)
        {
            var site = world.Sites.Records.First(s => s != null && s.Id.Value == siteId);
            return new EmberCrpg.Domain.Actors.GridPosition(
                (site.MinBound.X + site.MaxBound.X) / 2, (site.MinBound.Y + site.MaxBound.Y) / 2);
        }
    }
}

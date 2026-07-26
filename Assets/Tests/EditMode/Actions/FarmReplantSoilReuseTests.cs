using System.Linq;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living.Actions;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// REGRESSION PIN for the playtest report "tarla harvestten sonra kaybolmustu" (field
    /// disappeared after harvest). The sim-side chain is CLOSED: HarvestCropAdvancer removes
    /// the plant AND rebuilds the soil with WithoutPlant() so the SoilComponent row survives;
    /// the daily ShortageResponseSystem then posts a planting job against FreeSoilPositionFor,
    /// which points at the JUST-EMPTIED cell; PlantSeedAdvancer plants a new PlantComponent
    /// with FarmOperations.PlantIdFor(soilId) = PlantIdBase + soilId.Value — the same
    /// deterministic id the harvested predecessor carried.
    ///
    /// This test pins the identity-reuse claim end-to-end: after harvest the soil row is
    /// preserved with HasPlant=false, and after the replant a new plant lives on the SAME
    /// soilId with the SAME plantId. The Presentation layer's plot GameObject follows soil
    /// identity, so the visual bed must never vanish if this test is green.
    ///
    /// Also pins the shortage cascade re-armament (obs 19374): even after every ripe plant
    /// vanishes to harvest, ShortageResponseSystem must keep firing for "wheat" because the
    /// staple tag lives in FoodPileCache.FoodTags, not in the plants table. A regression that
    /// re-empties the tag set trips the second assertion: no replant, no reuse.
    /// </summary>
    public sealed class FarmReplantSoilReuseTests
    {
        [Test]
        public void HarvestThenReplant_ReusesSameSoilAndPlantId()
        {
            // The stock is BELOW the shortage threshold (4) so the daily sweep MUST post a
            // planting job. One ripe plant to harvest; one free soil to replant into.
            var world = FarmSliceWorld.Build(seedStock: 3, soilCells: 2);
            var soil0Id = FarmSliceWorld.SoilId(0);
            var expectedPlantId = FarmOperations.PlantIdFor(soil0Id).Value;

            var originalPlant = FarmSliceWorld.PlantRipe(world, soilIndex: 0);
            Assume.That(originalPlant.Id.Value, Is.EqualTo(expectedPlantId),
                "the fixture's plant id must match FarmOperations.PlantIdFor — pin the invariant");

            world.Actors.Add(FarmSliceWorld.Farmer(7, 9, 9));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            // Advance until the standing crop is harvested. Bound generously — the chain can
            // detour through eat/sleep needs in a real day. The FarmStoryChainTests bound (5
            // days) is our precedent.
            int tick = 0;
            bool harvested = false;
            for (var end = 5 * WorldTickComposer.TicksPerGameDay; tick < end; )
            {
                composer.Advance(world, ++tick);
                if (world.Events.Events.Any(e => e.Kind == WorldEventKind.PlantHarvested))
                { harvested = true; break; }
            }
            Assert.That(harvested, Is.True, "the standing ripe crop must be harvested — no harvest, nothing to pin");

            // POST-HARVEST: the SoilComponent row survives with HasPlant=false. This is the
            // sim-side guarantee that lets the Presentation plot GameObject stay visible.
            var soilAfterHarvest = world.Soils.Get(soil0Id);
            Assert.That(soilAfterHarvest, Is.Not.Null, "the soil row must survive harvest — its identity is the plot's identity");
            Assert.That(soilAfterHarvest.SiteId.Equals(FarmSliceWorld.Site), Is.True,
                "soil identity fields (SiteId) preserved across WithoutPlant()");
            Assert.That(soilAfterHarvest.Position, Is.EqualTo(originalPlant.Position),
                "soil position preserved across WithoutPlant() — the plot's local cell does not move");
            Assert.That(world.Plants.Rows.Any(r => r.Value != null && r.Value.Id.Value == expectedPlantId), Is.False,
                "the harvested plant row is gone — its stalk must vanish, but its bed must not");

            // Now run forward until a replant lands on that soil. Shortage sweep is Daily:27;
            // planting job → move → sow chain takes further ticks. Bound: another 5 days.
            bool replanted = false;
            for (var end = tick + 5 * WorldTickComposer.TicksPerGameDay; tick < end; )
            {
                composer.Advance(world, ++tick);
                var soilNow = world.Soils.Get(soil0Id);
                if (soilNow != null && soilNow.HasPlant) { replanted = true; break; }
            }
            Assert.That(replanted, Is.True,
                "the daily shortage cascade must replant on the free soil — regression: FoodTags empty after last plant died");

            // IDENTITY REUSE: the new plant carries the SAME deterministic id the harvested one
            // carried. FarmOperations.PlantIdFor(soilId) = PlantIdBase + soilId.Value —
            // the Presentation SimFieldView keys its stalk lookup by plant.Id and its plot
            // lookup by soil.Id; a fresh plant with the reused id + soilId means: same plot,
            // same stalk cell, no plot destroyed, no flicker.
            var replantRow = world.Plants.Rows.FirstOrDefault(r => r.Value != null && r.Value.Id.Value == expectedPlantId);
            Assert.That(replantRow.Value, Is.Not.Null,
                $"replant must carry PlantIdBase+soil.Id = {expectedPlantId} — deterministic reuse of the harvested id");
            Assert.That(replantRow.Value.Position, Is.EqualTo(originalPlant.Position),
                "replant sits on the SAME grid cell — the plot GameObject at that local position stays");

            // The soil's back-reference agrees: the tilled bed OWNS the new plant.
            var soilAfterReplant = world.Soils.Get(soil0Id);
            Assert.That(soilAfterReplant.PlantId.Value, Is.EqualTo(expectedPlantId),
                "soil.PlantId points at the reused id — the sim considers this the same plot as before");
        }
    }
}

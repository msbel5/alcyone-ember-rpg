using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>
    /// W33 pin migration (DOC4 §2 row 1): the Phase-5 HarvestSystem — "ripe plant becomes
    /// stockpile inventory in one call" — is RETIRED with the fiat lane that called it; the
    /// live harvest commit lives in HarvestCropAdvancer. The surviving pins migrate here with
    /// ONE change: the output address is the ACTOR'S HANDS, never the pile (the pile only
    /// rises at the HaulCrop deposit). The old stockpile-capacity refusal retired with
    /// InventoryState: the site-pile deposit is Add-only by design (W33-01 §5).
    /// </summary>
    public sealed class HarvestSystemTests
    {
        [Test]
        public void HarvestCommit_YieldsToHands_ClearsSoil_NeverTouchesThePile()
        {
            var world = FarmSliceWorld.Build(seedStock: 0, soilCells: 1);
            var plant = FarmSliceWorld.PlantRipe(world);
            world.Actors.Add(FarmSliceWorld.Farmer(7, 5, 5));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            ActorRecord A() => world.Actors.Get(new ActorId(7));

            int tick = 0;
            while (!world.Events.Events.Any(e => e.Kind == WorldEventKind.PlantHarvested) && tick < 100)
                composer.Advance(world, ++tick);

            // The commit is atomic: unplant + yield-to-hands + event + plot→carry row swap in
            // ONE step — sampled here BEFORE the haul walk reaches the pile.
            var evt = world.Events.Events.Single(e => e.Kind == WorldEventKind.PlantHarvested);
            Assert.That(evt.ActorId.Value, Is.EqualTo(7UL), "the harvester AUTHORS the event");
            Assert.That(world.Plants.Contains(plant.Id), Is.False, "the plant left the world");
            Assert.That(world.Soils.Get(FarmSliceWorld.SoilId(0)).HasPlant, Is.False, "the soil is clear");
            Assert.That(A().ActionState.CarriedUnits, Is.EqualTo(FarmSliceWorld.HarvestYield),
                "the yield rides in the HANDS — the old code's stockpile address is dead");
            Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.CropTag), Is.Zero,
                "the pile is untouched at the commit — only the deposit raises stock");
            Assert.That(world.Reservations.TryGetByActor(7UL, out var row), Is.True);
            Assert.That(row.ItemTag, Is.EqualTo(FarmSliceWorld.CarryKeyPrefix + FarmSliceWorld.CropTag),
                "the plot claim swapped into a carry row in the same step");
        }

        [Test]
        public void UnripePlant_IsRefusedWithoutMutation()
        {
            // The decision layer never targets an unripe plant (IsHarvestable gate), so the
            // refusal is probed the attacker's way: the phase is FORCED to HarvestCrop beside
            // a sprout — the advancer's own validation refuses, mutation-free (the old
            // "unripe returns false without mutation" pin, alive at the new seam).
            var world = FarmSliceWorld.Build(seedStock: 0, soilCells: 1);
            var plant = FarmSliceWorld.Plant(world, 0, "sprout");
            var reaper = FarmSliceWorld.Farmer(9, 0, 1); // adjacent: distance is NOT the refusal
            world.Actors.Add(reaper);
            Assert.That(world.Reservations.TryReserve(FarmSliceWorld.Site.Value,
                FarmSliceWorld.PlotKeyPrefix + FarmSliceWorld.SoilId(0).Value, 9UL,
                untilMinutes: 9_999L, pileCount: 1, out var claim), Is.True);
            reaper.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Harvest).Start(
                ActorActionType.HarvestCrop, FarmSliceWorld.Site, ItemId.Empty,
                new ReservationId(claim), startedAtMinutes: 360, ActionInterruptPolicy.Interruptible));

            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            composer.Advance(world, 1); // one advancement: the harvestable gate fires pre-commit

            Assert.That(world.Plants.Contains(plant.Id), Is.True, "the sprout still stands");
            Assert.That(world.Soils.Get(FarmSliceWorld.SoilId(0)).PlantId, Is.EqualTo(plant.Id),
                "the soil link is untouched");
            Assert.That(reaper.ActionState.Phase, Is.EqualTo(ActionPhase.Failed), "the action fell");
            Assert.That(reaper.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.CropGone),
                "'not harvestable' is the CropGone story, never a silent skip");
            Assert.That(reaper.ActionState.CarriedUnits, Is.Zero, "no yield was minted");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.PlantHarvested), Is.False);
        }
    }
}

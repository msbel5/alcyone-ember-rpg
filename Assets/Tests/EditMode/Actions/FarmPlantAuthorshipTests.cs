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
    /// W33 DOC4 F1: in the live flow a plant's ONLY parent is a COMPLETED PlantSeed action.
    /// Worldgen aside, no system may conjure a plant: the seed leaves the pile and the
    /// PlantComponent is born in the same atomic commit, authored by a named actor who was
    /// physically at the plot. Half an intent births nothing; remote planting is refused.
    /// </summary>
    public sealed class FarmPlantAuthorshipTests
    {
        [Test]
        public void Plant_IsBornOnlyFromACompletedPlantSeedAction_WithAuthorAndMatterTrail()
        {
            var world = FarmSliceWorld.Build(seedStock: 2, soilCells: 1);
            world.Actors.Add(FarmSliceWorld.Farmer(7, 9, 9));
            FarmSliceWorld.PostPlantingJob(world); // the cascade's job, posted by hand (DOC 02 seam)
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            int plantsBefore = world.Plants.Count;

            int tick = 0;
            while (world.Plants.Count == plantsBefore && tick < 2000)
                composer.Advance(world, ++tick);
            Assert.That(world.Plants.Count, Is.GreaterThan(plantsBefore), "the chain never planted");

            var planted = world.Events.Events.Single(e => e.Kind == WorldEventKind.PlantPlanted);
            Assert.That(planted.ActorId.Value, Is.EqualTo(7UL), "the planter is NAMED on the event");
            // Trace: the SAME actor lived a PlantSeed Running->Succeeded transition on the SAME
            // tick the plant was born — authorship, not coincidence (one ActionTrace line).
            Assert.That(ActionTrace.Of(world),
                Does.Contain($"{planted.Tick.TotalMinutes}:7:Plant:PlantSeed/Running->PlantSeed/Succeeded"),
                "the plant's parent is a completed PlantSeed by actor 7 at the commit tick");
            // Matter: the seed stock dropped by EXACTLY one — no output without input, and no
            // input burned without output.
            Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.SeedTag), Is.EqualTo(1),
                "one seed in the ground, one seed left in the pile");
        }

        [Test]
        public void InterruptedWalk_BirthsNoPlant_BurnsNoSeed_FreesThePlot()
        {
            var world = FarmSliceWorld.Build(seedStock: 2, soilCells: 1);
            world.Actors.Add(FarmSliceWorld.Farmer(7, 9, 9));
            FarmSliceWorld.PostPlantingJob(world);
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            ActorRecord A() => world.Actors.Get(new ActorId(7));

            int tick = 0;
            while (!(A().ActionState.CurrentAction == ActorActionType.MoveToPlot
                     && A().ActionState.Phase == ActionPhase.Running) && tick < 200)
                composer.Advance(world, ++tick);
            Assert.That(A().ActionState.CurrentAction, Is.EqualTo(ActorActionType.MoveToPlot),
                "run-up never reached the walk");

            FarmSliceWorld.Interrupt(world, 7UL); // cut mid-walk (the W32 pursuit gate)
            composer.Advance(world, ++tick);

            Assert.That(world.Plants.Count, Is.Zero, "half an intent births NO plant");
            Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.SeedTag), Is.EqualTo(2),
                "the seed never left the pile");
            Assert.That(FarmSliceWorld.PlotClaims(world), Is.Zero, "the plot claim is freed");
        }

        [Test]
        public void RemotePlanting_IsPhysicallyRefused()
        {
            // Attacker setup (W32 T1 pattern): the phase is FORCED to PlantSeed while the actor
            // stands 40 cells away — the operation itself validates distance, whatever the
            // system order, so the fiat write is refused and nothing mutates.
            var world = FarmSliceWorld.Build(seedStock: 2, soilCells: 1);
            var far = FarmSliceWorld.Farmer(9, 40, 40);
            world.Actors.Add(far);
            Assert.That(world.Reservations.TryReserve(FarmSliceWorld.Site.Value,
                FarmSliceWorld.PlotKeyPrefix + FarmSliceWorld.SoilId(0).Value, 9UL,
                untilMinutes: 9_999L, pileCount: 1, out var claim), Is.True);
            far.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Plant).Start(
                ActorActionType.PlantSeed, FarmSliceWorld.Site, ItemId.Empty,
                new ReservationId(claim), startedAtMinutes: 360, ActionInterruptPolicy.Interruptible));

            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            composer.Advance(world, 1); // one advancement: the reach gate fires before any commit

            Assert.That(world.Plants.Count, Is.Zero, "remote planting is REFUSED");
            Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.SeedTag), Is.EqualTo(2), "no seed burned");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.PlantPlanted), Is.False);
        }
    }
}

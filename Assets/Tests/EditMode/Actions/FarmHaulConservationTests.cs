using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living.Actions;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W33 DOC4 F4: hauling is the slice's new PHYSICS — the unit TRAVELS plot → hands → pile.
    /// At every interruption point total matter is flat (TotalCrop: pile + hands + ripe-plot
    /// potential): no dup, no loss. WHERE the unit lands on interruption is DOC 03's business;
    /// this test pins only the CONSERVATION (the W32 T5 simplification lesson).
    /// </summary>
    public sealed class FarmHaulConservationTests
    {
        [TestCase(ActorActionType.MoveToPlot)]
        [TestCase(ActorActionType.HarvestCrop)]
        [TestCase(ActorActionType.HaulCrop)]
        public void Interrupt_AtLink_ConservesCropAndFreesClaims(ActorActionType at)
        {
            var world = FarmSliceWorld.Build(seedStock: 0, soilCells: 1);
            FarmSliceWorld.PlantRipe(world);
            world.Actors.Add(FarmSliceWorld.Farmer(7, 12, 12));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            ActorRecord A() => world.Actors.Get(new ActorId(7));

            int tick = 0; // deterministic run-up to the link under test
            while (!(A().ActionState.CurrentAction == at && A().ActionState.Phase == ActionPhase.Running)
                   && tick < 200)
                composer.Advance(world, ++tick);
            Assert.That(A().ActionState.CurrentAction, Is.EqualTo(at), "run-up never reached the link");

            int before = FarmSliceWorld.TotalCrop(world); // plot + hands + pile
            FarmSliceWorld.Interrupt(world, 7UL);
            composer.Advance(world, ++tick); // the probe fires before the step
            composer.Advance(world, ++tick); // Failed -> Idle settles

            Assert.That(FarmSliceWorld.TotalCrop(world), Is.EqualTo(before),
                "MATTER CONSERVATION: no dup, no loss at this link");
            Assert.That(world.Reservations.Rows, Is.Empty, "plot AND carry claims are freed");
            Assert.That(A().ActionState.IsIdle, Is.True, "the action fell; replan is the next decision");
        }

        [Test]
        public void HappyPath_TheUnitIsAlwaysInExactlyOnePlace()
        {
            var world = FarmSliceWorld.Build(seedStock: 0, soilCells: 1);
            FarmSliceWorld.PlantRipe(world);
            world.Actors.Add(FarmSliceWorld.Farmer(7, 12, 12));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            ActorRecord A() => world.Actors.Get(new ActorId(7));

            long harvestedAt = 0, depositedAt = 0;
            int tick = 0;
            while (depositedAt == 0 && tick < 300)
            {
                composer.Advance(world, ++tick);
                Assert.That(FarmSliceWorld.TotalCrop(world), Is.EqualTo(FarmSliceWorld.HarvestYield),
                    $"t={tick}: the yield exists exactly ONCE across plot/hands/pile");
                if (harvestedAt == 0 && world.Events.Events.Any(e => e.Kind == WorldEventKind.PlantHarvested))
                    harvestedAt = world.Time.TotalMinutes;
                if (harvestedAt != 0 && depositedAt == 0)
                {
                    if (world.Stockpiles[0].Get(FarmSliceWorld.CropTag) == FarmSliceWorld.HarvestYield)
                        depositedAt = world.Time.TotalMinutes;
                    else
                        Assert.That(A().ActionState.CarriedUnits, Is.EqualTo(FarmSliceWorld.HarvestYield),
                            "between harvest commit and deposit the unit rides in the HANDS — " +
                            "never two places, never zero places");
                }
            }
            Assert.That(depositedAt, Is.GreaterThan(harvestedAt), "the deposit followed the harvest");
            Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.CropTag), Is.EqualTo(FarmSliceWorld.HarvestYield),
                "pile delta == hands delta: the chain's one stock-raising gate is the deposit");
            Assert.That(A().ActionState.CarriedUnits, Is.Zero, "hands empty after the deposit");
        }

        [Test]
        public void ExpiredCarryReservation_RefundsTaggedMatterBeforeFailure()
        {
            var world = FarmSliceWorld.Build(seedStock: 0, soilCells: 1);
            var farmer = FarmSliceWorld.Farmer(7, 12, 12);
            world.Actors.Add(farmer);
            Assert.That(world.Reservations.TryReserve(
                FarmSliceWorld.Site.Value,
                FarmSliceWorld.CarryKeyPrefix + FarmSliceWorld.CropTag,
                farmer.Id.Value,
                untilMinutes: 0,
                pileCount: int.MaxValue,
                out var claim), Is.True);
            farmer.ApplyActionState(
                ActorActionState.ForIntent(ActorIntent.Harvest)
                    .Start(
                        ActorActionType.HaulCrop,
                        FarmSliceWorld.Site,
                        ItemId.Empty,
                        new ReservationId(claim),
                        startedAtMinutes: 0,
                        policy: ActionInterruptPolicy.Interruptible)
                    .WithCarriedMatter(FarmSliceWorld.CropTag, FarmSliceWorld.HarvestYield));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            var before = world.Stockpiles[0].Get(FarmSliceWorld.CropTag)
                + farmer.ActionState.CarriedUnits;

            composer.Advance(world, 1);

            var after = world.Stockpiles[0].Get(FarmSliceWorld.CropTag)
                + farmer.ActionState.CarriedUnits;
            Assert.That(after, Is.EqualTo(before).And.EqualTo(FarmSliceWorld.HarvestYield));
            Assert.That(farmer.ActionState.Phase, Is.EqualTo(ActionPhase.Failed));
            Assert.That(farmer.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.ReservationLost));
            Assert.That(farmer.ActionState.CarriedUnits, Is.Zero);
            Assert.That(world.Reservations.Rows, Is.Empty);
            var recovery = world.Events.Events.Single(e => e.Kind == WorldEventKind.MatterRecovered);
            Assert.That(recovery.Reason,
                Does.Contain("item:wheat").And.Contain("qty:2").And.Contain("path:missing_reservation"));
        }

        [Test]
        public void StrikeInterruption_AfterHarvestSucceeded_RefundsMatterAndClaimBeforeIdle()
        {
            var world = FarmSliceWorld.Build(seedStock: 0, soilCells: 1);
            var carrier = FarmSliceWorld.Farmer(7, 1, 0);
            var hunter = new ActorRecord(
                new ActorId(8), "Hunter", ActorRole.Enemy,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(
                    new VitalStat(30, 30),
                    new VitalStat(10, 10),
                    new VitalStat(10, 10)),
                new GridPosition(0, 0),
                accuracy: 100, dodge: 0, armor: 0, baseDamage: 100);
            world.Actors.Add(carrier);
            world.Actors.Add(hunter);
            Assert.That(world.Reservations.TryReserve(
                FarmSliceWorld.Site.Value,
                FarmSliceWorld.CarryKeyPrefix + FarmSliceWorld.CropTag,
                carrier.Id.Value,
                untilMinutes: 600,
                pileCount: int.MaxValue,
                out var claim), Is.True);
            carrier.ApplyActionState(
                ActorActionState.ForIntent(ActorIntent.Harvest)
                    .Start(
                        ActorActionType.HaulCrop,
                        FarmSliceWorld.Site,
                        ItemId.Empty,
                        new ReservationId(claim),
                        startedAtMinutes: 1,
                        policy: ActionInterruptPolicy.Interruptible)
                    .WithCarriedMatter(FarmSliceWorld.CropTag, FarmSliceWorld.HarvestYield)
                    .Succeeded());
            hunter.ApplyActionState(
                ActorActionState.ForIntent(ActorIntent.Hunt).Start(
                    ActorActionType.StrikeQuarry,
                    FarmSliceWorld.Site,
                    ItemId.Empty,
                    ReservationId.Empty,
                    startedAtMinutes: 1,
                    policy: ActionInterruptPolicy.Interruptible,
                    targetActor: carrier.Id));
            world.HuntTargets.Add(new HuntTargetRecord
            {
                HunterId = hunter.Id.Value,
                TargetId = carrier.Id.Value,
                UntilMinutes = 600,
            });
            var before = FarmSliceWorld.TotalCrop(world);

            new StrikeQuarryAdvancer(new ActionLogManager())
                .Advance(world, hunter, new GameTime(60));

            global::EmberCrpg.Tests.EditMode.TestAssert.Multiple(() =>
            {
                Assert.That(carrier.ActionState.IsIdle, Is.True);
                Assert.That(carrier.ActionState.CarriedUnits, Is.Zero);
                Assert.That(world.Reservations.Rows, Is.Empty);
                Assert.That(FarmSliceWorld.TotalCrop(world), Is.EqualTo(before));
                Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.CropTag),
                    Is.EqualTo(FarmSliceWorld.HarvestYield));
            });
        }
    }
}

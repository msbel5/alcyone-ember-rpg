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
    /// PRD-02 story tests: movement actions consume explicit bounded-route outcomes.
    /// Before the recovery fix a long wall produced an endless greedy freeze while still
    /// increasing ProgressTicks; no-route also left reservations/jobs claimed.
    ///
    /// Matter conservation is preserved by construction (this test drives ONLY movement — no
    /// deposit/take) and the actor's own Position rows are the ground truth (no guess labels).
    /// </summary>
    public sealed class MoveAvoidsBlockedCellsTests
    {
        [Test]
        public void MoveToFood_StepDoesNotEnterBlockedCandidate_UsesDeterministicDetour()
        {
            // Seat is (5,5); the direct candidate (4,4) is walled.
            var world = EatSliceWorld.Build();
            var diner = EatSliceWorld.Hungry(1, 3, 3);
            world.Actors.Add(diner);
            Assert.That(world.Reservations.TryReserve(1UL, "wheat", 1UL,
                untilMinutes: 999, pileCount: 10, out var rid), Is.True);
            world.Blocked.Add(new GridPosition(4, 4));
            diner.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Eat).Start(
                ActorActionType.MoveToFood, new SiteId(1), ItemId.Empty,
                new ReservationId(rid), startedAtMinutes: 61, ActionInterruptPolicy.Interruptible));

            new MoveToFoodAdvancer(new ActionLogManager()).Advance(world, diner, new GameTime(61));

            Assert.That(world.Blocked.Contains(diner.Position), Is.False,
                "the actor MUST NOT stand on a blocked cell");
            Assert.That(diner.Position, Is.EqualTo(new GridPosition(4, 3)),
                "equal detours use the fixed east-before-north tie-break");
        }

        [Test]
        public void MoveToFood_WithoutBlockers_StillTakesTheDiagonalCandidate()
        {
            // Regression guard: the nav-aware path MUST NOT slow anyone down when nothing is blocked.
            var world = EatSliceWorld.Build();
            var diner = EatSliceWorld.Hungry(2, 4, 4);
            world.Actors.Add(diner);
            Assert.That(world.Reservations.TryReserve(1UL, "wheat", 2UL,
                untilMinutes: 999, pileCount: 10, out var rid), Is.True);
            diner.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Eat).Start(
                ActorActionType.MoveToFood, new SiteId(1), ItemId.Empty,
                new ReservationId(rid), startedAtMinutes: 61, ActionInterruptPolicy.Interruptible));

            new MoveToFoodAdvancer(new ActionLogManager()).Advance(world, diner, new GameTime(61));

            Assert.That(diner.Position, Is.EqualTo(new GridPosition(5, 5)),
                "no blockers ⇒ the diagonal Chebyshev step still wins (no regression on open ground)");
        }

        [Test]
        public void MoveToFood_UnreachablePreservesExistingProgress_AndReleasesReservation()
        {
            var world = EatSliceWorld.Build();
            var diner = EatSliceWorld.Hungry(3, 0, 0);
            world.Actors.Add(diner);
            Assert.That(world.Reservations.TryReserve(1UL, "wheat", diner.Id.Value,
                untilMinutes: 999, pileCount: 10, out var rid), Is.True);
            for (var x = -1; x <= 1; x++)
                for (var y = -1; y <= 1; y++)
                    if (x != 0 || y != 0)
                        world.Blocked.Add(new GridPosition(x, y));
            var alreadyProgressed = ActorActionState.ForIntent(ActorIntent.Eat).Start(
                ActorActionType.MoveToFood, new SiteId(1), ItemId.Empty,
                new ReservationId(rid), startedAtMinutes: 61,
                ActionInterruptPolicy.Interruptible).Advanced().Advanced();
            diner.ApplyActionState(alreadyProgressed);

            new MoveToFoodAdvancer(new ActionLogManager()).Advance(world, diner, new GameTime(61));

            Assert.That(diner.Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(diner.ActionState.Phase, Is.EqualTo(ActionPhase.Failed));
            Assert.That(diner.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Unreachable));
            Assert.That(diner.ActionState.ProgressTicks, Is.EqualTo(2),
                "a no-route tick preserves prior progress; it cannot manufacture another tick");
            Assert.That(world.Reservations.TryGetByActor(diner.Id.Value, out _), Is.False,
                "terminal unreachable releases the reservation exactly once");
        }

        [Test]
        public void MoveToWorksite_UnreachableReleasesJobClaim_AndClearsSchedule()
        {
            var world = WorkSliceWorld.Build();
            var smith = WorkSliceWorld.Smith(7, 9, 9);
            world.Actors.Add(smith);
            WorkSliceWorld.PostSmeltJob(world);
            Assert.That(world.Jobs.TryClaim(WorkSliceWorld.Job, smith.Id, out _), Is.True);
            smith.ApplyScheduleState(ActorScheduleState.Assigned(
                WorkSliceWorld.Job, WorkSliceWorld.Site, WorkSliceWorld.Bench));
            smith.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Work).Start(
                ActorActionType.MoveToWorksite, WorkSliceWorld.Site, ItemId.Empty,
                ReservationId.Empty, startedAtMinutes: 360,
                ActionInterruptPolicy.Interruptible));
            for (var x = 8; x <= 10; x++)
                for (var y = 8; y <= 10; y++)
                    if (x != 9 || y != 9)
                        world.Blocked.Add(new GridPosition(x, y));

            new MoveToWorksiteAdvancer(new ActionLogManager())
                .Advance(world, smith, new GameTime(360));

            Assert.That(smith.ActionState.Phase, Is.EqualTo(ActionPhase.Failed));
            Assert.That(smith.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Unreachable));
            Assert.That(smith.ActionState.ProgressTicks, Is.EqualTo(0));
            Assert.That(world.Jobs.Contains(WorkSliceWorld.Job), Is.True,
                "cleanup returns the job to pending; it does not delete work");
            Assert.That(world.Jobs.IsClaimed(WorkSliceWorld.Job), Is.False);
            Assert.That(smith.ScheduleState.IsIdle, Is.True);
        }

        [Test]
        public void HaulCrop_UnreachableDoesNotAdvance_AndRefundsCarriedMatter()
        {
            var world = FarmSliceWorld.Build(seedStock: 0, soilCells: 1);
            var farmer = FarmSliceWorld.Farmer(7, 0, 0);
            world.Actors.Add(farmer);
            Assert.That(world.Reservations.TryReserve(
                FarmSliceWorld.Site.Value,
                FarmSliceWorld.CarryKeyPrefix + FarmSliceWorld.CropTag,
                farmer.Id.Value,
                untilMinutes: 999,
                pileCount: int.MaxValue,
                out var rid), Is.True);
            farmer.ApplyActionState(
                ActorActionState.ForIntent(ActorIntent.Harvest).Start(
                    ActorActionType.HaulCrop,
                    FarmSliceWorld.Site,
                    ItemId.Empty,
                    new ReservationId(rid),
                    startedAtMinutes: 360,
                    ActionInterruptPolicy.Interruptible)
                .WithCarriedMatter(FarmSliceWorld.CropTag, FarmSliceWorld.HarvestYield));
            SealActor(world, farmer.Position);

            new HaulCropAdvancer(new ActionLogManager())
                .Advance(world, farmer, new GameTime(360));

            Assert.That(farmer.Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(farmer.ActionState.Phase, Is.EqualTo(ActionPhase.Failed));
            Assert.That(farmer.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Unreachable));
            Assert.That(farmer.ActionState.ProgressTicks, Is.Zero);
            Assert.That(farmer.ActionState.CarriedUnits, Is.Zero);
            Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.CropTag),
                Is.EqualTo(FarmSliceWorld.HarvestYield));
            Assert.That(world.Reservations.TryGetByActor(farmer.Id.Value, out _), Is.False);
        }

        [Test]
        public void OnWatch_UnreachableDoesNotAdvance()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            var guard = new ActorRecord(
                new ActorId(7), "Guard", ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(
                    new VitalStat(10, 10),
                    new VitalStat(10, 10),
                    new VitalStat(10, 10)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 5,
                armor: 0,
                baseDamage: 1,
                home: new GridPosition(0, 0),
                dayAnchor: new GridPosition(5, 5));
            world.Actors.Add(guard);
            guard.ApplyActionState(
                ActorActionState.ForIntent(ActorIntent.Watch).Start(
                    ActorActionType.OnWatch,
                    default,
                    ItemId.Empty,
                    ReservationId.Empty,
                    startedAtMinutes: 360,
                    ActionInterruptPolicy.Interruptible));
            SealActor(world, guard.Position);

            new OnWatchAdvancer(new ActionLogManager())
                .Advance(world, guard, new GameTime(360));

            Assert.That(guard.Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(guard.ActionState.Phase, Is.EqualTo(ActionPhase.Failed));
            Assert.That(guard.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Unreachable));
            Assert.That(guard.ActionState.ProgressTicks, Is.Zero);
        }

        [Test]
        public void SymmetricEqualCostDetours_OneHundredRuns_HaveIdenticalNextStepAndWorldDigest()
        {
            GridPosition expectedStep = default;
            string expectedDigest = null;

            for (var run = 0; run < 100; run++)
            {
                var world = EatSliceWorld.Build();
                var diner = EatSliceWorld.Hungry(1, 0, 0);
                world.Actors.Add(diner);
                world.Blocked.Add(new GridPosition(1, 0));

                // (1,1) and (1,-1) are both two-step routes to (2,0).
                // The fixed north-before-south tie-break must select (1,1).
                var movement = MovementService.RouteToward(
                    diner.Position, new GridPosition(2, 0), world.NavView);
                Assert.That(movement.Outcome, Is.EqualTo(MovementStepOutcome.Moved));
                diner.MoveTo(movement.Position);
                var digest = WorldStateDigest.Compute(world);

                if (run == 0)
                {
                    expectedStep = diner.Position;
                    expectedDigest = digest;
                }
                else
                {
                    Assert.That(diner.Position, Is.EqualTo(expectedStep), $"next-step drift on run {run}");
                    Assert.That(digest, Is.EqualTo(expectedDigest), $"digest drift on run {run}");
                }
            }

            Assert.That(expectedStep, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(expectedDigest, Is.Not.Null.And.Not.Empty);
        }

        private static void SealActor(WorldState world, GridPosition position)
        {
            for (var x = position.X - 1; x <= position.X + 1; x++)
                for (var y = position.Y - 1; y <= position.Y + 1; y++)
                    if (x != position.X || y != position.Y)
                        world.Blocked.Add(new GridPosition(x, y));
        }
    }
}

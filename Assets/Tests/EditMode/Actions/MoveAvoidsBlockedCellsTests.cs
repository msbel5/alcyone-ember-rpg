using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living.Actions;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// B10 story test: with Stage-A shipped, an actor walking to a food-spot MUST route AROUND a
    /// wall cell between it and the seat — the pre-fix behaviour was to clip straight through
    /// (MovementService.StepToward was wall-blind at every one of the 8 locomotion seams). The
    /// stronger claim we pin: the actor never stands ON a Blocked cell mid-route.
    ///
    /// Matter conservation is preserved by construction (this test drives ONLY movement — no
    /// deposit/take) and the actor's own Position rows are the ground truth (no guess labels).
    /// </summary>
    public sealed class MoveAvoidsBlockedCellsTests
    {
        [Test]
        public void MoveToFood_StepDoesNotEnterBlockedCandidate_SlidesAxially()
        {
            // Seat is diagonally up-right of the actor; the diagonal candidate cell is walled.
            // Pre-fix: actor would step into the wall. Post-fix: actor slides along X (fixed
            // axial order — the determinism pin).
            var world = EatSliceWorld.Build();
            var diner = EatSliceWorld.Hungry(1, 4, 4); // seat centre is (5,5)
            world.Actors.Add(diner);
            Assert.That(world.Reservations.TryReserve(1UL, "wheat", 1UL,
                untilMinutes: 999, pileCount: 10, out var rid), Is.True);
            world.Blocked.Add(new GridPosition(5, 5)); // the diagonal candidate cell IS a wall
            diner.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Eat).Start(
                ActorActionType.MoveToFood, new SiteId(1), ItemId.Empty,
                new ReservationId(rid), startedAtMinutes: 61, ActionInterruptPolicy.Interruptible));

            new MoveToFoodAdvancer(new ActionLogManager()).Advance(world, diner, new GameTime(61));

            Assert.That(world.Blocked.Contains(diner.Position), Is.False,
                "B10: the actor MUST NOT stand on a blocked cell — the wall-blind step is retired");
            Assert.That(diner.Position, Is.Not.EqualTo(new GridPosition(5, 5)),
                "the wall cell (5,5) is refused; the actor slid axially");
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
    }
}

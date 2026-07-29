using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Movement
{
    /// <summary>
    /// PRD-02: deterministic bounded routing over the existing navigability authority.
    /// Before the recovery fix a greedy neighbour probe could not route around a long wall
    /// and represented "no route" by returning the unchanged start cell.
    /// </summary>
    public sealed class MovementServiceBlockerTests
    {
        // Tiny stub — the interface is 2 methods. Keeps the tests literal; no world fixture noise.
        private sealed class NavStub : IWorldNavigability
        {
            private readonly System.Collections.Generic.HashSet<long> _blocked = new System.Collections.Generic.HashSet<long>();

            public NavStub Block(int x, int y) { _blocked.Add(Key(x, y)); return this; }

            public bool IsWalkable(GridPosition cell) => !_blocked.Contains(Key(cell.X, cell.Y));

            public bool BlocksDiagonal(GridPosition from, GridPosition to)
                => _blocked.Contains(Key(to.X, from.Y)) && _blocked.Contains(Key(from.X, to.Y));

            private static long Key(int x, int y) => ((long)y * 1_000_000L) + x;
        }

        [Test]
        public void NullNav_PreservesLegacyChebyshevPrimitive()
        {
            // Legacy callers (and every existing pin) pass no nav. The two-line Chebyshev step MUST
            // be bit-identical to the pre-B10 primitive — no digest surprise for wall-blind runs.
            var step = MovementService.StepToward(new GridPosition(0, 0), new GridPosition(5, 3));
            Assert.That(step, Is.EqualTo(new GridPosition(1, 1)));
        }

        [Test]
        public void CompatibilityStep_DoesNotApplyActionRouteRadius()
        {
            var step = MovementService.StepToward(
                new GridPosition(0, 0),
                new GridPosition(MovementService.RouteRadiusBudget + 1, 0),
                MovementService.OpenNav);

            Assert.That(step, Is.EqualTo(new GridPosition(1, 0)),
                "actionless schedule movement stays unbounded one-cell compatibility movement");
        }

        [Test]
        public void OpenCandidate_TakesTheDiagonal()
        {
            var nav = new NavStub();
            var step = MovementService.StepToward(new GridPosition(0, 0), new GridPosition(5, 5), nav);
            Assert.That(step, Is.EqualTo(new GridPosition(1, 1)));
        }

        [Test]
        public void AlreadyWithinGoalRadius_ReturnsArrivedWithoutMovement()
        {
            var from = new GridPosition(4, 4);
            var result = MovementService.RouteToward(
                from, new GridPosition(5, 5), new NavStub(), goalRadius: 1);

            Assert.That(result.Outcome, Is.EqualTo(MovementStepOutcome.Arrived));
            Assert.That(result.Moved, Is.False);
            Assert.That(result.Position, Is.EqualTo(from));
        }

        [Test]
        public void NodeBudgetExhaustion_FailsClosed_EvenWhenGoalWasAlreadyDiscovered()
        {
            // Budget 2 holds start + east/goal. Exploring the next unseen neighbour
            // exhausts the graph budget; returning the retained goal would silently
            // certify a route after discarding equal alternatives.
            var from = new GridPosition(0, 0);
            var result = MovementService.RouteToward(
                from, new GridPosition(1, 0), new NavStub(),
                goalRadius: 0, nodeBudget: 2, radiusBudget: 4);

            Assert.That(result.Outcome, Is.EqualTo(MovementStepOutcome.NoRoute));
            Assert.That(result.Moved, Is.False);
            Assert.That(result.Position, Is.EqualTo(from));
        }

        [Test]
        public void BlockedDiagonalCandidate_DetoursViaX_FirstInFixedOrder()
        {
            // Diagonal target is blocked, X-axial is open, Y-axial is open too — X wins (the pin).
            var nav = new NavStub().Block(1, 1);
            var step = MovementService.StepToward(new GridPosition(0, 0), new GridPosition(5, 5), nav);
            Assert.That(step, Is.EqualTo(new GridPosition(1, 0)));
        }

        [Test]
        public void BlockedDiagonalAndXAxial_DetoursViaY()
        {
            // Diagonal + X blocked; Y open — the second axial in fixed order wins.
            var nav = new NavStub().Block(1, 1).Block(1, 0);
            var step = MovementService.StepToward(new GridPosition(0, 0), new GridPosition(5, 5), nav);
            Assert.That(step, Is.EqualTo(new GridPosition(0, 1)));
        }

        [Test]
        public void SealedStart_ReturnsExplicitNoRoute()
        {
            var from = new GridPosition(0, 0);
            var nav = new NavStub();
            for (var x = -1; x <= 1; x++)
                for (var y = -1; y <= 1; y++)
                    if (x != 0 || y != 0)
                        nav.Block(x, y);

            var result = MovementService.RouteToward(from, new GridPosition(5, 5), nav);

            Assert.That(result.Outcome, Is.EqualTo(MovementStepOutcome.NoRoute));
            Assert.That(result.Moved, Is.False);
            Assert.That(result.Position, Is.EqualTo(from));
        }

        [Test]
        public void DiagonalCornerCut_ThroughWallCrack_IsRefused()
        {
            // Diagonal target cell is OPEN, but both orthogonal neighbours between from and to are
            // blocked (the "wall crack"). Standard rule: refuse the diagonal, slide axially instead.
            // Here: (1,0) blocked, (0,1) blocked, (1,1) itself open — diagonal is refused.
            // The bounded route may go around the crack, but never takes (1,1) directly.
            var from = new GridPosition(0, 0);
            var nav = new NavStub().Block(1, 0).Block(0, 1);
            var result = MovementService.RouteToward(from, new GridPosition(5, 5), nav);
            Assert.That(result.Outcome, Is.EqualTo(MovementStepOutcome.Moved));
            Assert.That(result.Position, Is.Not.EqualTo(new GridPosition(1, 1)));
        }

        [Test]
        public void StraightAxialTarget_IsUnaffectedByDiagonalRule()
        {
            // Pure east step: no diagonal, corner-cut check is inert — the actor takes the axial.
            var step = MovementService.StepToward(new GridPosition(0, 0), new GridPosition(5, 0), new NavStub());
            Assert.That(step, Is.EqualTo(new GridPosition(1, 0)));
        }

        [Test]
        public void IntegerBoundary_DoesNotOverflowOrTeleportAcrossTheGrid()
        {
            var nav = new NavStub();
            var edge = new GridPosition(int.MaxValue, 0);

            var impossible = MovementService.RouteToward(
                edge, new GridPosition(int.MinValue, 0), nav);
            Assert.That(impossible.Outcome, Is.EqualTo(MovementStepOutcome.NoRoute));
            Assert.That(impossible.Position, Is.EqualTo(edge),
                "long delta exceeds the radius; int wrap must not turn it into one step");

            var safeStep = MovementService.RouteToward(
                edge, new GridPosition(int.MaxValue - 1, 0), nav);
            Assert.That(safeStep.Outcome, Is.EqualTo(MovementStepOutcome.Moved));
            Assert.That(safeStep.Position, Is.EqualTo(new GridPosition(int.MaxValue - 1, 0)),
                "out-of-range east neighbours are rejected before construction");
        }

        [Test]
        public void OneCellWall_RouteReachesTargetWithoutEnteringWall()
        {
            var nav = new NavStub().Block(1, 0);
            var cursor = new GridPosition(0, 0);
            var target = new GridPosition(4, 0);

            for (var tick = 0; tick < 8 && !cursor.Equals(target); tick++)
            {
                var result = MovementService.RouteToward(cursor, target, nav);
                Assert.That(result.Outcome, Is.EqualTo(MovementStepOutcome.Moved));
                cursor = result.Position;
                Assert.That(nav.IsWalkable(cursor), Is.True);
            }

            Assert.That(cursor, Is.EqualTo(target));
        }

        [Test]
        public void LongWall_RouteFindsDetourWithinPinnedBudgets()
        {
            var nav = new NavStub();
            for (var y = -8; y <= 8; y++)
                nav.Block(1, y);
            var cursor = new GridPosition(0, 0);
            var target = new GridPosition(6, 0);

            for (var tick = 0; tick < 40 && !cursor.Equals(target); tick++)
            {
                var result = MovementService.RouteToward(cursor, target, nav);
                Assert.That(result.Outcome, Is.EqualTo(MovementStepOutcome.Moved),
                    $"bounded route unexpectedly failed at {cursor}");
                cursor = result.Position;
                Assert.That(nav.IsWalkable(cursor), Is.True);
            }

            Assert.That(cursor, Is.EqualTo(target), "the actor walked around the long wall");
            Assert.That(MovementService.RouteNodeBudget, Is.EqualTo(4096));
            Assert.That(MovementService.RouteRadiusBudget, Is.EqualTo(256));
        }
    }
}

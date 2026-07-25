using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Movement
{
    /// <summary>
    /// B10 §A4: nav-aware MovementService.StepToward pins. Table of five cases spelled out one
    /// arrow at a time — legacy nullness, blocked-diagonal corner cut, blocked-candidate axial
    /// fallback (X first, then Y), and both-blocked freeze. The wall the actor CANNOT walk through
    /// used to just be ignored (B10 root cause); these tests make that a compile-in guarantee.
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
        public void OpenCandidate_TakesTheDiagonal()
        {
            var nav = new NavStub();
            var step = MovementService.StepToward(new GridPosition(0, 0), new GridPosition(5, 5), nav);
            Assert.That(step, Is.EqualTo(new GridPosition(1, 1)));
        }

        [Test]
        public void BlockedDiagonalCandidate_FallsBackToXAxial_FirstInFixedOrder()
        {
            // Diagonal target is blocked, X-axial is open, Y-axial is open too — X wins (the pin).
            var nav = new NavStub().Block(1, 1);
            var step = MovementService.StepToward(new GridPosition(0, 0), new GridPosition(5, 5), nav);
            Assert.That(step, Is.EqualTo(new GridPosition(1, 0)));
        }

        [Test]
        public void BlockedDiagonalAndXAxial_FallsBackToYAxial()
        {
            // Diagonal + X blocked; Y open — the second axial in fixed order wins.
            var nav = new NavStub().Block(1, 1).Block(1, 0);
            var step = MovementService.StepToward(new GridPosition(0, 0), new GridPosition(5, 5), nav);
            Assert.That(step, Is.EqualTo(new GridPosition(0, 1)));
        }

        [Test]
        public void AllThreeBlocked_FreezesOneTick_ByReturningFrom()
        {
            // Every option blocked ⇒ the action's arrival predicate is unchanged, the actor retries
            // next tick. Never introduce a "give up" enum — W32 taxonomy owns that story.
            var from = new GridPosition(0, 0);
            var nav = new NavStub().Block(1, 1).Block(1, 0).Block(0, 1);
            var step = MovementService.StepToward(from, new GridPosition(5, 5), nav);
            Assert.That(step, Is.EqualTo(from));
        }

        [Test]
        public void DiagonalCornerCut_ThroughWallCrack_IsRefused()
        {
            // Diagonal target cell is OPEN, but both orthogonal neighbours between from and to are
            // blocked (the "wall crack"). Standard rule: refuse the diagonal, slide axially instead.
            // Here: (1,0) blocked, (0,1) blocked, (1,1) itself open — diagonal is refused, freezes
            // because the fallback axials are the same blocked cells.
            var from = new GridPosition(0, 0);
            var nav = new NavStub().Block(1, 0).Block(0, 1);
            var step = MovementService.StepToward(from, new GridPosition(5, 5), nav);
            Assert.That(step, Is.EqualTo(from));
        }

        [Test]
        public void StraightAxialTarget_IsUnaffectedByDiagonalRule()
        {
            // Pure east step: no diagonal, corner-cut check is inert — the actor takes the axial.
            var step = MovementService.StepToward(new GridPosition(0, 0), new GridPosition(5, 0), new NavStub());
            Assert.That(step, Is.EqualTo(new GridPosition(1, 0)));
        }
    }
}

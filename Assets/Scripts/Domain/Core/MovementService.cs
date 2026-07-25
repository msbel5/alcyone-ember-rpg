using EmberCrpg.Domain.Actors;

// Design note:
// W32-03 §5: the ONE home of grid stepping (extracted verbatim from ScheduleSystem.StepToward).
// Chebyshev 8-direction, one cell per axis per tick, monotone convergence, never overshoots.
// Pure function, ZERO state; pathfinding plugs in behind this seam later.
namespace EmberCrpg.Domain.Core
{
    /// <summary>Deterministic one-tile grid step toward a target.</summary>
    public static class MovementService
    {
        // B10 §A4: nav-aware overload. `nav == null` is the legacy wall-blind primitive so
        // unmodified callers/tests keep working; nav != null refuses diagonals cutting a wall
        // corner and falls back to axial neighbours in FIXED order (X-axis first — the
        // determinism pin). Both axials blocked ⇒ freeze one tick (return `from`).
        public static GridPosition StepToward(GridPosition from, GridPosition to, IWorldNavigability nav = null)
        {
            int dx = System.Math.Sign(to.X - from.X);
            int dy = System.Math.Sign(to.Y - from.Y);
            var candidate = new GridPosition(from.X + dx, from.Y + dy);
            if (nav == null) return candidate;

            bool diagonal = dx != 0 && dy != 0;
            if (diagonal && nav.BlocksDiagonal(from, candidate))
                return AxialFallback(from, dx, dy, nav);

            if (nav.IsWalkable(candidate)) return candidate;

            return AxialFallback(from, dx, dy, nav);
        }

        // Fixed axial order: X-axis first, then Y-axis. Both blocked ⇒ freeze (return `from`);
        // the caller's arrival predicate stays unchanged and retries next tick.
        private static GridPosition AxialFallback(GridPosition from, int dx, int dy, IWorldNavigability nav)
        {
            if (dx != 0)
            {
                var xAxial = new GridPosition(from.X + dx, from.Y);
                if (nav.IsWalkable(xAxial)) return xAxial;
            }
            if (dy != 0)
            {
                var yAxial = new GridPosition(from.X, from.Y + dy);
                if (nav.IsWalkable(yAxial)) return yAxial;
            }
            return from;
        }
    }
}

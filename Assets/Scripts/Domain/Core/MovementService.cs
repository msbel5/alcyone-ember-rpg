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
        public static GridPosition StepToward(GridPosition from, GridPosition to)
        {
            return new GridPosition(
                from.X + System.Math.Sign(to.X - from.X),
                from.Y + System.Math.Sign(to.Y - from.Y));
        }
    }
}

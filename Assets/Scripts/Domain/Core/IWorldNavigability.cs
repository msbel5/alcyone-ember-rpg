using EmberCrpg.Domain.Actors;

// Design note:
// B10 §A1: the pure-Domain navigability seam. MovementService.StepToward and every locomotion
// call site consults ONE view: "can this cell be stepped into?" + "can this diagonal cut a wall
// corner?". No allocation, no mutation, O(1) per probe (HashSet.Contains for the base impl).
// Kept Domain-side so MovementService stays Unity/IO/RNG-free.
namespace EmberCrpg.Domain.Core
{
    /// <summary>Read-only view of grid-cell walkability for one WorldState.</summary>
    public interface IWorldNavigability
    {
        /// <summary>True if an actor may occupy this cell (nothing on Domain grid blocks it).</summary>
        bool IsWalkable(GridPosition cell);

        /// <summary>True iff the diagonal from -> to cuts a corner between two blocked orthogonals
        /// (standard "no squeezing through a wall crack" rule). Callers already know the axis deltas.</summary>
        bool BlocksDiagonal(GridPosition from, GridPosition to);
    }
}

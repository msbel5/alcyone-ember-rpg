using System;

// Design note:
// GridPosition is Sprint 1's deterministic room-space coordinate.
// Inputs: integer x/y coordinates and discrete movement deltas.
// Outputs: immutable grid positions plus distance helpers.
// Bible reference: ARCHITECTURE.md implementation order movement, PRD FR-01/FR-03.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Immutable integer grid coordinate inside the vertical-slice room.</summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public GridPosition Translate(int deltaX, int deltaY)
        {
            return new GridPosition(X + deltaX, Y + deltaY);
        }

        public int ManhattanDistanceTo(GridPosition other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X},{Y})";
        }
    }
}

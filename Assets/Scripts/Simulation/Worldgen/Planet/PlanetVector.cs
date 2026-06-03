using System;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Engine-free 3D vector for deterministic unit-sphere planet math.</summary>
    public struct PlanetVector
    {
        public PlanetVector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public double Length => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

        public PlanetVector Add(PlanetVector other)
        {
            return new PlanetVector(X + other.X, Y + other.Y, Z + other.Z);
        }

        public PlanetVector Subtract(PlanetVector other)
        {
            return new PlanetVector(X - other.X, Y - other.Y, Z - other.Z);
        }

        public PlanetVector Scale(double factor)
        {
            return new PlanetVector(X * factor, Y * factor, Z * factor);
        }

        public PlanetVector Normalize()
        {
            double length = Length;
            if (length <= 0d)
                throw new InvalidOperationException("Cannot normalize a zero-length planet vector.");
            return Scale(1d / length);
        }

        public static double Dot(PlanetVector left, PlanetVector right)
        {
            return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
        }

        public static PlanetVector Cross(PlanetVector left, PlanetVector right)
        {
            return new PlanetVector(
                (left.Y * right.Z) - (left.Z * right.Y),
                (left.Z * right.X) - (left.X * right.Z),
                (left.X * right.Y) - (left.Y * right.X));
        }

        public static PlanetVector UnitMidpoint(PlanetVector left, PlanetVector right)
        {
            return left.Add(right).Normalize();
        }
    }
}

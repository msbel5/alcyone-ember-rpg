using System;

namespace EmberCrpg.Simulation.Movement
{
    /// <summary>Small engine-agnostic vector used by combat movement tests and adapters.</summary>
    public readonly struct CombatVector3 : IEquatable<CombatVector3>
    {
        public static readonly CombatVector3 Zero = new CombatVector3(0f, 0f, 0f);

        public CombatVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public float SqrMagnitude => (X * X) + (Y * Y) + (Z * Z);
        public float Magnitude => MathF.Sqrt(SqrMagnitude);

        public CombatVector3 Normalized
        {
            get
            {
                var magnitude = Magnitude;
                return magnitude <= 0.00001f ? Zero : this / magnitude;
            }
        }

        public static CombatVector3 ClampMagnitude(CombatVector3 value, float maxMagnitude)
        {
            if (maxMagnitude <= 0f)
                return Zero;

            var sqrMagnitude = value.SqrMagnitude;
            var maxSqrMagnitude = maxMagnitude * maxMagnitude;
            return sqrMagnitude <= maxSqrMagnitude ? value : value.Normalized * maxMagnitude;
        }

        public static CombatVector3 operator +(CombatVector3 left, CombatVector3 right)
            => new CombatVector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

        public static CombatVector3 operator -(CombatVector3 left, CombatVector3 right)
            => new CombatVector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

        public static CombatVector3 operator *(CombatVector3 value, float scalar)
            => new CombatVector3(value.X * scalar, value.Y * scalar, value.Z * scalar);

        public static CombatVector3 operator /(CombatVector3 value, float scalar)
            => new CombatVector3(value.X / scalar, value.Y / scalar, value.Z / scalar);

        public bool Equals(CombatVector3 other)
            => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

        public override bool Equals(object obj)
            => obj is CombatVector3 other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(X, Y, Z);

        public override string ToString()
            => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }
}

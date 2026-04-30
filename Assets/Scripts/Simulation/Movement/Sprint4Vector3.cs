using System;

namespace EmberCrpg.Simulation.Movement
{
    /// <summary>Small engine-agnostic vector used by Sprint 4 movement tests and adapters.</summary>
    public readonly struct Sprint4Vector3 : IEquatable<Sprint4Vector3>
    {
        public static readonly Sprint4Vector3 Zero = new Sprint4Vector3(0f, 0f, 0f);

        public Sprint4Vector3(float x, float y, float z)
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

        public Sprint4Vector3 Normalized
        {
            get
            {
                var magnitude = Magnitude;
                return magnitude <= 0.00001f ? Zero : this / magnitude;
            }
        }

        public static Sprint4Vector3 ClampMagnitude(Sprint4Vector3 value, float maxMagnitude)
        {
            if (maxMagnitude <= 0f)
                return Zero;

            var sqrMagnitude = value.SqrMagnitude;
            var maxSqrMagnitude = maxMagnitude * maxMagnitude;
            return sqrMagnitude <= maxSqrMagnitude ? value : value.Normalized * maxMagnitude;
        }

        public static Sprint4Vector3 operator +(Sprint4Vector3 left, Sprint4Vector3 right)
            => new Sprint4Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

        public static Sprint4Vector3 operator -(Sprint4Vector3 left, Sprint4Vector3 right)
            => new Sprint4Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

        public static Sprint4Vector3 operator *(Sprint4Vector3 value, float scalar)
            => new Sprint4Vector3(value.X * scalar, value.Y * scalar, value.Z * scalar);

        public static Sprint4Vector3 operator /(Sprint4Vector3 value, float scalar)
            => new Sprint4Vector3(value.X / scalar, value.Y / scalar, value.Z / scalar);

        public bool Equals(Sprint4Vector3 other)
            => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

        public override bool Equals(object obj)
            => obj is Sprint4Vector3 other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(X, Y, Z);

        public override string ToString()
            => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }
}

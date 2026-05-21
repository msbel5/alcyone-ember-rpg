using System;

namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// Bounded -100..+100 reputation scalar between two factions. Lower means
    /// more hostile, higher means more allied. Pure value object.
    /// Faz 6 Atom 2.
    /// </summary>
    public readonly struct FactionReputation : IEquatable<FactionReputation>
    {
        public const int Min = -100;
        public const int Max = 100;

        private readonly int _value;

        public FactionReputation(int value)
        {
            _value = Clamp(value);
        }

        public static FactionReputation Neutral { get; } = new FactionReputation(0);

        public int Value => _value;

        public FactionReputation Apply(int delta)
        {
            // Codex audit (second pass A-P2): `_value + delta` could overflow
            // int before Clamp ran (e.g. _value=Int32.MaxValue-1 + delta=100
            // wrapped to a large negative, which Clamp pinned to Min instead
            // of saturating at Max). Promote to long first.
            long next = (long)_value + delta;
            if (next > int.MaxValue) next = int.MaxValue;
            else if (next < int.MinValue) next = int.MinValue;
            return new FactionReputation(Clamp((int)next));
        }

        public FactionReputation Decay(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (_value > 0)
                return new FactionReputation(Math.Max(0, _value - amount));
            if (_value < 0)
                return new FactionReputation(Math.Min(0, _value + amount));
            return this;
        }

        public FactionRelationKind ToRelationKind() => FactionRelationKind.FromReputation(_value);

        public bool Equals(FactionReputation other) => _value == other._value;
        public override bool Equals(object obj) => obj is FactionReputation other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => _value.ToString();
        public static bool operator ==(FactionReputation a, FactionReputation b) => a.Equals(b);
        public static bool operator !=(FactionReputation a, FactionReputation b) => !a.Equals(b);

        private static int Clamp(int v) => v < Min ? Min : (v > Max ? Max : v);
    }
}

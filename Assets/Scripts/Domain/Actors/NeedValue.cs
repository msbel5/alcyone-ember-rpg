using System;

// Design note:
// NeedValue is Phase 4's bounded 0-100 pressure scalar. Higher values are worse:
// 0 means comfortable, 100 means max pressure. It deliberately has no tick
// rates, recovery recipes, job refusal policy, or EventLog output.
// Atom-map ref: docs/sprint-phase-4-atom-map.md Pure needs component rail.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Immutable 0-100 actor-need pressure value.</summary>
    public readonly struct NeedValue : IEquatable<NeedValue>, IComparable<NeedValue>
    {
        public const int Min = 0;
        public const int Max = 100;

        public NeedValue(int value)
        {
            Value = Clamp(value);
        }

        public static NeedValue Comfortable
        {
            get { return default; }
        }

        public static NeedValue Critical
        {
            get { return new NeedValue(Max); }
        }

        public int Value { get; }

        public bool IsAtLeast(NeedValue threshold)
        {
            return Value >= threshold.Value;
        }

        public NeedValue Increase(int amount)
        {
            if (amount <= 0)
                return this;

            var headroom = Max - Value;
            if (amount >= headroom)
                return Critical;

            return new NeedValue(Value + amount);
        }

        public NeedValue Decrease(int amount)
        {
            return new NeedValue(Value - Math.Max(0, amount));
        }

        public int CompareTo(NeedValue other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(NeedValue other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is NeedValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return $"NeedValue({Value})";
        }

        public static bool operator ==(NeedValue left, NeedValue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NeedValue left, NeedValue right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(NeedValue left, NeedValue right)
        {
            return left.Value < right.Value;
        }

        public static bool operator >(NeedValue left, NeedValue right)
        {
            return left.Value > right.Value;
        }

        private static int Clamp(int value)
        {
            if (value < Min)
                return Min;
            if (value > Max)
                return Max;
            return value;
        }
    }
}

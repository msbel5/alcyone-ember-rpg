using System;

// Design note:
// ActorMood is Phase 4's bounded willingness scalar. Lower values mean the actor
// is less willing to work, but refusal policy belongs to a later PROCESS atom.
// The default struct value intentionally resolves to Neutral so existing actor
// construction does not become low-mood before mood derivation is wired.
// Atom-map ref: docs/sprint-phase-4-atom-map.md Mood derivation rail.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Immutable 0-100 actor mood value where lower means less willing.</summary>
    public readonly struct ActorMood : IEquatable<ActorMood>, IComparable<ActorMood>
    {
        public const int Min = 0;
        public const int Max = 100;
        public const int NeutralValue = 50;
        public const int LowMoodThreshold = 25;

        private readonly int _storedValue;

        public ActorMood(int value)
        {
            _storedValue = Clamp(value) + 1;
        }

        public static ActorMood Neutral
        {
            get { return default; }
        }

        public static ActorMood Lowest
        {
            get { return new ActorMood(Min); }
        }

        public static ActorMood Highest
        {
            get { return new ActorMood(Max); }
        }

        public int Value
        {
            get { return _storedValue == 0 ? NeutralValue : _storedValue - 1; }
        }

        public bool IsLow
        {
            get { return Value <= LowMoodThreshold; }
        }

        public bool IsAtMost(ActorMood threshold)
        {
            return Value <= threshold.Value;
        }

        public int CompareTo(ActorMood other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(ActorMood other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorMood other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return $"ActorMood({Value})";
        }

        public static bool operator ==(ActorMood left, ActorMood right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorMood left, ActorMood right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(ActorMood left, ActorMood right)
        {
            return left.Value < right.Value;
        }

        public static bool operator >(ActorMood left, ActorMood right)
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

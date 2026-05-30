using System;

// Design note:
// WorldComponentId is Phase 5's stable handle for non-actor world components
// such as soil and plants. It is intentionally generic; concrete behavior
// stays in typed component records and systems.
namespace EmberCrpg.Domain.World
{
    /// <summary>Stable handle for a world component row. Zero is the empty sentinel.</summary>
    public readonly struct WorldComponentId : IEquatable<WorldComponentId>
    {
        private readonly ulong _value;

        public WorldComponentId(ulong value)
        {
            _value = value;
        }

        public ulong Value { get { return _value; } }
        public bool IsEmpty { get { return _value == 0UL; } }

        public bool Equals(WorldComponentId other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldComponentId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return IsEmpty ? "WorldComponentId.Empty" : $"WorldComponentId({_value})";
        }

        public static bool operator ==(WorldComponentId left, WorldComponentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WorldComponentId left, WorldComponentId right)
        {
            return !left.Equals(right);
        }
    }
}

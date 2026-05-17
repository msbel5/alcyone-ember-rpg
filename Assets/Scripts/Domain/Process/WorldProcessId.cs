using System;

namespace EmberCrpg.Domain.Process
{
    /// <summary>Stable data/runtime id for a slow non-crafting world process.</summary>
    public readonly struct WorldProcessId : IEquatable<WorldProcessId>
    {
        private readonly string _value;

        public WorldProcessId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("World process id is required.", nameof(value));
            _value = value.Trim();
        }

        public string Value { get { return _value ?? string.Empty; } }
        public bool IsEmpty { get { return string.IsNullOrEmpty(Value); } }

        public bool Equals(WorldProcessId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldProcessId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return IsEmpty ? "WorldProcessId.Empty" : Value;
        }
    }
}

using System;

namespace EmberCrpg.Domain.Process
{
    /// <summary>Stable data id for one plant growth stage.</summary>
    public readonly struct PlantStageId : IEquatable<PlantStageId>
    {
        private readonly string _value;

        public PlantStageId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Plant stage id is required.", nameof(value));
            _value = value.Trim();
        }

        public string Value { get { return _value ?? string.Empty; } }
        public bool IsEmpty { get { return string.IsNullOrEmpty(Value); } }

        public bool Equals(PlantStageId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlantStageId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return IsEmpty ? "PlantStageId.Empty" : Value;
        }
    }
}

using System;

namespace EmberCrpg.Domain.Magic
{
    /// <summary>
    /// Immutable operation row: kind + magnitude + target rule + cost. Faz 8 Atom 2.
    /// </summary>
    public readonly struct EffectOperation : IEquatable<EffectOperation>
    {
        public EffectOperation(EffectOperationKind kind, int magnitude, string targetRule, int cost)
        {
            if (kind.IsEmpty) throw new ArgumentException("EffectOperation.Kind must be non-empty.", nameof(kind));
            if (magnitude < 0) throw new ArgumentOutOfRangeException(nameof(magnitude));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            Kind = kind;
            Magnitude = magnitude;
            TargetRule = targetRule ?? string.Empty;
            Cost = cost;
        }

        public EffectOperationKind Kind { get; }
        public int Magnitude { get; }
        public string TargetRule { get; }
        public int Cost { get; }

        public bool Equals(EffectOperation other) => Kind.Equals(other.Kind) && Magnitude == other.Magnitude && TargetRule == other.TargetRule && Cost == other.Cost;
        public override bool Equals(object obj) => obj is EffectOperation o && Equals(o);
        public override int GetHashCode() { unchecked { var h = Kind.GetHashCode(); h = (h * 31) ^ Magnitude; h = (h * 31) ^ (TargetRule?.GetHashCode() ?? 0); h = (h * 31) ^ Cost; return h; } }
    }
}

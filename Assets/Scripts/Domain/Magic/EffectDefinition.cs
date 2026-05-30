using System;
using System.Collections.Generic;

namespace EmberCrpg.Domain.Magic
{
    /// <summary>
    /// Immutable data row for an effect (spell). Replaces SpellEffectCode enum.
    /// Phase 8 Atom 3.
    /// </summary>
    public sealed class EffectDefinition : IEquatable<EffectDefinition>
    {
        public EffectDefinition(EffectId id, string schoolTag, IEnumerable<EffectOperation> operations, int cost, int cooldownTicks)
        {
            if (id.IsEmpty) throw new ArgumentException("EffectDefinition.Id must be non-empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(schoolTag)) throw new ArgumentException("SchoolTag must be non-blank.", nameof(schoolTag));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (cooldownTicks < 0) throw new ArgumentOutOfRangeException(nameof(cooldownTicks));

            Id = id;
            SchoolTag = schoolTag.Trim();
            Cost = cost;
            CooldownTicks = cooldownTicks;
            // PR#164 bot review fix: EffectOperation is a struct so callers can
            // slip default-initialized rows past the constructor type check.
            // Validate each row's kind/magnitude/cost before storing.
            if (operations == null)
            {
                Operations = new EffectOperation[0];
            }
            else
            {
                var list = new List<EffectOperation>(operations);
                for (int i = 0; i < list.Count; i++)
                {
                    var op = list[i];
                    if (op.Kind.IsEmpty)
                        throw new ArgumentException(
                            $"EffectDefinition operation at index {i} has no EffectOperationKind (default-initialized).",
                            nameof(operations));
                    if (op.Magnitude < 0)
                        throw new ArgumentException(
                            $"EffectDefinition operation at index {i} has negative magnitude {op.Magnitude}.",
                            nameof(operations));
                    if (op.Cost < 0)
                        throw new ArgumentException(
                            $"EffectDefinition operation at index {i} has negative cost {op.Cost}.",
                            nameof(operations));
                }
                Operations = list.AsReadOnly();
            }
        }

        public EffectId Id { get; }
        public string SchoolTag { get; }
        public int Cost { get; }
        public int CooldownTicks { get; }
        public IReadOnlyList<EffectOperation> Operations { get; }

        public bool Equals(EffectDefinition other) => other != null && Id.Equals(other.Id);
        public override bool Equals(object obj) => Equals(obj as EffectDefinition);
        public override int GetHashCode() => Id.GetHashCode();
    }

    public readonly struct EffectId : IEquatable<EffectId>
    {
        private readonly ulong _value;
        public EffectId(ulong value) { _value = value; }
        public ulong Value => _value;
        public bool IsEmpty => _value == 0UL;
        public bool Equals(EffectId other) => _value == other._value;
        public override bool Equals(object obj) => obj is EffectId o && Equals(o);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => $"EffectId({_value})";
        public static bool operator ==(EffectId a, EffectId b) => a.Equals(b);
        public static bool operator !=(EffectId a, EffectId b) => !a.Equals(b);
    }
}

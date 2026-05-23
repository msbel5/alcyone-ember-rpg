using System;

// Design note:
// SettlementId is Ember's stable settlement handle for the worldgen FOUNDATION
// pass. It mirrors the FactionId / RegionId pattern: zero is the empty
// sentinel, value-typed, pure Domain, no Unity. SettlementRecord pins this
// id at construction so the registry shape stays consistent with ActorStore
// / SiteStore.
namespace EmberCrpg.Domain.Worldgen
{
    /// <summary>
    /// Stable handle to a procedurally-generated settlement. Value type; default value means no settlement.
    /// </summary>
    public readonly struct SettlementId : IEquatable<SettlementId>
    {
        private readonly ulong _value;

        public SettlementId(ulong value)
        {
            _value = value;
        }

        public ulong Value
        {
            get { return _value; }
        }

        public bool IsEmpty
        {
            get { return _value == 0UL; }
        }

        public bool Equals(SettlementId other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is SettlementId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return IsEmpty ? "SettlementId.Empty" : $"SettlementId({_value})";
        }

        public static bool operator ==(SettlementId left, SettlementId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SettlementId left, SettlementId right)
        {
            return !left.Equals(right);
        }
    }
}

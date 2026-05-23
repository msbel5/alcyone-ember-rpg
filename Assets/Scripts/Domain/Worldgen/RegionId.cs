using System;

// Design note:
// RegionId is Ember's stable region handle for the worldgen FOUNDATION pass.
// It mirrors ActorId / SiteId / FactionId so RegionRecord and downstream
// stores can ride the same registry shape used by ActorStore / SiteStore.
// Zero is reserved as the empty sentinel. Worldgen ids live in Domain.Worldgen
// (not Domain.Core) because they have no consumer outside the deterministic
// world generator yet — promoting one would happen alongside its first
// non-worldgen consumer.
namespace EmberCrpg.Domain.Worldgen
{
    /// <summary>
    /// Stable handle to a procedurally-generated region. Value type; default value means no region.
    /// </summary>
    public readonly struct RegionId : IEquatable<RegionId>
    {
        private readonly ulong _value;

        public RegionId(ulong value)
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

        public bool Equals(RegionId other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is RegionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return IsEmpty ? "RegionId.Empty" : $"RegionId({_value})";
        }

        public static bool operator ==(RegionId left, RegionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RegionId left, RegionId right)
        {
            return !left.Equals(right);
        }
    }
}

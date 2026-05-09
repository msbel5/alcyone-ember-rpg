using System;

// Design note:
// SiteId is Ember's smallest stable site handle: a pure Domain value with no lookup,
// allocation, logging, serialization, or Unity dependency. Zero is reserved as the empty sentinel.
// Mirrors ActorId / ItemId so SiteStore can ride the same registry shape used by ActorStore.
namespace EmberCrpg.Domain.Core
{
    /// <summary>
    /// Stable handle to a site (region / settlement / dungeon) in the world. Value type; default value means no site.
    /// </summary>
    public readonly struct SiteId : IEquatable<SiteId>
    {
        private readonly ulong _value;

        /// <summary>
        /// Creates a site handle from its raw stable identifier.
        /// </summary>
        public SiteId(ulong value)
        {
            _value = value;
        }

        /// <summary>
        /// Raw stable identifier carried by this site handle.
        /// </summary>
        public ulong Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True when this handle is the empty no-site sentinel.
        /// </summary>
        public bool IsEmpty
        {
            get { return _value == 0UL; }
        }

        /// <summary>
        /// Returns true when both site handles carry the same raw identifier.
        /// </summary>
        public bool Equals(SiteId other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Returns true when the object is a site handle with the same raw identifier.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is SiteId other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code derived only from the raw stable identifier.
        /// </summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Returns a compact debug label for this site handle.
        /// </summary>
        public override string ToString()
        {
            return IsEmpty ? "SiteId.Empty" : $"SiteId({_value})";
        }

        /// <summary>
        /// Returns true when both site handles carry the same raw identifier.
        /// </summary>
        public static bool operator ==(SiteId left, SiteId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true when site handles carry different raw identifiers.
        /// </summary>
        public static bool operator !=(SiteId left, SiteId right)
        {
            return !left.Equals(right);
        }
    }
}

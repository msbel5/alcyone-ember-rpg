using System;

// Design note:
// FactionId is Ember's smallest stable faction handle: a pure Domain value with no lookup,
// allocation, logging, serialization, or Unity dependency. Zero is reserved as the empty sentinel.
// Mirrors ActorId / ItemId / SiteId so FactionStore can ride the same registry shape used by
// ActorStore / ItemStore / SiteStore.
namespace EmberCrpg.Domain.Core
{
    /// <summary>
    /// Stable handle to a faction in the world. Value type; default value means no faction.
    /// </summary>
    public readonly struct FactionId : IEquatable<FactionId>
    {
        private readonly ulong _value;

        /// <summary>
        /// Creates a faction handle from its raw stable identifier.
        /// </summary>
        public FactionId(ulong value)
        {
            _value = value;
        }

        /// <summary>
        /// Raw stable identifier carried by this faction handle.
        /// </summary>
        public ulong Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True when this handle is the empty no-faction sentinel.
        /// </summary>
        public bool IsEmpty
        {
            get { return _value == 0UL; }
        }

        /// <summary>
        /// Returns true when both faction handles carry the same raw identifier.
        /// </summary>
        public bool Equals(FactionId other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Returns true when the object is a faction handle with the same raw identifier.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is FactionId other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code derived only from the raw stable identifier.
        /// </summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Returns a compact debug label for this faction handle.
        /// </summary>
        public override string ToString()
        {
            return IsEmpty ? "FactionId.Empty" : $"FactionId({_value})";
        }

        /// <summary>
        /// Returns true when both faction handles carry the same raw identifier.
        /// </summary>
        public static bool operator ==(FactionId left, FactionId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true when faction handles carry different raw identifiers.
        /// </summary>
        public static bool operator !=(FactionId left, FactionId right)
        {
            return !left.Equals(right);
        }
    }
}

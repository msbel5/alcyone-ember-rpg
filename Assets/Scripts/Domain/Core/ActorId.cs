using System;

// Design note:
// ActorId is Ember's smallest stable actor handle: a pure Domain value with no lookup,
// allocation, logging, serialization, or Unity dependency. Zero is reserved as the empty sentinel.
namespace EmberCrpg.Domain.Core
{
    /// <summary>
    /// Stable handle to an actor in the world. Value type; default value means no actor.
    /// </summary>
    public readonly struct ActorId : IEquatable<ActorId>
    {
        private readonly ulong _value;

        /// <summary>
        /// Creates an actor handle from its raw stable identifier.
        /// </summary>
        public ActorId(ulong value)
        {
            _value = value;
        }

        /// <summary>
        /// Raw stable identifier carried by this actor handle.
        /// </summary>
        public ulong Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True when this handle is the empty no-actor sentinel.
        /// </summary>
        public bool IsEmpty
        {
            get { return _value == 0UL; }
        }

        /// <summary>
        /// Returns true when both actor handles carry the same raw identifier.
        /// </summary>
        public bool Equals(ActorId other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Returns true when the object is an actor handle with the same raw identifier.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is ActorId other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code derived only from the raw stable identifier.
        /// </summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Returns a compact debug label for this actor handle.
        /// </summary>
        public override string ToString()
        {
            return IsEmpty ? "ActorId.Empty" : $"ActorId({_value})";
        }

        /// <summary>
        /// Returns true when both actor handles carry the same raw identifier.
        /// </summary>
        public static bool operator ==(ActorId left, ActorId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true when actor handles carry different raw identifiers.
        /// </summary>
        public static bool operator !=(ActorId left, ActorId right)
        {
            return !left.Equals(right);
        }
    }
}

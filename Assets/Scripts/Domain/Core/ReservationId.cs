using System;

// Design note:
// ReservationId is the stable handle for one ReservationLedger row (W32 EAT slice),
// mirroring ItemId: pure Domain value, zero reserved as the empty sentinel.
namespace EmberCrpg.Domain.Core
{
    /// <summary>
    /// Stable handle to a reservation ledger row. Value type; default value means no reservation.
    /// </summary>
    public readonly struct ReservationId : IEquatable<ReservationId>
    {
        private readonly ulong _value;

        /// <summary>
        /// Creates a reservation handle from its raw stable identifier.
        /// </summary>
        public ReservationId(ulong value)
        {
            _value = value;
        }

        /// <summary>The empty no-reservation sentinel.</summary>
        public static ReservationId Empty
        {
            get { return default; }
        }

        /// <summary>
        /// Raw stable identifier carried by this reservation handle.
        /// </summary>
        public ulong Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True when this handle is the empty no-reservation sentinel.
        /// </summary>
        public bool IsEmpty
        {
            get { return _value == 0UL; }
        }

        /// <summary>
        /// Returns true when both reservation handles carry the same raw identifier.
        /// </summary>
        public bool Equals(ReservationId other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Returns true when the object is a reservation handle with the same raw identifier.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is ReservationId other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code derived only from the raw stable identifier.
        /// </summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Returns a compact debug label for this reservation handle.
        /// </summary>
        public override string ToString()
        {
            return IsEmpty ? "ReservationId.Empty" : $"ReservationId({_value})";
        }

        /// <summary>
        /// Returns true when both reservation handles carry the same raw identifier.
        /// </summary>
        public static bool operator ==(ReservationId left, ReservationId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true when reservation handles carry different raw identifiers.
        /// </summary>
        public static bool operator !=(ReservationId left, ReservationId right)
        {
            return !left.Equals(right);
        }
    }
}

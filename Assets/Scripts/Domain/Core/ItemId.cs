using System;

// Design note:
// ItemId is Ember's smallest stable item handle, mirroring DFU's ulong item UID idea
// without carrying inventory, stack, durability, logging, serialization, or Unity concerns.
namespace EmberCrpg.Domain.Core
{
    /// <summary>
    /// Stable handle to an item in the world. Value type; default value means no item.
    /// </summary>
    public readonly struct ItemId : IEquatable<ItemId>
    {
        private readonly ulong _value;

        /// <summary>
        /// Creates an item handle from its raw stable identifier.
        /// </summary>
        public ItemId(ulong value)
        {
            _value = value;
        }

        /// <summary>
        /// Raw stable identifier carried by this item handle.
        /// </summary>
        public ulong Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True when this handle is the empty no-item sentinel.
        /// </summary>
        public bool IsEmpty
        {
            get { return _value == 0UL; }
        }

        /// <summary>
        /// Returns true when both item handles carry the same raw identifier.
        /// </summary>
        public bool Equals(ItemId other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Returns true when the object is an item handle with the same raw identifier.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is ItemId other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code derived only from the raw stable identifier.
        /// </summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Returns a compact debug label for this item handle.
        /// </summary>
        public override string ToString()
        {
            return IsEmpty ? "ItemId.Empty" : $"ItemId({_value})";
        }

        /// <summary>
        /// Returns true when both item handles carry the same raw identifier.
        /// </summary>
        public static bool operator ==(ItemId left, ItemId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true when item handles carry different raw identifiers.
        /// </summary>
        public static bool operator !=(ItemId left, ItemId right)
        {
            return !left.Equals(right);
        }
    }
}

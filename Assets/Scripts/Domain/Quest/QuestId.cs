using System;

// Design note:
// QuestId is the deterministic quest handle for the engine-free Domain quest model.
// Pattern: Value object.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Stable handle to a quest definition or quest instance. Default value means no quest.</summary>
    public readonly struct QuestId : IEquatable<QuestId>
    {
        private readonly ulong _value;

        /// <summary>Creates a quest handle from its raw stable identifier.</summary>
        public QuestId(ulong value)
        {
            _value = value;
        }

        /// <summary>Raw stable identifier carried by this quest handle.</summary>
        public ulong Value
        {
            get { return _value; }
        }

        /// <summary>True when this handle is the empty no-quest sentinel.</summary>
        public bool IsEmpty
        {
            get { return _value == 0UL; }
        }

        /// <summary>Returns true when both quest handles carry the same raw identifier.</summary>
        public bool Equals(QuestId other)
        {
            return _value == other._value;
        }

        /// <summary>Returns true when the object is a quest handle with the same raw identifier.</summary>
        public override bool Equals(object obj)
        {
            return obj is QuestId other && Equals(other);
        }

        /// <summary>Returns a hash code derived only from the raw stable identifier.</summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>Returns a compact debug label for this quest handle.</summary>
        public override string ToString()
        {
            return IsEmpty ? "QuestId.Empty" : $"QuestId({_value})";
        }

        /// <summary>Returns true when both quest handles carry the same raw identifier.</summary>
        public static bool operator ==(QuestId left, QuestId right)
        {
            return left.Equals(right);
        }

        /// <summary>Returns true when quest handles carry different raw identifiers.</summary>
        public static bool operator !=(QuestId left, QuestId right)
        {
            return !left.Equals(right);
        }
    }
}

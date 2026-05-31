using System;

namespace EmberCrpg.Data.Save
{
    public readonly struct SaveSlotId : IEquatable<SaveSlotId>
    {
        public SaveSlotId(SaveSlotKind kind, int index = 0)
        {
            if (kind == SaveSlotKind.Manual)
            {
                if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), "Manual slot index must be >= 0.");
                Index = index;
            }
            else
            {
                Index = 0;
            }

            Kind = kind;
        }

        public SaveSlotKind Kind { get; }

        public int Index { get; }

        public static SaveSlotId Manual(int index) => new SaveSlotId(SaveSlotKind.Manual, index);

        public static SaveSlotId Auto => new SaveSlotId(SaveSlotKind.Auto, 0);

        public static SaveSlotId Quick => new SaveSlotId(SaveSlotKind.Quick, 0);

        public string FileStem()
        {
            switch (Kind)
            {
                case SaveSlotKind.Manual: return "manual_" + Index;
                case SaveSlotKind.Auto: return "auto";
                case SaveSlotKind.Quick: return "quick";
                default: throw new ArgumentOutOfRangeException(nameof(Kind), "Unsupported save slot kind.");
            }
        }

        public bool Equals(SaveSlotId other) => Kind == other.Kind && Index == other.Index;

        public override bool Equals(object obj) => obj is SaveSlotId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ Index;
            }
        }

        public static bool operator ==(SaveSlotId left, SaveSlotId right) => left.Equals(right);

        public static bool operator !=(SaveSlotId left, SaveSlotId right) => !left.Equals(right);

        public override string ToString() => Kind == SaveSlotKind.Manual ? "Manual(" + Index + ")" : Kind.ToString();
    }
}

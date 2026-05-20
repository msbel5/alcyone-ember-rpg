// Design note:
// EquipmentSlot names the tiny Sprint 4 equipment surface without leaking UI concerns into rules.
// Inputs: equipment-capable item definitions and equip/unequip requests.
// Outputs: deterministic slot ids for domain state, save/load, and presentation labels.
// Bible reference: ARCHITECTURE.md inventory/equipment kernel direction, Sprint 4 Faz 4 roadmap.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Stable equipment slot code. Legacy int casts are retained for old saves.</summary>
    public readonly struct EquipmentSlot : System.IEquatable<EquipmentSlot>
    {
        private readonly string _code;
        private readonly int _legacyValue;

        private EquipmentSlot(string code, int legacyValue)
        {
            _code = code;
            _legacyValue = legacyValue;
        }

        public static EquipmentSlot None { get; } = new EquipmentSlot("none", 0);
        public static EquipmentSlot Weapon { get; } = new EquipmentSlot("main_hand", 1);

        public string Code => _code ?? None.Code;
        public bool IsEmpty => string.IsNullOrEmpty(_code) || Code == None.Code;

        public static EquipmentSlot FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return None;
            var normalized = code.Trim().ToLowerInvariant();
            if (normalized == "none") return None;
            if (normalized == "main_hand" || normalized == "weapon") return Weapon;
            return new EquipmentSlot(normalized, 0);
        }

        public static EquipmentSlot FromLegacyValue(int value)
        {
            return value == 1 ? Weapon : None;
        }

        public bool Equals(EquipmentSlot other) => Code == other.Code;
        public override bool Equals(object obj) => obj is EquipmentSlot other && Equals(other);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(EquipmentSlot left, EquipmentSlot right) => left.Equals(right);
        public static bool operator !=(EquipmentSlot left, EquipmentSlot right) => !left.Equals(right);
        public static explicit operator int(EquipmentSlot slot) => slot._legacyValue;
        public static explicit operator EquipmentSlot(int legacyValue) => FromLegacyValue(legacyValue);
    }
}

// Design note:
// EquipmentState separates equipped items from backpack inventory without leaking UI concerns.
// Inputs: exact non-stackable item instances routed through simulation services.
// Outputs: deterministic weapon/armor slot state for saves, queries, and HUD formatting.
// Bible reference: ARCHITECTURE.md equipment tier, Sprint 3 expanded item state.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Two-slot equipment loadout for the slice player.</summary>
    public sealed class EquipmentState
    {
        private InventoryItem _weapon;
        private InventoryItem _armor;

        public InventoryItem Weapon => _weapon;
        public InventoryItem Armor => _armor;

        public InventoryItem Get(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.Weapon
                ? _weapon
                : slot == EquipmentSlot.Armor
                    ? _armor
                    : null;
        }

        public void Set(InventoryItem item)
        {
            if (item == null)
                return;

            Set(item.EquipSlot, item);
        }

        public void Set(EquipmentSlot slot, InventoryItem item)
        {
            if (slot == EquipmentSlot.None)
                return;
            if (item != null && item.EquipSlot != slot)
                throw new System.ArgumentException("Item slot does not match equipment slot.", nameof(item));

            var clone = item == null ? null : item.Clone();
            if (slot == EquipmentSlot.Weapon)
                _weapon = clone;
            else if (slot == EquipmentSlot.Armor)
                _armor = clone;
        }

        public InventoryItem Clear(EquipmentSlot slot)
        {
            var item = Get(slot);
            if (slot == EquipmentSlot.Weapon)
                _weapon = null;
            else if (slot == EquipmentSlot.Armor)
                _armor = null;
            return item == null ? null : item.Clone();
        }

        public EquipmentState Clone()
        {
            var clone = new EquipmentState();
            if (_weapon != null)
                clone._weapon = _weapon.Clone();
            if (_armor != null)
                clone._armor = _armor.Clone();
            return clone;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;

// Design note:
// EquipmentState stores item identity references for equipped gear while inventory owns the items.
// Inputs: validated equip/unequip decisions from pure simulation services.
// Outputs: stable slot -> ItemId state for combat modifiers, save/load, and HUD text.
// Bible reference: ARCHITECTURE.md inventory/equipment kernel direction, Sprint 4 Phase 4 roadmap.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Mutable equipment slots keyed by stable item identity.</summary>
    public sealed class EquipmentState
    {
        private readonly Dictionary<EquipmentSlot, ItemId> _equipped = new Dictionary<EquipmentSlot, ItemId>();

        public ItemId GetEquippedItemId(EquipmentSlot slot)
        {
            return _equipped.TryGetValue(slot, out var itemId) ? itemId : new ItemId(0);
        }

        public bool IsEquipped(ItemId itemId)
        {
            return !itemId.IsEmpty && _equipped.ContainsValue(itemId);
        }

        public void Equip(EquipmentSlot slot, ItemId itemId)
        {
            if (slot == EquipmentSlot.None || itemId.IsEmpty)
                return;
            _equipped[slot] = itemId;
        }

        public void Unequip(EquipmentSlot slot)
        {
            _equipped.Remove(slot);
        }

        public EquipmentState Clone()
        {
            var clone = new EquipmentState();
            foreach (var pair in _equipped)
                clone.Equip(pair.Key, pair.Value);
            return clone;
        }

        /// <summary>
        /// Codex audit (A/P2): enumerate every equipped (slot, itemId) pair in
        /// stable slot-code order. SliceSaveMapper used to hardcode "weapon
        /// only", silently dropping future slots; the dictionary's natural
        /// enumeration order is also non-deterministic. Sorting by Code gives
        /// the save layer a stable canonical list.
        /// </summary>
        public IEnumerable<KeyValuePair<EquipmentSlot, ItemId>> EnumerateEquipped()
        {
            return _equipped
                .Where(pair => !pair.Value.IsEmpty)
                .OrderBy(pair => pair.Key.Code, System.StringComparer.Ordinal);
        }
    }
}

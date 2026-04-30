using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;

// Design note:
// EquipmentService owns pure equip/unequip validation and equipment-derived combat stats.
// Inputs: inventory items, stable item ids, target slots, and current equipment state.
// Outputs: deterministic action results plus additive mechanics modifiers for combat.
// Bible reference: ARCHITECTURE.md inventory/equipment kernel direction, Sprint 4 Faz 4 roadmap.
namespace EmberCrpg.Simulation.Inventory
{
    /// <summary>Pure deterministic equipment rules for the Sprint 4 inventory UI slice.</summary>
    public sealed class EquipmentService
    {
        public EquipmentActionResult TryEquip(InventoryState inventory, EquipmentState equipment, ItemId itemId)
        {
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));
            if (equipment == null)
                throw new ArgumentNullException(nameof(equipment));

            var item = inventory.FindById(itemId);
            if (item == null)
                return EquipmentActionResult.Fail(EquipmentActionError.ItemNotFound, "That item is not in your inventory.");
            if (!item.IsEquipment)
                return EquipmentActionResult.Fail(EquipmentActionError.ItemNotEquippable, $"{item.DisplayName} cannot be equipped.");
            if (equipment.IsEquipped(item.Id))
                return EquipmentActionResult.Fail(EquipmentActionError.AlreadyEquipped, $"{item.DisplayName} is already equipped.");

            var current = equipment.GetEquippedItemId(item.EquipmentSlot);
            if (!current.IsEmpty)
                return EquipmentActionResult.Fail(EquipmentActionError.SlotOccupied, $"Unequip your current {GetSlotLabel(item.EquipmentSlot)} first.");

            equipment.Equip(item.EquipmentSlot, item.Id);
            return EquipmentActionResult.Ok($"Equipped {item.DisplayName} as {GetSlotLabel(item.EquipmentSlot)}.");
        }

        public EquipmentActionResult TryUnequip(EquipmentState equipment, EquipmentSlot slot)
        {
            if (equipment == null)
                throw new ArgumentNullException(nameof(equipment));
            if (slot == EquipmentSlot.None)
                return EquipmentActionResult.Fail(EquipmentActionError.InvalidSlot, "Choose a real equipment slot.");

            var current = equipment.GetEquippedItemId(slot);
            if (current.IsEmpty)
                return EquipmentActionResult.Fail(EquipmentActionError.SlotEmpty, $"No {GetSlotLabel(slot)} is equipped.");

            equipment.Unequip(slot);
            return EquipmentActionResult.Ok($"Unequipped {GetSlotLabel(slot)}.");
        }

        public EquipmentCombatStats GetCombatStats(InventoryState inventory, EquipmentState equipment)
        {
            if (inventory == null || equipment == null)
                return new EquipmentCombatStats(0, 0);

            var accuracy = 0;
            var damage = 0;
            var weapon = inventory.FindById(equipment.GetEquippedItemId(EquipmentSlot.Weapon));
            if (weapon != null)
            {
                accuracy += weapon.AccuracyBonus;
                damage += weapon.DamageBonus;
            }

            return new EquipmentCombatStats(accuracy, damage);
        }

        public static string GetSlotLabel(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.Weapon ? "weapon" : "slot";
        }
    }
}

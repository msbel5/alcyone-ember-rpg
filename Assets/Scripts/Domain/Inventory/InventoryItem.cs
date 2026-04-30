using System;
using EmberCrpg.Domain.Core;

// Design note:
// InventoryItem is the smallest serializable item instance needed by inventory, pickups, and gear.
// Inputs: stable item id, template id, display name, quantity, optional equipment slot, and bonuses.
// Outputs: immutable pure-Domain item records with item identity clear enough for equipment references.
// Bible reference: ARCHITECTURE.md Part 2, PRD FR-05, Sprint 4 Faz 4 equipment roadmap.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Minimal item instance used by the slice inventory kernel.</summary>
    public sealed class InventoryItem
    {
        public InventoryItem(ItemId id, string templateId, string displayName, int quantity)
            : this(id, templateId, displayName, quantity, EquipmentSlot.None, 0, 0)
        {
        }

        public InventoryItem(
            ItemId id,
            string templateId,
            string displayName,
            int quantity,
            EquipmentSlot equipmentSlot,
            int accuracyBonus,
            int damageBonus)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ArgumentException("Template id is required.", nameof(templateId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required.", nameof(displayName));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Inventory quantities must stay positive.");
            if (equipmentSlot != EquipmentSlot.None && quantity != 1)
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Equipment items must be single item instances.");
            if (equipmentSlot == EquipmentSlot.None && (accuracyBonus != 0 || damageBonus != 0))
                throw new ArgumentException("Only equipment items can carry combat bonuses.", nameof(equipmentSlot));

            Id = id;
            TemplateId = templateId;
            DisplayName = displayName;
            Quantity = quantity;
            EquipmentSlot = equipmentSlot;
            AccuracyBonus = accuracyBonus;
            DamageBonus = damageBonus;
        }

        public ItemId Id { get; }
        public string TemplateId { get; }
        public string DisplayName { get; }
        public int Quantity { get; private set; }
        public EquipmentSlot EquipmentSlot { get; }
        public int AccuracyBonus { get; }
        public int DamageBonus { get; }
        public bool IsEquipment => EquipmentSlot != EquipmentSlot.None;

        public void AddQuantity(int amount)
        {
            Quantity += Math.Max(0, amount);
        }

        public void RemoveQuantity(int amount)
        {
            Quantity = Math.Max(0, Quantity - Math.Max(0, amount));
        }

        public InventoryItem Clone()
        {
            return new InventoryItem(Id, TemplateId, DisplayName, Quantity, EquipmentSlot, AccuracyBonus, DamageBonus);
        }
    }
}

using System;
using EmberCrpg.Domain.Core;

// Design note:
// InventoryItem is the smallest serializable item instance needed by Sprint 1 inventory and Sprint 3 equipment.
// Inputs: stable item id, template id, display name, quantity, stack rule, and optional equip slot.
// Outputs: immutable pure-Domain item records.
// Bible reference: ARCHITECTURE.md Part 2, PRD FR-05, Sprint 3 expanded item state.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Minimal item instance used by the slice inventory kernel.</summary>
    public sealed class InventoryItem
    {
        public InventoryItem(ItemId id, string templateId, string displayName, int quantity, bool isStackable = true, EquipmentSlot equipSlot = EquipmentSlot.None)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ArgumentException("Template id is required.", nameof(templateId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required.", nameof(displayName));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Inventory quantities must stay positive.");
            if (!isStackable && quantity != 1)
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Non-stackable items must stay at quantity 1.");

            Id = id;
            TemplateId = templateId;
            DisplayName = displayName;
            Quantity = quantity;
            IsStackable = isStackable;
            EquipSlot = equipSlot;
        }

        public ItemId Id { get; }
        public string TemplateId { get; }
        public string DisplayName { get; }
        public int Quantity { get; private set; }
        public bool IsStackable { get; }
        public EquipmentSlot EquipSlot { get; }
        public bool CanEquip => EquipSlot != EquipmentSlot.None;

        public void AddQuantity(int amount)
        {
            if (!IsStackable)
                return;

            Quantity += Math.Max(0, amount);
        }

        public void RemoveQuantity(int amount)
        {
            Quantity = Math.Max(0, Quantity - Math.Max(0, amount));
        }

        public InventoryItem Clone()
        {
            return new InventoryItem(Id, TemplateId, DisplayName, Quantity, IsStackable, EquipSlot);
        }
    }
}

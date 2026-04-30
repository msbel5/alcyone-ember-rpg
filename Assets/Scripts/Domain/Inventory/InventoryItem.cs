using System;
using EmberCrpg.Domain.Core;

// Design note:
// InventoryItem is the smallest serializable item instance needed by Sprint 1 inventory and pickups.
// Inputs: stable item id, template id, display name, and stack quantity.
// Outputs: immutable pure-Domain item records.
// Bible reference: ARCHITECTURE.md Part 2, PRD FR-05.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Minimal item instance used by the slice inventory kernel.</summary>
    public sealed class InventoryItem
    {
        public InventoryItem(ItemId id, string templateId, string displayName, int quantity)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ArgumentException("Template id is required.", nameof(templateId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required.", nameof(displayName));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Inventory quantities must stay positive.");

            Id = id;
            TemplateId = templateId;
            DisplayName = displayName;
            Quantity = quantity;
        }

        public ItemId Id { get; }
        public string TemplateId { get; }
        public string DisplayName { get; }
        public int Quantity { get; private set; }

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
            return new InventoryItem(Id, TemplateId, DisplayName, Quantity);
        }
    }
}

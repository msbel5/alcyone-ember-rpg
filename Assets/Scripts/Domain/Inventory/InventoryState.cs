using System.Collections.Generic;
using System.Linq;

// Design note:
// InventoryState owns the deterministic backpack and keeps equipment item identity unmerged.
// Inputs: item add/remove/equipment lookup requests from pure simulation services.
// Outputs: bounded inventory state suitable for combat, pickup, equipment, and save/load tests.
// Bible reference: ARCHITECTURE.md inventory kernel, PRD FR-05, Sprint 4 Faz 4.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Mutable inventory state with stack merge support and fixed capacity.</summary>
    public sealed class InventoryState
    {
        private readonly List<InventoryItem> _items = new List<InventoryItem>();

        public InventoryState(int capacity)
        {
            Capacity = capacity;
        }

        public int Capacity { get; }
        public IReadOnlyList<InventoryItem> Items => _items;
        public bool IsFull => _items.Count >= Capacity;

        public bool TryAdd(InventoryItem item)
        {
            var existing = item.IsEquipment ? null : _items.FirstOrDefault(candidate => !candidate.IsEquipment && candidate.TemplateId == item.TemplateId);
            if (existing != null)
            {
                existing.AddQuantity(item.Quantity);
                return true;
            }

            if (IsFull)
                return false;

            _items.Add(item.Clone());
            return true;
        }

        public bool TryRemove(string templateId, int quantity, EquipmentState equipment = null)
        {
            var existing = _items.FirstOrDefault(candidate => candidate.TemplateId == templateId);
            if (existing == null || existing.Quantity < quantity)
                return false;
            if (equipment != null && equipment.IsEquipped(existing.Id) && quantity >= existing.Quantity)
                return false;

            existing.RemoveQuantity(quantity);
            if (existing.Quantity == 0)
                _items.Remove(existing);

            return true;
        }

        public bool TryRemoveStackable(string templateId, int quantity)
        {
            var existing = _items.FirstOrDefault(candidate => !candidate.IsEquipment && candidate.TemplateId == templateId);
            if (existing == null || existing.Quantity < quantity)
                return false;

            existing.RemoveQuantity(quantity);
            if (existing.Quantity == 0)
                _items.Remove(existing);

            return true;
        }

        public bool Contains(string templateId)
        {
            return _items.Any(candidate => candidate.TemplateId == templateId);
        }

        public InventoryItem FindById(EmberCrpg.Domain.Core.ItemId itemId)
        {
            return _items.FirstOrDefault(candidate => candidate.Id == itemId);
        }

        public InventoryItem FindFirstEquipment(EquipmentSlot slot)
        {
            return _items.FirstOrDefault(candidate => candidate.EquipmentSlot == slot);
        }

        public InventoryState Clone()
        {
            var clone = new InventoryState(Capacity);
            foreach (var item in _items)
                clone.TryAdd(item.Clone());
            return clone;
        }
    }
}

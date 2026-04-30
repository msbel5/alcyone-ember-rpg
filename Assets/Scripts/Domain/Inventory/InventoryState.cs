using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;

// Design note:
// InventoryState owns the Sprint 1 ten-slot deterministic backpack.
// Inputs: item add/remove requests from pure simulation services.
// Outputs: bounded inventory state suitable for combat, pickup, trade, and save/load tests.
// Bible reference: ARCHITECTURE.md inventory kernel, PRD FR-05.
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
            var existing = _items.FirstOrDefault(candidate => candidate.TemplateId == item.TemplateId);
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

        public bool TryRemove(string templateId, int quantity)
        {
            var existing = _items.FirstOrDefault(candidate => candidate.TemplateId == templateId);
            if (existing == null || existing.Quantity < quantity)
                return false;

            existing.RemoveQuantity(quantity);
            if (existing.Quantity == 0)
                _items.Remove(existing);

            return true;
        }

        public bool TryTake(string templateId, int quantity, ItemInstanceSequence itemIds, out InventoryItem item)
        {
            item = null;
            var existing = _items.FirstOrDefault(candidate => candidate.TemplateId == templateId);
            if (quantity <= 0 || existing == null || existing.Quantity < quantity)
                return false;

            if (existing.Quantity == quantity)
            {
                _items.Remove(existing);
                item = existing;
                return true;
            }

            if (itemIds == null)
                return false;

            existing.RemoveQuantity(quantity);
            item = new InventoryItem(itemIds.TakeNext(), existing.TemplateId, existing.DisplayName, quantity);
            return true;
        }

        public bool Contains(string templateId)
        {
            return _items.Any(candidate => candidate.TemplateId == templateId);
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

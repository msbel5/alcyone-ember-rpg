using System.Collections.Generic;
using System.Linq;

// Design note:
// InventoryState owns the deterministic backpack and keeps equipment item identity unmerged.
// Inputs: item add/remove/equipment lookup requests from pure simulation services.
// Outputs: bounded inventory state suitable for combat, pickup, equipment, and save/load tests.
// Bible reference: ARCHITECTURE.md inventory kernel, PRD FR-05, Sprint 4 Phase 4.
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
            // Codex audit (second pass A-P2): non-positive quantities used to
            // no-op but return true, lying to callers about a "successful"
            // removal of 0 / negative units. Reject explicitly.
            if (quantity <= 0) return false;
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
            // Codex audit (second pass A-P2): mirror the TryRemove guard.
            if (quantity <= 0) return false;
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

        // Deterministic next-id seed: max(existing ids) + 1, floor 1 so `new ItemId(0)` (Empty) never leaks.
        // The mint owner walks BOTH inventories (player+merchant) when needed — see the two-arg overload.
        public EmberCrpg.Domain.Core.ItemId NextItemId()
        {
            ulong max = 0UL;
            for (int i = 0; i < _items.Count; i++)
                if (_items[i].Id.Value > max) max = _items[i].Id.Value;
            return new EmberCrpg.Domain.Core.ItemId(max + 1UL);
        }

        /// <summary>Cross-inventory seed for mints that span two bags (e.g. player+merchant trade).</summary>
        public static EmberCrpg.Domain.Core.ItemId NextItemIdAcross(InventoryState first, InventoryState second)
        {
            ulong max = 0UL;
            max = MaxIdIn(first, max);
            max = MaxIdIn(second, max);
            return new EmberCrpg.Domain.Core.ItemId(max + 1UL);
        }

        private static ulong MaxIdIn(InventoryState inv, ulong seed)
        {
            if (inv == null) return seed;
            for (int i = 0; i < inv._items.Count; i++)
                if (inv._items[i].Id.Value > seed) seed = inv._items[i].Id.Value;
            return seed;
        }
    }
}

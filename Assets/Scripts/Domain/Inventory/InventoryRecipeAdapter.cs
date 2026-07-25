using System;
using EmberCrpg.Domain.Process;

// Design note:
// W33-02 §8 (B06): InventoryState seen through the tag-count seam. Behaviour mirrors the
// legacy recipe path VERBATIM — CountOf sums non-equipment units (RecipeSystem.HasInputs),
// TryConsume is TryRemoveStackable, TryAccept mints one unit item per unit via the factory
// that migrated INTO the adapter (unique ItemId generation is now the concern of the side
// that needs identity, never the recipe caller). CONSTRAINT: pure Domain, deterministic.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Tag-count recipe IO over an InventoryState (the player-crafting lane).</summary>
    public sealed class InventoryRecipeAdapter : IRecipeInventory
    {
        private readonly InventoryState _inventory;
        private readonly Func<string, InventoryItem> _mint;

        /// <param name="mint">tag -> one fresh 1-quantity item; required before TryAccept.</param>
        public InventoryRecipeAdapter(InventoryState inventory, Func<string, InventoryItem> mint = null)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _mint = mint;
        }

        public int CountOf(string itemTag)
        {
            var available = 0;
            foreach (var item in _inventory.Items)
                if (!item.IsEquipment && string.Equals(item.TemplateId, itemTag, StringComparison.Ordinal))
                    available += item.Quantity;
            return available;
        }

        public bool TryConsume(string itemTag, int quantity)
            => quantity >= 0 && _inventory.TryRemoveStackable(itemTag, quantity);

        /// <summary>Capacity refusal is REAL here (IsFull) — the preflight clone catches it.</summary>
        public bool TryAccept(string itemTag, int quantity)
        {
            if (quantity < 0 || _mint == null)
                return false;
            for (var i = 0; i < quantity; i++)
            {
                var item = _mint(itemTag);
                if (item == null || item.Quantity != 1
                    || !string.Equals(item.TemplateId, itemTag, StringComparison.Ordinal)
                    || !_inventory.TryAdd(item))
                    return false;
            }
            return true;
        }

        public IRecipeInventory CloneForPreflight()
            => new InventoryRecipeAdapter(_inventory.Clone(), _mint);
    }
}

// Design note:
// W33-02 §8 (B06): RecipeSystem only ever does TAG-COUNT arithmetic — item IDENTITY (ItemId,
// mint factories) is InventoryState's private need. This seam is that observation made a type,
// so village production can run on the worksite's real container (StockpileComponent) instead
// of cooking in the player's bag. CONSTRAINT: pure Domain — no Unity, no IO, no RNG.
namespace EmberCrpg.Domain.Process
{
    /// <summary>Tag-count IO surface a recipe execution consumes and fills.</summary>
    public interface IRecipeInventory
    {
        /// <summary>Non-equipment unit count for a tag.</summary>
        int CountOf(string itemTag);

        /// <summary>All-or-nothing consumption; partial consumption is FORBIDDEN.</summary>
        bool TryConsume(string itemTag, int quantity);

        /// <summary>Accepts output units; an implementation may refuse for capacity.</summary>
        bool TryAccept(string itemTag, int quantity);

        /// <summary>Independent copy for CanStartRequestedQuantity clone-proofing — the probe
        /// must never touch the live container.</summary>
        IRecipeInventory CloneForPreflight();
    }
}

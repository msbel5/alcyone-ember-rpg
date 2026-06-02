using System;

// Design note:
// GrantItemAction is the Command that deterministically mutates player inventory by tag.
// Pattern: Command.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Command that grants a positive quantity of a deterministic inventory item tag.</summary>
    public sealed class GrantItemAction : IQuestAction
    {
        public GrantItemAction(string itemTag, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Granted quest item tag is required.", nameof(itemTag));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Granted quest item quantity must be positive.");

            ItemTag = itemTag.Trim();
            Quantity = quantity;
        }

        /// <summary>Deterministic inventory item tag granted by this command.</summary>
        public string ItemTag { get; }
        /// <summary>Positive quantity granted by this command.</summary>
        public int Quantity { get; }

        /// <summary>Grants the configured item tag and quantity through the mutation context.</summary>
        public void Apply(QuestMutationContext context)
        {
            context.GrantItem(ItemTag, Quantity);
        }
    }
}

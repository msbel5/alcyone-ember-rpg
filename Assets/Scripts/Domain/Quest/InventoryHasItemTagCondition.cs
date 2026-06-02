using System;

// Design note:
// InventoryHasItemTagCondition is a Specification over the player's deterministic inventory snapshot.
// Pattern: Specification.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Condition satisfied when the player's inventory holds at least the required quantity of a tag.</summary>
    public sealed class InventoryHasItemTagCondition : IQuestCondition
    {
        public InventoryHasItemTagCondition(string itemTag, int count)
        {
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Quest inventory item tag is required.", nameof(itemTag));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Quest inventory count must be positive.");

            ItemTag = itemTag.Trim();
            Count = count;
        }

        /// <summary>Deterministic inventory item tag to test.</summary>
        public string ItemTag { get; }
        /// <summary>Minimum required quantity of the tag.</summary>
        public int Count { get; }

        /// <summary>Returns true when the inventory holds at least <see cref="Count"/> of <see cref="ItemTag"/>.</summary>
        public bool IsMet(in QuestWorldView world, QuestState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            return world.CountInventoryTag(ItemTag) >= Count;
        }
    }
}

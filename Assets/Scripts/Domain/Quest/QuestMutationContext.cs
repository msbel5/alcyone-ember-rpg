using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.World;

// Design note:
// QuestMutationContext is the Command-pattern write facade for deterministic quest actions.
// Pattern: Command write context.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Constrained mutable quest context exposing only deterministic quest-safe world/state mutations.</summary>
    public sealed class QuestMutationContext
    {
        private ulong _nextGrantedItemId;

        public QuestMutationContext(WorldState world, QuestState state, ActorId eventActorId = default, SiteId eventSiteId = default)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            State = state ?? throw new ArgumentNullException(nameof(state));
            EventActorId = eventActorId;
            EventSiteId = eventSiteId;
            _nextGrantedItemId = FindNextItemIdSeed(world);
        }

        /// <summary>Mutable deterministic world owned by the caller.</summary>
        public WorldState World { get; }
        /// <summary>Mutable quest runtime state owned by the caller.</summary>
        public QuestState State { get; }
        /// <summary>Default actor subject used when quest actions append world events.</summary>
        public ActorId EventActorId { get; }
        /// <summary>Default site subject used when quest actions append world events.</summary>
        public SiteId EventSiteId { get; }

        /// <summary>Marks the quest runtime complete and stores its success state.</summary>
        public void CompleteQuest(bool success)
        {
            State.SetCompleted(success);
        }

        /// <summary>Appends a deterministic quest world event using the context's default subject ids.</summary>
        public void AppendEvent(WorldEventKind kind, string reason)
        {
            World.Events.Append(new WorldEvent(World.Time, kind, EventActorId, EventSiteId, reason));
        }

        /// <summary>Grants a stackable inventory item with a deterministic synthetic id and exact requested quantity.</summary>
        public void GrantItem(string itemTag, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Granted quest item tag is required.", nameof(itemTag));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Granted quest item quantity must be positive.");
            if (World.PlayerInventory == null)
                throw new InvalidOperationException("Quest item grants require a player inventory.");

            var item = new InventoryItem(new ItemId(_nextGrantedItemId++), itemTag.Trim(), itemTag.Trim(), quantity);
            if (!World.PlayerInventory.TryAdd(item))
                throw new InvalidOperationException($"Quest item grant '{itemTag}' x{quantity} could not fit in the player inventory.");
        }

        private static ulong FindNextItemIdSeed(WorldState world)
        {
            ulong max = 1UL;

            if (world.PlayerInventory != null)
            {
                foreach (var item in world.PlayerInventory.Items)
                {
                    if (item.Id.Value >= max)
                        max = item.Id.Value + 1UL;
                }
            }

            if (world.Items != null)
            {
                foreach (var item in world.Items.Records)
                {
                    if (item.Id.Value >= max)
                        max = item.Id.Value + 1UL;
                }
            }

            return max;
        }
    }
}

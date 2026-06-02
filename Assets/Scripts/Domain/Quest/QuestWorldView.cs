using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// QuestWorldView is the Specification-pattern read facade conditions evaluate against.
// Pattern: Specification read model.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Read-only facade over the deterministic world state needed by quest conditions.</summary>
    public readonly struct QuestWorldView
    {
        private readonly WorldState _world;

        public QuestWorldView(WorldState world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>Current deterministic world time used by elapsed-time conditions.</summary>
        public GameTime Time
        {
            get { return _world.Time; }
        }

        /// <summary>Counts the total inventory quantity currently held for the supplied item tag.</summary>
        public int CountInventoryTag(string itemTag)
        {
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Inventory item tag is required.", nameof(itemTag));
            if (_world.PlayerInventory == null)
                return 0;

            var normalizedTag = itemTag.Trim();
            var total = 0;
            foreach (var item in _world.PlayerInventory.Items)
            {
                if (string.Equals(item.TemplateId, normalizedTag, StringComparison.Ordinal))
                    total += item.Quantity;
            }

            return total;
        }

        /// <summary>Returns true when the supplied actor exists and is currently dead.</summary>
        public bool IsActorDead(ActorId actorId)
        {
            if (actorId.IsEmpty)
                throw new ArgumentException("ActorId.Empty cannot be queried by quest conditions.", nameof(actorId));
            return _world.Actors != null
                && _world.Actors.TryGet(actorId, out var actor)
                && actor.Vitals.IsDead;
        }
    }
}

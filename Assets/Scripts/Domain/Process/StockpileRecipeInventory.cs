using System.Collections.Generic;

// Design note:
// W33-02 §8 (B06): the site stockpile as a recipe IO container. Atomicity is THIS adapter's
// duty: StockpileComponent.Remove is remove-up-to, so TryConsume pre-checks the count before
// removing — a recipe input can never be half-taken. CONSTRAINT: pure Domain, deterministic.
namespace EmberCrpg.Domain.Process
{
    /// <summary>Tag-count recipe IO over one site's StockpileComponent.</summary>
    public sealed class StockpileRecipeInventory : IRecipeInventory
    {
        private readonly StockpileComponent _pile;

        public StockpileRecipeInventory(StockpileComponent pile)
        {
            _pile = pile ?? throw new System.ArgumentNullException(nameof(pile));
        }

        public int CountOf(string itemTag) => _pile.Get(itemTag);

        public bool TryConsume(string itemTag, int quantity)
        {
            if (quantity < 0 || _pile.Get(itemTag) < quantity)
                return false;
            return _pile.Remove(itemTag, quantity) == quantity;
        }

        /// <summary>Stockpiles are count ledgers with no capacity — accept is total.</summary>
        public bool TryAccept(string itemTag, int quantity)
        {
            if (quantity < 0)
                return false;
            _pile.Add(itemTag, quantity);
            return true;
        }

        /// <summary>Detached tag-count copy; the live pile is NEVER touched by a preflight.</summary>
        public IRecipeInventory CloneForPreflight()
        {
            var copy = new Dictionary<string, int>();
            foreach (var entry in _pile.Entries)
                copy[entry.Key] = entry.Value;
            return new CountsProbe(copy);
        }

        /// <summary>Dictionary-backed probe used only by preflight clones.</summary>
        private sealed class CountsProbe : IRecipeInventory
        {
            private readonly Dictionary<string, int> _counts;

            public CountsProbe(Dictionary<string, int> counts) { _counts = counts; }

            public int CountOf(string itemTag)
                => itemTag != null && _counts.TryGetValue(itemTag.Trim(), out var count) ? count : 0;

            public bool TryConsume(string itemTag, int quantity)
            {
                if (itemTag == null || quantity < 0) return false;
                var key = itemTag.Trim();
                if (CountOf(key) < quantity) return false;
                _counts[key] = CountOf(key) - quantity;
                return true;
            }

            public bool TryAccept(string itemTag, int quantity)
            {
                if (itemTag == null || quantity < 0) return false;
                var key = itemTag.Trim();
                _counts[key] = CountOf(key) + quantity;
                return true;
            }

            public IRecipeInventory CloneForPreflight()
                => new CountsProbe(new Dictionary<string, int>(_counts));
        }
    }
}

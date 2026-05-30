using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Site-scoped count of items by tag. Phase 6 Atom 6. Pure data; no Unity
    /// types; deterministic enumeration of non-zero entries.
    /// </summary>
    public sealed class StockpileComponent
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        public StockpileComponent(SiteId siteId)
        {
            if (siteId.IsEmpty)
                throw new ArgumentException("SiteId.Empty cannot back a stockpile.", nameof(siteId));
            SiteId = siteId;
        }

        public SiteId SiteId { get; }

        /// <summary>Total number of distinct item tags currently tracked with non-zero count.</summary>
        public int Count
        {
            get
            {
                var n = 0;
                foreach (var kv in _counts)
                    if (kv.Value > 0) n++;
                return n;
            }
        }

        /// <summary>Returns the current count for an item tag; 0 when missing.</summary>
        public int Get(string itemTag)
        {
            if (string.IsNullOrWhiteSpace(itemTag)) return 0;
            return _counts.TryGetValue(itemTag.Trim(), out var count) ? count : 0;
        }

        /// <summary>Adds quantity to the running count. Negative throws; use Remove.</summary>
        public void Add(string itemTag, int quantity)
        {
            ValidateTag(itemTag);
            if (quantity < 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Use Remove for negative quantities.");
            var key = itemTag.Trim();
            // Codex audit (second pass A-P1): unchecked int addition could wrap
            // negative when (current + quantity) crossed Int32.MaxValue,
            // silently making inventory "disappear". Promote to long first,
            // clamp to Int32.MaxValue, then store.
            var current = _counts.TryGetValue(key, out var c) ? c : 0;
            var next = (long)current + quantity;
            _counts[key] = next > int.MaxValue ? int.MaxValue : (int)next;
        }

        /// <summary>
        /// Removes up to <paramref name="quantity"/> of an item. Returns how many
        /// were actually removed. Never goes below zero. Negative quantity throws.
        /// </summary>
        public int Remove(string itemTag, int quantity)
        {
            ValidateTag(itemTag);
            if (quantity < 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be non-negative.");
            var key = itemTag.Trim();
            if (!_counts.TryGetValue(key, out var current) || current <= 0)
                return 0;
            var removed = quantity > current ? current : quantity;
            _counts[key] = current - removed;
            return removed;
        }

        /// <summary>True when the stockpile holds at least one of the item.</summary>
        public bool Contains(string itemTag) => Get(itemTag) > 0;

        /// <summary>
        /// Codex audit (A/P3): yields entries in canonical item-tag order
        /// rather than Dictionary's implementation-defined enumeration, so the
        /// save layer and tests see byte-stable rows regardless of insertion
        /// sequence or CLR version.
        /// </summary>
        public IEnumerable<KeyValuePair<string, int>> Entries
        {
            get
            {
                return _counts
                    .Where(kv => kv.Value > 0)
                    .OrderBy(kv => kv.Key, System.StringComparer.Ordinal);
            }
        }

        private static void ValidateTag(string itemTag)
        {
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Item tag must be non-blank.", nameof(itemTag));
        }
    }
}

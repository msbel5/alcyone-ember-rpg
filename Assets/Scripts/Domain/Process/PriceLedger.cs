using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Per-site, per-item price ledger. Maintains a positive-integer scalar for
    /// each (SiteId, itemTag) cell with deterministic adjustment helpers.
    /// Faz 6 Atom 5.
    /// </summary>
    public sealed class PriceLedger
    {
        private readonly Dictionary<PriceKey, int> _prices = new Dictionary<PriceKey, int>();

        /// <summary>Sets the current price for an item at a site. Throws on blank tag.</summary>
        public void SetPrice(SiteId siteId, string itemTag, int price)
        {
            ValidateInputs(siteId, itemTag);
            if (price < 0)
                throw new ArgumentOutOfRangeException(nameof(price), "Price must be non-negative.");

            _prices[new PriceKey(siteId, itemTag.Trim())] = price;
        }

        /// <summary>Returns the current price; 0 when the item is not listed at this site.</summary>
        public int GetPrice(SiteId siteId, string itemTag)
        {
            if (siteId.IsEmpty || string.IsNullOrWhiteSpace(itemTag))
                return 0;
            return _prices.TryGetValue(new PriceKey(siteId, itemTag.Trim()), out var price) ? price : 0;
        }

        /// <summary>
        /// Adjusts the price by a signed delta, clamping at zero. Returns the new
        /// price. No-op when site/itemTag is blank.
        /// </summary>
        public int AdjustPrice(SiteId siteId, string itemTag, int delta)
        {
            if (siteId.IsEmpty || string.IsNullOrWhiteSpace(itemTag))
                return 0;
            var key = new PriceKey(siteId, itemTag.Trim());
            var current = _prices.TryGetValue(key, out var value) ? value : 0;
            // PR#152 bot review fix: `current + delta` in default unchecked
            // context wraps on int overflow, then the lower-bound clamp runs
            // *after* the wrap so a wrapped-negative value gets clamped to 0
            // even though the intended sum was huge positive. Promote to long
            // for the add, then clamp into the int range.
            var nextLong = (long)current + delta;
            if (nextLong < 0L) nextLong = 0L;
            if (nextLong > int.MaxValue) nextLong = int.MaxValue;
            var next = (int)nextLong;
            _prices[key] = next;
            return next;
        }

        /// <summary>True when this ledger has a row for the site/item pair.</summary>
        public bool Contains(SiteId siteId, string itemTag)
        {
            if (siteId.IsEmpty || string.IsNullOrWhiteSpace(itemTag))
                return false;
            return _prices.ContainsKey(new PriceKey(siteId, itemTag.Trim()));
        }

        /// <summary>Total number of (site, item) cells tracked.</summary>
        public int Count => _prices.Count;

        /// <summary>
        /// Codex audit (A/P3): yields rows in canonical (SiteId.Value, ItemTag)
        /// order rather than Dictionary's implementation-defined enumeration,
        /// so downstream save layers and tests get a byte-stable layout
        /// regardless of insertion sequence or CLR version.
        /// </summary>
        public IEnumerable<PriceLedgerEntry> Entries
        {
            get
            {
                return _prices
                    .OrderBy(kv => kv.Key.SiteId.Value)
                    .ThenBy(kv => kv.Key.ItemTag, System.StringComparer.Ordinal)
                    .Select(kv => new PriceLedgerEntry(kv.Key.SiteId, kv.Key.ItemTag, kv.Value));
            }
        }

        private static void ValidateInputs(SiteId siteId, string itemTag)
        {
            if (siteId.IsEmpty)
                throw new ArgumentException("SiteId.Empty cannot back a price row.", nameof(siteId));
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Item tag must be non-blank.", nameof(itemTag));
        }

        /// <summary>Composite key for (site, itemTag).</summary>
        private readonly struct PriceKey : IEquatable<PriceKey>
        {
            public PriceKey(SiteId siteId, string itemTag)
            {
                SiteId = siteId;
                ItemTag = itemTag;
            }

            public SiteId SiteId { get; }
            public string ItemTag { get; }

            public bool Equals(PriceKey other) => SiteId.Equals(other.SiteId) && string.Equals(ItemTag, other.ItemTag, StringComparison.Ordinal);
            public override bool Equals(object obj) => obj is PriceKey other && Equals(other);
            public override int GetHashCode() => unchecked((SiteId.GetHashCode() * 397) ^ (ItemTag?.GetHashCode() ?? 0));
        }
    }

    /// <summary>Serializable view of one price ledger cell.</summary>
    public readonly struct PriceLedgerEntry : IEquatable<PriceLedgerEntry>
    {
        public PriceLedgerEntry(SiteId siteId, string itemTag, int price)
        {
            SiteId = siteId;
            ItemTag = itemTag ?? string.Empty;
            Price = price;
        }

        public SiteId SiteId { get; }
        public string ItemTag { get; }
        public int Price { get; }

        public bool Equals(PriceLedgerEntry other) => SiteId.Equals(other.SiteId) && ItemTag == other.ItemTag && Price == other.Price;
        public override bool Equals(object obj) => obj is PriceLedgerEntry other && Equals(other);
        public override int GetHashCode() => unchecked((SiteId.GetHashCode() * 397) ^ ((ItemTag?.GetHashCode() ?? 0) * 31) ^ Price);
    }
}

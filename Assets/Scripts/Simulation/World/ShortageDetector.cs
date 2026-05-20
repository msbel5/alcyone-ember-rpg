using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.World
{
    /// <summary>
    /// Emits a ShortageDetected event the FIRST tick a stockpile count drops
    /// below the configured threshold for a given item tag. PR#162 bot review
    /// fix: previously the detector emitted on every tick while the count was
    /// below the threshold, spamming the event log. Now we track the
    /// (site, item) cells that are currently in-shortage and only emit on the
    /// transition into shortage.
    /// </summary>
    public sealed class ShortageDetector
    {
        private readonly HashSet<ShortageKey> _belowThreshold = new HashSet<ShortageKey>();

        public void Check(
            StockpileComponent stockpile,
            string itemTag,
            int threshold,
            GameTime now,
            WorldEventLog events)
        {
            if (stockpile == null) throw new ArgumentNullException(nameof(stockpile));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Item tag must be non-blank.", nameof(itemTag));
            if (threshold < 0)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be non-negative.");

            var key = new ShortageKey(stockpile.SiteId, itemTag);
            var count = stockpile.Get(itemTag);
            if (count >= threshold)
            {
                _belowThreshold.Remove(key);
                return;
            }

            if (!_belowThreshold.Add(key)) return; // already-in-shortage, don't re-emit

            events.Append(new WorldEvent(
                now,
                WorldEventKind.ShortageDetected,
                default,
                stockpile.SiteId,
                $"shortage item:{itemTag} stock:{count} threshold:{threshold}"));
        }

        private readonly struct ShortageKey : IEquatable<ShortageKey>
        {
            private readonly SiteId _site;
            private readonly string _itemTag;
            public ShortageKey(SiteId site, string itemTag) { _site = site; _itemTag = itemTag; }
            public bool Equals(ShortageKey other) => _site.Equals(other._site) && _itemTag == other._itemTag;
            public override bool Equals(object obj) => obj is ShortageKey o && Equals(o);
            public override int GetHashCode() => unchecked((_site.GetHashCode() * 397) ^ (_itemTag?.GetHashCode() ?? 0));
        }
    }
}

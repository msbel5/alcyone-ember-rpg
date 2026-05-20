using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.World
{
    /// <summary>
    /// Emits a ShortageDetected event when a stockpile count drops below the
    /// configured threshold for a given item tag. Faz 6 Atom 13.
    /// </summary>
    public sealed class ShortageDetector
    {
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

            var count = stockpile.Get(itemTag);
            if (count >= threshold) return;

            events.Append(new WorldEvent(
                now,
                WorldEventKind.ShortageDetected,
                default,
                stockpile.SiteId,
                $"shortage item:{itemTag} stock:{count} threshold:{threshold}"));
        }
    }
}

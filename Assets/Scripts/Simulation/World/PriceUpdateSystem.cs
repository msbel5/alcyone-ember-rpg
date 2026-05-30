using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.World
{
    /// <summary>
    /// Adjusts site prices based on stockpile counts. Below low threshold,
    /// price rises by a delta; above high threshold, price falls by a delta.
    /// Emits PriceChanged for replay. Phase 6 Atom 10.
    /// </summary>
    public sealed class PriceUpdateSystem
    {
        public void Recompute(
            PriceLedger ledger,
            StockpileComponent stockpile,
            string itemTag,
            int lowThreshold,
            int highThreshold,
            int delta,
            GameTime now,
            WorldEventLog events)
        {
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (stockpile == null) throw new ArgumentNullException(nameof(stockpile));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Item tag must be non-blank.", nameof(itemTag));
            if (delta < 0)
                throw new ArgumentOutOfRangeException(nameof(delta), "Delta must be non-negative.");

            var count = stockpile.Get(itemTag);
            var before = ledger.GetPrice(stockpile.SiteId, itemTag);
            int after;
            string direction;

            if (count < lowThreshold)
            {
                after = ledger.AdjustPrice(stockpile.SiteId, itemTag, +delta);
                direction = "up";
            }
            else if (count > highThreshold)
            {
                after = ledger.AdjustPrice(stockpile.SiteId, itemTag, -delta);
                direction = "down";
            }
            else
            {
                return;
            }

            if (after == before) return;

            events.Append(new WorldEvent(
                now,
                WorldEventKind.PriceChanged,
                default,
                stockpile.SiteId,
                $"price_{direction} item:{itemTag} from:{before} to:{after} stock:{count}"));
        }
    }
}

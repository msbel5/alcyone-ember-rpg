using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.World
{
    /// <summary>
    /// Atomic trade between a buyer-site stockpile and a seller-site stockpile
    /// using the local PriceLedger. Pays a quantity from one, delivers to the
    /// other, optionally adjusts faction reputation, and emits TradeCompleted.
    /// Faz 6 Atom 11.
    /// </summary>
    public sealed class TradeService
    {
        public bool TryTrade(
            PriceLedger ledger,
            StockpileComponent buyerStockpile,
            StockpileComponent sellerStockpile,
            string itemTag,
            int quantity,
            GameTime now,
            WorldEventLog events)
        {
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (buyerStockpile == null) throw new ArgumentNullException(nameof(buyerStockpile));
            if (sellerStockpile == null) throw new ArgumentNullException(nameof(sellerStockpile));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Item tag must be non-blank.", nameof(itemTag));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

            var available = sellerStockpile.Get(itemTag);
            if (available < quantity)
                return false;

            var unitPrice = ledger.GetPrice(sellerStockpile.SiteId, itemTag);
            sellerStockpile.Remove(itemTag, quantity);
            buyerStockpile.Add(itemTag, quantity);

            events.Append(new WorldEvent(
                now,
                WorldEventKind.TradeCompleted,
                default,
                sellerStockpile.SiteId,
                $"trade item:{itemTag} qty:{quantity} unit:{unitPrice} buyer_site:{buyerStockpile.SiteId} seller_site:{sellerStockpile.SiteId}"));
            return true;
        }
    }
}

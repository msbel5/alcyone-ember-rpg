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
            WorldEventLog events,
            string currencyTag = null,
            StockpileComponent buyerCurrencyStockpile = null,
            StockpileComponent sellerCurrencyStockpile = null,
            FactionStore factions = null,
            FactionId buyerFaction = default,
            FactionId sellerFaction = default,
            int reputationDelta = 0,
            FactionReputationSystem reputationSystem = null)
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

            var normalizedCurrency = string.IsNullOrWhiteSpace(currencyTag) ? null : currencyTag.Trim();
            // Codex audit (A/P2): when a currency tag is supplied but the ledger
            // has no price row for the item, GetPrice returns 0 and the payment
            // block below is skipped entirely — a priced trade silently became
            // free. Refuse the trade rather than letting items move without
            // payment. Unpriced (currencyTag == null) trades are still allowed.
            if (normalizedCurrency != null && !ledger.Contains(sellerStockpile.SiteId, itemTag))
                return false;

            var unitPrice = ledger.GetPrice(sellerStockpile.SiteId, itemTag);
            var totalPrice = unitPrice * quantity;
            if (normalizedCurrency != null)
            {
                var sourceCurrency = buyerCurrencyStockpile ?? buyerStockpile;
                if (totalPrice > 0 && sourceCurrency.Get(normalizedCurrency) < totalPrice)
                    return false;
            }

            sellerStockpile.Remove(itemTag, quantity);
            buyerStockpile.Add(itemTag, quantity);
            if (normalizedCurrency != null && totalPrice > 0)
            {
                var sourceCurrency = buyerCurrencyStockpile ?? buyerStockpile;
                var destinationCurrency = sellerCurrencyStockpile ?? sellerStockpile;
                sourceCurrency.Remove(normalizedCurrency, totalPrice);
                destinationCurrency.Add(normalizedCurrency, totalPrice);
            }

            if (factions != null && reputationDelta != 0 && !buyerFaction.IsEmpty && !sellerFaction.IsEmpty)
            {
                var system = reputationSystem ?? new FactionReputationSystem();
                system.ApplyDelta(factions, buyerFaction, sellerFaction, reputationDelta, "trade_completed", now, events);
            }

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

using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>
    /// Codex audit Batch 5 / A-P2 regression: when a buyer offers currency but
    /// the seller's ledger has no price row for the item, GetPrice used to
    /// return 0 and the payment block was skipped — items moved for free.
    /// </summary>
    public sealed class TradeServiceMissingPriceTests
    {
        private static readonly SiteId Buyer = new SiteId(1UL);
        private static readonly SiteId Seller = new SiteId(2UL);

        [Test]
        public void TryTrade_NoPriceRow_RejectsCurrencyTrade()
        {
            var ledger = new PriceLedger();
            // intentionally leave ledger empty for "iron_ingot" @ Seller
            var sellerStock = new StockpileComponent(Seller);
            sellerStock.Add("iron_ingot", 10);
            var buyerStock = new StockpileComponent(Buyer);
            buyerStock.Add("coin", 1000);
            var events = new WorldEventLog();

            var ok = new TradeService().TryTrade(
                ledger, buyerStock, sellerStock,
                itemTag: "iron_ingot", quantity: 3,
                now: default, events: events,
                currencyTag: "coin");

            Assert.That(ok, Is.False, "missing price row must reject a currency-tagged trade");
            Assert.That(sellerStock.Get("iron_ingot"), Is.EqualTo(10), "seller stock must remain untouched");
            Assert.That(buyerStock.Get("iron_ingot"), Is.EqualTo(0), "buyer must not receive items for free");
            Assert.That(buyerStock.Get("coin"), Is.EqualTo(1000), "buyer's coin must not be spent");
        }

        [Test]
        public void TryTrade_NoCurrencyTag_AllowedEvenWithoutPriceRow()
        {
            // Free / barter trades (currencyTag == null) remain permitted when
            // no ledger row exists — only currency-tagged trades require a price.
            var ledger = new PriceLedger();
            var sellerStock = new StockpileComponent(Seller);
            sellerStock.Add("iron_ingot", 10);
            var buyerStock = new StockpileComponent(Buyer);
            var events = new WorldEventLog();

            var ok = new TradeService().TryTrade(
                ledger, buyerStock, sellerStock,
                itemTag: "iron_ingot", quantity: 4,
                now: default, events: events,
                currencyTag: null);

            Assert.That(ok, Is.True);
            Assert.That(sellerStock.Get("iron_ingot"), Is.EqualTo(6));
            Assert.That(buyerStock.Get("iron_ingot"), Is.EqualTo(4));
        }

        [Test]
        public void TryTrade_PriceRowPresent_StillChargesCurrency()
        {
            // Sanity: when the ledger DOES have a price row, the existing
            // payment behavior is preserved by the new guard.
            var ledger = new PriceLedger();
            ledger.SetPrice(Seller, "iron_ingot", 5);
            var sellerStock = new StockpileComponent(Seller);
            sellerStock.Add("iron_ingot", 10);
            var buyerStock = new StockpileComponent(Buyer);
            buyerStock.Add("coin", 100);
            var events = new WorldEventLog();

            var ok = new TradeService().TryTrade(
                ledger, buyerStock, sellerStock,
                itemTag: "iron_ingot", quantity: 3,
                now: default, events: events,
                currencyTag: "coin");

            Assert.That(ok, Is.True);
            Assert.That(buyerStock.Get("coin"), Is.EqualTo(100 - 15), "buyer must pay unitPrice*quantity");
            Assert.That(sellerStock.Get("coin"), Is.EqualTo(15), "seller must receive the payment");
        }
    }
}

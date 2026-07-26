using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    public sealed class TradeBundleTests
    {
        private static readonly SiteId Buyer = new SiteId(1UL);
        private static readonly SiteId Seller = new SiteId(2UL);

        // ----- PriceUpdateSystem -----
        [Test]
        public void PriceUpdate_LowStock_PriceRises()
        {
            var ledger = new PriceLedger();
            ledger.SetPrice(Seller, "iron_ingot", 10);
            var stockpile = new StockpileComponent(Seller);
            stockpile.Add("iron_ingot", 1);
            var events = new WorldEventLog();
            new PriceUpdateSystem().Recompute(ledger, stockpile, "iron_ingot", 5, 20, 3, default, events);

            Assert.That(ledger.GetPrice(Seller, "iron_ingot"), Is.EqualTo(13));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.Events[0].Kind, Is.EqualTo(WorldEventKind.PriceChanged));
        }

        [Test]
        public void PriceUpdate_HighStock_PriceFalls()
        {
            var ledger = new PriceLedger();
            ledger.SetPrice(Seller, "iron_ingot", 10);
            var stockpile = new StockpileComponent(Seller);
            stockpile.Add("iron_ingot", 50);
            var events = new WorldEventLog();
            new PriceUpdateSystem().Recompute(ledger, stockpile, "iron_ingot", 5, 20, 3, default, events);

            Assert.That(ledger.GetPrice(Seller, "iron_ingot"), Is.EqualTo(7));
        }

        [Test]
        public void PriceUpdate_InRange_NoChange()
        {
            var ledger = new PriceLedger();
            ledger.SetPrice(Seller, "iron_ingot", 10);
            var stockpile = new StockpileComponent(Seller);
            stockpile.Add("iron_ingot", 10);
            var events = new WorldEventLog();
            new PriceUpdateSystem().Recompute(ledger, stockpile, "iron_ingot", 5, 20, 3, default, events);

            Assert.That(ledger.GetPrice(Seller, "iron_ingot"), Is.EqualTo(10));
            Assert.That(events.Count, Is.EqualTo(0));
        }

        // ----- ShortageDetector -----
        [Test]
        public void Shortage_BelowThreshold_Emits()
        {
            var stockpile = new StockpileComponent(Seller);
            stockpile.Add("bread", 2);
            var events = new WorldEventLog();
            new ShortageDetector().Check(stockpile, "bread", 5, default, events);

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.Events[0].Kind, Is.EqualTo(WorldEventKind.ShortageDetected));
        }

        [Test]
        public void Shortage_AboveThreshold_NoEvent()
        {
            var stockpile = new StockpileComponent(Seller);
            stockpile.Add("bread", 10);
            var events = new WorldEventLog();
            new ShortageDetector().Check(stockpile, "bread", 5, default, events);
            Assert.That(events.Count, Is.EqualTo(0));
        }

        // TradeService coverage retired 2026-07-26: the service dropped with the DEAD-1
        // sweep (obsolete since B29 / 2026-07-25 — SettlementTradeService is the live seam).
    }
}

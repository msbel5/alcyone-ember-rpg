using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Pins PriceLedger per-(site, item) scalar behaviour. Phase 6 Atom 5.</summary>
    public sealed class PriceLedgerTests
    {
        private static readonly SiteId Settlement = new SiteId(1UL);
        private static readonly SiteId Outpost = new SiteId(2UL);

        [Test]
        public void GetPrice_Unset_ReturnsZero()
        {
            var ledger = new PriceLedger();
            Assert.That(ledger.GetPrice(Settlement, "iron_ingot"), Is.EqualTo(0));
        }

        [Test]
        public void SetPrice_StoresAndReadsBack()
        {
            var ledger = new PriceLedger();
            ledger.SetPrice(Settlement, "iron_ingot", 42);
            Assert.That(ledger.GetPrice(Settlement, "iron_ingot"), Is.EqualTo(42));
            Assert.That(ledger.Contains(Settlement, "iron_ingot"), Is.True);
            Assert.That(ledger.Count, Is.EqualTo(1));
        }

        [Test]
        public void AdjustPrice_AppliesSignedDelta_AndClampsAtZero()
        {
            var ledger = new PriceLedger();
            ledger.SetPrice(Settlement, "iron_ingot", 50);

            Assert.That(ledger.AdjustPrice(Settlement, "iron_ingot", +10), Is.EqualTo(60));
            Assert.That(ledger.AdjustPrice(Settlement, "iron_ingot", -100), Is.EqualTo(0));
        }

        [Test]
        public void DifferentSites_AreIndependent()
        {
            var ledger = new PriceLedger();
            ledger.SetPrice(Settlement, "iron_ingot", 42);
            ledger.SetPrice(Outpost, "iron_ingot", 99);

            Assert.That(ledger.GetPrice(Settlement, "iron_ingot"), Is.EqualTo(42));
            Assert.That(ledger.GetPrice(Outpost, "iron_ingot"), Is.EqualTo(99));
            Assert.That(ledger.Count, Is.EqualTo(2));
        }

        [Test]
        public void DifferentItems_AreIndependent()
        {
            var ledger = new PriceLedger();
            ledger.SetPrice(Settlement, "iron_ingot", 42);
            ledger.SetPrice(Settlement, "bread", 5);

            Assert.That(ledger.GetPrice(Settlement, "iron_ingot"), Is.EqualTo(42));
            Assert.That(ledger.GetPrice(Settlement, "bread"), Is.EqualTo(5));
        }

        [Test]
        public void SetPrice_RejectsBlankTagOrEmptySite_OrNegativePrice()
        {
            var ledger = new PriceLedger();
            Assert.Throws<System.ArgumentException>(() => ledger.SetPrice(default, "iron_ingot", 10));
            Assert.Throws<System.ArgumentException>(() => ledger.SetPrice(Settlement, "", 10));
            Assert.Throws<System.ArgumentException>(() => ledger.SetPrice(Settlement, "   ", 10));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => ledger.SetPrice(Settlement, "ok", -1));
        }

        [Test]
        public void Contains_ReturnsFalseForUnsetOrInvalid()
        {
            var ledger = new PriceLedger();
            Assert.That(ledger.Contains(Settlement, "iron_ingot"), Is.False);
            Assert.That(ledger.Contains(default, "iron_ingot"), Is.False);
            Assert.That(ledger.Contains(Settlement, ""), Is.False);
        }
    }
}

using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Pins StockpileComponent count + add/remove behaviour. Faz 6 Atom 6.</summary>
    public sealed class StockpileComponentTests
    {
        private static readonly SiteId Site = new SiteId(1UL);

        [Test]
        public void Constructor_RejectsEmptySite()
        {
            Assert.Throws<System.ArgumentException>(() => new StockpileComponent(default));
        }

        [Test]
        public void Get_UnsetTag_ReturnsZero()
        {
            var stockpile = new StockpileComponent(Site);
            Assert.That(stockpile.Get("bread"), Is.EqualTo(0));
            Assert.That(stockpile.Contains("bread"), Is.False);
            Assert.That(stockpile.Count, Is.EqualTo(0));
        }

        [Test]
        public void Add_AccumulatesCount()
        {
            var stockpile = new StockpileComponent(Site);
            stockpile.Add("bread", 3);
            stockpile.Add("bread", 2);

            Assert.That(stockpile.Get("bread"), Is.EqualTo(5));
            Assert.That(stockpile.Contains("bread"), Is.True);
            Assert.That(stockpile.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_ReturnsActualAmountRemoved_NeverGoesBelowZero()
        {
            var stockpile = new StockpileComponent(Site);
            stockpile.Add("iron_ingot", 4);

            Assert.That(stockpile.Remove("iron_ingot", 1), Is.EqualTo(1));
            Assert.That(stockpile.Get("iron_ingot"), Is.EqualTo(3));

            Assert.That(stockpile.Remove("iron_ingot", 100), Is.EqualTo(3));
            Assert.That(stockpile.Get("iron_ingot"), Is.EqualTo(0));
            Assert.That(stockpile.Contains("iron_ingot"), Is.False);
            Assert.That(stockpile.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_UnknownItem_ReturnsZero()
        {
            var stockpile = new StockpileComponent(Site);
            Assert.That(stockpile.Remove("ghost_item", 5), Is.EqualTo(0));
        }

        [Test]
        public void Add_RejectsBlankTagOrNegativeQuantity()
        {
            var stockpile = new StockpileComponent(Site);
            Assert.Throws<System.ArgumentException>(() => stockpile.Add("", 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => stockpile.Add("bread", -1));
        }

        [Test]
        public void Entries_OnlyEnumeratesNonZero()
        {
            var stockpile = new StockpileComponent(Site);
            stockpile.Add("bread", 2);
            stockpile.Add("iron_ingot", 3);
            stockpile.Remove("bread", 2);

            var entries = stockpile.Entries.ToList();
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Key, Is.EqualTo("iron_ingot"));
            Assert.That(entries[0].Value, Is.EqualTo(3));
        }
    }
}

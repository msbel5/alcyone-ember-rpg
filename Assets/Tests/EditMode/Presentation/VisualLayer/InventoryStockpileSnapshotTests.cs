using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Presentation.VisualLayer;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation.VisualLayer
{
    /// <summary>Pins inventory stockpile aggregation snapshot rows.</summary>
    public sealed class InventoryStockpileSnapshotTests
    {
        [Test]
        public void NullInventory_ProducesEmptySnapshot()
        {
            var snapshot = InventoryStockpileSnapshot.FromInventory(null);
            Assert.That(snapshot.Rows, Is.Empty);
        }

        [Test]
        public void EmptyInventory_ProducesEmptySnapshot()
        {
            var snapshot = InventoryStockpileSnapshot.FromInventory(new InventoryState(10));
            Assert.That(snapshot.Rows, Is.Empty);
        }

        [Test]
        public void SingleItem_SurfacesTemplateDisplayQuantity()
        {
            var inventory = new InventoryState(10);
            inventory.TryAdd(new InventoryItem(new ItemId(1UL), "iron_ingot", "Iron Ingot", 3));

            var snapshot = InventoryStockpileSnapshot.FromInventory(inventory);

            Assert.That(snapshot.Rows.Count, Is.EqualTo(1));
            Assert.That(snapshot.Rows[0].TemplateId, Is.EqualTo("iron_ingot"));
            Assert.That(snapshot.Rows[0].DisplayName, Is.EqualTo("Iron Ingot"));
            Assert.That(snapshot.Rows[0].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void MultipleItemsSameTemplate_AggregateQuantity()
        {
            var inventory = new InventoryState(10);
            inventory.TryAdd(new InventoryItem(new ItemId(1UL), "iron_ingot", "Iron Ingot", 3));
            inventory.TryAdd(new InventoryItem(new ItemId(2UL), "iron_ingot", "Iron Ingot", 4));
            inventory.TryAdd(new InventoryItem(new ItemId(3UL), "bread", "Bread", 2));

            var snapshot = InventoryStockpileSnapshot.FromInventory(inventory);

            Assert.That(snapshot.Rows.Count, Is.EqualTo(2));
            Assert.That(snapshot.Rows[0].TemplateId, Is.EqualTo("iron_ingot"));
            Assert.That(snapshot.Rows[0].Quantity, Is.EqualTo(7));
            Assert.That(snapshot.Rows[1].TemplateId, Is.EqualTo("bread"));
            Assert.That(snapshot.Rows[1].Quantity, Is.EqualTo(2));
        }
    }
}

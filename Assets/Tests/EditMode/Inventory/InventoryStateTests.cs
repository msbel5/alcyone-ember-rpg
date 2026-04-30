using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using NUnit.Framework;

// Design note:
// These tests pin Sprint 1's fixed-capacity backpack behavior.
// They cover add/remove/full semantics plus Sprint 3 take/split identity behavior.
namespace EmberCrpg.Tests.EditMode.Inventory
{
    /// <summary>Verifies deterministic ten-slot inventory behavior.</summary>
    public sealed class InventoryStateTests
    {
        [Test]
        public void TryAdd_NewItem_UsesOneSlot()
        {
            var inventory = new InventoryState(10);
            var added = inventory.TryAdd(new InventoryItem(new ItemId(1), "ember_shard", "Ember Shard", 1));
            Assert.That(added && inventory.Items.Count == 1, Is.True);
        }

        [Test]
        public void TryAdd_SameTemplate_StacksInsteadOfUsingNewSlot()
        {
            var inventory = new InventoryState(10);
            inventory.TryAdd(new InventoryItem(new ItemId(1), "ember_shard", "Ember Shard", 1));
            inventory.TryAdd(new InventoryItem(new ItemId(2), "ember_shard", "Ember Shard", 2));
            Assert.That(new[] { inventory.Items.Count, inventory.Items[0].Quantity }, Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void TryAdd_NonStackableDuplicateTemplate_KeepsDistinctStableItems()
        {
            var inventory = new InventoryState(10);
            inventory.TryAdd(new InventoryItem(new ItemId(10), "warden_blade", "Warden Blade", 1, false, EquipmentSlot.Weapon));
            inventory.TryAdd(new InventoryItem(new ItemId(11), "warden_blade", "Warden Blade", 1, false, EquipmentSlot.Weapon));

            Assert.That(inventory.Items.Count, Is.EqualTo(2));
            Assert.That(inventory.Items[0].Id, Is.Not.EqualTo(inventory.Items[1].Id));
        }

        [Test]
        public void TryAdd_WhenFull_ReturnsFalse()
        {
            var inventory = new InventoryState(1);
            inventory.TryAdd(new InventoryItem(new ItemId(1), "ember_shard", "Ember Shard", 1));
            var added = inventory.TryAdd(new InventoryItem(new ItemId(2), "rat_tooth", "Rat Tooth", 1));
            Assert.That(added, Is.False);
        }

        [Test]
        public void TryRemove_RemovesItemWhenQuantityHitsZero()
        {
            var inventory = new InventoryState(10);
            inventory.TryAdd(new InventoryItem(new ItemId(1), "ember_shard", "Ember Shard", 1));
            inventory.TryRemove("ember_shard", 1);
            Assert.That(inventory.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryTake_FromStack_SplitsRequestedQuantityIntoNewStableItem()
        {
            var inventory = new InventoryState(10);
            var itemIds = new ItemInstanceSequence(42);
            var expectedId = itemIds.Clone().TakeNext();
            inventory.TryAdd(new InventoryItem(new ItemId(1), "ember_shard", "Ember Shard", 2));

            InventoryItem taken;
            var success = inventory.TryTake("ember_shard", 1, itemIds, out taken);

            Assert.That(success, Is.True);
            Assert.That(taken.Id, Is.EqualTo(expectedId));
            Assert.That(inventory.Items[0].Quantity, Is.EqualTo(1));
        }

        [Test]
        public void TryTakeById_NonStackableItem_RemovesExactInstance()
        {
            var inventory = new InventoryState(10);
            inventory.TryAdd(new InventoryItem(new ItemId(20), "warden_coat", "Warden Coat", 1, false, EquipmentSlot.Armor));
            inventory.TryAdd(new InventoryItem(new ItemId(21), "warden_blade", "Warden Blade", 1, false, EquipmentSlot.Weapon));

            InventoryItem taken;
            var success = inventory.TryTakeById(new ItemId(21), out taken);

            Assert.That(success, Is.True);
            Assert.That(taken.TemplateId, Is.EqualTo("warden_blade"));
            Assert.That(inventory.Contains(new ItemId(21)), Is.False);
            Assert.That(inventory.Contains(new ItemId(20)), Is.True);
        }
    }
}

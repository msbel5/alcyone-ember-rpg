using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin the deterministic Sprint 2 merchant exchange.
// They cover stock, payment, inventory-capacity behavior, and Sprint 3 item identity flow.
namespace EmberCrpg.Tests.EditMode.Inventory
{
    /// <summary>Verifies the slice merchant trade loop.</summary>
    public sealed class MerchantTradeServiceTests
    {
        [Test]
        public void TradeGateWrit_WithPayment_TransfersStableItemInstances()
        {
            var world = CreateMerchantReadyWorld();
            var payment = SliceItemCatalog.CreateEmberShard(world.ItemIds);
            world.PlayerInventory.TryAdd(payment);
            var expectedWritId = world.MerchantInventory.Items[0].Id;

            var reply = new MerchantTradeService().TradeGateWrit(world);

            Assert.That(reply, Does.Contain("gate writ"));
            Assert.That(world.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId), Is.True);
            Assert.That(world.PlayerInventory.Items[0].Id, Is.EqualTo(expectedWritId));
            Assert.That(world.MerchantInventory.Contains(SliceItemCatalog.GateWritTemplateId), Is.False);
            Assert.That(world.MerchantInventory.Contains(SliceItemCatalog.EmberShardTemplateId), Is.True);
            Assert.That(world.MerchantInventory.Items[0].Id, Is.EqualTo(payment.Id));
        }

        [Test]
        public void TradeGateWrit_WhenInventoryRemainsFull_RefusesTradeWithoutAdvancingItemIds()
        {
            var world = CreateMerchantReadyWorld();
            var nextId = world.ItemIds.Clone().TakeNext();
            world.PlayerInventory = new InventoryState(2);
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(1), SliceItemCatalog.EmberShardTemplateId, "Ember Shard", 2));
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(2), "satchel_note", "Satchel Note", 1));

            var reply = new MerchantTradeService().TradeGateWrit(world);

            Assert.That(reply, Does.Contain("inventory is too full"));
            Assert.That(world.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId), Is.False);
            Assert.That(world.MerchantInventory.Contains(SliceItemCatalog.GateWritTemplateId), Is.True);
            Assert.That(world.ItemIds.TakeNext(), Is.EqualTo(nextId));
        }

        private static SliceWorldState CreateMerchantReadyWorld()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.Player.MoveTo(world.Merchant.Position.Translate(0, 1));
            return world;
        }
    }
}

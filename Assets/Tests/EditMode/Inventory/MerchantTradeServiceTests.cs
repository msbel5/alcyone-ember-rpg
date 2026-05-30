using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.World;
using NUnit.Framework;
using EmberCrpg.Domain.Actors;

// Design note:
// These tests pin the deterministic Sprint 2 merchant exchange.
// They cover stock, payment, and inventory-capacity behavior only.
namespace EmberCrpg.Tests.EditMode.Inventory
{
    /// <summary>Verifies the slice merchant trade loop.</summary>
    public sealed class MerchantTradeServiceTests
    {
        [Test]
        public void TradeGateWrit_WithPayment_TransfersItemAndConsumesStock()
        {
            var world = CreateMerchantReadyWorld();
            world.PlayerInventory.TryAdd(WorldItemCatalog.CreateEmberShard());

            var reply = new MerchantTradeService().TradeGateWrit(world);

            Assert.That(reply, Does.Contain("gate writ"));
            Assert.That(world.PlayerInventory.Contains(WorldItemCatalog.GateWritTemplateId), Is.True);
            Assert.That(world.MerchantInventory.Contains(WorldItemCatalog.GateWritTemplateId), Is.False);
            Assert.That(world.MerchantInventory.Contains(WorldItemCatalog.EmberShardTemplateId), Is.True);
        }

        [Test]
        public void TradeGateWrit_WhenInventoryRemainsFull_RefusesTrade()
        {
            var world = CreateMerchantReadyWorld();
            world.PlayerInventory = new InventoryState(2);
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(1), WorldItemCatalog.EmberShardTemplateId, "Ember Shard", 2));
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(2), "satchel_note", "Satchel Note", 1));

            var reply = new MerchantTradeService().TradeGateWrit(world);

            Assert.That(reply, Does.Contain("inventory is too full"));
            Assert.That(world.PlayerInventory.Contains(WorldItemCatalog.GateWritTemplateId), Is.False);
            Assert.That(world.MerchantInventory.Contains(WorldItemCatalog.GateWritTemplateId), Is.True);
        }

        private static WorldState CreateMerchantReadyWorld()
        {
            var world = new WorldFactory().Create(1337);
            world.Actors.FirstByRole(ActorRole.Player).MoveTo(world.Actors.FirstByRole(ActorRole.Merchant).Position.Translate(0, 1));
            return world;
        }
    }
}

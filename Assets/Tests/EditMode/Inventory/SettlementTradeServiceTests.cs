using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Inventory
{
    public sealed class SettlementTradeServiceTests
    {
        [Test]
        public void EnsureMerchantStock_SeedsDefaultsOnlyOnce()
        {
            var world = new WorldFactory().Create(7);
            var service = new SettlementTradeService();
            var seed = new[]
            {
                new TradeSeedItem("rope", "Rope", 3),
                new TradeSeedItem("torch", "Torch", 5),
            };

            service.EnsureMerchantStock(world, seed);
            var firstCount = world.MerchantInventory.Items.Count;
            service.EnsureMerchantStock(world, seed);

            Assert.That(world.MerchantStoreSeeded, Is.True);
            Assert.That(firstCount, Is.EqualTo(world.MerchantInventory.Items.Count));
            Assert.That(world.MerchantInventory.Contains("rope"), Is.True);
            Assert.That(world.MerchantInventory.Contains("torch"), Is.True);
        }

        [Test]
        public void TryBuy_MovesItemAndGold()
        {
            var world = new WorldFactory().Create(7);
            var service = new SettlementTradeService();

            var result = service.TryBuy(world, WorldItemCatalog.GateWritTemplateId, 18);

            Assert.That(result.Success, Is.True);
            Assert.That(world.PlayerGold, Is.EqualTo(222));
            Assert.That(world.MerchantGold, Is.EqualTo(1218));
            Assert.That(world.PlayerInventory.Contains(WorldItemCatalog.GateWritTemplateId), Is.True);
        }

        [Test]
        public void TrySell_RespectsEquipmentAndTransfersGold()
        {
            var world = new WorldFactory().Create(7);
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(9001UL), "torch", "Torch", 2));
            var service = new SettlementTradeService();

            var sold = service.TrySell(world, "torch", 1);

            Assert.That(sold.Success, Is.True);
            Assert.That(world.PlayerGold, Is.EqualTo(241));
            Assert.That(world.MerchantGold, Is.EqualTo(1199));
            Assert.That(world.MerchantInventory.Contains("torch"), Is.True);
        }
    }
}

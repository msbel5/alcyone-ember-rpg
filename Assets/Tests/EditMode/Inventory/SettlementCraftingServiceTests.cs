using EmberCrpg.Data.Recipes;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Inventory
{
    public sealed class SettlementCraftingServiceTests
    {
        [Test]
        public void TryCraft_SmeltIronIngot_ConsumesInputsAndAddsOutput()
        {
            var world = CreateWorld();
            var service = new SettlementCraftingService();
            var recipe = ProductionRecipeRegistry.SmeltIronIngot();

            var ok = service.TryCraft(world, recipe, new ActorId(1UL), Humanize, out var message);

            Assert.That(ok, Is.True, message);
            Assert.That(Count(world.PlayerInventory, "iron_ore"), Is.EqualTo(0));
            Assert.That(Count(world.PlayerInventory, "fuel"), Is.EqualTo(0));
            Assert.That(Count(world.PlayerInventory, "iron_ingot"), Is.EqualTo(1));
            Assert.That(message, Is.EqualTo("Crafted Iron Ingot."));
        }

        [Test]
        public void TryCraft_MissingInput_ReturnsReasonWithoutMutatingInventory()
        {
            var world = CreateWorld();
            world.PlayerInventory.TryRemoveStackable("fuel", 1);
            var service = new SettlementCraftingService();

            var ok = service.TryCraft(world, ProductionRecipeRegistry.SmeltIronIngot(), new ActorId(1UL), Humanize, out var message);

            Assert.That(ok, Is.False);
            Assert.That(message, Is.EqualTo("Missing Fuel x1."));
            Assert.That(Count(world.PlayerInventory, "iron_ore"), Is.EqualTo(2));
            Assert.That(Count(world.PlayerInventory, "iron_ingot"), Is.EqualTo(0));
        }

        private static WorldState CreateWorld()
        {
            var world = new WorldState
            {
                PlayerInventory = new InventoryState(12),
                Worksites = new WorksiteStore(),
            };
            world.Worksites.Add(new WorksiteRecord(new SiteId(1UL), new GridPosition(0, 0), WorksiteKind.Furnace, true));
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(10UL), "iron_ore", "Iron Ore", 2));
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(11UL), "fuel", "Fuel", 1));
            return world;
        }

        private static int Count(InventoryState inventory, string templateId)
        {
            int total = 0;
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                if (inventory.Items[i].TemplateId == templateId)
                    total += inventory.Items[i].Quantity;
            }
            return total;
        }

        private static string Humanize(string templateId)
        {
            if (templateId == "iron_ingot") return "Iron Ingot";
            if (templateId == "iron_ore") return "Iron Ore";
            if (templateId == "fuel") return "Fuel";
            return templateId;
        }
    }
}

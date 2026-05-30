using System;
using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Save;
using EmberCrpg.Simulation.Process;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// Pins the first Phase 2 TIME rail for process state: active furnace worksite
// records and a partially progressed recipe work order survive the JSON DTO
// boundary, then continue ticking to one visible RecipeCompleted event.
namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>Verifies recipe/worksite save DTO round-trips before a broader job scheduler exists.</summary>
    public sealed class RecipeWorksiteRoundTripTests
    {
        private static readonly SiteId FurnaceSite = new SiteId(77UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);
        private static readonly ActorId Worker = new ActorId(12UL);

        [Test]
        public void JsonDto_RoundTripsActiveWorksiteProgressAndProducedStock()
        {
            var recipe = CreateSmeltIronIngotRecipe();
            var worksites = CreateActiveFurnaceStore();
            var world = new WorldFactory().Create(2026);
            world.PlayerInventory = CreateSmeltingInventory();

            var system = new RecipeSystem();
            Assert.That(system.TryStart(recipe, worksites, FurnaceSite, FurnacePosition, world.PlayerInventory, Worker, out var order), Is.True);
            for (var i = 0; i < 17; i++)
                Assert.That(system.Tick(order, world.PlayerInventory, world.Events, CreateOutputItem), Is.False);

            var service = new JsonSliceSaveService(ResolveRecipe) { Worksites = worksites };
            service.ReplaceRecipeWorkOrders(new[] { order });

            var json = service.SaveToJson(world);
            Assert.That(json, Does.Contain("worksites"));
            Assert.That(json, Does.Contain("recipeWorkOrders"));

            var loadedWorld = service.LoadFromJson(json);
            var loadedWorksites = service.Worksites;
            var loadedOrder = service.RecipeWorkOrders.Single();

            Assert.That(loadedWorksites.Get(FurnaceSite, FurnacePosition).IsActive, Is.True);
            Assert.That(loadedOrder.Recipe.Id, Is.EqualTo(recipe.Id));
            Assert.That(loadedOrder.SiteId, Is.EqualTo(FurnaceSite));
            Assert.That(loadedOrder.Position, Is.EqualTo(FurnacePosition));
            Assert.That(loadedOrder.ActorId, Is.EqualTo(Worker));
            Assert.That(loadedOrder.ProgressTicks, Is.EqualTo(17));
            Assert.That(Quantity(loadedWorld.PlayerInventory, "iron_ore"), Is.EqualTo(0));
            Assert.That(Quantity(loadedWorld.PlayerInventory, "fuel"), Is.EqualTo(0));
            Assert.That(Quantity(loadedWorld.PlayerInventory, "iron_ingot"), Is.EqualTo(0));

            for (var i = 0; i < 22; i++)
                Assert.That(system.Tick(loadedOrder, loadedWorld.PlayerInventory, loadedWorld.Events, CreateOutputItem), Is.False);

            Assert.That(system.Tick(loadedOrder, loadedWorld.PlayerInventory, loadedWorld.Events, CreateOutputItem), Is.True);
            Assert.That(loadedOrder.ProgressTicks, Is.EqualTo(40));
            Assert.That(Quantity(loadedWorld.PlayerInventory, "iron_ingot"), Is.EqualTo(1));
            Assert.That(loadedWorld.Events.Count, Is.EqualTo(1));
            Assert.That(loadedWorld.Events.Events.Single().Kind, Is.EqualTo(WorldEventKind.RecipeCompleted));
        }

        private static RecipeDef ResolveRecipe(RecipeId recipeId)
        {
            var recipe = CreateSmeltIronIngotRecipe();
            return recipe.Id.Equals(recipeId) ? recipe : null;
        }

        private static RecipeDef CreateSmeltIronIngotRecipe()
        {
            return new RecipeDef(
                new RecipeId(1001UL),
                "furnace",
                "smelting",
                40,
                new[]
                {
                    new RecipeIngredient("iron_ore", 2),
                    new RecipeIngredient("fuel", 1),
                },
                new[]
                {
                    new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1),
                });
        }

        private static WorksiteStore CreateActiveFurnaceStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(FurnaceSite, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            return store;
        }

        private static InventoryState CreateSmeltingInventory()
        {
            var inventory = new InventoryState(8);
            inventory.TryAdd(new InventoryItem(new ItemId(1UL), "iron_ore", "Iron Ore", 2));
            inventory.TryAdd(new InventoryItem(new ItemId(2UL), "fuel", "Fuel", 1));
            return inventory;
        }

        private static InventoryItem CreateOutputItem(RecipeOutput output)
        {
            return new InventoryItem(new ItemId(9001UL), output.ItemTag, "Iron Ingot", 1);
        }

        private static int Quantity(InventoryState inventory, string templateId)
        {
            return inventory.Items.Where(item => string.Equals(item.TemplateId, templateId, StringComparison.Ordinal))
                .Sum(item => item.Quantity);
        }
    }
}

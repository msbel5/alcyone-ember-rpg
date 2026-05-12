using System;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

// Design note:
// These tests pin the first visible Faz 2 RecipeSystem slice: an active furnace
// consumes ore/fuel, advances for 40 deterministic ticks, produces an iron ingot,
// and emits a WorldEventLog line. Save/load and playable replay docs remain later
// atoms in DOCS/sprint-faz-2-atom-map.md.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies the narrow deterministic RecipeSystem smelting contract.</summary>
    public sealed class RecipeSystemTests
    {
        private static readonly SiteId FurnaceSite = new SiteId(77UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);
        private static readonly ActorId Worker = new ActorId(12UL);

        [Test]
        public void TryStart_ConsumesInputsAtActiveMatchingFurnace()
        {
            var system = new RecipeSystem();
            var inventory = CreateSmeltingInventory();

            var started = system.TryStart(
                CreateSmeltIronIngotRecipe(),
                CreateActiveFurnaceStore(),
                FurnaceSite,
                FurnacePosition,
                inventory,
                Worker,
                out var order);

            Assert.That(started, Is.True);
            Assert.That(order, Is.Not.Null);
            Assert.That(order.ProgressTicks, Is.EqualTo(0));
            Assert.That(Quantity(inventory, "iron_ore"), Is.EqualTo(0));
            Assert.That(Quantity(inventory, "fuel"), Is.EqualTo(0));
            Assert.That(Quantity(inventory, "iron_ingot"), Is.EqualTo(0));
        }

        [Test]
        public void TryStart_ReturnsFalseAndKeepsInventoryWhenInputsAreMissing()
        {
            var system = new RecipeSystem();
            var inventory = new InventoryState(8);
            inventory.TryAdd(new InventoryItem(new ItemId(1UL), "iron_ore", "Iron Ore", 1));
            inventory.TryAdd(new InventoryItem(new ItemId(2UL), "fuel", "Fuel", 1));

            var started = system.TryStart(
                CreateSmeltIronIngotRecipe(),
                CreateActiveFurnaceStore(),
                FurnaceSite,
                FurnacePosition,
                inventory,
                Worker,
                out var order);

            Assert.That(started, Is.False);
            Assert.That(order, Is.Null);
            Assert.That(Quantity(inventory, "iron_ore"), Is.EqualTo(1));
            Assert.That(Quantity(inventory, "fuel"), Is.EqualTo(1));
        }

        [Test]
        public void TryStart_ReturnsFalseForInactiveOrMissingWorksite()
        {
            var system = new RecipeSystem();
            var inactive = new WorksiteStore();
            inactive.Add(new WorksiteRecord(FurnaceSite, FurnacePosition, WorksiteKind.Furnace, isActive: false));

            Assert.That(system.TryStart(
                CreateSmeltIronIngotRecipe(),
                inactive,
                FurnaceSite,
                FurnacePosition,
                CreateSmeltingInventory(),
                Worker,
                out var inactiveOrder), Is.False);
            Assert.That(inactiveOrder, Is.Null);

            Assert.That(system.TryStart(
                CreateSmeltIronIngotRecipe(),
                new WorksiteStore(),
                FurnaceSite,
                FurnacePosition,
                CreateSmeltingInventory(),
                Worker,
                out var missingOrder), Is.False);
            Assert.That(missingOrder, Is.Null);
        }

        [Test]
        public void Tick_CompletesAfterFortyTicksAndProducesIronIngot()
        {
            var system = new RecipeSystem();
            var inventory = CreateSmeltingInventory();
            var eventLog = new WorldEventLog();
            Assert.That(system.TryStart(
                CreateSmeltIronIngotRecipe(),
                CreateActiveFurnaceStore(),
                FurnaceSite,
                FurnacePosition,
                inventory,
                Worker,
                out var order), Is.True);

            for (var i = 0; i < 39; i++)
                Assert.That(system.Tick(order, inventory, eventLog, CreateOutputItem), Is.False);

            Assert.That(order.ProgressTicks, Is.EqualTo(39));
            Assert.That(order.IsComplete, Is.False);
            Assert.That(Quantity(inventory, "iron_ingot"), Is.EqualTo(0));
            Assert.That(eventLog.Count, Is.EqualTo(0));

            Assert.That(system.Tick(order, inventory, eventLog, CreateOutputItem), Is.True);

            Assert.That(order.ProgressTicks, Is.EqualTo(40));
            Assert.That(order.IsComplete, Is.True);
            Assert.That(Quantity(inventory, "iron_ingot"), Is.EqualTo(1));
            Assert.That(system.Tick(order, inventory, eventLog, CreateOutputItem), Is.False);
            Assert.That(Quantity(inventory, "iron_ingot"), Is.EqualTo(1));
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

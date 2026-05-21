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
// atoms in docs/sprint-faz-2-atom-map.md.
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

        [Test]
        public void Tick_RejectsFactoriesThatReturnBundledOutputQuantities()
        {
            var system = new RecipeSystem();
            var inventory = CreateSmeltingInventory();
            var eventLog = new WorldEventLog();
            Assert.That(system.TryStart(
                CreateDoubleIngotRecipe(),
                CreateActiveFurnaceStore(),
                FurnaceSite,
                FurnacePosition,
                inventory,
                Worker,
                out var order), Is.True);

            for (var i = 0; i < 39; i++)
                Assert.That(system.Tick(order, inventory, eventLog, CreateBundledOutputItem), Is.False);

            var ex = Assert.Throws<InvalidOperationException>(() => system.Tick(order, inventory, eventLog, CreateBundledOutputItem));
            Assert.That(ex.Message, Does.Contain("exactly one item unit"));
        }

        [Test]
        public void Tick_WhenOutputPlacementFails_DoesNotCompleteWorkOrder()
        {
            var system = new RecipeSystem();
            var inventory = CreateSmeltingInventory(capacity: 3);
            var eventLog = new WorldEventLog();
            Assert.That(system.TryStart(
                CreateSmeltIronIngotRecipe(),
                CreateActiveFurnaceStore(),
                FurnaceSite,
                FurnacePosition,
                inventory,
                Worker,
                out var order), Is.True);

            inventory.TryAdd(new InventoryItem(new ItemId(7001UL), "junk_a", "Junk A", 1));
            inventory.TryAdd(new InventoryItem(new ItemId(7002UL), "junk_b", "Junk B", 1));
            inventory.TryAdd(new InventoryItem(new ItemId(7003UL), "junk_c", "Junk C", 1));

            for (var i = 0; i < 39; i++)
                Assert.That(system.Tick(order, inventory, eventLog, CreateOutputItem), Is.False);

            var ex = Assert.Throws<InvalidOperationException>(() => system.Tick(order, inventory, eventLog, CreateOutputItem));
            Assert.That(ex.Message, Does.Contain("cannot accept recipe output"));
            Assert.That(order.ProgressTicks, Is.EqualTo(39));
            Assert.That(order.IsComplete, Is.False);
            Assert.That(eventLog.Count, Is.EqualTo(0));

            Assert.That(inventory.TryRemoveStackable("junk_c", 1), Is.True);
            Assert.That(system.Tick(order, inventory, eventLog, CreateOutputItem), Is.True);
            Assert.That(order.ProgressTicks, Is.EqualTo(40));
            Assert.That(order.IsComplete, Is.True);
            Assert.That(Quantity(inventory, "iron_ingot"), Is.EqualTo(1));
        }

        [Test]
        public void TryStart_ConsumesOnlyStackableInputsWhenEquipmentSharesTemplate()
        {
            var system = new RecipeSystem();
            var inventory = new InventoryState(8);
            var equipmentOre = new InventoryItem(new ItemId(3001UL), "iron_ore", "Iron Ore Amulet", 1, EquipmentSlot.Weapon, 0, 0);
            inventory.TryAdd(equipmentOre);
            inventory.TryAdd(new InventoryItem(new ItemId(1UL), "iron_ore", "Iron Ore", 2));
            inventory.TryAdd(new InventoryItem(new ItemId(2UL), "fuel", "Fuel", 1));

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
            Assert.That(inventory.FindById(equipmentOre.Id), Is.Not.Null);
            Assert.That(StackableQuantity(inventory, "iron_ore"), Is.EqualTo(0));
            Assert.That(StackableQuantity(inventory, "fuel"), Is.EqualTo(0));
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

        private static RecipeDef CreateDoubleIngotRecipe()
        {
            return new RecipeDef(
                new RecipeId(1002UL),
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
                    new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 2),
                });
        }

        private static WorksiteStore CreateActiveFurnaceStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(FurnaceSite, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            return store;
        }

        private static InventoryState CreateSmeltingInventory(int capacity = 8)
        {
            var inventory = new InventoryState(capacity);
            inventory.TryAdd(new InventoryItem(new ItemId(1UL), "iron_ore", "Iron Ore", 2));
            inventory.TryAdd(new InventoryItem(new ItemId(2UL), "fuel", "Fuel", 1));
            return inventory;
        }

        private static InventoryItem CreateOutputItem(RecipeOutput output)
        {
            return new InventoryItem(new ItemId(9001UL), output.ItemTag, "Iron Ingot", 1);
        }

        private static InventoryItem CreateBundledOutputItem(RecipeOutput output)
        {
            return new InventoryItem(new ItemId(9002UL), output.ItemTag, "Iron Ingot", output.Quantity);
        }

        private static int StackableQuantity(InventoryState inventory, string templateId)
        {
            return inventory.Items.Where(item => !item.IsEquipment && string.Equals(item.TemplateId, templateId, StringComparison.Ordinal))
                .Sum(item => item.Quantity);
        }

        private static int Quantity(InventoryState inventory, string templateId)
        {
            return inventory.Items.Where(item => string.Equals(item.TemplateId, templateId, StringComparison.Ordinal))
                .Sum(item => item.Quantity);
        }
    }
}

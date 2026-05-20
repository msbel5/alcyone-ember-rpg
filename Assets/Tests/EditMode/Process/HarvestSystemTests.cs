using System;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies deterministic ripe-plant harvest into the food stockpile.</summary>
    public sealed class HarvestSystemTests
    {
        [Test]
        public void TryHarvest_ConvertsRipePlantToStockpileOutputAndClearsSoil()
        {
            var species = CreateWheat();
            var plants = CreatePlants(new PlantStageId("ripe"));
            var soils = CreateSoils();
            var stockpile = new InventoryState(4);
            var log = new WorldEventLog();
            var now = new GameTime(GameTime.MinutesPerDay * 91);

            Assert.That(new HarvestSystem().TryHarvest(
                species,
                plants,
                soils,
                new WorldComponentId(90),
                stockpile,
                log,
                now,
                CreateHarvestItem), Is.True);

            Assert.That(plants.Contains(new WorldComponentId(90)), Is.False);
            Assert.That(soils.Get(new WorldComponentId(10)).HasPlant, Is.False);
            Assert.That(Quantity(stockpile, "wheat"), Is.EqualTo(1));

            var evt = log.Events.Single();
            Assert.That(evt.Kind, Is.EqualTo(WorldEventKind.PlantHarvested));
            Assert.That(evt.Tick, Is.EqualTo(now));
            Assert.That(evt.SiteId, Is.EqualTo(new SiteId(5)));
            Assert.That(evt.ReasonTrace.Causes, Is.EqualTo(new[]
            {
                "plant_harvest",
                "site:5",
                "soil:10",
                "plant:90",
                "species:wheat",
                "item:wheat",
            }));
        }

        [Test]
        public void TryHarvest_ReturnsFalseForUnripePlantWithoutMutation()
        {
            var plants = CreatePlants(new PlantStageId("seed"));
            var soils = CreateSoils();
            var stockpile = new InventoryState(4);
            var log = new WorldEventLog();

            Assert.That(new HarvestSystem().TryHarvest(CreateWheat(), plants, soils, new WorldComponentId(90), stockpile, log, new GameTime(0), CreateHarvestItem), Is.False);
            Assert.That(plants.Contains(new WorldComponentId(90)), Is.True);
            Assert.That(soils.Get(new WorldComponentId(10)).HasPlant, Is.True);
            Assert.That(stockpile.Items.Count, Is.EqualTo(0));
            Assert.That(log.IsEmpty, Is.True);
        }

        [Test]
        public void TryHarvest_ReturnsFalseWhenStockpileCannotAcceptOutputWithoutMutation()
        {
            var plants = CreatePlants(new PlantStageId("ripe"));
            var soils = CreateSoils();
            var stockpile = new InventoryState(1);
            stockpile.TryAdd(new InventoryItem(new ItemId(9), "stone", "Stone", 1));
            var log = new WorldEventLog();

            Assert.That(new HarvestSystem().TryHarvest(CreateWheat(), plants, soils, new WorldComponentId(90), stockpile, log, new GameTime(0), CreateHarvestItem), Is.False);
            Assert.That(plants.Contains(new WorldComponentId(90)), Is.True);
            Assert.That(soils.Get(new WorldComponentId(10)).HasPlant, Is.True);
            Assert.That(Quantity(stockpile, "wheat"), Is.EqualTo(0));
            Assert.That(log.IsEmpty, Is.True);
        }

        [Test]
        public void TryHarvest_RejectsNullInputsAndBadFactoryOutput()
        {
            var system = new HarvestSystem();
            var species = CreateWheat();
            var plants = CreatePlants(new PlantStageId("ripe"));
            var soils = CreateSoils();
            var stockpile = new InventoryState(4);
            var log = new WorldEventLog();

            Assert.Throws<ArgumentNullException>(() => system.TryHarvest(null, plants, soils, new WorldComponentId(90), stockpile, log, new GameTime(0), CreateHarvestItem));
            Assert.Throws<ArgumentNullException>(() => system.TryHarvest(species, null, soils, new WorldComponentId(90), stockpile, log, new GameTime(0), CreateHarvestItem));
            Assert.Throws<ArgumentNullException>(() => system.TryHarvest(species, plants, null, new WorldComponentId(90), stockpile, log, new GameTime(0), CreateHarvestItem));
            Assert.Throws<ArgumentNullException>(() => system.TryHarvest(species, plants, soils, new WorldComponentId(90), null, log, new GameTime(0), CreateHarvestItem));
            Assert.Throws<ArgumentNullException>(() => system.TryHarvest(species, plants, soils, new WorldComponentId(90), stockpile, null, new GameTime(0), CreateHarvestItem));
            Assert.Throws<ArgumentNullException>(() => system.TryHarvest(species, plants, soils, new WorldComponentId(90), stockpile, log, new GameTime(0), null));
            Assert.Throws<ArgumentException>(() => system.TryHarvest(species, plants, soils, default, stockpile, log, new GameTime(0), CreateHarvestItem));
            Assert.Throws<InvalidOperationException>(() => system.TryHarvest(species, plants, soils, new WorldComponentId(90), stockpile, log, new GameTime(0), tag => null));
            Assert.Throws<InvalidOperationException>(() => system.TryHarvest(species, plants, soils, new WorldComponentId(90), stockpile, log, new GameTime(0), tag => new InventoryItem(new ItemId(700), "wrong", "Wrong", 1)));
        }

        private static ComponentStore<PlantComponent> CreatePlants(PlantStageId stageId)
        {
            var plants = new ComponentStore<PlantComponent>();
            plants.Add(new WorldComponentId(90), new PlantComponent(new WorldComponentId(90), new SiteId(5), new GridPosition(1, 2), "wheat", stageId, 0));
            return plants;
        }

        private static ComponentStore<SoilComponent> CreateSoils()
        {
            var soils = new ComponentStore<SoilComponent>();
            soils.Add(new WorldComponentId(10), new SoilComponent(new WorldComponentId(10), new SiteId(5), new GridPosition(1, 2), 80, 60, new WorldComponentId(90)));
            return soils;
        }

        private static InventoryItem CreateHarvestItem(string templateId)
        {
            return new InventoryItem(new ItemId(700), templateId, "Wheat", 1);
        }

        private static int Quantity(InventoryState inventory, string templateId)
        {
            return inventory.Items.Where(item => item.TemplateId == templateId).Sum(item => item.Quantity);
        }

        private static PlantSpeciesDef CreateWheat()
        {
            return new PlantSpeciesDef(
                "wheat",
                "wheat_seed",
                "wheat",
                new[]
                {
                    new PlantGrowthStageDef(new PlantStageId("seed"), "Seed", 2, false),
                    new PlantGrowthStageDef(new PlantStageId("ripe"), "Ripe Wheat", 0, true),
                },
                new[] { new PlantGrowthRule(Season.Spring, true, true) });
        }
    }
}

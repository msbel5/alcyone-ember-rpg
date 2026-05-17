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
    /// <summary>Verifies seed consumption and soil attachment for Faz 5 planting.</summary>
    public sealed class PlantingSystemTests
    {
        [Test]
        public void TryPlant_ConsumesSeedAddsPlantUpdatesSoilAndLogsEvent()
        {
            var soils = CreateSoils();
            var plants = new ComponentStore<PlantComponent>();
            var inventory = CreateInventory(seedQuantity: 2);
            var log = new WorldEventLog();
            var now = new GameTime(GameTime.MinutesPerDay);
            var plantId = new WorldComponentId(90);

            var planted = new PlantingSystem().TryPlant(
                CreateWheat(),
                soils,
                plants,
                new WorldComponentId(10),
                plantId,
                inventory,
                log,
                now);

            Assert.That(planted, Is.True);
            Assert.That(Quantity(inventory, "wheat_seed"), Is.EqualTo(1));
            Assert.That(plants.TryGet(plantId, out var plant), Is.True);
            Assert.That(plant.SpeciesId, Is.EqualTo("wheat"));
            Assert.That(plant.StageId, Is.EqualTo(new PlantStageId("seed")));
            Assert.That(soils.Get(new WorldComponentId(10)).PlantId, Is.EqualTo(plantId));

            var evt = log.Events.Single();
            Assert.That(evt.Kind, Is.EqualTo(WorldEventKind.PlantPlanted));
            Assert.That(evt.Tick, Is.EqualTo(now));
            Assert.That(evt.SiteId, Is.EqualTo(new SiteId(5)));
            Assert.That(evt.ReasonTrace.Causes, Is.EqualTo(new[]
            {
                "plant_seed",
                "site:5",
                "soil:10",
                "plant:90",
                "species:wheat",
                "stage:seed",
            }));
        }

        [Test]
        public void TryPlant_ReturnsFalseWithoutMutatingWhenSeedMissingOrSoilOccupied()
        {
            var species = CreateWheat();
            var system = new PlantingSystem();
            var soils = CreateSoils();
            var plants = new ComponentStore<PlantComponent>();
            var inventory = CreateInventory(seedQuantity: 0);
            var log = new WorldEventLog();

            Assert.That(system.TryPlant(species, soils, plants, new WorldComponentId(10), new WorldComponentId(90), inventory, log, new GameTime(0)), Is.False);
            Assert.That(plants.Count, Is.EqualTo(0));
            Assert.That(log.IsEmpty, Is.True);

            inventory = CreateInventory(seedQuantity: 1);
            var occupied = soils.Get(new WorldComponentId(10)).WithPlant(new WorldComponentId(88));
            Assert.That(soils.Replace(new WorldComponentId(10), occupied), Is.True);

            Assert.That(system.TryPlant(species, soils, plants, new WorldComponentId(10), new WorldComponentId(91), inventory, log, new GameTime(0)), Is.False);
            Assert.That(Quantity(inventory, "wheat_seed"), Is.EqualTo(1));
            Assert.That(plants.Count, Is.EqualTo(0));
            Assert.That(log.IsEmpty, Is.True);
        }

        [Test]
        public void TryPlant_RejectsNullInputs()
        {
            var system = new PlantingSystem();
            var species = CreateWheat();
            var soils = CreateSoils();
            var plants = new ComponentStore<PlantComponent>();
            var inventory = CreateInventory(1);
            var log = new WorldEventLog();

            Assert.Throws<ArgumentNullException>(() => system.TryPlant(null, soils, plants, new WorldComponentId(10), new WorldComponentId(90), inventory, log, new GameTime(0)));
            Assert.Throws<ArgumentNullException>(() => system.TryPlant(species, null, plants, new WorldComponentId(10), new WorldComponentId(90), inventory, log, new GameTime(0)));
            Assert.Throws<ArgumentNullException>(() => system.TryPlant(species, soils, null, new WorldComponentId(10), new WorldComponentId(90), inventory, log, new GameTime(0)));
            Assert.Throws<ArgumentNullException>(() => system.TryPlant(species, soils, plants, new WorldComponentId(10), new WorldComponentId(90), null, log, new GameTime(0)));
            Assert.Throws<ArgumentNullException>(() => system.TryPlant(species, soils, plants, new WorldComponentId(10), new WorldComponentId(90), inventory, null, new GameTime(0)));
            Assert.Throws<ArgumentException>(() => system.TryPlant(species, soils, plants, default, new WorldComponentId(90), inventory, log, new GameTime(0)));
            Assert.Throws<ArgumentException>(() => system.TryPlant(species, soils, plants, new WorldComponentId(10), default, inventory, log, new GameTime(0)));
        }

        private static ComponentStore<SoilComponent> CreateSoils()
        {
            var soils = new ComponentStore<SoilComponent>();
            soils.Add(new WorldComponentId(10), new SoilComponent(new WorldComponentId(10), new SiteId(5), new GridPosition(2, 3), 70, 40, default));
            return soils;
        }

        private static InventoryState CreateInventory(int seedQuantity)
        {
            var inventory = new InventoryState(4);
            if (seedQuantity > 0)
                inventory.TryAdd(new InventoryItem(new ItemId(1), "wheat_seed", "Wheat Seed", seedQuantity));
            return inventory;
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

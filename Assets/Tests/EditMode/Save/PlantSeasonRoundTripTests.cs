using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Save;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>Verifies Phase 5 plant/soil process components survive JSON DTO round-trip.</summary>
    public sealed class PlantSeasonRoundTripTests
    {
        [Test]
        public void JsonDto_RoundTripsSoilAndPlantComponents()
        {
            var world = new WorldFactory().Create(2026);
            world.Time = new GameTime(GameTime.MinutesPerDay * 91);
            var service = new JsonSliceSaveService
            {
                Soils = CreateSoils(),
                Plants = CreatePlants(),
            };

            var json = service.SaveToJson(world);
            Assert.That(json, Does.Contain("soils"));
            Assert.That(json, Does.Contain("plants"));

            var loadedWorld = service.LoadFromJson(json);
            Assert.That(loadedWorld.Time, Is.EqualTo(world.Time));

            var loadedSoil = service.Soils.Get(new WorldComponentId(10));
            Assert.That(loadedSoil.SiteId, Is.EqualTo(new SiteId(5)));
            Assert.That(loadedSoil.Position, Is.EqualTo(new GridPosition(1, 2)));
            Assert.That(loadedSoil.Fertility, Is.EqualTo(80));
            Assert.That(loadedSoil.Moisture, Is.EqualTo(60));
            Assert.That(loadedSoil.PlantId, Is.EqualTo(new WorldComponentId(90)));

            var loadedPlant = service.Plants.Get(new WorldComponentId(90));
            Assert.That(loadedPlant.SiteId, Is.EqualTo(new SiteId(5)));
            Assert.That(loadedPlant.Position, Is.EqualTo(new GridPosition(1, 2)));
            Assert.That(loadedPlant.SpeciesId, Is.EqualTo("wheat"));
            Assert.That(loadedPlant.StageId, Is.EqualTo(new PlantStageId("ripe")));
            Assert.That(loadedPlant.DaysInStage, Is.EqualTo(1));
            Assert.That(service.Plants.Rows.Select(row => row.Key), Is.EqualTo(new[] { new WorldComponentId(90) }));
        }

        private static ComponentStore<SoilComponent> CreateSoils()
        {
            var soils = new ComponentStore<SoilComponent>();
            soils.Add(new WorldComponentId(10), new SoilComponent(new WorldComponentId(10), new SiteId(5), new GridPosition(1, 2), 80, 60, new WorldComponentId(90)));
            return soils;
        }

        private static ComponentStore<PlantComponent> CreatePlants()
        {
            var plants = new ComponentStore<PlantComponent>();
            plants.Add(new WorldComponentId(90), new PlantComponent(new WorldComponentId(90), new SiteId(5), new GridPosition(1, 2), "wheat", new PlantStageId("ripe"), 1));
            return plants;
        }
    }
}

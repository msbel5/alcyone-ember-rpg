using System;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies deterministic season/weather plant growth for Faz 5.</summary>
    public sealed class PlantGrowthSystemTests
    {
        [Test]
        public void AdvanceOneDay_IncrementsAgeUntilStageBoundaryThenLogsAdvance()
        {
            var species = CreateWheat();
            var plants = CreatePlants(daysInStage: 0);
            var log = new WorldEventLog();
            var system = new PlantGrowthSystem();
            var now = new GameTime(GameTime.MinutesPerDay * 40);

            Assert.That(system.AdvanceOneDay(species, plants, log, now, Season.Spring, isSnowing: false), Is.EqualTo(0));
            Assert.That(plants.Get(new WorldComponentId(90)).DaysInStage, Is.EqualTo(1));
            Assert.That(log.IsEmpty, Is.True);

            Assert.That(system.AdvanceOneDay(species, plants, log, now.AddDays(1), Season.Spring, isSnowing: false), Is.EqualTo(1));

            var plant = plants.Get(new WorldComponentId(90));
            Assert.That(plant.StageId, Is.EqualTo(new PlantStageId("ripe")));
            Assert.That(plant.DaysInStage, Is.EqualTo(0));

            var evt = log.Events.Single();
            Assert.That(evt.Kind, Is.EqualTo(WorldEventKind.PlantStageAdvanced));
            Assert.That(evt.Tick, Is.EqualTo(now.AddDays(1)));
            Assert.That(evt.SiteId, Is.EqualTo(new SiteId(5)));
            Assert.That(evt.ReasonTrace.Causes, Is.EqualTo(new[]
            {
                "plant_growth",
                "site:5",
                "plant:90",
                "species:wheat",
                "from:seed",
                "to:ripe",
            }));
        }

        [Test]
        public void AdvanceOneDay_BlocksGrowthWhenSeasonOrSnowRuleDisallowsIt()
        {
            var species = CreateWheat();
            var plants = CreatePlants(daysInStage: 1);
            var log = new WorldEventLog();
            var system = new PlantGrowthSystem();

            Assert.That(system.AdvanceOneDay(species, plants, log, new GameTime(0), Season.Winter, isSnowing: false), Is.EqualTo(0));
            Assert.That(plants.Get(new WorldComponentId(90)).DaysInStage, Is.EqualTo(1));

            Assert.That(system.AdvanceOneDay(species, plants, log, new GameTime(0), Season.Spring, isSnowing: true), Is.EqualTo(0));
            Assert.That(plants.Get(new WorldComponentId(90)).DaysInStage, Is.EqualTo(1));
            Assert.That(log.IsEmpty, Is.True);
        }

        [Test]
        public void AdvanceOneDay_SkipsOtherSpeciesAndHarvestableFinalStage()
        {
            var species = CreateWheat();
            var plants = new ComponentStore<PlantComponent>();
            plants.Add(new WorldComponentId(90), new PlantComponent(new WorldComponentId(90), new SiteId(5), new GridPosition(1, 2), "wheat", new PlantStageId("ripe"), 0));
            plants.Add(new WorldComponentId(91), new PlantComponent(new WorldComponentId(91), new SiteId(5), new GridPosition(2, 2), "barley", new PlantStageId("seed"), 1));
            var log = new WorldEventLog();

            Assert.That(new PlantGrowthSystem().AdvanceOneDay(species, plants, log, new GameTime(0), Season.Spring, isSnowing: false), Is.EqualTo(0));
            Assert.That(plants.Get(new WorldComponentId(90)).StageId, Is.EqualTo(new PlantStageId("ripe")));
            Assert.That(plants.Get(new WorldComponentId(91)).DaysInStage, Is.EqualTo(1));
            Assert.That(log.IsEmpty, Is.True);
        }

        [Test]
        public void AdvanceOneDay_RejectsNullInputsAndUnknownStage()
        {
            var system = new PlantGrowthSystem();
            var species = CreateWheat();
            var plants = CreatePlants(daysInStage: 0);
            var log = new WorldEventLog();

            Assert.Throws<ArgumentNullException>(() => system.AdvanceOneDay(null, plants, log, new GameTime(0), Season.Spring, false));
            Assert.Throws<ArgumentNullException>(() => system.AdvanceOneDay(species, null, log, new GameTime(0), Season.Spring, false));
            Assert.Throws<ArgumentNullException>(() => system.AdvanceOneDay(species, plants, null, new GameTime(0), Season.Spring, false));

            plants = new ComponentStore<PlantComponent>();
            plants.Add(new WorldComponentId(90), new PlantComponent(new WorldComponentId(90), new SiteId(5), new GridPosition(1, 2), "wheat", new PlantStageId("broken"), 0));
            Assert.Throws<InvalidOperationException>(() => system.AdvanceOneDay(species, plants, log, new GameTime(0), Season.Spring, false));
        }

        private static ComponentStore<PlantComponent> CreatePlants(int daysInStage)
        {
            var plants = new ComponentStore<PlantComponent>();
            plants.Add(new WorldComponentId(90), new PlantComponent(new WorldComponentId(90), new SiteId(5), new GridPosition(1, 2), "wheat", new PlantStageId("seed"), daysInStage));
            return plants;
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

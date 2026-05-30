using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies the Phase 5 plant component primitive.</summary>
    public sealed class PlantComponentTests
    {
        [Test]
        public void Constructor_StoresPlantState()
        {
            var plant = CreatePlant();

            Assert.That(plant.Id, Is.EqualTo(new WorldComponentId(11)));
            Assert.That(plant.SiteId, Is.EqualTo(new SiteId(2)));
            Assert.That(plant.Position, Is.EqualTo(new GridPosition(3, 4)));
            Assert.That(plant.SpeciesId, Is.EqualTo("wheat"));
            Assert.That(plant.StageId, Is.EqualTo(new PlantStageId("seed")));
            Assert.That(plant.DaysInStage, Is.EqualTo(0));
        }

        [Test]
        public void WithStage_ChangesStageAndResetsAge()
        {
            var plant = CreatePlant().WithDaysInStage(2);

            var ripe = plant.WithStage(new PlantStageId("ripe"));

            Assert.That(ripe.StageId, Is.EqualTo(new PlantStageId("ripe")));
            Assert.That(ripe.DaysInStage, Is.EqualTo(0));
            Assert.That(ripe.SpeciesId, Is.EqualTo("wheat"));
        }

        [Test]
        public void Constructor_RejectsInvalidValues()
        {
            Assert.Throws<ArgumentException>(() => new PlantComponent(default, new SiteId(1), new GridPosition(0, 0), "wheat", new PlantStageId("seed"), 0));
            Assert.Throws<ArgumentException>(() => new PlantComponent(new WorldComponentId(1), default, new GridPosition(0, 0), "wheat", new PlantStageId("seed"), 0));
            Assert.Throws<ArgumentException>(() => new PlantComponent(new WorldComponentId(1), new SiteId(1), new GridPosition(0, 0), " ", new PlantStageId("seed"), 0));
            Assert.Throws<ArgumentException>(() => new PlantComponent(new WorldComponentId(1), new SiteId(1), new GridPosition(0, 0), "wheat", default, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlantComponent(new WorldComponentId(1), new SiteId(1), new GridPosition(0, 0), "wheat", new PlantStageId("seed"), -1));
        }

        private static PlantComponent CreatePlant()
        {
            return new PlantComponent(
                new WorldComponentId(11),
                new SiteId(2),
                new GridPosition(3, 4),
                "wheat",
                new PlantStageId("seed"),
                0);
        }
    }
}

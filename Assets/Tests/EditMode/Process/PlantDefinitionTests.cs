using System;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies Phase 5 data-driven plant definitions.</summary>
    public sealed class PlantDefinitionTests
    {
        [Test]
        public void WheatDefinition_ProvidesOrderedStagesAndItemTags()
        {
            var wheat = CreateWheat();

            Assert.That(wheat.SpeciesId, Is.EqualTo("wheat"));
            Assert.That(wheat.SeedItemTag, Is.EqualTo("wheat_seed"));
            Assert.That(wheat.HarvestItemTag, Is.EqualTo("wheat"));
            Assert.That(wheat.FirstStage.Id, Is.EqualTo(new PlantStageId("seed")));
            Assert.That(wheat.TryGetNextStage(new PlantStageId("seed"), out var sprout), Is.True);
            Assert.That(sprout.Id, Is.EqualTo(new PlantStageId("sprout")));
            Assert.That(wheat.TryGetStage(new PlantStageId("ripe"), out var ripe), Is.True);
            Assert.That(ripe.IsHarvestable, Is.True);
        }

        [Test]
        public void GrowthRules_AllowSpringAndSummerButSnowBlocks()
        {
            var wheat = CreateWheat();

            Assert.That(wheat.CanGrow(Season.Spring, isSnowing: false), Is.True);
            Assert.That(wheat.CanGrow(Season.Summer, isSnowing: false), Is.True);
            Assert.That(wheat.CanGrow(Season.Spring, isSnowing: true), Is.False);
            Assert.That(wheat.CanGrow(Season.Winter, isSnowing: false), Is.False);
        }

        [Test]
        public void Constructor_RejectsInvalidRows()
        {
            Assert.Throws<ArgumentException>(() => new PlantStageId(" "));
            Assert.Throws<ArgumentException>(() => new PlantGrowthStageDef(default, "seed", 1, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlantGrowthStageDef(new PlantStageId("seed"), "seed", -1, false));
            Assert.Throws<ArgumentException>(() => new PlantSpeciesDef("wheat", "seed", "wheat", new PlantGrowthStageDef[0], new[] { new PlantGrowthRule(Season.Spring, true, true) }));
            Assert.Throws<ArgumentException>(() => new PlantSpeciesDef("wheat", "seed", "wheat", new[] { Stage("seed", false), Stage("seed", true) }, new[] { new PlantGrowthRule(Season.Spring, true, true) }));
            Assert.Throws<ArgumentException>(() => new PlantSpeciesDef("wheat", "seed", "wheat", new[] { Stage("seed", false) }, new PlantGrowthRule[0]));
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
                    new PlantGrowthStageDef(new PlantStageId("sprout"), "Sprout", 3, false),
                    new PlantGrowthStageDef(new PlantStageId("ripe"), "Ripe Wheat", 0, true),
                },
                new[]
                {
                    new PlantGrowthRule(Season.Spring, true, true),
                    new PlantGrowthRule(Season.Summer, true, true),
                    new PlantGrowthRule(Season.Autumn, false, true),
                    new PlantGrowthRule(Season.Winter, false, true),
                });
        }

        private static PlantGrowthStageDef Stage(string id, bool harvestable)
        {
            return new PlantGrowthStageDef(new PlantStageId(id), id, 1, harvestable);
        }
    }
}

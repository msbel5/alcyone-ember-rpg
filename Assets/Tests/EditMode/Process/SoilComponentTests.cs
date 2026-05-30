using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies the Phase 5 soil component primitive.</summary>
    public sealed class SoilComponentTests
    {
        [Test]
        public void Constructor_StoresSitePositionAndClampedSoilValues()
        {
            var soil = new SoilComponent(
                new WorldComponentId(1),
                new SiteId(2),
                new GridPosition(3, 4),
                120,
                -5,
                default);

            Assert.That(soil.Id, Is.EqualTo(new WorldComponentId(1)));
            Assert.That(soil.SiteId, Is.EqualTo(new SiteId(2)));
            Assert.That(soil.Position, Is.EqualTo(new GridPosition(3, 4)));
            Assert.That(soil.Fertility, Is.EqualTo(100));
            Assert.That(soil.Moisture, Is.EqualTo(0));
            Assert.That(soil.HasPlant, Is.False);
        }

        [Test]
        public void WithPlantAndWithoutPlant_UpdatePlantHandleImmutably()
        {
            var soil = CreateSoil();
            var planted = soil.WithPlant(new WorldComponentId(99));
            var cleared = planted.WithoutPlant();

            Assert.That(soil.HasPlant, Is.False);
            Assert.That(planted.HasPlant, Is.True);
            Assert.That(planted.PlantId, Is.EqualTo(new WorldComponentId(99)));
            Assert.That(cleared.HasPlant, Is.False);
            Assert.That(cleared.Position, Is.EqualTo(soil.Position));
        }

        [Test]
        public void Constructor_RejectsEmptyIdentityOrSite()
        {
            Assert.Throws<ArgumentException>(() => new SoilComponent(default, new SiteId(1), new GridPosition(0, 0), 50, 50, default));
            Assert.Throws<ArgumentException>(() => new SoilComponent(new WorldComponentId(1), default, new GridPosition(0, 0), 50, 50, default));
            Assert.Throws<ArgumentException>(() => CreateSoil().WithPlant(default));
        }

        private static SoilComponent CreateSoil()
        {
            return new SoilComponent(
                new WorldComponentId(1),
                new SiteId(2),
                new GridPosition(3, 4),
                60,
                70,
                default);
        }
    }
}

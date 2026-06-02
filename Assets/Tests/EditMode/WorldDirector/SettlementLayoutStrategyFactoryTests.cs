using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.WorldDirector;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.WorldDirector
{
    /// <summary>Locks the kind → strategy mapping (Factory) so a new settlement kind cannot silently fall
    /// through to the wrong shape, and unknown kinds degrade gracefully to the village ring.</summary>
    public sealed class SettlementLayoutStrategyFactoryTests
    {
        [Test]
        public void For_PopulousKinds_UseVillageStrategy()
        {
            Assert.That(SettlementLayoutStrategyFactory.For(SettlementKind.City), Is.TypeOf<VillageLayoutStrategy>());
            Assert.That(SettlementLayoutStrategyFactory.For(SettlementKind.Town), Is.TypeOf<VillageLayoutStrategy>());
            Assert.That(SettlementLayoutStrategyFactory.For(SettlementKind.Village), Is.TypeOf<VillageLayoutStrategy>());
        }

        [Test]
        public void For_SmallKinds_UseCompactStrategy()
        {
            Assert.That(SettlementLayoutStrategyFactory.For(SettlementKind.Hamlet), Is.TypeOf<CompactLayoutStrategy>());
            Assert.That(SettlementLayoutStrategyFactory.For(SettlementKind.Inn), Is.TypeOf<CompactLayoutStrategy>());
            Assert.That(SettlementLayoutStrategyFactory.For(SettlementKind.Shrine), Is.TypeOf<CompactLayoutStrategy>());
            Assert.That(SettlementLayoutStrategyFactory.For(SettlementKind.Dungeon), Is.TypeOf<CompactLayoutStrategy>());
        }

        [Test]
        public void For_UnknownKind_DefaultsToVillage()
        {
            Assert.That(SettlementLayoutStrategyFactory.For((SettlementKind)999), Is.TypeOf<VillageLayoutStrategy>());
        }
    }
}

using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.WorldDirector;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.WorldDirector
{
    /// <summary>SettlementLayoutGraph v1 guarantees: deterministic streets, clear plaza, no overlaps.</summary>
    public sealed class StreetLayoutStrategyTests
    {
        private static SettlementLayout Plan(SettlementKind kind, uint seed)
            => new StreetLayoutStrategy().Plan(new SettlementContext("T", kind, BiomeKind.Plains, seed));

        [Test]
        public void SameSeed_ProducesIdenticalLayout()
        {
            var a = Plan(SettlementKind.Town, 1234u);
            var b = Plan(SettlementKind.Town, 1234u);
            Assert.That(a.Buildings.Count, Is.EqualTo(b.Buildings.Count));
            for (int i = 0; i < a.Buildings.Count; i++)
            {
                Assert.That(a.Buildings[i].OriginX, Is.EqualTo(b.Buildings[i].OriginX));
                Assert.That(a.Buildings[i].OriginZ, Is.EqualTo(b.Buildings[i].OriginZ));
            }
        }

        [Test]
        public void City_HasMoreBuildings_ThanTown_ForSameSeed()
        {
            Assert.That(Plan(SettlementKind.City, 77u).Buildings.Count,
                Is.GreaterThan(Plan(SettlementKind.Town, 77u).Buildings.Count));
        }

        [Test]
        public void Plaza_StaysClear_AndNoBuildingPairOverlaps()
        {
            var layout = Plan(SettlementKind.City, 99u);
            var b = layout.Buildings;
            Assert.That(b.Count, Is.GreaterThan(10), "a city must actually have streets of buildings");
            for (int i = 0; i < b.Count; i++)
            {
                double half = System.Math.Sqrt((b[i].SizeX * b[i].SizeX) + (b[i].SizeZ * b[i].SizeZ)) * 0.5d;
                double dist = System.Math.Sqrt((b[i].OriginX * b[i].OriginX) + (b[i].OriginZ * b[i].OriginZ));
                Assert.That(dist - half, Is.GreaterThan(7.9d), $"building {i} intrudes into the plaza");

                for (int j = i + 1; j < b.Count; j++)
                {
                    bool apart = System.Math.Abs(b[i].OriginX - b[j].OriginX) >= ((b[i].SizeX + b[j].SizeX) * 0.5f)
                              || System.Math.Abs(b[i].OriginZ - b[j].OriginZ) >= ((b[i].SizeZ + b[j].SizeZ) * 0.5f);
                    Assert.That(apart, Is.True, $"buildings {i} and {j} overlap");
                }
            }
        }
    }
}

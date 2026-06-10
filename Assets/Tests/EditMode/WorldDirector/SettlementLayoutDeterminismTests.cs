using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.WorldDirector;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.WorldDirector
{
    /// <summary>
    /// Locks the core invariant of the procedural World Scene Director: the layout is a PURE function of the
    /// settlement seed, so the same world always rebuilds the identical town (and different worlds differ).
    /// This is the headless guarantee behind "New Game generates a deterministic place you stand in".
    /// </summary>
    public sealed class SettlementLayoutDeterminismTests
    {
        [Test]
        public void Plan_SameSeed_ProducesIdenticalLayout()
        {
            var ctx = new SettlementContext("Testburg", SettlementKind.Town, BiomeKind.Plains, 12345u);
            var strategy = SettlementLayoutStrategyFactory.For(ctx.Kind);

            var a = strategy.Plan(ctx);
            var b = strategy.Plan(ctx);

            Assert.That(b.Buildings.Count, Is.EqualTo(a.Buildings.Count));
            Assert.That(b.GroundRadius, Is.EqualTo(a.GroundRadius));
            for (int i = 0; i < a.Buildings.Count; i++)
            {
                Assert.That(b.Buildings[i].OriginX, Is.EqualTo(a.Buildings[i].OriginX), "OriginX[" + i + "]");
                Assert.That(b.Buildings[i].OriginZ, Is.EqualTo(a.Buildings[i].OriginZ), "OriginZ[" + i + "]");
                Assert.That(b.Buildings[i].SizeX, Is.EqualTo(a.Buildings[i].SizeX), "SizeX[" + i + "]");
                Assert.That(b.Buildings[i].Height, Is.EqualTo(a.Buildings[i].Height), "Height[" + i + "]");
                Assert.That(b.Buildings[i].MaterialIndex, Is.EqualTo(a.Buildings[i].MaterialIndex), "Material[" + i + "]");
            }
        }

        [Test]
        public void Plan_DifferentSeed_ProducesDifferentLayout()
        {
            var strategy = new VillageLayoutStrategy();
            var a = strategy.Plan(new SettlementContext("A", SettlementKind.Village, BiomeKind.Forest, 1u));
            var b = strategy.Plan(new SettlementContext("B", SettlementKind.Village, BiomeKind.Forest, 999u));

            bool differs = a.Buildings.Count != b.Buildings.Count;
            if (!differs)
            {
                for (int i = 0; i < a.Buildings.Count; i++)
                {
                    if (a.Buildings[i].OriginX != b.Buildings[i].OriginX) { differs = true; break; }
                }
            }
            Assert.That(differs, Is.True, "Different seeds must yield a different layout.");
        }

        [Test]
        public void Plan_BuildingsSitWithinTheGroundPlane()
        {
            var strategy = new VillageLayoutStrategy();
            var layout = strategy.Plan(new SettlementContext("Cityburg", SettlementKind.City, BiomeKind.Coast, 42u));

            Assert.That(layout.Buildings.Count, Is.GreaterThanOrEqualTo(1));
            foreach (var b in layout.Buildings)
            {
                Assert.That(System.Math.Abs(b.OriginX), Is.LessThan(layout.GroundRadius));
                Assert.That(System.Math.Abs(b.OriginZ), Is.LessThan(layout.GroundRadius));
                Assert.That(b.SizeX, Is.GreaterThan(0f));
                Assert.That(b.SizeZ, Is.GreaterThan(0f));
                Assert.That(b.Height, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void Plan_BuildingsKeepStreetClearance()
        {
            var strategy = new VillageLayoutStrategy();
            var layout = strategy.Plan(new SettlementContext("Walkable", SettlementKind.Town, BiomeKind.Plains, 777u));

            for (int i = 0; i < layout.Buildings.Count; i++)
            {
                for (int j = i + 1; j < layout.Buildings.Count; j++)
                {
                    Assert.That(HasStreetGap(layout.Buildings[i], layout.Buildings[j]), Is.True, "Buildings " + i + " and " + j + " block each other.");
                }
            }
        }

        private static bool HasStreetGap(BuildingPlacement a, BuildingPlacement b)
        {
            const float expectedClearance = 3.75f;
            float minGapX = ((a.SizeX + b.SizeX) * 0.5f) + expectedClearance;
            float minGapZ = ((a.SizeZ + b.SizeZ) * 0.5f) + expectedClearance;
            return System.Math.Abs(a.OriginX - b.OriginX) >= minGapX
                || System.Math.Abs(a.OriginZ - b.OriginZ) >= minGapZ;
        }
    }
}

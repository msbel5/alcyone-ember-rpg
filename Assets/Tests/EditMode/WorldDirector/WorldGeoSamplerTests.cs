using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Overland;
using EmberCrpg.Simulation.WorldDirector;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.WorldDirector
{
    /// <summary>
    /// Headless guarantee behind "the terrain you walk IS the map": one canonical metres↔tile projection
    /// (scale comes from the Domain, not a presentation constant) and a deterministic, continuous geography
    /// sampler bound to the same WorldGeography snapshot the atlas image renders from.
    /// </summary>
    public sealed class WorldGeoSamplerTests
    {
        private static OverlandMap Map() => OverlandWorldgen.Generate(42u, OverlandParameters.Default);

        private static GridPosition HomeLandTile(OverlandMap map)
        {
            var settlements = map.Settlements;
            return settlements.Count > 0
                ? settlements[0].TilePosition
                : new GridPosition(map.Width / 2, map.Height / 2);
        }

        [Test]
        public void Projection_ScaleComesFromDomain_AndNorthShrinksTileY()
        {
            Assert.That(WorldSpaceProjection.MetersPerTile,
                Is.EqualTo(OverlandParameters.DefaultRegionEdgeKm * 1000d).Within(1e-9));
            Assert.That(WorldSpaceProjection.TileFracX(3, 0d), Is.EqualTo(3.5d).Within(1e-9));
            Assert.That(WorldSpaceProjection.TileFracX(3, WorldSpaceProjection.MetersPerTile), Is.EqualTo(4.5d).Within(1e-9));
            // +Z is north; atlas row 0 is the northern edge, so tile Y must SHRINK as the player walks north.
            Assert.That(WorldSpaceProjection.TileFracY(3, WorldSpaceProjection.MetersPerTile), Is.EqualTo(2.5d).Within(1e-9));
        }

        [Test]
        public void TryCreate_FailsWithoutMap_AndSucceedsForGeneratedWorld()
        {
            Assert.That(WorldGeoSampler.TryCreate(null, new GridPosition(0, 0), 42u, out _), Is.False);

            var map = Map();
            Assert.That(WorldGeoSampler.TryCreate(map, HomeLandTile(map), 42u, out var sampler), Is.True);
            Assert.That(sampler, Is.Not.Null);
        }

        [Test]
        public void Sampling_IsDeterministic_ForSameSeedAndPosition()
        {
            var map = Map();
            var home = HomeLandTile(map);
            WorldGeoSampler.TryCreate(map, home, 42u, out var a);
            WorldGeoSampler.TryCreate(map, home, 42u, out var b);

            for (int i = 0; i < 50; i++)
            {
                double x = (i * 137.31d) - 3000d, z = (i * 89.7d) - 2000d;
                Assert.That(a.Sample(x, z).ElevationMeters, Is.EqualTo(b.Sample(x, z).ElevationMeters).Within(1e-9));
            }
        }

        [Test]
        public void HomeSettlementPad_IsFlatAtGroundZero_AndDry()
        {
            var map = Map();
            WorldGeoSampler.TryCreate(map, HomeLandTile(map), 42u, out var sampler);

            foreach (var (x, z) in new[] { (0d, 0d), (25d, 10d), (-40d, 30d) })
            {
                var s = sampler.Sample(x, z);
                Assert.That(s.ElevationMeters, Is.EqualTo(0d).Within(0.001d), $"pad must be flat at ({x},{z})");
                Assert.That(s.IsWater, Is.False, "the home settlement can never start underwater");
            }
        }

        [Test]
        public void Elevation_IsContinuous_OneMetreStepsStaySmall()
        {
            var map = Map();
            WorldGeoSampler.TryCreate(map, HomeLandTile(map), 42u, out var sampler);

            double prev = sampler.Sample(500d, 800d).ElevationMeters;
            for (int i = 1; i <= 400; i++)
            {
                double cur = sampler.Sample(500d + i, 800d).ElevationMeters;
                Assert.That(System.Math.Abs(cur - prev), Is.LessThan(3d), $"height jump at metre step {i}");
                prev = cur;
            }
        }

        [Test]
        public void WaterFlag_MatchesElevationAgainstSeaLevel_Everywhere()
        {
            var map = Map();
            WorldGeoSampler.TryCreate(map, HomeLandTile(map), 42u, out var sampler);

            for (int i = 0; i < 200; i++)
            {
                double x = (i * 911.7d) - 90000d, z = (i * 533.3d) - 50000d;
                var s = sampler.Sample(x, z);
                Assert.That(s.IsWater, Is.EqualTo(s.ElevationMeters < sampler.SeaLevelMeters));
                if (s.ElevationMeters <= sampler.SeaLevelMeters)
                    Assert.That(s.SandBlend01, Is.EqualTo(1d).Within(1e-9), "seabed must read as sand");
            }
        }
    }
}

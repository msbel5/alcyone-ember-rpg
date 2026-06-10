using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Overland;
using EmberCrpg.Simulation.WorldDirector;
using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Simulation.Worldgen.Planet;
using EmberCrpg.Simulation.Worldgen.PlanetIntegration;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.WorldDirector
{
    /// <summary>
    /// "kıyıda olmalıydı, yine su göremedim" guard: a settlement whose planet tile has water within ~3 tiles
    /// must realize a WALKABLE waterline inside the streamed bubble (640m), while its plaza stays dry.
    /// Runs the LIVE-SHAPED planet so the guarantee covers the world the player actually gets.
    /// </summary>
    public sealed class WorldGeoSamplerShoreTests
    {
        // F1-DoD: the guarantee must hold on MORE THAN the shipped seed — three live-shaped planets.
        [TestCase(42u)]
        [TestCase(7u)]
        [TestCase(1337u)]
        public void CoastalSettlements_RealizeWalkableWater_WithinTheStreamedBubble(uint seed)
        {
            var map = GenerateLiveShapedMap(seed);
            Assert.That(map.Settlements.Count, Is.GreaterThan(0));

            int shoreSettlements = 0;
            for (int i = 0; i < map.Settlements.Count; i++)
            {
                var s = map.Settlements[i];
                Assert.That(WorldGeoSampler.TryCreate(map, s.TilePosition, seed, out var sampler), Is.True);
                Assert.That(sampler.Sample(0d, 0d).IsWater, Is.False, s.Name + " plaza must stay dry");
                if (!sampler.HasLocalShore) continue;
                shoreSettlements++;

                // March 24 bearings at 360..640m: the shore ramp ends at ~560m, so water MUST appear
                // somewhere inside the streamed bubble for every settlement that claims a local shore.
                bool foundWater = false;
                for (int step = 0; step <= 8 && !foundWater; step++)
                {
                    double r = 360d + (step * 35d);
                    for (int b = 0; b < 24 && !foundWater; b++)
                    {
                        double a = (System.Math.PI * 2d * b) / 24d;
                        foundWater = sampler.Sample(System.Math.Cos(a) * r, System.Math.Sin(a) * r).IsWater;
                    }
                }

                Assert.That(foundWater, Is.True, s.Name + " claims a local shore but no water within 640m");
            }

            Assert.That(shoreSettlements, Is.GreaterThan(0),
                "a 0.62-ocean planet must yield at least one settlement with a realizable shore");
        }

        // F1-DoD: the full worldgen chain (planet → world → overland) is deterministic per seed — the same
        // seed twice yields identical settlement rosters (count, names, tiles, kinds).
        [TestCase(42u)]
        [TestCase(7u)]
        [TestCase(1337u)]
        public void WorldgenChain_SameSeed_YieldsIdenticalSettlements(uint seed)
        {
            var a = GenerateLiveShapedMap(seed);
            var b = GenerateLiveShapedMap(seed);
            Assert.That(b.Settlements.Count, Is.EqualTo(a.Settlements.Count), "settlement count must be deterministic");
            for (int i = 0; i < a.Settlements.Count; i++)
            {
                Assert.That(b.Settlements[i].Name, Is.EqualTo(a.Settlements[i].Name));
                Assert.That(b.Settlements[i].Kind, Is.EqualTo(a.Settlements[i].Kind));
                Assert.That(b.Settlements[i].TilePosition.X, Is.EqualTo(a.Settlements[i].TilePosition.X));
                Assert.That(b.Settlements[i].TilePosition.Y, Is.EqualTo(a.Settlements[i].TilePosition.Y));
            }
        }

        private static OverlandMap GenerateLiveShapedMap(uint seed)
        {
            var field = PlanetGenerator.Generate(seed, new PlanetParameters(4, 32, 0.62d, 0d, 0.04d));
            var world = PlanetToWorldMapper.Map(field, WorldgenParameters.Default);
            return OverlandWorldgen.Generate(world, new OverlandParameters(PlanetToWorldMapper.GeographyWidth, PlanetToWorldMapper.GeographyHeight));
        }
    }
}

using System.Text;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Overland;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Overland
{
    /// <summary>Determinism and topology checks for the engine-free overland world layer.</summary>
    public sealed class OverlandWorldgenTests
    {
        [Test]
        public void Generate_SameSeed_ProducesIdenticalMap()
        {
            var parameters = OverlandParameters.Default;
            var mapA = OverlandWorldgen.Generate(42u, parameters);
            var mapB = OverlandWorldgen.Generate(42u, parameters);

            Assert.That(Snapshot(mapA), Is.EqualTo(Snapshot(mapB)));
        }

        [Test]
        public void Generate_SameSeed_ProducesIdenticalBiomeGrid()
        {
            var parameters = OverlandParameters.Default;
            var mapA = OverlandWorldgen.Generate(4242u, parameters);
            var mapB = OverlandWorldgen.Generate(4242u, parameters);

            Assert.That(BiomeSnapshot(mapA), Is.EqualTo(BiomeSnapshot(mapB)));
        }

        [Test]
        public void Generate_DifferentSeed_ProducesDifferentContinent()
        {
            var parameters = OverlandParameters.Default;
            var manager = new WorldGenerationManager();
            var continentA = manager.Generate(42u, parameters.Width, parameters.Height);
            var continentB = manager.Generate(43u, parameters.Width, parameters.Height);

            Assert.That(FieldSnapshot(continentA), Is.Not.EqualTo(FieldSnapshot(continentB)));
        }

        [Test]
        public void Generate_ContinentalFields_HaveSaneLandFraction()
        {
            var parameters = OverlandParameters.Default;
            var continent = new WorldGenerationManager().Generate(42u, parameters.Width, parameters.Height);
            double landFraction = CountLand(continent) / (double)(parameters.Width * parameters.Height);

            Assert.That(landFraction, Is.InRange(0.35d, 0.75d));
        }

        [Test]
        public void Generate_AssignsValidBiomes_AndSmoothsSingleTileIslands()
        {
            var map = OverlandWorldgen.Generate(42u, OverlandParameters.Default);

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var tile = map.TileAt(x, y);
                    Assert.That(System.Enum.IsDefined(typeof(BiomeKind), tile.Biome), Is.True);
                    Assert.That(CountMatchingNeighbors(map, x, y, tile.Biome), Is.GreaterThan(0),
                        $"Tile ({x},{y}) remained a single-tile {tile.Biome} island.");
                }
            }
        }

        [Test]
        public void Generate_PlacesSettlementsInBounds_WithinDensityBand_AndWithoutOrphans()
        {
            var parameters = OverlandParameters.Default;
            var map = OverlandWorldgen.Generate(42u, parameters);
            var continent = new WorldGenerationManager().Generate(42u, parameters.Width, parameters.Height);
            int capacity = EstimateSettlementCapacity(map, parameters, continent);

            Assert.That(map.Settlements.Count, Is.GreaterThanOrEqualTo(12));
            Assert.That(map.Settlements.Count, Is.LessThanOrEqualTo(capacity));

            for (int i = 0; i < map.Settlements.Count; i++)
            {
                var settlement = map.Settlements[i];
                Assert.That(settlement.TilePosition.X, Is.InRange(0, map.Width - 1));
                Assert.That(settlement.TilePosition.Y, Is.InRange(0, map.Height - 1));
                Assert.That(continent.IsLandAt(settlement.TilePosition.X, settlement.TilePosition.Y), Is.True,
                    $"Settlement {settlement.Name} spawned in ocean at {settlement.TilePosition}.");

                if (map.Settlements.Count > 1)
                {
                    int nearestOther = NearestOtherDistance(map, i);
                    Assert.That(nearestOther, Is.LessThanOrEqualTo(10),
                        $"Settlement {settlement.Name} is too isolated at {settlement.TilePosition}.");
                }
            }
        }

        [Test]
        public void MapHelpers_ReturnSaneDistancesAndNearestSettlement()
        {
            var map = OverlandWorldgen.Generate(42u, OverlandParameters.Default);
            Assert.That(map.Settlements.Count, Is.GreaterThanOrEqualTo(2));

            var first = map.Settlements[0];
            var second = map.Settlements[1];

            int expectedDistance = OverlandMap.ChebyshevDistance(first.TilePosition, second.TilePosition);
            Assert.That(map.DistanceBetween(first.Id, second.Id), Is.EqualTo(expectedDistance));

            var probe = new GridPosition(first.TilePosition.X, first.TilePosition.Y);
            var found = map.TryGetNearestSettlement(probe, out var nearest, out var distance);

            Assert.That(found, Is.True);
            Assert.That(nearest.Id, Is.EqualTo(first.Id));
            Assert.That(distance, Is.EqualTo(0));
        }

        private static string Snapshot(OverlandMap map)
        {
            var builder = new StringBuilder();
            builder.Append(map.Width).Append('x').Append(map.Height).Append('|');
            for (int i = 0; i < map.Tiles.Count; i++)
            {
                var tile = map.Tiles[i];
                builder.Append(tile.X).Append(',').Append(tile.Y).Append(':').Append(tile.RegionId.Value).Append(':').Append((int)tile.Biome).Append(':').Append(tile.PropVariationSeed).Append('[');
                for (int settlementIndex = 0; settlementIndex < tile.SettlementIds.Count; settlementIndex++)
                    builder.Append(tile.SettlementIds[settlementIndex].Value).Append(',');
                builder.Append(']');
            }

            builder.Append('|');
            for (int i = 0; i < map.Settlements.Count; i++)
            {
                var settlement = map.Settlements[i];
                builder.Append(settlement.Id.Value).Append(':').Append((int)settlement.Kind).Append(':').Append(settlement.TilePosition.X).Append(',').Append(settlement.TilePosition.Y).Append(':').Append(settlement.TemplatePackTag).Append(':').Append(settlement.Name).Append('|');
            }

            return builder.ToString();
        }

        private static string BiomeSnapshot(OverlandMap map)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < map.Tiles.Count; i++)
                builder.Append((int)map.Tiles[i].Biome).Append(',');
            return builder.ToString();
        }

        private static string FieldSnapshot(OverlandWorldFields fields)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < fields.Biomes.Count; i++)
                builder.Append(fields.LandMask[i] ? 'L' : 'W').Append((int)fields.Biomes[i]).Append(',');
            return builder.ToString();
        }

        private static int CountLand(OverlandWorldFields fields)
        {
            int count = 0;
            for (int i = 0; i < fields.LandMask.Count; i++)
            {
                if (fields.LandMask[i])
                    count++;
            }

            return count;
        }

        private static int CountMatchingNeighbors(OverlandMap map, int x, int y, BiomeKind biome)
        {
            int count = 0;
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height)
                        continue;
                    if (map.TileAt(nx, ny).Biome == biome)
                        count++;
                }
            }

            return count;
        }

        private static int EstimateSettlementCapacity(OverlandMap map, OverlandParameters parameters, OverlandWorldFields fields)
        {
            double capacity = 0d;
            for (int i = 0; i < map.Tiles.Count; i++)
                capacity += BiomeDensityWeight(map.Tiles[i].Biome, fields.LandMask[i]) * parameters.SettlementDensity;
            return (int)System.Math.Round(capacity, System.MidpointRounding.AwayFromZero);
        }

        private static double BiomeDensityWeight(BiomeKind biome, bool isLand)
        {
            if (!isLand)
                return 0d;

            switch (biome)
            {
                case BiomeKind.Plains: return 1.20d;
                case BiomeKind.Forest: return 0.95d;
                case BiomeKind.Coast: return 1.10d;
                case BiomeKind.Mountain: return 0.55d;
                case BiomeKind.Swamp: return 0.45d;
                case BiomeKind.Desert: return 0.35d;
                case BiomeKind.Tundra: return 0.40d;
                default: return 0.30d;
            }
        }

        private static int NearestOtherDistance(OverlandMap map, int settlementIndex)
        {
            var source = map.Settlements[settlementIndex];
            int best = int.MaxValue;
            for (int i = 0; i < map.Settlements.Count; i++)
            {
                if (i == settlementIndex)
                    continue;
                int distance = map.DistanceBetween(source.Id, map.Settlements[i].Id);
                if (distance < best)
                    best = distance;
            }

            return best;
        }
    }
}

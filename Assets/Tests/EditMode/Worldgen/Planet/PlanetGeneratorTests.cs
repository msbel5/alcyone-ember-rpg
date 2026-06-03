using System;
using System.Globalization;
using EmberCrpg.Simulation.Rng;
using EmberCrpg.Simulation.Worldgen.Planet;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Worldgen.Planet
{
    /// <summary>Proof tests for the deterministic phase-1a spherical planet substrate.</summary>
    public sealed class PlanetGeneratorTests
    {
        private static readonly PlanetParameters TestParameters = new PlanetParameters(
            subdivisionLevel: 3,
            plateCount: 10,
            oceanicFraction: 0.65d,
            seaLevelThreshold: 0d,
            driftScale: 0.035d);

        [Test]
        public void IcosphereGrid_SubdivisionCountAndAdjacency_AreSane()
        {
            for (int level = 0; level <= 4; level++)
            {
                IcosphereGrid grid = IcosphereGrid.Build(level);

                Assert.That(grid.Count, Is.EqualTo(IcosphereGrid.ExpectedTileCount(level)), $"level={level}");
                for (int tileId = 0; tileId < grid.Count; tileId++)
                {
                    IcosphereTile tile = grid.TileAt(tileId);
                    Assert.That(tile.Id, Is.EqualTo(tileId));
                    Assert.That(tile.Position.Length, Is.EqualTo(1d).Within(0.000000001d), $"tile={tileId}");
                    Assert.That(tile.Neighbors.Count, Is.InRange(5, 6), $"tile={tileId}");

                    for (int neighborIndex = 0; neighborIndex < tile.Neighbors.Count; neighborIndex++)
                    {
                        int neighborId = tile.Neighbors[neighborIndex];
                        Assert.That(Contains(grid.TileAt(neighborId), tileId), Is.True, $"edge {tileId}<->{neighborId} should be symmetric.");
                    }
                }
            }
        }

        [Test]
        public void PlanetGenerator_SameSeedProducesByteIdenticalDigest_AndDifferentSeedDiffers()
        {
            PlanetField first = PlanetGenerator.Generate(42u, TestParameters);
            PlanetField second = PlanetGenerator.Generate(42u, TestParameters);
            PlanetField different = PlanetGenerator.Generate(43u, TestParameters);

            Assert.That(Digest(first), Is.EqualTo(Digest(second)));
            Assert.That(Digest(first), Is.Not.EqualTo(Digest(different)));
        }

        [Test]
        public void PlatePartition_CoversEveryTileWithValidPlateIds()
        {
            PlanetField field = PlanetGenerator.Generate(42u, TestParameters);
            var seen = new bool[TestParameters.PlateCount];

            Assert.That(field.Plates.PlateCount, Is.EqualTo(TestParameters.PlateCount));
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                int plateId = field.TileAt(tileId).PlateId;
                Assert.That(plateId, Is.InRange(0, TestParameters.PlateCount - 1), $"tile={tileId}");
                seen[plateId] = true;
            }

            for (int plateId = 0; plateId < seen.Length; plateId++)
                Assert.That(seen[plateId], Is.True, $"plate={plateId} should own at least its seed tile.");
        }

        [Test]
        public void PlanetGenerator_LandOceanSplitTracksOceanicFraction()
        {
            PlanetField field = PlanetGenerator.Generate(42u, TestParameters);
            double landFraction = LandFraction(field);
            double expectedLand = 1d - TestParameters.OceanicFraction;

            Assert.That(landFraction, Is.InRange(expectedLand - 0.20d, expectedLand + 0.25d));
        }

        [Test]
        public void PlanetGenerator_BiomeCoverage_AssignsEveryTileAndKeepsOceanTilesOcean()
        {
            PlanetField field = PlanetGenerator.Generate(42u, TestParameters);

            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField tile = field.TileAt(tileId);
                Assert.That((int)tile.Biome, Is.InRange((int)PlanetBiome.Ocean, (int)PlanetBiome.Mountain), $"tile={tileId}");
                Assert.That(tile.Moisture, Is.InRange(0d, 1d), $"tile={tileId}");
                Assert.That(tile.Temperature, Is.InRange(0d, 1d), $"tile={tileId}");
                if (!tile.IsLand)
                    Assert.That(tile.Biome, Is.EqualTo(PlanetBiome.Ocean), $"tile={tileId}");
            }
        }

        [Test]
        public void ClimateStage_RainShadow_MakesLeewardMountainTilesDrier()
        {
            PlanetField field = PlanetGenerator.Generate(42u, TestParameters);
            RainShadowFigure figure = MeasureRainShadow(field);

            Assert.That(figure.Samples, Is.GreaterThan(3));
            Assert.That(figure.LeewardMean, Is.LessThan(figure.WindwardMean - 0.015d));
        }

        [Test]
        public void HydrologyStage_RiversDescendToCoastOrLake()
        {
            PlanetField field = PlanetGenerator.Generate(42u, TestParameters);
            int riverTiles = 0;

            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (!field.TileAt(tileId).IsRiver)
                    continue;

                riverTiles++;
                Assert.That(HasNonHigherNeighbor(field, tileId), Is.True, $"river tile={tileId} should have a non-higher outlet.");
                Assert.That(RiverPathReachesCoastOrLake(field, tileId), Is.True, $"river tile={tileId} should reach a coast or lake.");
            }

            Assert.That(riverTiles, Is.GreaterThan(0));
        }

        [Test]
        public void ErosionStage_LowersMeanElevationOfTopFlowTiles()
        {
            PlanetField before = BuildPreErosionField(42u, TestParameters);
            PlanetField after = new ErosionStage().Apply(before, new XorShiftRng(42u ^ 0x45524F53u));
            int topCount = Math.Max(8, before.TileCount / 36);
            int[] topFlowTiles = TopFlowTiles(before, topCount);

            Assert.That(before.TileAt(topFlowTiles[0]).Flow, Is.GreaterThan(HydrologyStage.RiverFlowThreshold(before.TileCount)));
            Assert.That(
                MeanElevation(before, topFlowTiles, topCount),
                Is.GreaterThan(MeanElevation(after, topFlowTiles, topCount) + 0.0005d));
        }

        [Test]
        public void TectonicElevation_ConvergentBoundariesRaiseMountains()
        {
            PlanetField field = PlanetGenerator.Generate(42u, TestParameters);
            bool[] convergentAdjacent = MarkConvergentAdjacentTiles(field);
            double globalMean = MeanElevation(field);
            double convergentMean = MeanElevation(field, convergentAdjacent, out int convergentTileCount);

            Assert.That(convergentTileCount, Is.GreaterThan(0));
            Assert.That(convergentMean, Is.GreaterThan(globalMean + 0.05d));
        }

        [Test]
        public void PlanetGenerator_Seed42Sample_RemainsReadable()
        {
            PlanetField field = PlanetGenerator.Generate(42u, TestParameters);
            bool[] convergentAdjacent = MarkConvergentAdjacentTiles(field);
            double globalMean = MeanElevation(field);
            double convergentMean = MeanElevation(field, convergentAdjacent, out _);
            int convergentEdges = CountBoundaries(field, PlateBoundaryKind.Convergent);
            double landFraction = LandFraction(field);

            TestContext.WriteLine(
                "seed=42 tiles={0} plates={1} land={2}% convergentEdges={3} meanElevation={4} convergentAdjacentMean={5}",
                field.TileCount,
                field.Plates.PlateCount,
                Format(landFraction * 100d),
                convergentEdges,
                Format(globalMean),
                Format(convergentMean));

            Assert.That(field.TileCount, Is.EqualTo(IcosphereGrid.ExpectedTileCount(TestParameters.SubdivisionLevel)));
            Assert.That(convergentEdges, Is.GreaterThan(0));
            Assert.That(landFraction, Is.InRange(0.10d, 0.70d));
        }

        [Test]
        public void PlanetGenerator_Seed42Phase2Sample_RemainsReadable()
        {
            var sampleParameters = new PlanetParameters(
                subdivisionLevel: 4,
                plateCount: 12,
                oceanicFraction: 0.62d,
                seaLevelThreshold: 0d,
                driftScale: 0.035d);
            PlanetField field = PlanetGenerator.Generate(42u, sampleParameters);
            RainShadowFigure rainShadow = MeasureRainShadow(field);

            TestContext.WriteLine(
                "seed=42 phase2 tiles={0} land={1}% biomes={2} riverTiles={3} temperature={4}..{5} rainShadowWindward={6} rainShadowLeeward={7} rainShadowSamples={8}",
                field.TileCount,
                Format(LandFraction(field) * 100d),
                BiomeHistogram(field),
                CountRiverTiles(field),
                Format(MinTemperature(field)),
                Format(MaxTemperature(field)),
                Format(rainShadow.WindwardMean),
                Format(rainShadow.LeewardMean),
                rainShadow.Samples);

            Assert.That(field.TileCount, Is.EqualTo(IcosphereGrid.ExpectedTileCount(sampleParameters.SubdivisionLevel)));
            Assert.That(CountRiverTiles(field), Is.GreaterThan(0));
            Assert.That(rainShadow.Samples, Is.GreaterThan(0));
        }

        private static bool Contains(IcosphereTile tile, int neighborId)
        {
            for (int i = 0; i < tile.Neighbors.Count; i++)
            {
                if (tile.Neighbors[i] == neighborId)
                    return true;
            }

            return false;
        }

        private static ulong Digest(PlanetField field)
        {
            ulong hash = 14695981039346656037UL;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField tile = field.TileAt(tileId);
                hash = AddInt(hash, tile.PlateId);
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.Elevation));
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.Temperature));
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.Moisture));
                hash = AddInt(hash, (int)tile.Biome);
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.Flow));
            }

            return hash;
        }

        private static ulong AddInt(ulong hash, int value)
        {
            return AddUInt(hash, (uint)value);
        }

        private static ulong AddLong(ulong hash, long value)
        {
            hash = AddUInt(hash, (uint)value);
            return AddUInt(hash, (uint)(value >> 32));
        }

        private static ulong AddUInt(ulong hash, uint value)
        {
            for (int i = 0; i < 4; i++)
            {
                hash ^= (byte)(value >> (i * 8));
                hash *= 1099511628211UL;
            }

            return hash;
        }

        private static double LandFraction(PlanetField field)
        {
            int land = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (field.TileAt(tileId).IsLand)
                    land++;
            }

            return land / (double)field.TileCount;
        }

        private static bool[] MarkConvergentAdjacentTiles(PlanetField field)
        {
            var adjacent = new bool[field.TileCount];
            for (int i = 0; i < field.Boundaries.Edges.Count; i++)
            {
                PlateBoundaryEdge edge = field.Boundaries.Edges[i];
                if (edge.Kind != PlateBoundaryKind.Convergent)
                    continue;

                adjacent[edge.TileA] = true;
                adjacent[edge.TileB] = true;
            }

            return adjacent;
        }

        private static int CountBoundaries(PlanetField field, PlateBoundaryKind kind)
        {
            int count = 0;
            for (int i = 0; i < field.Boundaries.Edges.Count; i++)
            {
                if (field.Boundaries.Edges[i].Kind == kind)
                    count++;
            }

            return count;
        }

        private static double MeanElevation(PlanetField field)
        {
            double sum = 0d;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
                sum += field.TileAt(tileId).Elevation;
            return sum / field.TileCount;
        }

        private static double MeanElevation(PlanetField field, bool[] mask, out int count)
        {
            double sum = 0d;
            count = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (!mask[tileId])
                    continue;

                sum += field.TileAt(tileId).Elevation;
                count++;
            }

            return count == 0 ? 0d : sum / count;
        }

        private static string Format(double value)
        {
            return value.ToString("0.000", CultureInfo.InvariantCulture);
        }

        private static PlanetField BuildPreErosionField(uint seed, PlanetParameters parameters)
        {
            IcosphereGrid grid = IcosphereGrid.Build(parameters.SubdivisionLevel);
            PlatePartitionResult plates = new PlatePartition().Build(
                grid,
                parameters.PlateCount,
                parameters.OceanicFraction,
                parameters.DriftScale,
                new XorShiftRng(seed ^ 0x70514E45u));
            PlateBoundarySet boundaries = new PlateBoundaries().Build(grid, plates);
            PlanetField field = new TectonicElevation().Build(seed, parameters, grid, plates, boundaries);
            field = new ElevationNoise().Apply(field, new XorShiftRng(seed ^ 0x454E4F49u));
            field = new ClimateStage().Apply(field, new XorShiftRng(seed ^ 0x434C494Du));
            return new HydrologyStage().Apply(field, new XorShiftRng(seed ^ 0x48594452u));
        }

        private static int[] TopFlowTiles(PlanetField field, int count)
        {
            var tileIds = new int[field.TileCount];
            for (int tileId = 0; tileId < tileIds.Length; tileId++)
                tileIds[tileId] = tileId;

            Array.Sort(tileIds, (left, right) =>
            {
                int flow = field.TileAt(right).Flow.CompareTo(field.TileAt(left).Flow);
                return flow != 0 ? flow : left.CompareTo(right);
            });

            return tileIds;
        }

        private static double MeanElevation(PlanetField field, int[] tileIds, int count)
        {
            double sum = 0d;
            for (int i = 0; i < count; i++)
                sum += field.TileAt(tileIds[i]).Elevation;
            return sum / count;
        }

        private static bool HasNonHigherNeighbor(PlanetField field, int tileId)
        {
            PlanetTileField tile = field.TileAt(tileId);
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (field.TileAt(neighbors[i]).Elevation <= tile.Elevation + 0.000000001d)
                    return true;
            }

            return false;
        }

        private static bool RiverPathReachesCoastOrLake(PlanetField field, int startTile)
        {
            var seen = new bool[field.TileCount];
            int current = startTile;
            for (int step = 0; step < field.TileCount; step++)
            {
                PlanetTileField tile = field.TileAt(current);
                if (tile.IsLake || HasOceanNeighbor(field, current))
                    return true;
                if (seen[current])
                    return false;

                seen[current] = true;
                int next = LowestNonHigherLandNeighbor(field, current);
                if (next < 0)
                    return false;

                current = next;
            }

            return false;
        }

        private static bool HasOceanNeighbor(PlanetField field, int tileId)
        {
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (!field.TileAt(neighbors[i]).IsLand)
                    return true;
            }

            return false;
        }

        private static int LowestNonHigherLandNeighbor(PlanetField field, int tileId)
        {
            double currentElevation = field.TileAt(tileId).Elevation;
            int best = -1;
            double bestElevation = currentElevation;
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                int neighbor = neighbors[i];
                PlanetTileField neighborTile = field.TileAt(neighbor);
                if (!neighborTile.IsLand)
                    continue;

                double candidate = neighborTile.Elevation;
                if (candidate <= currentElevation + 0.000000001d &&
                    (best < 0 || candidate < bestElevation || (Math.Abs(candidate - bestElevation) <= 0.000000001d && neighbor < best)))
                {
                    best = neighbor;
                    bestElevation = candidate;
                }
            }

            return best;
        }

        private static RainShadowFigure MeasureRainShadow(PlanetField field)
        {
            double windward = 0d;
            double leeward = 0d;
            int samples = 0;
            double highElevation = field.Parameters.SeaLevelThreshold + 0.24d;

            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField tile = field.TileAt(tileId);
                if (!tile.IsLand || tile.Elevation < highElevation)
                    continue;

                PlanetVector position = field.Grid.TileAt(tileId).Position;
                PlanetVector wind = ClimateStage.PrevailingWind(position);
                int windwardNeighbor = -1;
                int leewardNeighbor = -1;
                double windwardDot = double.PositiveInfinity;
                double leewardDot = double.NegativeInfinity;
                var neighbors = field.Grid.TileAt(tileId).Neighbors;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    int neighbor = neighbors[i];
                    PlanetTileField neighborTile = field.TileAt(neighbor);
                    if (!neighborTile.IsLand || Math.Abs(field.Grid.TileAt(neighbor).Position.Y - position.Y) > 0.28d)
                        continue;

                    PlanetVector direction = field.Grid.TileAt(neighbor).Position.Subtract(position);
                    double length = direction.Length;
                    if (length <= 0.000000001d)
                        continue;

                    double dot = PlanetVector.Dot(direction.Scale(1d / length), wind);
                    if (dot < windwardDot)
                    {
                        windwardDot = dot;
                        windwardNeighbor = neighbor;
                    }

                    if (dot > leewardDot)
                    {
                        leewardDot = dot;
                        leewardNeighbor = neighbor;
                    }
                }

                if (windwardNeighbor < 0 || leewardNeighbor < 0 || windwardDot > -0.18d || leewardDot < 0.18d)
                    continue;

                windward += field.TileAt(windwardNeighbor).Moisture;
                leeward += field.TileAt(leewardNeighbor).Moisture;
                samples++;
            }

            return new RainShadowFigure(
                samples == 0 ? 0d : windward / samples,
                samples == 0 ? 0d : leeward / samples,
                samples);
        }

        private static string BiomeHistogram(PlanetField field)
        {
            var counts = new int[(int)PlanetBiome.Mountain + 1];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
                counts[(int)field.TileAt(tileId).Biome]++;

            return string.Format(
                CultureInfo.InvariantCulture,
                "Ocean:{0},Ice:{1},Tundra:{2},Taiga:{3},TemperateForest:{4},Grassland:{5},Desert:{6},Savanna:{7},TropicalRainforest:{8},Mountain:{9}",
                counts[(int)PlanetBiome.Ocean],
                counts[(int)PlanetBiome.Ice],
                counts[(int)PlanetBiome.Tundra],
                counts[(int)PlanetBiome.Taiga],
                counts[(int)PlanetBiome.TemperateForest],
                counts[(int)PlanetBiome.Grassland],
                counts[(int)PlanetBiome.Desert],
                counts[(int)PlanetBiome.Savanna],
                counts[(int)PlanetBiome.TropicalRainforest],
                counts[(int)PlanetBiome.Mountain]);
        }

        private static int CountRiverTiles(PlanetField field)
        {
            int count = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (field.TileAt(tileId).IsRiver)
                    count++;
            }

            return count;
        }

        private static double MinTemperature(PlanetField field)
        {
            double minimum = double.PositiveInfinity;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
                minimum = Math.Min(minimum, field.TileAt(tileId).Temperature);
            return minimum;
        }

        private static double MaxTemperature(PlanetField field)
        {
            double maximum = double.NegativeInfinity;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
                maximum = Math.Max(maximum, field.TileAt(tileId).Temperature);
            return maximum;
        }

        private struct RainShadowFigure
        {
            public RainShadowFigure(double windwardMean, double leewardMean, int samples)
            {
                WindwardMean = windwardMean;
                LeewardMean = leewardMean;
                Samples = samples;
            }

            public double WindwardMean { get; }
            public double LeewardMean { get; }
            public int Samples { get; }
        }
    }
}

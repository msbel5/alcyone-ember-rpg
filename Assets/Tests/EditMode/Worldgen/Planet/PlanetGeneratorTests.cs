using System;
using System.Globalization;
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
    }
}

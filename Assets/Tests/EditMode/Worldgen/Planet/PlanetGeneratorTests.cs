using System;
using System.Collections.Generic;
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
        // Re-baselined 2026-06-10 for v0.2 density-B: SettlementStage gained a relaxed-spacing second pass
        // (non-forest only) growing the site pool toward ~150 — the settlement layer, and so the digest,
        // changed by design. Determinism itself is re-proven every run by the same-seed double generate.
        private const ulong Seed42Digest = 11414931061137974583UL;
        private const double MeaningfulOreThreshold = 0.05d;
        private const double DepositCoreOreThreshold = 0.50d;
        private const double RichMiningOreThreshold = 0.68d;

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
            ulong firstDigest = Digest(first);

            TestContext.WriteLine("seed=42 digest={0}", firstDigest);
            Assert.That(firstDigest, Is.EqualTo(Seed42Digest));
            Assert.That(firstDigest, Is.EqualTo(Digest(second)));
            Assert.That(firstDigest, Is.Not.EqualTo(Digest(different)));
        }

        [Test]
        public void PlanetGenerationManager_ObserverReportsStagesInCanonicalOrder()
        {
            IReadOnlyList<IPlanetStage> stages = PlanetStageFactory.CreateStages(TestParameters);
            var observer = new RecordingPlanetObserver();
            PlanetField field = new PlanetGenerationManager(stages).Generate(
                new PlanetGenerationContext(42u, TestParameters),
                observer);
            var stageNames = new string[stages.Count];

            Assert.That(field, Is.Not.Null);
            Assert.That(observer.Reports.Count, Is.EqualTo(stages.Count));
            for (int i = 0; i < observer.Reports.Count; i++)
            {
                PlanetStageReport report = observer.Reports[i];
                Assert.That(report.StageIndex, Is.EqualTo(i));
                Assert.That(report.StageCount, Is.EqualTo(stages.Count));
                Assert.That(report.StageName, Is.EqualTo(stages[i].Name));
                Assert.That(report.StageName, Is.Not.Empty);
                Assert.That(report.Summary, Is.Not.Empty);
                stageNames[i] = report.StageName;
            }

            TestContext.WriteLine("observer stage order={0}", string.Join(" -> ", stageNames));
            Assert.That(stageNames, Is.EqualTo(new[]
            {
                "Icosphere",
                "Plates",
                "Boundaries",
                "TectonicElevation",
                "ElevationNoise",
                "Climate",
                "Hydrology",
                "Erosion",
                "Resources",
                "Settlements",
            }));
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

        [Test]
        public void ResourceStage_GeophysicalProxies_ConcentrateExpectedResources()
        {
            PlanetField field = PlanetGenerator.Generate(42u, new PlanetParameters(4, 12, 0.62d, 0d, 0.035d));
            double smearedBaseline = SmearedConvergentLandFraction(field, 2);
            double oreLandFraction = OreLandFraction(field, MeaningfulOreThreshold);
            bool[] oreClusterMask = MarkOreClusters(field, DepositCoreOreThreshold, 2, out int oreCoreCount);
            int oreTiles = CountLandOreTiles(field, MeaningfulOreThreshold);
            int clusteredOreTiles = CountLandOreTiles(field, MeaningfulOreThreshold, oreClusterMask);

            Assert.That(field.ResourceImpacts.Count, Is.GreaterThan(0));
            Assert.That(smearedBaseline, Is.GreaterThan(0.20d));
            Assert.That(oreCoreCount, Is.GreaterThan(0));
            Assert.That(oreLandFraction, Is.LessThan(smearedBaseline * 0.35d));
            Assert.That(oreLandFraction, Is.LessThan(0.10d));
            Assert.That(clusteredOreTiles, Is.GreaterThanOrEqualTo((int)Math.Ceiling(oreTiles * 0.85d)));

            double coalSource = MeanResource(field, PlanetResourceKind.Coal, IsCoalSourceTile(field), out int coalSourceCount);
            double coalBackground = MeanResource(field, PlanetResourceKind.Coal, tileId => field.TileAt(tileId).IsLand && !IsCoalSourceTile(field)(tileId), out int coalBackgroundCount);
            Assert.That(coalSourceCount, Is.GreaterThan(0));
            Assert.That(coalBackgroundCount, Is.GreaterThan(0));
            Assert.That(coalSource, Is.GreaterThan(coalBackground + 0.03d));

            double oilCoastal = MeanResource(field, PlanetResourceKind.OilGas, IsLowCoastalLand(field), out int oilCoastalCount);
            double oilBackground = MeanResource(field, PlanetResourceKind.OilGas, tileId => field.TileAt(tileId).IsLand && !IsLowCoastalLand(field)(tileId), out int oilBackgroundCount);
            Assert.That(oilCoastalCount, Is.GreaterThan(0));
            Assert.That(oilBackgroundCount, Is.GreaterThan(0));
            Assert.That(oilCoastal, Is.GreaterThan(oilBackground + 0.08d));

            int forestWoodTiles = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                PlanetTileField tile = field.TileAt(tileId);
                if (ResourceStage.IsForestBiome(tile.Biome) && tile.Wood > 0d)
                    forestWoodTiles++;
                if (!ResourceStage.IsForestBiome(tile.Biome))
                    Assert.That(tile.Wood, Is.EqualTo(0d).Within(0.000000001d), $"tile={tileId}");
            }

            Assert.That(forestWoodTiles, Is.GreaterThan(0));
            TestContext.WriteLine(
                "seed=42 phase3 ore level=4 smearedBaseline={0}% meaningfulOre={1}% oreCores={2} clusteredOreTiles={3}/{4}",
                Format(smearedBaseline * 100d),
                Format(oreLandFraction * 100d),
                oreCoreCount,
                clusteredOreTiles,
                oreTiles);
        }

        [Test]
        public void SettlementStage_LocalMaxima_CreateSpacedHabitableEconomies()
        {
            PlanetField field = PlanetGenerator.Generate(42u, new PlanetParameters(4, 12, 0.62d, 0d, 0.035d));

            Assert.That(field.Settlements.Count, Is.GreaterThan(0));
            int spacing = SettlementStage.MinimumSpacingFor(field);
            for (int i = 0; i < field.Settlements.Count; i++)
            {
                PlanetSettlement settlement = field.Settlements[i];
                PlanetTileField tile = field.TileAt(settlement.TileId);
                Assert.That(tile.IsLand, Is.True, $"settlement tile={settlement.TileId}");
                Assert.That(tile.Biome, Is.Not.EqualTo(PlanetBiome.Ice), $"settlement tile={settlement.TileId}");
                Assert.That(tile.FreshWater, Is.GreaterThanOrEqualTo(SettlementStage.MinimumFreshWater), $"settlement tile={settlement.TileId}");

                // CONTRACT CHANGE (v0.2 density-B): the pool now grows with a SECOND, relaxed-spacing pass
                // (non-forest candidates only, so the farm>forest mix holds by construction). The pairwise
                // floor is therefore spacing-1, not spacing — still clear rings between every two towns.
                int relaxedFloor = Math.Max(2, spacing - 1);
                for (int j = i + 1; j < field.Settlements.Count; j++)
                {
                    int distance = GraphDistance(field, settlement.TileId, field.Settlements[j].TileId, relaxedFloor);
                    Assert.That(distance, Is.GreaterThanOrEqualTo(relaxedFloor), $"settlements {settlement.TileId} and {field.Settlements[j].TileId}");
                }
            }

            PlanetSettlement fertile = ExtremalSettlementByAgrarianScore(field, highest: true);
            PlanetSettlement barren = ExtremalSettlementByAgrarianScore(field, highest: false);
            Assert.That(AgrarianScore(field, fertile.TileId), Is.GreaterThan(AgrarianScore(field, barren.TileId) + 0.08d));
            Assert.That(fertile.Population, Is.GreaterThan(barren.Population));

            int[] typeCounts = SettlementTypeCounts(field);
            int miningTowns = typeCounts[(int)PlanetSettlementType.MiningTown];
            for (int i = 0; i < field.Settlements.Count; i++)
            {
                PlanetSettlement settlement = field.Settlements[i];
                if (settlement.Type != PlanetSettlementType.MiningTown)
                    continue;

                Assert.That(MaxAdjacentOre(field, settlement.TileId), Is.GreaterThanOrEqualTo(RichMiningOreThreshold), $"mining tile={settlement.TileId}");
            }

            Assert.That(miningTowns, Is.LessThan(field.Settlements.Count / 2));
            Assert.That(CountSettlementTypes(field), Is.GreaterThanOrEqualTo(2));
            TestContext.WriteLine("seed=42 phase3 types={0}", SettlementTypeHistogram(field));
        }

        [Test]
        public void PlanetGenerator_Seed42Phase3Level5Sample_RemainsReadable()
        {
            var sampleParameters = new PlanetParameters(
                subdivisionLevel: 5,
                plateCount: 16,
                oceanicFraction: 0.62d,
                seaLevelThreshold: 0d,
                driftScale: 0.035d);
            var level6Parameters = new PlanetParameters(
                subdivisionLevel: 6,
                plateCount: 20,
                oceanicFraction: 0.62d,
                seaLevelThreshold: 0d,
                driftScale: 0.035d);
            PlanetField field = PlanetGenerator.Generate(42u, sampleParameters);
            double smearedBaseline = SmearedConvergentLandFraction(field, 2);
            double oreLandFraction = OreLandFraction(field, MeaningfulOreThreshold);
            bool[] oreClusterMask = MarkOreClusters(field, DepositCoreOreThreshold, 2, out int oreCoreCount);
            int oreTiles = CountLandOreTiles(field, MeaningfulOreThreshold);
            int clusteredOreTiles = CountLandOreTiles(field, MeaningfulOreThreshold, oreClusterMask);
            int[] typeCounts = SettlementTypeCounts(field);
            int nonCapitalSettlements = field.Settlements.Count - typeCounts[(int)PlanetSettlementType.Capital];
            PlanetField level6Field = PlanetGenerator.Generate(42u, level6Parameters);
            double level6SmearedBaseline = SmearedConvergentLandFraction(level6Field, 2);
            double level6OreLandFraction = OreLandFraction(level6Field, MeaningfulOreThreshold);
            bool[] level6OreClusterMask = MarkOreClusters(level6Field, DepositCoreOreThreshold, 2, out int level6OreCoreCount);
            int level6OreTiles = CountLandOreTiles(level6Field, MeaningfulOreThreshold);
            int level6ClusteredOreTiles = CountLandOreTiles(level6Field, MeaningfulOreThreshold, level6OreClusterMask);

            TestContext.WriteLine(
                "seed=42 phase3 level=5 settlements={0} types={1} totalPopulation={2} top3={3} smearedBaseline={4}% meaningfulOre={5}% oreCores={6} clusteredOreTiles={7}/{8}",
                field.Settlements.Count,
                SettlementTypeHistogram(field),
                TotalPopulation(field),
                TopSettlements(field, 3),
                Format(smearedBaseline * 100d),
                Format(oreLandFraction * 100d),
                oreCoreCount,
                clusteredOreTiles,
                oreTiles);
            TestContext.WriteLine(
                "seed=42 phase3 level=6 smearedBaseline={0}% meaningfulOre={1}% oreCores={2} clusteredOreTiles={3}/{4}",
                Format(level6SmearedBaseline * 100d),
                Format(level6OreLandFraction * 100d),
                level6OreCoreCount,
                level6ClusteredOreTiles,
                level6OreTiles);

            Assert.That(field.TileCount, Is.EqualTo(IcosphereGrid.ExpectedTileCount(sampleParameters.SubdivisionLevel)));
            Assert.That(field.Settlements.Count, Is.GreaterThan(0));
            Assert.That(TotalPopulation(field), Is.GreaterThan(0));
            Assert.That(oreLandFraction, Is.LessThan(smearedBaseline * 0.35d));
            Assert.That(oreLandFraction, Is.LessThan(0.08d));
            Assert.That(clusteredOreTiles, Is.GreaterThanOrEqualTo((int)Math.Ceiling(oreTiles * 0.85d)));
            Assert.That(typeCounts[(int)PlanetSettlementType.FarmVillage], Is.GreaterThan(typeCounts[(int)PlanetSettlementType.MiningTown]));
            Assert.That(typeCounts[(int)PlanetSettlementType.FarmVillage], Is.GreaterThan(typeCounts[(int)PlanetSettlementType.Port]));
            Assert.That(typeCounts[(int)PlanetSettlementType.FarmVillage], Is.GreaterThan(typeCounts[(int)PlanetSettlementType.ForestHamlet]));
            Assert.That(typeCounts[(int)PlanetSettlementType.FarmVillage], Is.GreaterThan(typeCounts[(int)PlanetSettlementType.MarketTown]));
            Assert.That(typeCounts[(int)PlanetSettlementType.MiningTown], Is.GreaterThan(0));
            Assert.That(typeCounts[(int)PlanetSettlementType.MiningTown], Is.LessThan(Math.Max(2, nonCapitalSettlements / 3)));
            for (int i = 0; i < field.Settlements.Count; i++)
            {
                PlanetSettlement settlement = field.Settlements[i];
                if (settlement.Type == PlanetSettlementType.MiningTown)
                    Assert.That(MaxAdjacentOre(field, settlement.TileId), Is.GreaterThanOrEqualTo(RichMiningOreThreshold), $"mining tile={settlement.TileId}");
            }

            Assert.That(level6Field.TileCount, Is.EqualTo(IcosphereGrid.ExpectedTileCount(level6Parameters.SubdivisionLevel)));
            Assert.That(level6OreLandFraction, Is.LessThan(level6SmearedBaseline * 0.35d));
            Assert.That(level6OreLandFraction, Is.LessThan(0.06d));
            Assert.That(level6ClusteredOreTiles, Is.GreaterThanOrEqualTo((int)Math.Ceiling(level6OreTiles * 0.85d)));
            Assert.That(level6OreCoreCount, Is.InRange(24, 60));
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
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.IronOre));
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.PreciousMetal));
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.Coal));
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.OilGas));
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.Stone));
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.Clay));
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.Wood));
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.SoilFertility));
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(tile.FreshWater));
            }

            hash = AddInt(hash, field.ResourceImpacts.Count);
            for (int i = 0; i < field.ResourceImpacts.Count; i++)
            {
                PlanetImpactSite impact = field.ResourceImpacts[i];
                hash = AddInt(hash, impact.TileId);
                hash = AddInt(hash, impact.Radius);
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(impact.IronAbundance));
                hash = AddLong(hash, BitConverter.DoubleToInt64Bits(impact.PreciousMetalAbundance));
            }

            hash = AddInt(hash, field.Settlements.Count);
            for (int i = 0; i < field.Settlements.Count; i++)
            {
                PlanetSettlement settlement = field.Settlements[i];
                hash = AddInt(hash, settlement.TileId);
                hash = AddInt(hash, (int)settlement.Type);
                hash = AddInt(hash, settlement.Population);
                hash = AddInt(hash, settlement.DominantResources.Count);
                for (int resourceIndex = 0; resourceIndex < settlement.DominantResources.Count; resourceIndex++)
                    hash = AddInt(hash, (int)settlement.DominantResources[resourceIndex]);
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

        private static bool[] MarkConvergentTiles(PlanetField field, int radius)
        {
            var mask = new bool[field.TileCount];
            var brush = new MaskBrush(field.Grid);
            for (int i = 0; i < field.Boundaries.Edges.Count; i++)
            {
                PlateBoundaryEdge edge = field.Boundaries.Edges[i];
                if (edge.Kind != PlateBoundaryKind.Convergent)
                    continue;

                brush.Mark(mask, edge.TileA, radius);
                brush.Mark(mask, edge.TileB, radius);
            }

            return mask;
        }

        private static double SmearedConvergentLandFraction(PlanetField field, int radius)
        {
            bool[] mask = MarkConvergentTiles(field, radius);
            int landTiles = 0;
            int smearedTiles = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (!field.TileAt(tileId).IsLand)
                    continue;

                landTiles++;
                if (mask[tileId])
                    smearedTiles++;
            }

            return landTiles == 0 ? 0d : smearedTiles / (double)landTiles;
        }

        private static double OreLandFraction(PlanetField field, double threshold)
        {
            int landTiles = CountLandTiles(field);
            return landTiles == 0 ? 0d : CountLandOreTiles(field, threshold) / (double)landTiles;
        }

        private static int CountLandTiles(PlanetField field)
        {
            int count = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (field.TileAt(tileId).IsLand)
                    count++;
            }

            return count;
        }

        private static int CountLandOreTiles(PlanetField field, double threshold)
        {
            return CountLandOreTiles(field, threshold, null);
        }

        private static int CountLandOreTiles(PlanetField field, double threshold, bool[] mask)
        {
            int count = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (!field.TileAt(tileId).IsLand || (mask != null && !mask[tileId]))
                    continue;
                if (MaxOre(field.TileAt(tileId)) >= threshold)
                    count++;
            }

            return count;
        }

        private static bool[] MarkOreClusters(PlanetField field, double coreThreshold, int radius, out int coreCount)
        {
            var mask = new bool[field.TileCount];
            var brush = new MaskBrush(field.Grid);
            coreCount = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (!field.TileAt(tileId).IsLand ||
                    MaxOre(field.TileAt(tileId)) < coreThreshold ||
                    !IsOreLocalMaximum(field, tileId))
                {
                    continue;
                }

                coreCount++;
                brush.Mark(mask, tileId, radius);
            }

            return mask;
        }

        private static bool IsOreLocalMaximum(PlanetField field, int tileId)
        {
            double ore = MaxOre(field.TileAt(tileId));
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                int neighbor = neighbors[i];
                double neighborOre = MaxOre(field.TileAt(neighbor));
                if (neighborOre > ore + 0.000000001d)
                    return false;
                if (Math.Abs(neighborOre - ore) <= 0.000000001d && neighbor < tileId)
                    return false;
            }

            return true;
        }

        private static double MaxOre(PlanetTileField tile)
        {
            return Math.Max(tile.IronOre, tile.PreciousMetal);
        }

        private static Func<int, bool> IsCoalSourceTile(PlanetField field)
        {
            return tileId =>
            {
                PlanetTileField tile = field.TileAt(tileId);
                double height = tile.Elevation - field.Parameters.SeaLevelThreshold;
                return tile.IsLand &&
                    height <= 0.88d &&
                    (ResourceStage.IsForestBiome(tile.Biome) || tile.Moisture >= 0.60d);
            };
        }

        private static Func<int, bool> IsLowCoastalLand(PlanetField field)
        {
            return tileId =>
            {
                PlanetTileField tile = field.TileAt(tileId);
                return tile.IsLand &&
                    tile.Elevation <= field.Parameters.SeaLevelThreshold + 0.38d &&
                    HasOceanNeighbor(field, tileId);
            };
        }

        private static double MeanResource(PlanetField field, PlanetResourceKind kind, Func<int, bool> predicate, out int count)
        {
            double sum = 0d;
            count = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (!predicate(tileId))
                    continue;

                sum += ResourceValue(field.TileAt(tileId), kind);
                count++;
            }

            return count == 0 ? 0d : sum / count;
        }

        private static double ResourceValue(PlanetTileField tile, PlanetResourceKind kind)
        {
            switch (kind)
            {
                case PlanetResourceKind.IronOre:
                    return tile.IronOre;
                case PlanetResourceKind.PreciousMetal:
                    return tile.PreciousMetal;
                case PlanetResourceKind.Coal:
                    return tile.Coal;
                case PlanetResourceKind.OilGas:
                    return tile.OilGas;
                case PlanetResourceKind.Stone:
                    return tile.Stone;
                case PlanetResourceKind.Clay:
                    return tile.Clay;
                case PlanetResourceKind.Wood:
                    return tile.Wood;
                case PlanetResourceKind.SoilFertility:
                    return tile.SoilFertility;
                case PlanetResourceKind.FreshWater:
                    return tile.FreshWater;
                default:
                    return 0d;
            }
        }

        private static int GraphDistance(PlanetField field, int start, int target, int stopAt)
        {
            if (start == target)
                return 0;

            var seen = new bool[field.TileCount];
            var distance = new int[field.TileCount];
            var queue = new int[field.TileCount];
            int head = 0;
            int tail = 0;
            queue[tail++] = start;
            seen[start] = true;

            while (head < tail)
            {
                int tileId = queue[head++];
                int currentDistance = distance[tileId];
                if (currentDistance >= stopAt)
                    continue;

                var neighbors = field.Grid.TileAt(tileId).Neighbors;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    int neighbor = neighbors[i];
                    if (seen[neighbor])
                        continue;

                    int nextDistance = currentDistance + 1;
                    if (neighbor == target)
                        return nextDistance;

                    seen[neighbor] = true;
                    distance[neighbor] = nextDistance;
                    queue[tail++] = neighbor;
                }
            }

            return stopAt;
        }

        private static PlanetSettlement ExtremalSettlementByAgrarianScore(PlanetField field, bool highest)
        {
            PlanetSettlement best = field.Settlements[0];
            double bestScore = AgrarianScore(field, best.TileId);
            for (int i = 1; i < field.Settlements.Count; i++)
            {
                PlanetSettlement candidate = field.Settlements[i];
                double score = AgrarianScore(field, candidate.TileId);
                bool better = highest
                    ? score > bestScore + 0.000000001d || (Math.Abs(score - bestScore) <= 0.000000001d && candidate.TileId < best.TileId)
                    : score < bestScore - 0.000000001d || (Math.Abs(score - bestScore) <= 0.000000001d && candidate.TileId < best.TileId);
                if (!better)
                    continue;

                best = candidate;
                bestScore = score;
            }

            return best;
        }

        private static double AgrarianScore(PlanetField field, int tileId)
        {
            PlanetTileField tile = field.TileAt(tileId);
            double sum = tile.SoilFertility + tile.FreshWater;
            int count = 2;
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                PlanetTileField neighbor = field.TileAt(neighbors[i]);
                sum += neighbor.SoilFertility + neighbor.FreshWater;
                count += 2;
            }

            return sum / count;
        }

        private static double MaxAdjacentOre(PlanetField field, int tileId)
        {
            double best = MaxOre(field.TileAt(tileId));
            var neighbors = field.Grid.TileAt(tileId).Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                int neighbor = neighbors[i];
                best = Math.Max(best, MaxOre(field.TileAt(neighbor)));
            }

            return best;
        }

        private static int CountSettlementTypes(PlanetField field)
        {
            int[] counts = SettlementTypeCounts(field);

            int types = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] > 0)
                    types++;
            }

            return types;
        }

        private static string SettlementTypeHistogram(PlanetField field)
        {
            int[] counts = SettlementTypeCounts(field);

            return string.Format(
                CultureInfo.InvariantCulture,
                "FarmVillage:{0},MiningTown:{1},Port:{2},ForestHamlet:{3},MarketTown:{4},Capital:{5}",
                counts[(int)PlanetSettlementType.FarmVillage],
                counts[(int)PlanetSettlementType.MiningTown],
                counts[(int)PlanetSettlementType.Port],
                counts[(int)PlanetSettlementType.ForestHamlet],
                counts[(int)PlanetSettlementType.MarketTown],
                counts[(int)PlanetSettlementType.Capital]);
        }

        private static int[] SettlementTypeCounts(PlanetField field)
        {
            var counts = new int[(int)PlanetSettlementType.Capital + 1];
            for (int i = 0; i < field.Settlements.Count; i++)
                counts[(int)field.Settlements[i].Type]++;
            return counts;
        }

        private static int TotalPopulation(PlanetField field)
        {
            int total = 0;
            for (int i = 0; i < field.Settlements.Count; i++)
                total += field.Settlements[i].Population;
            return total;
        }

        private static string TopSettlements(PlanetField field, int count)
        {
            var settlements = new PlanetSettlement[field.Settlements.Count];
            for (int i = 0; i < settlements.Length; i++)
                settlements[i] = field.Settlements[i];

            Array.Sort(settlements, CompareSettlementsByPopulation);
            int take = Math.Min(count, settlements.Length);
            var parts = new string[take];
            for (int i = 0; i < take; i++)
            {
                PlanetSettlement settlement = settlements[i];
                parts[i] = string.Format(
                    CultureInfo.InvariantCulture,
                    "tile={0}:{1}:pop={2}:why={3}",
                    settlement.TileId,
                    settlement.Type,
                    settlement.Population,
                    SettlementWhy(field, settlement));
            }

            return string.Join(" | ", parts);
        }

        private static string SettlementWhy(PlanetField field, PlanetSettlement settlement)
        {
            PlanetTileField tile = field.TileAt(settlement.TileId);
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}/{2}",
                settlement.DominantResources[0],
                Format(tile.SoilFertility),
                Format(tile.FreshWater));
        }

        private static int CompareSettlementsByPopulation(PlanetSettlement left, PlanetSettlement right)
        {
            int population = right.Population.CompareTo(left.Population);
            return population != 0 ? population : left.TileId.CompareTo(right.TileId);
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

        private sealed class RecordingPlanetObserver : IPlanetGenerationObserver
        {
            public List<PlanetStageReport> Reports { get; } = new List<PlanetStageReport>();

            public void OnStageCompleted(PlanetStageReport report)
            {
                Reports.Add(report);
            }
        }

        private sealed class MaskBrush
        {
            private readonly IcosphereGrid _grid;
            private readonly int[] _distance;
            private readonly int[] _queue;
            private readonly int[] _seen;
            private int _stamp;

            public MaskBrush(IcosphereGrid grid)
            {
                _grid = grid;
                _distance = new int[grid.Count];
                _queue = new int[grid.Count];
                _seen = new int[grid.Count];
            }

            public void Mark(bool[] mask, int center, int radius)
            {
                _stamp++;
                if (_stamp == int.MaxValue)
                {
                    Array.Clear(_seen, 0, _seen.Length);
                    _stamp = 1;
                }

                int head = 0;
                int tail = 0;
                _queue[tail++] = center;
                _distance[center] = 0;
                _seen[center] = _stamp;

                while (head < tail)
                {
                    int tileId = _queue[head++];
                    int distance = _distance[tileId];
                    mask[tileId] = true;
                    if (distance >= radius)
                        continue;

                    var neighbors = _grid.TileAt(tileId).Neighbors;
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        int neighbor = neighbors[i];
                        if (_seen[neighbor] == _stamp)
                            continue;

                        _seen[neighbor] = _stamp;
                        _distance[neighbor] = distance + 1;
                        _queue[tail++] = neighbor;
                    }
                }
            }
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

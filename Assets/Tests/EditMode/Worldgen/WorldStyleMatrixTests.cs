using System.Linq;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Worldgen;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Worldgen
{
    public sealed class WorldStyleMatrixTests
    {
        [Test]
        public void DefaultLowFantasySurvival_StaysDaggerfallScale()
        {
            var parameters = WorldgenParameters.For(WorldStyle.LowFantasy, WorldGenre.Survival);
            var world = WorldgenService.Generate(42u, parameters);

            Assert.That(parameters.RegionCount, Is.EqualTo(50));
            Assert.That(parameters.HistoryYears, Is.EqualTo(400));
            Assert.That(world.Settlements.Count, Is.LessThanOrEqualTo(parameters.SettlementCount));
            Assert.That(world.Settlements.Count, Is.GreaterThan(150));
            Assert.That(world.TotalPopulation, Is.Not.EqualTo(parameters.TargetPopulation));
            Assert.That(world.TotalPopulation, Is.GreaterThan(0));
            Assert.That(world.NotableFigures.Count, Is.GreaterThan(0));
            Assert.That(world.History.Count, Is.GreaterThanOrEqualTo(150));
        }

        [Test]
        public void DarkPolitical_ProfileMatchesIntrigueSkew()
        {
            var parameters = WorldgenParameters.For(WorldStyle.DarkFantasyGrim, WorldGenre.PoliticalIntrigue);
            var world = WorldgenService.Generate(42u, parameters);

            Assert.That(world.Regions.Count, Is.EqualTo(47));
            Assert.That(parameters.SettlementCount, Is.EqualTo(213));
            Assert.That(world.Settlements.Count, Is.LessThanOrEqualTo(parameters.SettlementCount));
            Assert.That(world.Settlements.Count, Is.GreaterThan(180));
            Assert.That(world.Factions.Count, Is.EqualTo(32));
            Assert.That(world.Npcs.Count, Is.LessThanOrEqualTo(parameters.NpcCount));
            Assert.That(world.Npcs.Count, Is.GreaterThan(parameters.NpcCount / 2));
            Assert.That(world.TotalPopulation, Is.Not.EqualTo(parameters.TargetPopulation));
        }

        [Test]
        public void SteampunkMerchantEmpire_ShiftsTowardUrbanSettlements()
        {
            var baseline = WorldgenParameters.For(WorldStyle.LowFantasy, WorldGenre.Survival);
            var steampunk = WorldgenParameters.For(WorldStyle.SteampunkRevolution, WorldGenre.MerchantEmpire);

            Assert.That(steampunk.CityCount + steampunk.TownCount, Is.GreaterThan(baseline.CityCount + baseline.TownCount));
            Assert.That(steampunk.FactionCount, Is.GreaterThan(baseline.FactionCount));
            Assert.That(steampunk.NpcCount, Is.GreaterThan(baseline.NpcCount));
        }

        [Test]
        public void AncientPilgrimage_RegionsUseAuthoritativeGeographyTiles()
        {
            var world = WorldgenService.Generate(42u, WorldgenParameters.For(WorldStyle.AncientMythology, WorldGenre.Pilgrimage));

            Assert.That(world.Geography, Is.Not.Null);
            foreach (var region in world.Regions)
            {
                Assert.That(region.HasTilePosition, Is.True);
                Assert.That(world.Geography.RegionAt(region.TileX, region.TileY), Is.EqualTo(region.Id));

                bool biomeAppearsInRegion = false;
                for (int tile = 0; tile < world.Geography.TileCount; tile++)
                {
                    if (world.Geography.RegionIds[tile] == region.Id && world.Geography.WorldBiomes[tile] == region.Biome)
                    {
                        biomeAppearsInRegion = true;
                        break;
                    }
                }

                Assert.That(biomeAppearsInRegion, Is.True, $"{region.Name} should carry a biome present in its tile cluster.");
            }
        }

        [Test]
        public void DarkPolitical_HistorySimulationFavorsConflictAndCourtEvents()
        {
            var baseline = WorldgenService.Generate(42u, WorldgenParameters.For(WorldStyle.LowFantasy, WorldGenre.Survival));
            var dark = WorldgenService.Generate(42u, WorldgenParameters.For(WorldStyle.DarkFantasyGrim, WorldGenre.PoliticalIntrigue));

            int baselinePressure = CountPressureHistory(baseline);
            int darkPressure = CountPressureHistory(dark);
            Assert.That(darkPressure, Is.GreaterThan(baselinePressure));
        }

        [Test]
        public void EveryStyleGenrePair_IsDeterministicForSameSeed()
        {
            foreach (WorldStyle style in System.Enum.GetValues(typeof(WorldStyle)))
            foreach (WorldGenre genre in System.Enum.GetValues(typeof(WorldGenre)))
            {
                var parameters = WorldgenParameters.For(style, genre);
                var a = WorldgenService.Generate(777u, parameters);
                var b = WorldgenService.Generate(777u, parameters);

                Assert.That(a.TotalPopulation, Is.EqualTo(b.TotalPopulation), $"{style}/{genre} population");
                Assert.That(a.Regions[0].Name, Is.EqualTo(b.Regions[0].Name), $"{style}/{genre} first region");
                Assert.That(a.History[0].Kind, Is.EqualTo(b.History[0].Kind), $"{style}/{genre} first history kind");
            }
        }

        private static int CountPressureHistory(GeneratedWorld world)
        {
            return world.History.Count(h =>
                h.Kind == WorldHistoryKind.FactionWar
                || h.Kind == WorldHistoryKind.FactionAlliance
                || h.Kind == WorldHistoryKind.NobleMarriage
                || h.Kind == WorldHistoryKind.NobleDeath
                || h.Kind == WorldHistoryKind.Calamity
                || h.Kind == WorldHistoryKind.WarDeclared
                || h.Kind == WorldHistoryKind.BattleFought
                || h.Kind == WorldHistoryKind.SiteSacked
                || h.Kind == WorldHistoryKind.CivilizationDestroyed
                || h.Kind == WorldHistoryKind.BorderDispute
                || h.Kind == WorldHistoryKind.Famine
                || h.Kind == WorldHistoryKind.FigureAssassinated);
        }
    }
}

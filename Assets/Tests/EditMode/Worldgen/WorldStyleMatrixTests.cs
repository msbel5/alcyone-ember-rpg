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
            var parameters = WorldgenParameters.For(WorldStyle.LowFantasyMorrowind, WorldGenre.Survival);
            var world = WorldgenService.Generate(42u, parameters);

            Assert.That(parameters.RegionCount, Is.EqualTo(50));
            Assert.That(world.Settlements.Count, Is.EqualTo(200));
            Assert.That(world.TotalPopulation, Is.EqualTo(1_000_000));
            Assert.That(world.History.Count, Is.EqualTo(100));
        }

        [Test]
        public void DarkPolitical_ProfileMatchesIntrigueSkew()
        {
            var parameters = WorldgenParameters.For(WorldStyle.DarkFantasyGrim, WorldGenre.PoliticalIntrigue);
            var world = WorldgenService.Generate(42u, parameters);

            Assert.That(world.Regions.Count, Is.EqualTo(47));
            Assert.That(world.Settlements.Count, Is.EqualTo(213));
            Assert.That(world.Factions.Count, Is.EqualTo(32));
            Assert.That(world.Npcs.Count, Is.EqualTo(932));
            Assert.That(world.TotalPopulation, Is.EqualTo(1_043_217));
        }

        [Test]
        public void SteampunkMerchantEmpire_ShiftsTowardUrbanSettlements()
        {
            var baseline = WorldgenParameters.For(WorldStyle.LowFantasyMorrowind, WorldGenre.Survival);
            var steampunk = WorldgenParameters.For(WorldStyle.SteampunkRevolution, WorldGenre.MerchantEmpire);

            Assert.That(steampunk.CityCount + steampunk.TownCount, Is.GreaterThan(baseline.CityCount + baseline.TownCount));
            Assert.That(steampunk.FactionCount, Is.GreaterThan(baseline.FactionCount));
            Assert.That(steampunk.NpcCount, Is.GreaterThan(baseline.NpcCount));
        }

        [Test]
        public void AncientPilgrimage_BiomeWeightsFavorHarshSacredRoutes()
        {
            var world = WorldgenService.Generate(42u, WorldgenParameters.For(WorldStyle.AncientMythology, WorldGenre.Pilgrimage));

            int harsh = world.Regions.Count(r => r.Biome == BiomeKind.DesertWaste || r.Biome == BiomeKind.MountainHighland || r.Biome == BiomeKind.AridSteppe);
            int lush = world.Regions.Count(r => r.Biome == BiomeKind.TemperatePlain || r.Biome == BiomeKind.TropicalJungle);
            Assert.That(harsh, Is.GreaterThan(lush));
        }

        [Test]
        public void DarkPolitical_HistoryWeightsFavorConflictAndCourtEvents()
        {
            var baseline = WorldgenService.Generate(42u, WorldgenParameters.For(WorldStyle.LowFantasyMorrowind, WorldGenre.Survival));
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
                || h.Kind == WorldHistoryKind.Calamity);
        }
    }
}

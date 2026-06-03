using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Simulation.Worldgen.Planet;
using EmberCrpg.Simulation.Worldgen.PlanetIntegration;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Worldgen.PlanetIntegration
{
    public sealed class PlanetToWorldMapperTests
    {
        private static readonly PlanetParameters SamplePlanetParameters = new PlanetParameters(
            subdivisionLevel: 4,
            plateCount: 12,
            oceanicFraction: 0.62d,
            seaLevelThreshold: 0d,
            driftScale: 0.035d);

        private static readonly WorldgenParameters SampleWorldParameters = new WorldgenParameters(
            regionCount: 16,
            capitalCount: 1,
            cityCount: 2,
            townCount: 6,
            villageCount: 20,
            factionCount: 8,
            npcCount: 64,
            historyYears: 160,
            worldStartYear: 1,
            targetPopulation: 250000);

        private static PlanetField _sampleField;

        [Test]
        public void PlanetToWorldMapper_SamePlanetAndSeed_ProducesStableDigest()
        {
            PlanetField field = SampleField();

            GeneratedWorld first = PlanetToWorldMapper.Map(field, SampleWorldParameters);
            GeneratedWorld second = PlanetToWorldMapper.Map(field, SampleWorldParameters);

            Assert.That(first.Regions.Count, Is.EqualTo(second.Regions.Count));
            Assert.That(first.Settlements.Count, Is.EqualTo(second.Settlements.Count));
            Assert.That(first.Factions.Count, Is.EqualTo(second.Factions.Count));
            Assert.That(first.FactionRelations.Count, Is.EqualTo(second.FactionRelations.Count));
            Assert.That(first.History.Count, Is.EqualTo(second.History.Count));
            Assert.That(Digest(first), Is.EqualTo(Digest(second)));
        }

        [Test]
        public void PlanetToWorldMapper_SettlementsAttachToValidProjectedRegions()
        {
            GeneratedWorld world = PlanetToWorldMapper.Map(SampleField(), SampleWorldParameters);
            var regionIds = new HashSet<RegionId>();
            for (int i = 0; i < world.Regions.Count; i++)
                regionIds.Add(world.Regions[i].Id);

            Assert.That(world.Geography, Is.Not.Null);
            for (int i = 0; i < world.Settlements.Count; i++)
            {
                SettlementRecord settlement = world.Settlements[i];
                Assert.That(regionIds.Contains(settlement.Region), Is.True, settlement.Name);
                Assert.That(settlement.TileX, Is.InRange(0, world.Geography.Width - 1), settlement.Name);
                Assert.That(settlement.TileY, Is.InRange(0, world.Geography.Height - 1), settlement.Name);
                Assert.That(world.Geography.IsLandAt(settlement.TileX, settlement.TileY), Is.True, settlement.Name);
                Assert.That(world.Geography.RegionAt(settlement.TileX, settlement.TileY), Is.EqualTo(settlement.Region), settlement.Name);
            }
        }

        [Test]
        public void PlanetToWorldMapper_ProjectedGeography_LandFractionTracksPlanet()
        {
            PlanetField field = SampleField();
            GeneratedWorld world = PlanetToWorldMapper.Map(field, SampleWorldParameters);

            double planetLandFraction = LandFraction(field);
            double projectedLandFraction = LandFraction(world.Geography);

            Assert.That(projectedLandFraction, Is.InRange(planetLandFraction - 0.18d, planetLandFraction + 0.18d));
        }

        [Test]
        public void PlanetToWorldMapper_HistoryComesFromExistingSimulator()
        {
            GeneratedWorld world = PlanetToWorldMapper.Map(SampleField(), SampleWorldParameters);

            Assert.That(world.History.Count, Is.GreaterThan(0));
            Assert.That(ContainsHistoryKind(world, WorldHistoryKind.LifeEmerged), Is.True);
        }

        [Test]
        public void PlanetToWorldMapper_Seed42SampleCounts_AreSane()
        {
            PlanetField field = SampleField();
            GeneratedWorld world = PlanetToWorldMapper.Map(field, SampleWorldParameters);

            TestContext.WriteLine(
                "seed=42 planet-to-world regions={0} settlements={1} factions={2} relations={3} npcs={4} history={5} geography={6}x{7} land={8:0.000}->{9:0.000}",
                world.Regions.Count,
                world.Settlements.Count,
                world.Factions.Count,
                world.FactionRelations.Count,
                world.Npcs.Count,
                world.History.Count,
                world.Geography.Width,
                world.Geography.Height,
                LandFraction(field),
                LandFraction(world.Geography));

            Assert.That(world.Regions.Count, Is.EqualTo(SampleWorldParameters.RegionCount));
            Assert.That(world.Settlements.Count, Is.GreaterThan(0));
            Assert.That(world.Settlements.Count, Is.LessThanOrEqualTo(field.Settlements.Count));
            Assert.That(world.Factions.Count, Is.InRange(1, SampleWorldParameters.FactionCount));
            Assert.That(world.History.Count, Is.GreaterThan(0));
            Assert.That(world.Geography.Width, Is.EqualTo(PlanetToWorldMapper.GeographyWidth));
            Assert.That(world.Geography.Height, Is.EqualTo(PlanetToWorldMapper.GeographyHeight));
        }

        private static PlanetField SampleField()
        {
            if (_sampleField == null)
                _sampleField = PlanetGenerator.Generate(42u, SamplePlanetParameters);
            return _sampleField;
        }

        private static bool ContainsHistoryKind(GeneratedWorld world, WorldHistoryKind kind)
        {
            for (int i = 0; i < world.History.Count; i++)
            {
                if (world.History[i].Kind == kind)
                    return true;
            }

            return false;
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

        private static double LandFraction(WorldGeography geography)
        {
            int land = 0;
            for (int i = 0; i < geography.TileCount; i++)
            {
                if (geography.LandMask[i])
                    land++;
            }

            return land / (double)geography.TileCount;
        }

        private static ulong Digest(GeneratedWorld world)
        {
            ulong hash = 14695981039346656037UL;
            hash = Add(hash, world.Seed);
            hash = Add(hash, (uint)world.Regions.Count);
            for (int i = 0; i < world.Regions.Count; i++)
            {
                RegionRecord region = world.Regions[i];
                hash = Add(hash, (uint)region.Id.Value);
                hash = Add(hash, region.Name);
                hash = Add(hash, (uint)region.PopulationLow);
                hash = Add(hash, (uint)region.PopulationHigh);
                hash = Add(hash, (uint)region.Biome);
                hash = Add(hash, (uint)region.TileX);
                hash = Add(hash, (uint)region.TileY);
            }

            hash = Add(hash, (uint)world.Settlements.Count);
            for (int i = 0; i < world.Settlements.Count; i++)
            {
                SettlementRecord settlement = world.Settlements[i];
                hash = Add(hash, (uint)settlement.Id.Value);
                hash = Add(hash, (uint)settlement.Region.Value);
                hash = Add(hash, settlement.Name);
                hash = Add(hash, (uint)settlement.Population);
                hash = Add(hash, (uint)settlement.Size);
                hash = Add(hash, (uint)settlement.TileX);
                hash = Add(hash, (uint)settlement.TileY);
            }

            hash = Add(hash, (uint)world.Factions.Count);
            for (int i = 0; i < world.Factions.Count; i++)
            {
                hash = Add(hash, (uint)world.Factions[i].Id.Value);
                hash = Add(hash, world.Factions[i].Name);
            }

            hash = Add(hash, (uint)world.History.Count);
            for (int i = 0; i < world.History.Count; i++)
            {
                WorldHistoryEvent history = world.History[i];
                hash = Add(hash, (uint)history.Year);
                hash = Add(hash, (uint)history.Kind);
                hash = Add(hash, history.Subject);
                hash = Add(hash, history.Detail);
            }

            return hash;
        }

        private static ulong Add(ulong hash, string value)
        {
            string safe = value ?? string.Empty;
            for (int i = 0; i < safe.Length; i++)
                hash = Add(hash, safe[i]);
            return hash;
        }

        private static ulong Add(ulong hash, uint value)
        {
            unchecked
            {
                hash ^= value & 0xffu;
                hash *= 1099511628211UL;
                hash ^= (value >> 8) & 0xffu;
                hash *= 1099511628211UL;
                hash ^= (value >> 16) & 0xffu;
                hash *= 1099511628211UL;
                hash ^= (value >> 24) & 0xffu;
                hash *= 1099511628211UL;
                return hash;
            }
        }

        private static ulong Add(ulong hash, char value)
        {
            unchecked
            {
                hash ^= value;
                hash *= 1099511628211UL;
                return hash;
            }
        }
    }
}

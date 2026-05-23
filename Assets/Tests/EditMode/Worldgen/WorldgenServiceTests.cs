using System.Collections.Generic;
using System.Diagnostics;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Worldgen;
using NUnit.Framework;

// Design note:
// These tests pin the FOUNDATION worldgen contract before any runtime
// consumer (faction-store hydration, NPC dialog tree, region map UI) exists.
// Coverage stays scoped to the deterministic generator surface:
//  - same seed → same world (region count, first-NPC name identity);
//  - total population sums into the design band (>900K, <1.1M);
//  - settlement names are distinct globally;
//  - NPC names are distinct within each settlement;
//  - the 100-year history pass is deterministic for a given seed.
// The sample-output test is a Debug.WriteLine inspection helper and never
// asserts — it just dumps the first 5 regions, 10 settlements, and 20
// NPCs for seed=42 so the team can eyeball the generator output without
// spinning up the editor.
namespace EmberCrpg.Tests.EditMode.Worldgen
{
    /// <summary>Verifies the deterministic-replay invariants of the FOUNDATION worldgen.</summary>
    public sealed class WorldgenServiceTests
    {
        /// <summary>Same seed and parameters → byte-identical region count and identical first-NPC name.</summary>
        [Test]
        public void WorldgenService_SameSeedSameWorld()
        {
            var parameters = WorldgenParameters.Default;

            var worldA = WorldgenService.Generate(42u, parameters);
            var worldB = WorldgenService.Generate(42u, parameters);

            Assert.That(worldA.Regions.Count, Is.EqualTo(worldB.Regions.Count));
            Assert.That(worldA.Settlements.Count, Is.EqualTo(worldB.Settlements.Count));
            Assert.That(worldA.Factions.Count, Is.EqualTo(worldB.Factions.Count));
            Assert.That(worldA.FactionRelations.Count, Is.EqualTo(worldB.FactionRelations.Count));
            Assert.That(worldA.Npcs.Count, Is.EqualTo(worldB.Npcs.Count));
            Assert.That(worldA.History.Count, Is.EqualTo(worldB.History.Count));

            Assert.That(worldA.Regions[0].Name, Is.EqualTo(worldB.Regions[0].Name));
            Assert.That(worldA.Settlements[0].Name, Is.EqualTo(worldB.Settlements[0].Name));
            Assert.That(worldA.Npcs[0].Name, Is.EqualTo(worldB.Npcs[0].Name));
            Assert.That(worldA.Npcs[0].BirthYear, Is.EqualTo(worldB.Npcs[0].BirthYear));
            Assert.That(worldA.Npcs[0].Role, Is.EqualTo(worldB.Npcs[0].Role));
        }

        /// <summary>Different seeds → at least the first region's name differs (collision odds are vanishingly small).</summary>
        [Test]
        public void WorldgenService_DifferentSeedDifferentWorld()
        {
            var parameters = WorldgenParameters.Default;

            var worldA = WorldgenService.Generate(42u, parameters);
            var worldB = WorldgenService.Generate(99u, parameters);

            Assert.That(worldA.Regions[0].Name, Is.Not.EqualTo(worldB.Regions[0].Name));
        }

        /// <summary>Total realized population lands inside the design band [900K, 1.1M].</summary>
        [Test]
        public void WorldgenService_PopulationCount()
        {
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);

            Assert.That(world.TotalPopulation, Is.GreaterThan(900_000),
                "Total population should exceed 900K to feel Daggerfall-scale.");
            Assert.That(world.TotalPopulation, Is.LessThan(1_100_000),
                "Total population should stay below 1.1M so single-region densities stay sane.");
        }

        /// <summary>No two settlements share a name (the world's gazetteer is unique).</summary>
        [Test]
        public void WorldgenService_DistinctSettlementNames()
        {
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);

            var seen = new HashSet<string>();
            foreach (var settlement in world.Settlements)
            {
                Assert.That(seen.Add(settlement.Name), Is.True,
                    $"Settlement name collision: '{settlement.Name}' appears twice.");
            }
            Assert.That(seen.Count, Is.EqualTo(world.Settlements.Count));
        }

        /// <summary>Within a single settlement, no two NPCs share a name — a village should not have two Brytharlms.</summary>
        [Test]
        public void WorldgenService_DistinctNpcNames_within_settlement()
        {
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);

            var perSettlement = new Dictionary<SettlementId, HashSet<string>>();
            foreach (var npc in world.Npcs)
            {
                if (!perSettlement.TryGetValue(npc.Home, out var bag))
                {
                    bag = new HashSet<string>();
                    perSettlement[npc.Home] = bag;
                }
                Assert.That(bag.Add(npc.Name), Is.True,
                    $"NPC name collision in settlement {npc.Home}: '{npc.Name}' appears twice.");
            }
        }

        /// <summary>100-year history is deterministic for the same seed: same count, same first event, same last event.</summary>
        [Test]
        public void WorldgenService_HistoryDeterministic()
        {
            var parameters = WorldgenParameters.Default;

            var worldA = WorldgenService.Generate(42u, parameters);
            var worldB = WorldgenService.Generate(42u, parameters);

            Assert.That(worldA.History.Count, Is.EqualTo(parameters.HistoryYears));
            Assert.That(worldB.History.Count, Is.EqualTo(parameters.HistoryYears));

            for (int i = 0; i < worldA.History.Count; i++)
            {
                Assert.That(worldA.History[i].Year, Is.EqualTo(worldB.History[i].Year));
                Assert.That(worldA.History[i].Kind, Is.EqualTo(worldB.History[i].Kind));
                Assert.That(worldA.History[i].Subject, Is.EqualTo(worldB.History[i].Subject));
                Assert.That(worldA.History[i].Detail, Is.EqualTo(worldB.History[i].Detail));
            }
        }

        /// <summary>Every settlement points at a region that actually exists in the generated world.</summary>
        [Test]
        public void WorldgenService_SettlementsAttachToRealRegions()
        {
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);

            var regionIds = new HashSet<RegionId>();
            foreach (var region in world.Regions)
                regionIds.Add(region.Id);

            foreach (var settlement in world.Settlements)
            {
                Assert.That(regionIds.Contains(settlement.Region), Is.True,
                    $"Settlement {settlement.Name} attached to region {settlement.Region}, which is not in the world.");
            }
        }

        /// <summary>Inspection helper: dumps the first slice of seed=42's world to Debug for human eyeballing. Never asserts.</summary>
        [Test]
        public void WorldgenService_SampleSeed42_PrintsInspectionDump()
        {
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);

            Debug.WriteLine($"--- WorldgenService sample, seed=42 ---");
            Debug.WriteLine($"regions={world.Regions.Count} settlements={world.Settlements.Count} npcs={world.Npcs.Count} totalPop={world.TotalPopulation} history={world.History.Count}");

            Debug.WriteLine("first 5 regions:");
            for (int i = 0; i < 5 && i < world.Regions.Count; i++)
            {
                var r = world.Regions[i];
                Debug.WriteLine($"  R{r.Id.Value}: {r.Name} ({r.Biome}, pop[{r.PopulationLow}..{r.PopulationHigh}])");
            }

            Debug.WriteLine("first 10 settlements:");
            for (int i = 0; i < 10 && i < world.Settlements.Count; i++)
            {
                var s = world.Settlements[i];
                Debug.WriteLine($"  S{s.Id.Value}: {s.Name} ({s.Size}, pop={s.Population}, region={s.Region.Value})");
            }

            Debug.WriteLine("first 20 NPCs:");
            for (int i = 0; i < 20 && i < world.Npcs.Count; i++)
            {
                var n = world.Npcs[i];
                Debug.WriteLine($"  N{n.Id.Value}: {n.Name} ({n.Role}, born {n.BirthYear}, home={n.Home.Value}, faction={n.Faction.Value})");
            }

            // Always pass — this is an inspection dump only.
            Assert.That(world.Settlements.Count, Is.GreaterThan(0));
        }
    }
}

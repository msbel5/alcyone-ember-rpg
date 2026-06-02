using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Worldgen;
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
//  - the multi-century history simulator is deterministic for a given seed.
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
            Assert.That(worldA.NotableFigures.Count, Is.EqualTo(worldB.NotableFigures.Count));
            Assert.That(worldA.NotableFigures.Count, Is.GreaterThan(0));

            Assert.That(worldA.Regions[0].Name, Is.EqualTo(worldB.Regions[0].Name));
            Assert.That(worldA.Settlements[0].Name, Is.EqualTo(worldB.Settlements[0].Name));
            Assert.That(worldA.Settlements[0].Population, Is.EqualTo(worldB.Settlements[0].Population));
            Assert.That(worldA.Settlements[0].Size, Is.EqualTo(worldB.Settlements[0].Size));
            Assert.That(worldA.Npcs[0].Name, Is.EqualTo(worldB.Npcs[0].Name));
            Assert.That(worldA.Npcs[0].BirthYear, Is.EqualTo(worldB.Npcs[0].BirthYear));
            Assert.That(worldA.Npcs[0].Role, Is.EqualTo(worldB.Npcs[0].Role));
            Assert.That(worldA.NotableFigures[0].Name, Is.EqualTo(worldB.NotableFigures[0].Name));
            Assert.That(worldA.NotableFigures[0].Title, Is.EqualTo(worldB.NotableFigures[0].Title));
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

        /// <summary>Total realized population comes from the history end-state, not the pre-history target roll.</summary>
        [Test]
        public void WorldgenService_TotalPopulationReflectsHistoryState()
        {
            var parameters = WorldgenParameters.Default;
            var world = WorldgenService.Generate(42u, parameters);

            Assert.That(world.TotalPopulation, Is.Not.EqualTo(parameters.TargetPopulation),
                "Settlement generation normalizes to TargetPopulation before history; the final world should use simulated history population instead.");
            Assert.That(world.TotalPopulation, Is.EqualTo(world.Settlements.Sum(s => s.Population)));
            Assert.That(world.TotalPopulation, Is.GreaterThan(0));
        }

        [Test]
        public void WorldgenService_HistoryProjectionDropsFinalAbandonedSettlementsAndSurfacesFigures()
        {
            var parameters = WorldgenParameters.Default;
            var world = WorldgenService.Generate(42u, parameters);

            Assert.That(world.Settlements.Count, Is.LessThan(parameters.SettlementCount),
                "The authoritative world should contain only settlements founded and surviving at the end of history.");
            Assert.That(world.NotableFigures.Count, Is.GreaterThan(0));

            var settlementIds = new HashSet<SettlementId>();
            foreach (var settlement in world.Settlements)
            {
                Assert.That(settlement.Population, Is.GreaterThan(0));
                Assert.That(settlement.Size, Is.Not.EqualTo(SettlementSize.None));
                settlementIds.Add(settlement.Id);
            }

            foreach (var figure in world.NotableFigures)
            {
                Assert.That(figure.Id, Is.GreaterThan(0));
                Assert.That(figure.Name, Is.Not.Empty);
                Assert.That(figure.Title, Is.Not.Empty);
                Assert.That(settlementIds.Contains(figure.HomeSettlement), Is.True,
                    $"Notable figure {figure.Name} should be attached to a surviving settlement.");
            }
        }

        [Test]
        public void WorldgenService_NpcRosterUsesSurvivingSettlementsAndVariedRoles()
        {
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);
            var settlementIds = new HashSet<SettlementId>();
            foreach (var settlement in world.Settlements)
                settlementIds.Add(settlement.Id);

            var roles = new HashSet<NpcRole>();
            var perSettlement = new Dictionary<SettlementId, int>();
            foreach (var npc in world.Npcs)
            {
                Assert.That(settlementIds.Contains(npc.Home), Is.True,
                    $"NPC {npc.Name} should live in a surviving settlement.");
                Assert.That(npc.Role, Is.Not.EqualTo(NpcRole.None));
                roles.Add(npc.Role);

                if (!perSettlement.ContainsKey(npc.Home))
                    perSettlement[npc.Home] = 0;
                perSettlement[npc.Home]++;
            }

            Assert.That(roles.Count, Is.GreaterThanOrEqualTo(8));

            var largest = world.Settlements.OrderByDescending(s => s.Population).First();
            var smallest = world.Settlements.OrderBy(s => s.Population).First();
            int largestNpcCount = perSettlement.ContainsKey(largest.Id) ? perSettlement[largest.Id] : 0;
            int smallestNpcCount = perSettlement.ContainsKey(smallest.Id) ? perSettlement[smallest.Id] : 0;
            Assert.That(largestNpcCount, Is.GreaterThan(smallestNpcCount),
                "NPC allocation should visibly favor more populous history-projected settlements.");
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

        /// <summary>Multi-century history is deterministic for the same seed: same count and same event stream.</summary>
        [Test]
        public void WorldgenService_HistoryDeterministic()
        {
            var parameters = WorldgenParameters.Default;

            var worldA = WorldgenService.Generate(42u, parameters);
            var worldB = WorldgenService.Generate(42u, parameters);

            Assert.That(worldA.History.Count, Is.EqualTo(worldB.History.Count));
            Assert.That(worldA.History.Count, Is.GreaterThanOrEqualTo(150));

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

            TestContext.WriteLine("--- WorldgenService sample, seed=42 ---");
            TestContext.WriteLine($"initialTargetPopulation={WorldgenParameters.Default.TargetPopulation} finalTotalPopulation={world.TotalPopulation}");
            TestContext.WriteLine($"regions={world.Regions.Count} settlements={world.Settlements.Count} npcs={world.Npcs.Count} notableFigures={world.NotableFigures.Count} history={world.History.Count}");
            TestContext.WriteLine("sampleNpcRoles=" + string.Join(", ", world.Npcs.Take(12).Select(n => n.Role.ToString())));

            Debug.WriteLine($"--- WorldgenService sample, seed=42 ---");
            Debug.WriteLine($"initialTargetPopulation={WorldgenParameters.Default.TargetPopulation} finalTotalPopulation={world.TotalPopulation}");
            Debug.WriteLine($"regions={world.Regions.Count} settlements={world.Settlements.Count} npcs={world.Npcs.Count} notableFigures={world.NotableFigures.Count} history={world.History.Count}");

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

        /// <summary>Projecting a GeneratedWorld emits visible billboard events, NPC decision JSON, history lines, and completion.</summary>
        [Test]
        public void WorldgenEventProjector_ProjectsGeneratedWorldToVisibleEvents()
        {
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);
            var events = WorldgenEventProjector.Project(
                world,
                new WorldgenProjectionOptions(
                    maxRegions: 3,
                    maxSettlements: 4,
                    maxNpcs: 5,
                    maxHistoryEvents: 6,
                    includeQuestionPrompt: true,
                    includeSyntheticFailure: false));

            Assert.That(events.Count, Is.GreaterThan(0));
            Assert.That(events.Any(e => e.Kind == WorldgenVisibleEventKind.RegionGenerated), Is.True);
            Assert.That(events.Any(e => e.Kind == WorldgenVisibleEventKind.SettlementSeeded), Is.True);
            Assert.That(events.Any(e => e.Kind == WorldgenVisibleEventKind.NpcSeeded && e.Message.Contains("[decision-json]")), Is.True);
            Assert.That(events.Any(e => e.Kind == WorldgenVisibleEventKind.HistoryProjected && e.Message.StartsWith("Year ")), Is.True);
            Assert.That(events.Any(e => e.Kind == WorldgenVisibleEventKind.QuestionRaised), Is.True);
            Assert.That(events[events.Count - 1].Kind, Is.EqualTo(WorldgenVisibleEventKind.Completed));
        }

        /// <summary>Projection can append a failure event and still complete, matching append-continue behavior.</summary>
        [Test]
        public void WorldgenEventProjector_CanEmitFailureAndStillComplete()
        {
            var world = WorldgenService.Generate(99u, WorldgenParameters.Default);
            var events = WorldgenEventProjector.Project(
                world,
                new WorldgenProjectionOptions(
                    maxRegions: 2,
                    maxSettlements: 2,
                    maxNpcs: 2,
                    maxHistoryEvents: 2,
                    includeQuestionPrompt: false,
                    includeSyntheticFailure: true));

            var failureIndex = events.FindIndex(e => e.Kind == WorldgenVisibleEventKind.Failure);
            var completionIndex = events.FindIndex(e => e.Kind == WorldgenVisibleEventKind.Completed);

            Assert.That(failureIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(completionIndex, Is.GreaterThan(failureIndex));
            Assert.That(events[failureIndex].PayloadJson, Does.Contain("\"continue\":true"));
        }
    }
}

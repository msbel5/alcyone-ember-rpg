using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Rng;
using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Simulation.Worldgen.History;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Worldgen
{
    public sealed class WorldHistorySimulatorTests
    {
        [Test]
        public void SameSeed_ProducesIdenticalHistoryHash()
        {
            var parameters = WorldgenParameters.Default;
            var worldA = WorldgenService.Generate(42u, parameters);
            var worldB = WorldgenService.Generate(42u, parameters);

            Assert.That(worldA.History.Count, Is.EqualTo(worldB.History.Count));
            Assert.That(StableHistoryHash(worldA.History), Is.EqualTo(StableHistoryHash(worldB.History)));
        }

        [Test]
        public void DifferentSeed_ChangesHistoryHash()
        {
            var parameters = WorldgenParameters.Default;
            var worldA = WorldgenService.Generate(42u, parameters);
            var worldB = WorldgenService.Generate(99u, parameters);

            Assert.That(StableHistoryHash(worldA.History), Is.Not.EqualTo(StableHistoryHash(worldB.History)));
        }

        [Test]
        public void DefaultHistory_ProducesHundredsOfCausalEvents()
        {
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);

            Assert.That(world.History.Count, Is.GreaterThanOrEqualTo(150));
            Assert.That(CountKind(world.History, WorldHistoryKind.SettlementFounded), Is.GreaterThan(0));
            Assert.That(CountKind(world.History, WorldHistoryKind.RoadBuilt), Is.GreaterThan(0));
            Assert.That(CountKind(world.History, WorldHistoryKind.BattleFought), Is.GreaterThan(0));
            Assert.That(CountKind(world.History, WorldHistoryKind.LeaderCrowned), Is.GreaterThan(0));
        }

        [Test]
        public void FoundedSettlementEvents_ReferenceValidRegions()
        {
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);
            var regionIds = new HashSet<RegionId>();
            for (int i = 0; i < world.Regions.Count; i++)
                regionIds.Add(world.Regions[i].Id);

            int founded = 0;
            for (int i = 0; i < world.History.Count; i++)
            {
                var historyEvent = world.History[i];
                if (historyEvent.Kind != WorldHistoryKind.SettlementFounded)
                    continue;

                founded++;
                Assert.That(historyEvent.PrimaryRegion.HasValue, Is.True, "SettlementFounded should carry a region reference.");
                Assert.That(regionIds.Contains(historyEvent.PrimaryRegion.Value), Is.True);
                Assert.That(historyEvent.PrimarySettlement.HasValue, Is.True, "SettlementFounded should carry a settlement reference.");
            }

            Assert.That(founded, Is.GreaterThan(0));
        }

        [Test]
        public void RelationMatrix_StaysWithinRange()
        {
            var parameters = WorldgenParameters.Default;
            var world = WorldgenService.Generate(42u, parameters);
            var simulator = new WorldHistorySimulator();
            var result = simulator.Simulate(42u, parameters, world.Regions, world.Factions, world.Settlements);

            for (int i = 0; i < result.State.Factions.Length; i++)
            {
                for (int j = 0; j < result.State.Factions.Length; j++)
                {
                    Assert.That(result.State.Relations[i, j], Is.GreaterThanOrEqualTo(-1.0));
                    Assert.That(result.State.Relations[i, j], Is.LessThanOrEqualTo(1.0));
                }
            }
        }

        [Test]
        public void EachHistorySystem_IsDeterministicInIsolation()
        {
            var tuning = HistorySimulationTuning.From(TinyParameters());
            var systems = new IHistorySystem[]
            {
                new LifeEmergenceSystem(),
                new PopulationGrowthSystem(tuning),
                new MigrationSystem(tuning),
                new SettlementFoundingSystem(tuning),
                new RoadNetworkSystem(tuning),
                new DiplomacySystem(tuning),
                new WarSystem(tuning),
                new HistoricalFigureSystem(tuning),
            };

            for (int i = 0; i < systems.Length; i++)
            {
                string first = RunSystemSnapshot(systems[i], primeState: systems[i].SystemKey != HistorySystemKeys.LifeEmergence);
                string second = RunSystemSnapshot(systems[i], primeState: systems[i].SystemKey != HistorySystemKeys.LifeEmergence);
                Assert.That(first, Is.EqualTo(second), systems[i].Name);
            }
        }

        private static string RunSystemSnapshot(IHistorySystem system, bool primeState)
        {
            var state = TinyState(primeState);
            var sink = new HistoryEventBuffer(16);
            int year = -9;
            IDeterministicRng rng = HistoryRandom.Create(123u, system.SystemKey, 0, year);
            system.Tick(state, year, rng, sink);
            return StableHistoryHash(sink.Events) + ":" + StableStateHash(state);
        }

        private static HistoryState TinyState(bool prime)
        {
            var parameters = TinyParameters();
            var tuning = HistorySimulationTuning.From(parameters);
            var regions = new[]
            {
                new RegionRecord(new RegionId(1), "Aster Plain", 1000, 8000, BiomeKind.TemperatePlain),
                new RegionRecord(new RegionId(2), "Bryn Wood", 1000, 7000, BiomeKind.BorealForest),
                new RegionRecord(new RegionId(3), "Cairn Ridge", 1000, 6000, BiomeKind.MountainHighland),
                new RegionRecord(new RegionId(4), "Dawn Coast", 1000, 9000, BiomeKind.CoastalMarsh),
            };
            var factions = new[]
            {
                new FactionRecord(new FactionId(1), "House Aster", new string[0]),
                new FactionRecord(new FactionId(2), "League Dawn", new string[0]),
            };
            var settlements = new[]
            {
                new SettlementRecord(new SettlementId(1), new RegionId(1), "Asterford", 2500, SettlementSize.Town),
                new SettlementRecord(new SettlementId(2), new RegionId(4), "Dawnhaven", 900, SettlementSize.Village),
                new SettlementRecord(new SettlementId(3), new RegionId(2), "Brynwick", 300, SettlementSize.Hamlet),
            };

            var state = HistoryState.Create(123u, parameters, regions, factions, settlements, tuning);
            if (!prime)
                return state;

            state.LifeEmerged = true;
            for (int i = 0; i < state.Regions.Length; i++)
                state.Regions[i].Population = state.Regions[i].CarryingCapacity * 0.62;

            state.ForceFoundSettlement(0, -20, SettlementSize.Town);
            state.ForceFoundSettlement(1, -12, SettlementSize.Village);
            state.SetRelation(0, 1, -0.82);
            state.RecalculateFactionStrengths();
            return state;
        }

        private static WorldgenParameters TinyParameters()
        {
            return new WorldgenParameters(
                regionCount: 4,
                capitalCount: 0,
                cityCount: 0,
                townCount: 1,
                villageCount: 2,
                factionCount: 2,
                npcCount: 4,
                historyYears: 40,
                worldStartYear: 1);
        }

        private static int CountKind(IReadOnlyList<WorldHistoryEvent> history, WorldHistoryKind kind)
        {
            int count = 0;
            for (int i = 0; i < history.Count; i++)
            {
                if (history[i].Kind == kind)
                    count++;
            }
            return count;
        }

        private static string StableHistoryHash(IReadOnlyList<WorldHistoryEvent> history)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < history.Count; i++)
                {
                    var e = history[i];
                    hash = Mix(hash, e.Year);
                    hash = Mix(hash, (int)e.Kind);
                    hash = Mix(hash, e.Subject);
                    hash = Mix(hash, e.Detail);
                    hash = Mix(hash, e.PrimaryRegion.HasValue ? (int)e.PrimaryRegion.Value.Value : 0);
                    hash = Mix(hash, e.SecondaryRegion.HasValue ? (int)e.SecondaryRegion.Value.Value : 0);
                    hash = Mix(hash, e.PrimarySettlement.HasValue ? (int)e.PrimarySettlement.Value.Value : 0);
                    hash = Mix(hash, e.SecondarySettlement.HasValue ? (int)e.SecondarySettlement.Value.Value : 0);
                    hash = Mix(hash, e.PrimaryFaction.HasValue ? (int)e.PrimaryFaction.Value.Value : 0);
                    hash = Mix(hash, e.SecondaryFaction.HasValue ? (int)e.SecondaryFaction.Value.Value : 0);
                    hash = Mix(hash, e.PrimaryFigureId.HasValue ? e.PrimaryFigureId.Value : 0);
                    hash = Mix(hash, e.SecondaryFigureId.HasValue ? e.SecondaryFigureId.Value : 0);
                }
                return hash.ToString("X8");
            }
        }

        private static string StableStateHash(HistoryState state)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < state.Regions.Length; i++)
                {
                    hash = Mix(hash, (int)state.Regions[i].Record.Id.Value);
                    hash = Mix(hash, (int)state.Regions[i].Population);
                    hash = Mix(hash, state.Regions[i].RoadLevel);
                }
                for (int i = 0; i < state.Settlements.Length; i++)
                {
                    hash = Mix(hash, state.Settlements[i].Founded ? 1 : 0);
                    hash = Mix(hash, (int)state.Settlements[i].CurrentTier);
                    hash = Mix(hash, state.Settlements[i].FactionIndex);
                }
                for (int i = 0; i < state.Factions.Length; i++)
                {
                    for (int j = 0; j < state.Factions.Length; j++)
                        hash = Mix(hash, (int)(state.Relations[i, j] * 1000.0));
                }
                hash = Mix(hash, state.Figures.Count);
                return hash.ToString("X8");
            }
        }

        private static uint Mix(uint hash, string value)
        {
            for (int i = 0; i < value.Length; i++)
                hash = Mix(hash, value[i]);
            return hash;
        }

        private static uint Mix(uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 16777619u;
                return hash;
            }
        }
    }
}

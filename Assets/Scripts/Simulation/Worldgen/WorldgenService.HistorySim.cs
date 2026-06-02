// EMB-034: Deterministic multi-century world-history simulation phase.
using System.Collections.Generic;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Worldgen.History;

namespace EmberCrpg.Simulation.Worldgen
{
    public static partial class WorldgenService
    {
        private static List<WorldHistoryEvent> GenerateHistory(
            uint seed,
            WorldgenParameters parameters,
            IReadOnlyList<RegionRecord> regions,
            IReadOnlyList<FactionRecord> factions,
            IReadOnlyList<SettlementRecord> settlements)
        {
            var simulator = new WorldHistorySimulator();
            var result = simulator.Simulate(seed, parameters, regions, factions, settlements);
            return new List<WorldHistoryEvent>(result.Events);
        }
    }
}

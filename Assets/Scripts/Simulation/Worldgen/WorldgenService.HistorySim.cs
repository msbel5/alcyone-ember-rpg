// EMB-034: Deterministic multi-century world-history simulation phase.
using System.Collections.Generic;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Worldgen.History;

namespace EmberCrpg.Simulation.Worldgen
{
    public static partial class WorldgenService
    {
        private static WorldHistorySimulationResult GenerateHistory(
            uint seed,
            WorldgenParameters parameters,
            IReadOnlyList<RegionRecord> regions,
            IReadOnlyList<FactionRecord> factions,
            IReadOnlyList<SettlementRecord> settlements)
        {
            var simulator = new WorldHistorySimulator();
            return simulator.Simulate(seed, parameters, regions, factions, settlements);
        }
    }
}

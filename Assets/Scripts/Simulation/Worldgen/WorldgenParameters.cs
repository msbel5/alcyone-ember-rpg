using System;

// Design note:
// WorldgenParameters is the tunable knob set for the FOUNDATION generator.
// Kept as a plain immutable POCO with sensible Daggerfall-style defaults
// so tests can grab Default and the runtime can persist a custom set
// alongside the save without dragging a config-asset framework into
// Simulation. All counts are positive; the constructor pins invariants
// the same way the Domain records do.
namespace EmberCrpg.Simulation.Worldgen
{
    /// <summary>Tunable knobs for the deterministic worldgen pass.</summary>
    public sealed class WorldgenParameters
    {
        public WorldgenParameters(
            int regionCount,
            int capitalCount,
            int cityCount,
            int townCount,
            int villageCount,
            int factionCount,
            int npcCount,
            int historyYears,
            int worldStartYear)
        {
            if (regionCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(regionCount), regionCount, "regionCount must be positive.");
            if (capitalCount < 0)
                throw new ArgumentOutOfRangeException(nameof(capitalCount), capitalCount, "capitalCount must be non-negative.");
            if (cityCount < 0)
                throw new ArgumentOutOfRangeException(nameof(cityCount), cityCount, "cityCount must be non-negative.");
            if (townCount < 0)
                throw new ArgumentOutOfRangeException(nameof(townCount), townCount, "townCount must be non-negative.");
            if (villageCount < 0)
                throw new ArgumentOutOfRangeException(nameof(villageCount), villageCount, "villageCount must be non-negative.");
            if (capitalCount + cityCount + townCount + villageCount <= 0)
                throw new ArgumentException("At least one settlement bucket must be positive.", nameof(villageCount));
            if (factionCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(factionCount), factionCount, "factionCount must be positive.");
            if (npcCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(npcCount), npcCount, "npcCount must be positive.");
            if (historyYears <= 0)
                throw new ArgumentOutOfRangeException(nameof(historyYears), historyYears, "historyYears must be positive.");

            RegionCount = regionCount;
            CapitalCount = capitalCount;
            CityCount = cityCount;
            TownCount = townCount;
            VillageCount = villageCount;
            FactionCount = factionCount;
            NpcCount = npcCount;
            HistoryYears = historyYears;
            WorldStartYear = worldStartYear;
        }

        public int RegionCount { get; }
        public int CapitalCount { get; }
        public int CityCount { get; }
        public int TownCount { get; }
        public int VillageCount { get; }
        public int FactionCount { get; }
        public int NpcCount { get; }
        public int HistoryYears { get; }
        public int WorldStartYear { get; }

        public int SettlementCount
        {
            get { return CapitalCount + CityCount + TownCount + VillageCount; }
        }

        /// <summary>
        /// Default Daggerfall-style knob set: 50 regions, 1 capital + 8 cities
        /// + 40 towns + 151 villages = 200 settlements, 20 factions, 750
        /// NPCs, 100 years of history starting at year 1. Tuned so the
        /// expected total population lands in [900K, 1.1M] for the
        /// PopulationCount acceptance test.
        /// </summary>
        public static WorldgenParameters Default
        {
            get
            {
                return new WorldgenParameters(
                    regionCount: 50,
                    capitalCount: 1,
                    cityCount: 8,
                    townCount: 40,
                    villageCount: 151,
                    factionCount: 20,
                    npcCount: 750,
                    historyYears: 100,
                    worldStartYear: 1);
            }
        }
    }
}

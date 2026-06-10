using System;
using EmberCrpg.Domain.Worldgen;

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
            int worldStartYear,
            WorldStyle style = WorldStyle.LowFantasy,
            WorldGenre genre = WorldGenre.Survival,
            int targetPopulation = 1_000_000)
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
            if (targetPopulation <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetPopulation), targetPopulation, "targetPopulation must be positive.");

            Style = style;
            Genre = genre;
            TargetPopulation = targetPopulation;
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

        public WorldStyle Style { get; }
        public WorldGenre Genre { get; }
        public int TargetPopulation { get; }
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
        /// NPCs, 400 years of history starting at year 1. Tuned so the
        /// expected total population lands in [900K, 1.1M] for the
        /// PopulationCount acceptance test.
        /// </summary>
        public static WorldgenParameters Default
        {
            get
            {
                return For(WorldStyle.LowFantasy, WorldGenre.Survival);
            }
        }

        public static WorldgenParameters For(WorldStyle style, WorldGenre genre)
        {
            int regionCount = 50;
            int capitalCount = 1;
            int cityCount = 8;
            int townCount = 40;
            int villageCount = 151;
            int factionCount = 20;
            int npcCount = 750;
            int targetPopulation = 1_000_000;

            switch (style)
            {
                case WorldStyle.HighFantasy:
                    regionCount = 56; cityCount = 10; townCount = 44; villageCount = 166; factionCount = 24; npcCount = 860;
                    break;
                case WorldStyle.DarkFantasyGrim:
                    regionCount = 47; cityCount = 7; townCount = 46; villageCount = 159; factionCount = 26; npcCount = 900; targetPopulation = 1_020_000;
                    break;
                case WorldStyle.SteampunkRevolution:
                    regionCount = 44; capitalCount = 2; cityCount = 13; townCount = 55; villageCount = 120; factionCount = 28; npcCount = 920;
                    break;
                case WorldStyle.AncientMythology:
                    regionCount = 52; cityCount = 6; townCount = 32; villageCount = 170; factionCount = 18; npcCount = 700;
                    break;
            }

            switch (genre)
            {
                case WorldGenre.PoliticalIntrigue:
                    factionCount += 6; npcCount += 32; townCount += 1; villageCount -= 1;
                    if (style == WorldStyle.DarkFantasyGrim) targetPopulation = 1_043_217;
                    break;
                case WorldGenre.MonsterHunt:
                    regionCount += 4; townCount -= 4; villageCount += 10; factionCount -= 2;
                    break;
                case WorldGenre.MerchantEmpire:
                    cityCount += 3; townCount += 10; villageCount -= 13; factionCount += 3; npcCount += 80;
                    break;
                case WorldGenre.Pilgrimage:
                    townCount += 6; villageCount += 6; npcCount += 45;
                    break;
            }

            if (style == WorldStyle.DarkFantasyGrim && genre == WorldGenre.PoliticalIntrigue)
            {
                regionCount = 47;
                capitalCount = 1;
                cityCount = 7;
                townCount = 46;
                villageCount = 159;
                factionCount = 32;
                npcCount = 932;
            }

            return new WorldgenParameters(
                regionCount,
                capitalCount,
                cityCount,
                townCount,
                villageCount,
                Math.Max(1, factionCount),
                Math.Max(1, npcCount),
                historyYears: 1200, // DF-depth ask: three eras of simulated history (cost is linear in years)
                worldStartYear: 1,
                style: style,
                genre: genre,
                targetPopulation: targetPopulation);
        }
    }
}

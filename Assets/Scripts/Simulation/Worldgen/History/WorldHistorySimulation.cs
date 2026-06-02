using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.History
{
    public interface IHistoryEventSink
    {
        void Emit(WorldHistoryEvent historyEvent);
    }

    public interface IHistorySystem
    {
        int SystemKey { get; }
        string Name { get; }
        void Tick(HistoryState state, int year, IDeterministicRng rng, IHistoryEventSink sink);
    }

    public interface IEraStrategy
    {
        string Name { get; }
        bool IsActive(int yearOffset, int historyYears);
        IReadOnlyList<IHistorySystem> Systems { get; }
    }

    public sealed class WorldHistorySimulationResult
    {
        public WorldHistorySimulationResult(IReadOnlyList<WorldHistoryEvent> events, HistoryState state)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (state == null) throw new ArgumentNullException(nameof(state));

            var copy = new List<WorldHistoryEvent>(events.Count);
            for (int i = 0; i < events.Count; i++)
                copy.Add(events[i]);

            Events = new ReadOnlyCollection<WorldHistoryEvent>(copy);
            State = state;
        }

        public IReadOnlyList<WorldHistoryEvent> Events { get; }
        public HistoryState State { get; }
    }

    public sealed class WorldHistorySimulator
    {
        public WorldHistorySimulationResult Simulate(
            uint worldSeed,
            WorldgenParameters parameters,
            IReadOnlyList<RegionRecord> regions,
            IReadOnlyList<FactionRecord> factions,
            IReadOnlyList<SettlementRecord> settlements)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (regions == null) throw new ArgumentNullException(nameof(regions));

            var geography = WorldGeographyProvider.Generate(worldSeed, parameters, regions);
            return Simulate(worldSeed, parameters, geography, regions, factions, settlements);
        }

        public WorldHistorySimulationResult Simulate(
            uint worldSeed,
            WorldgenParameters parameters,
            WorldGeography geography,
            IReadOnlyList<RegionRecord> regions,
            IReadOnlyList<FactionRecord> factions,
            IReadOnlyList<SettlementRecord> settlements)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (geography == null) throw new ArgumentNullException(nameof(geography));
            if (regions == null) throw new ArgumentNullException(nameof(regions));
            if (factions == null) throw new ArgumentNullException(nameof(factions));
            if (settlements == null) throw new ArgumentNullException(nameof(settlements));

            var tuning = HistorySimulationTuning.From(parameters);
            var state = HistoryState.Create(worldSeed, parameters, geography, regions, factions, settlements, tuning);
            var sink = new HistoryEventBuffer(parameters.HistoryYears * 4);
            var eras = CreateEras(tuning);

            int startYear = parameters.WorldStartYear - parameters.HistoryYears;
            for (int offset = 0; offset < parameters.HistoryYears; offset++)
            {
                int year = startYear + offset;
                for (int eraIndex = 0; eraIndex < eras.Count; eraIndex++)
                {
                    var era = eras[eraIndex];
                    if (!era.IsActive(offset, parameters.HistoryYears))
                        continue;

                    var systems = era.Systems;
                    for (int systemIndex = 0; systemIndex < systems.Count; systemIndex++)
                    {
                        var system = systems[systemIndex];
                        var rng = HistoryRandom.Create(worldSeed, system.SystemKey, 0, year);
                        system.Tick(state, year, rng, sink);
                    }
                }
            }

            return new WorldHistorySimulationResult(sink.Events, state);
        }

        private static IReadOnlyList<IEraStrategy> CreateEras(HistorySimulationTuning tuning)
        {
            var formation = new IHistorySystem[]
            {
                new LifeEmergenceSystem(),
                new PopulationGrowthSystem(tuning),
            };
            var migration = new IHistorySystem[]
            {
                new PopulationGrowthSystem(tuning),
                new MigrationSystem(tuning),
                new SettlementFoundingSystem(tuning),
                new RoadNetworkSystem(tuning),
            };
            var civilization = new IHistorySystem[]
            {
                new PopulationGrowthSystem(tuning),
                new MigrationSystem(tuning),
                new SettlementFoundingSystem(tuning),
                new RoadNetworkSystem(tuning),
                new DiplomacySystem(tuning),
                new WarSystem(tuning),
                new HistoricalFigureSystem(tuning),
            };

            return new IEraStrategy[]
            {
                new BandedEraStrategy("Formation", 0, 18, formation),
                new BandedEraStrategy("Migration", 18, 48, migration),
                new BandedEraStrategy("Civilization", 48, 100, civilization),
            };
        }
    }

    public sealed class BandedEraStrategy : IEraStrategy
    {
        private readonly IHistorySystem[] _systems;
        private readonly int _startPercent;
        private readonly int _endPercent;

        public BandedEraStrategy(string name, int startPercent, int endPercent, IReadOnlyList<IHistorySystem> systems)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Era name is required.", nameof(name));
            if (startPercent < 0 || startPercent > 100) throw new ArgumentOutOfRangeException(nameof(startPercent));
            if (endPercent <= startPercent || endPercent > 100) throw new ArgumentOutOfRangeException(nameof(endPercent));
            if (systems == null) throw new ArgumentNullException(nameof(systems));

            Name = name;
            _startPercent = startPercent;
            _endPercent = endPercent;
            _systems = new IHistorySystem[systems.Count];
            for (int i = 0; i < systems.Count; i++)
                _systems[i] = systems[i];
        }

        public string Name { get; }
        public IReadOnlyList<IHistorySystem> Systems { get { return _systems; } }

        public bool IsActive(int yearOffset, int historyYears)
        {
            if (historyYears <= 0) return false;
            int start = (historyYears * _startPercent) / 100;
            int end = (historyYears * _endPercent) / 100;
            if (_endPercent == 100) end = historyYears;
            return yearOffset >= start && yearOffset < end;
        }
    }

    public sealed class HistorySimulationTuning
    {
        private HistorySimulationTuning()
        {
        }

        public double GrowthRate { get; private set; }
        public int ShockChancePerThousand { get; private set; }
        public double MigrationRate { get; private set; }
        public double BorderFriction { get; private set; }
        public double TradeBenefit { get; private set; }
        public double CourtEventChance { get; private set; }
        public double WarPressure { get; private set; }
        public int RoadCadenceYears { get; private set; }

        public static HistorySimulationTuning From(WorldgenParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var tuning = new HistorySimulationTuning
            {
                GrowthRate = 1.08,
                ShockChancePerThousand = 9,
                MigrationRate = 0.036,
                BorderFriction = 0.018,
                TradeBenefit = 0.012,
                CourtEventChance = 0.020,
                WarPressure = 0.015,
                RoadCadenceYears = 4,
            };

            switch (parameters.Style)
            {
                case WorldStyle.HighFantasy:
                    tuning.GrowthRate += 0.03;
                    tuning.TradeBenefit += 0.004;
                    tuning.CourtEventChance += 0.006;
                    break;
                case WorldStyle.DarkFantasyGrim:
                    tuning.ShockChancePerThousand += 13;
                    tuning.BorderFriction += 0.014;
                    tuning.WarPressure += 0.030;
                    break;
                case WorldStyle.SteampunkRevolution:
                    tuning.MigrationRate += 0.010;
                    tuning.TradeBenefit += 0.010;
                    tuning.RoadCadenceYears = 3;
                    break;
                case WorldStyle.AncientMythology:
                    tuning.ShockChancePerThousand += 6;
                    tuning.MigrationRate += 0.005;
                    break;
            }

            switch (parameters.Genre)
            {
                case WorldGenre.PoliticalIntrigue:
                    tuning.BorderFriction += 0.012;
                    tuning.CourtEventChance += 0.030;
                    tuning.WarPressure += 0.020;
                    break;
                case WorldGenre.MonsterHunt:
                    tuning.ShockChancePerThousand += 10;
                    tuning.MigrationRate += 0.004;
                    break;
                case WorldGenre.MerchantEmpire:
                    tuning.TradeBenefit += 0.018;
                    tuning.RoadCadenceYears = Math.Min(tuning.RoadCadenceYears, 3);
                    break;
                case WorldGenre.Pilgrimage:
                    tuning.MigrationRate += 0.008;
                    break;
            }

            return tuning;
        }
    }

    public sealed class HistoryState
    {
        private readonly Dictionary<RegionId, int> _regionIndexById;
        private readonly Dictionary<SettlementId, int> _settlementIndexById;
        private readonly int[][] _neighbors;
        private readonly HashSet<ulong> _settlementRoads;
        private readonly HashSet<ulong> _marriagePairs;
        private readonly int[,] _lastMigrationWaveYear;
        private readonly int[,] _lastBorderDisputeYear;
        private readonly int[,] _lastAllianceYear;
        private readonly bool[,] _civilizationDestroyed;
        private int _nextFigureId;

        private HistoryState(
            uint worldSeed,
            int startYear,
            int historyYears,
            int mapWidth,
            int mapHeight,
            WorldGeography geography,
            HistorySimulationTuning tuning,
            HistoryRegionState[] regions,
            HistoricalSettlementState[] settlements,
            HistoryFactionState[] factions,
            double[,] relations,
            Dictionary<RegionId, int> regionIndexById,
            Dictionary<SettlementId, int> settlementIndexById,
            int[][] neighbors)
        {
            WorldSeed = worldSeed;
            StartYear = startYear;
            HistoryYears = historyYears;
            GridWidth = mapWidth;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
            Geography = geography;
            Tuning = tuning;
            Regions = regions;
            Settlements = settlements;
            Factions = factions;
            Relations = relations;
            AtWar = new bool[factions.Length, factions.Length];
            WarStartedYear = new int[factions.Length, factions.Length];
            TradeLinks = new double[factions.Length, factions.Length];
            RegionDominantFaction = new int[regions.Length];
            Figures = new List<HistoricalFigureState>();
            _regionIndexById = regionIndexById;
            _settlementIndexById = settlementIndexById;
            _neighbors = neighbors;
            _settlementRoads = new HashSet<ulong>();
            _marriagePairs = new HashSet<ulong>();
            _lastMigrationWaveYear = CreateYearMatrix(regions.Length);
            _lastBorderDisputeYear = CreateYearMatrix(factions.Length);
            _lastAllianceYear = CreateYearMatrix(factions.Length);
            _civilizationDestroyed = new bool[factions.Length, factions.Length];
            _nextFigureId = 1;
            for (int i = 0; i < RegionDominantFaction.Length; i++)
                RegionDominantFaction[i] = -1;
        }

        public uint WorldSeed { get; }
        public int StartYear { get; }
        public int HistoryYears { get; }
        public int GridWidth { get; }
        public int MapWidth { get; }
        public int MapHeight { get; }
        public WorldGeography Geography { get; }
        public HistorySimulationTuning Tuning { get; }
        public HistoryRegionState[] Regions { get; }
        public HistoricalSettlementState[] Settlements { get; }
        public HistoryFactionState[] Factions { get; }
        public double[,] Relations { get; }
        public bool[,] AtWar { get; }
        public int[,] WarStartedYear { get; }
        public double[,] TradeLinks { get; }
        public int[] RegionDominantFaction { get; }
        public List<HistoricalFigureState> Figures { get; }
        public bool LifeEmerged { get; set; }
        public int LifeCradleRegionIndex { get; set; }

        public int FoundedSettlementCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Settlements.Length; i++)
                {
                    if (Settlements[i].Founded)
                        count++;
                }
                return count;
            }
        }

        public static HistoryState Create(
            uint worldSeed,
            WorldgenParameters parameters,
            IReadOnlyList<RegionRecord> regions,
            IReadOnlyList<FactionRecord> factions,
            IReadOnlyList<SettlementRecord> settlements,
            HistorySimulationTuning tuning)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (regions == null) throw new ArgumentNullException(nameof(regions));

            var geography = WorldGeographyProvider.Generate(worldSeed, parameters, regions);
            return Create(worldSeed, parameters, geography, regions, factions, settlements, tuning);
        }

        public static HistoryState Create(
            uint worldSeed,
            WorldgenParameters parameters,
            WorldGeography geography,
            IReadOnlyList<RegionRecord> regions,
            IReadOnlyList<FactionRecord> factions,
            IReadOnlyList<SettlementRecord> settlements,
            HistorySimulationTuning tuning)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (geography == null) throw new ArgumentNullException(nameof(geography));
            if (regions == null) throw new ArgumentNullException(nameof(regions));
            if (factions == null) throw new ArgumentNullException(nameof(factions));
            if (settlements == null) throw new ArgumentNullException(nameof(settlements));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (regions.Count == 0) throw new ArgumentException("At least one region is required.", nameof(regions));
            if (factions.Count == 0) throw new ArgumentException("At least one faction is required.", nameof(factions));

            var sortedRegions = new List<RegionRecord>(regions.Count);
            for (int i = 0; i < regions.Count; i++)
                sortedRegions.Add(regions[i]);
            sortedRegions.Sort(CompareRegionRecord);

            var sortedFactions = new List<FactionRecord>(factions.Count);
            for (int i = 0; i < factions.Count; i++)
                sortedFactions.Add(factions[i]);
            sortedFactions.Sort(CompareFactionRecord);

            var sortedSettlements = new List<SettlementRecord>(settlements.Count);
            for (int i = 0; i < settlements.Count; i++)
                sortedSettlements.Add(settlements[i]);
            sortedSettlements.Sort(CompareSettlementRecord);

            int startYear = parameters.WorldStartYear - parameters.HistoryYears;
            var regionIndexById = new Dictionary<RegionId, int>();
            var settlementPopulationByRegion = new long[sortedRegions.Count];

            for (int i = 0; i < sortedRegions.Count; i++)
                regionIndexById[sortedRegions[i].Id] = i;

            var regionTileGroups = BuildRegionTileGroups(geography, regionIndexById, sortedRegions.Count);

            for (int i = 0; i < sortedSettlements.Count; i++)
            {
                int regionIndex;
                if (regionIndexById.TryGetValue(sortedSettlements[i].Region, out regionIndex))
                    settlementPopulationByRegion[regionIndex] += sortedSettlements[i].Population;
            }

            var regionStates = new HistoryRegionState[sortedRegions.Count];
            int averagePopulationTarget = Math.Max(1, parameters.TargetPopulation / sortedRegions.Count);
            for (int i = 0; i < sortedRegions.Count; i++)
            {
                var record = sortedRegions[i];
                int centerTile = ChooseRegionCenterTile(geography, regionTileGroups[i], record);
                int x = centerTile % geography.Width;
                int y = centerTile / geography.Width;
                double settlementSignal = Clamp01((double)settlementPopulationByRegion[i] / (averagePopulationTarget * 1.8));
                double suitability = Clamp01(BiomeSuitability(record.Biome, parameters.Style, parameters.Genre) + (settlementSignal * 0.10));
                double kMax = record.PopulationHigh + (averagePopulationTarget * 2.6);
                double carryingCapacity = Math.Max(1_500.0, kMax * suitability);
                double movementCost = BiomeMovementCost(record.Biome);
                regionStates[i] = new HistoryRegionState(record, i, x, y, suitability, movementCost, carryingCapacity);
            }

            var factionStates = new HistoryFactionState[sortedFactions.Count];
            for (int i = 0; i < sortedFactions.Count; i++)
                factionStates[i] = new HistoryFactionState(sortedFactions[i], i);

            var settlementIndexById = new Dictionary<SettlementId, int>();
            var settlementStates = new HistoricalSettlementState[sortedSettlements.Count];
            var tileOccupancy = new int[geography.TileCount];
            for (int i = 0; i < sortedSettlements.Count; i++)
            {
                var record = sortedSettlements[i];
                int regionIndex;
                if (!regionIndexById.TryGetValue(record.Region, out regionIndex))
                    throw new ArgumentException("Settlement " + record.Name + " points at missing region " + record.Region + ".", nameof(settlements));

                int factionIndex = sortedFactions.Count == 0 ? 0 : (int)((record.Id.Value - 1UL) % (ulong)sortedFactions.Count);
                int tileIndex = ChooseSettlementTile(geography, record, regionStates[regionIndex], regionTileGroups[regionIndex], tileOccupancy);
                tileOccupancy[tileIndex]++;
                settlementStates[i] = new HistoricalSettlementState(record, i, regionIndex, factionIndex, tileIndex % geography.Width, tileIndex / geography.Width);
                settlementIndexById[record.Id] = i;
            }

            var relations = new double[sortedFactions.Count, sortedFactions.Count];
            for (int i = 0; i < sortedFactions.Count; i++)
            {
                for (int j = 0; j < sortedFactions.Count; j++)
                {
                    if (i == j)
                    {
                        relations[i, j] = 1.0;
                        continue;
                    }

                    if (i < j)
                    {
                        var pairRng = HistoryRandom.Create(worldSeed, HistorySystemKeys.Diplomacy, PairAgent(i, j), startYear);
                        double value = ((pairRng.NextInt(401) - 200) / 1000.0) + InitialRelationBias(parameters.Style, parameters.Genre);
                        value = Clamp(value, -1.0, 1.0);
                        relations[i, j] = value;
                        relations[j, i] = value;
                    }
                }
            }

            return new HistoryState(
                worldSeed,
                startYear,
                parameters.HistoryYears,
                geography.Width,
                geography.Height,
                geography,
                tuning,
                regionStates,
                settlementStates,
                factionStates,
                relations,
                regionIndexById,
                settlementIndexById,
                BuildNeighbors(geography, regionIndexById, sortedRegions.Count));
        }

        private static List<int>[] BuildRegionTileGroups(WorldGeography geography, Dictionary<RegionId, int> regionIndexById, int regionCount)
        {
            var groups = new List<int>[regionCount];
            for (int i = 0; i < groups.Length; i++)
                groups[i] = new List<int>();

            for (int tile = 0; tile < geography.TileCount; tile++)
            {
                int regionIndex;
                if (!regionIndexById.TryGetValue(geography.RegionIds[tile], out regionIndex))
                    throw new ArgumentException("Geography tile points at a region that is not part of this history state.", nameof(geography));

                if (geography.LandMask[tile])
                    groups[regionIndex].Add(tile);
            }

            return groups;
        }

        private static int ChooseRegionCenterTile(WorldGeography geography, IReadOnlyList<int> regionTiles, RegionRecord record)
        {
            if (regionTiles.Count == 0)
            {
                if (record.HasTilePosition && record.TileX < geography.Width && record.TileY < geography.Height)
                    return (record.TileY * geography.Width) + record.TileX;

                for (int tile = 0; tile < geography.TileCount; tile++)
                {
                    if (geography.LandMask[tile])
                        return tile;
                }

                return 0;
            }

            double centerX = 0.0;
            double centerY = 0.0;
            for (int i = 0; i < regionTiles.Count; i++)
            {
                centerX += regionTiles[i] % geography.Width;
                centerY += regionTiles[i] / geography.Width;
            }

            centerX /= regionTiles.Count;
            centerY /= regionTiles.Count;

            int best = regionTiles[0];
            double bestDistance = double.MaxValue;
            for (int i = 0; i < regionTiles.Count; i++)
            {
                int tile = regionTiles[i];
                int x = tile % geography.Width;
                int y = tile / geography.Width;
                double dx = x - centerX;
                double dy = y - centerY;
                double distance = (dx * dx) + (dy * dy);
                if (distance < bestDistance || (distance == bestDistance && tile < best))
                {
                    bestDistance = distance;
                    best = tile;
                }
            }

            return best;
        }

        private static int ChooseSettlementTile(
            WorldGeography geography,
            SettlementRecord record,
            HistoryRegionState region,
            IReadOnlyList<int> candidates,
            int[] tileOccupancy)
        {
            int bestTile = -1;
            int bestScore = int.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                int tile = candidates[i];
                int score = ScoreSettlementTile(geography, record, region, tile, tileOccupancy[tile]);
                if (score > bestScore || (score == bestScore && tile < bestTile))
                {
                    bestScore = score;
                    bestTile = tile;
                }
            }

            return bestTile >= 0 ? bestTile : ChooseGlobalSettlementTile(geography, record, region, tileOccupancy);
        }

        private static int ChooseGlobalSettlementTile(WorldGeography geography, SettlementRecord record, HistoryRegionState region, int[] tileOccupancy)
        {
            int bestTile = -1;
            int bestScore = int.MinValue;
            for (int tile = 0; tile < geography.TileCount; tile++)
            {
                if (!geography.LandMask[tile])
                    continue;

                int score = ScoreSettlementTile(geography, record, region, tile, tileOccupancy[tile]);
                if (score > bestScore || (score == bestScore && tile < bestTile))
                {
                    bestScore = score;
                    bestTile = tile;
                }
            }

            return bestTile >= 0 ? bestTile : 0;
        }

        private static int ScoreSettlementTile(WorldGeography geography, SettlementRecord record, HistoryRegionState region, int tile, int occupancy)
        {
            int x = tile % geography.Width;
            int y = tile / geography.Width;
            int distance = Math.Abs(region.X - x) + Math.Abs(region.Y - y);
            int score = (int)(SettlementBiomeWeight(geography.WorldBiomes[tile], record.Size) * 1000.0);
            score += (int)(geography.Elevation[tile] * 90.0);
            score -= occupancy * 90;
            score -= distance * ((int)record.Size >= (int)SettlementSize.Town ? 18 : 9);
            score += StableSettlementTie(record.Id, tile);
            return score;
        }

        private static double SettlementBiomeWeight(BiomeKind biome, SettlementSize size)
        {
            double value;
            switch (biome)
            {
                case BiomeKind.TemperatePlain: value = 1.18; break;
                case BiomeKind.CoastalMarsh: value = 1.08; break;
                case BiomeKind.BorealForest: value = 0.94; break;
                case BiomeKind.AridSteppe: value = 0.64; break;
                case BiomeKind.TropicalJungle: value = 0.62; break;
                case BiomeKind.MountainHighland: value = 0.44; break;
                case BiomeKind.DesertWaste: value = 0.34; break;
                case BiomeKind.FrozenTundra: value = 0.32; break;
                default: value = 0.50; break;
            }

            if (size == SettlementSize.Capital || size == SettlementSize.City)
            {
                if (biome == BiomeKind.TemperatePlain || biome == BiomeKind.CoastalMarsh || biome == BiomeKind.BorealForest)
                    value += 0.24;
                if (biome == BiomeKind.MountainHighland || biome == BiomeKind.DesertWaste || biome == BiomeKind.FrozenTundra)
                    value -= 0.16;
            }

            return value;
        }

        private static int StableSettlementTie(SettlementId id, int tile)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)id.Value) * 16777619u;
                hash = (hash ^ (uint)(id.Value >> 32)) * 16777619u;
                hash = (hash ^ (uint)tile) * 16777619u;
                return (int)(hash % 37u);
            }
        }

        public int YearOffset(int year)
        {
            return year - StartYear;
        }

        public IReadOnlyList<int> NeighborsOf(int regionIndex)
        {
            return _neighbors[regionIndex];
        }

        public bool TryGetRegionIndex(RegionId id, out int index)
        {
            return _regionIndexById.TryGetValue(id, out index);
        }

        public bool TryGetSettlementIndex(SettlementId id, out int index)
        {
            return _settlementIndexById.TryGetValue(id, out index);
        }

        public bool IsLocalSuitabilityMaximum(int regionIndex)
        {
            double suitability = Regions[regionIndex].Suitability;
            var neighbors = _neighbors[regionIndex];
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (Regions[neighbors[i]].Suitability > suitability)
                    return false;
            }
            return true;
        }

        public int FoundedSettlementsInRegion(int regionIndex)
        {
            int count = 0;
            for (int i = 0; i < Settlements.Length; i++)
            {
                if (Settlements[i].Founded && Settlements[i].RegionIndex == regionIndex)
                    count++;
            }
            return count;
        }

        public bool HasMinimumSpacing(int regionIndex, SettlementSize finalSize, double population, double threshold)
        {
            if (finalSize == SettlementSize.Hamlet || finalSize == SettlementSize.Village)
                return true;

            for (int i = 0; i < Settlements.Length; i++)
            {
                var settlement = Settlements[i];
                if (!settlement.Founded || (int)settlement.CurrentTier < (int)SettlementSize.Town)
                    continue;

                bool sameRegion = settlement.RegionIndex == regionIndex;
                bool adjacent = !sameRegion && AreRegionsAdjacent(settlement.RegionIndex, regionIndex);
                if ((sameRegion || adjacent) && population < threshold * 2.2)
                    return false;
            }

            return true;
        }

        public bool AreRegionsAdjacent(int a, int b)
        {
            var neighbors = _neighbors[a];
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] == b)
                    return true;
            }
            return false;
        }

        public double RegionStepCost(int fromRegionIndex, int toRegionIndex)
        {
            double from = Regions[fromRegionIndex].CurrentMovementCost;
            double to = Regions[toRegionIndex].CurrentMovementCost;
            return 1.0 + ((from + to) * 0.5);
        }

        public HistoryPathResult FindLeastCostPath(int startRegionIndex, int endRegionIndex, double maxCost)
        {
            if (startRegionIndex == endRegionIndex)
                return new HistoryPathResult(true, 0.0, new[] { startRegionIndex });

            int count = Regions.Length;
            var distances = new double[count];
            var previous = new int[count];
            var visited = new bool[count];
            for (int i = 0; i < count; i++)
            {
                distances[i] = double.PositiveInfinity;
                previous[i] = -1;
            }
            distances[startRegionIndex] = 0.0;

            for (int step = 0; step < count; step++)
            {
                int current = -1;
                double best = double.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    if (!visited[i] && distances[i] < best)
                    {
                        current = i;
                        best = distances[i];
                    }
                }

                if (current < 0 || best > maxCost)
                    break;
                if (current == endRegionIndex)
                    break;

                visited[current] = true;
                var neighbors = _neighbors[current];
                for (int i = 0; i < neighbors.Length; i++)
                {
                    int neighbor = neighbors[i];
                    if (visited[neighbor])
                        continue;

                    double stepCost = RegionStepCost(current, neighbor);
                    if (stepCost >= 80.0)
                        continue;

                    double candidate = distances[current] + stepCost;
                    if (candidate < distances[neighbor])
                    {
                        distances[neighbor] = candidate;
                        previous[neighbor] = current;
                    }
                }
            }

            if (double.IsPositiveInfinity(distances[endRegionIndex]) || distances[endRegionIndex] > maxCost)
                return new HistoryPathResult(false, distances[endRegionIndex], Array.Empty<int>());

            var reverse = new List<int>();
            int cursor = endRegionIndex;
            while (cursor >= 0)
            {
                reverse.Add(cursor);
                cursor = previous[cursor];
            }
            reverse.Reverse();
            return new HistoryPathResult(true, distances[endRegionIndex], reverse.ToArray());
        }

        public void ReinforceRoadPath(IReadOnlyList<int> path)
        {
            for (int i = 0; i < path.Count; i++)
                Regions[path[i]].AddRoadReinforcement();
        }

        public bool HasRoadConnection(int settlementA, int settlementB)
        {
            return _settlementRoads.Contains(PairKey(settlementA, settlementB));
        }

        public void AddRoadConnection(int settlementA, int settlementB)
        {
            _settlementRoads.Add(PairKey(settlementA, settlementB));
            int factionA = Settlements[settlementA].FactionIndex;
            int factionB = Settlements[settlementB].FactionIndex;
            if (factionA != factionB)
            {
                TradeLinks[factionA, factionB] += 1.0;
                TradeLinks[factionB, factionA] += 1.0;
            }
        }

        public bool WasRecentMigrationWave(int regionA, int regionB, int year, int cooldownYears)
        {
            int previous = _lastMigrationWaveYear[regionA, regionB];
            return previous != int.MinValue && year - previous < cooldownYears;
        }

        public void MarkMigrationWave(int regionA, int regionB, int year)
        {
            _lastMigrationWaveYear[regionA, regionB] = year;
            _lastMigrationWaveYear[regionB, regionA] = year;
        }

        public bool WasRecentBorderDispute(int factionA, int factionB, int year)
        {
            int previous = _lastBorderDisputeYear[factionA, factionB];
            return previous != int.MinValue && year - previous < 12;
        }

        public void MarkBorderDispute(int factionA, int factionB, int year)
        {
            _lastBorderDisputeYear[factionA, factionB] = year;
            _lastBorderDisputeYear[factionB, factionA] = year;
        }

        public bool WasRecentAlliance(int factionA, int factionB, int year)
        {
            int previous = _lastAllianceYear[factionA, factionB];
            return previous != int.MinValue && year - previous < 24;
        }

        public void MarkAlliance(int factionA, int factionB, int year)
        {
            _lastAllianceYear[factionA, factionB] = year;
            _lastAllianceYear[factionB, factionA] = year;
        }

        public bool HasMarriagePair(int factionA, int factionB)
        {
            return _marriagePairs.Contains(PairKey(factionA, factionB));
        }

        public void MarkMarriagePair(int factionA, int factionB)
        {
            _marriagePairs.Add(PairKey(factionA, factionB));
        }

        public bool WasCivilizationDestroyed(int factionA, int factionB)
        {
            return _civilizationDestroyed[factionA, factionB];
        }

        public void MarkCivilizationDestroyed(int factionA, int factionB)
        {
            _civilizationDestroyed[factionA, factionB] = true;
            _civilizationDestroyed[factionB, factionA] = true;
        }

        public void SetRelation(int factionA, int factionB, double value)
        {
            value = Clamp(value, -1.0, 1.0);
            Relations[factionA, factionB] = value;
            Relations[factionB, factionA] = value;
        }

        public void RecalculateFactionStrengths()
        {
            for (int i = 0; i < Factions.Length; i++)
            {
                Factions[i].Strength = 0.0;
                Factions[i].FoundedSettlementCount = 0;
            }

            var bestRegionWeight = new double[Regions.Length];
            for (int i = 0; i < RegionDominantFaction.Length; i++)
                RegionDominantFaction[i] = -1;

            for (int i = 0; i < Settlements.Length; i++)
            {
                var settlement = Settlements[i];
                if (!settlement.Founded)
                    continue;

                var faction = Factions[settlement.FactionIndex];
                if (!faction.Active)
                    continue;

                double tierWeight = TierWeight(settlement.CurrentTier);
                double populationWeight = Math.Max(50.0, Regions[settlement.RegionIndex].Population * 0.08);
                faction.Strength += populationWeight + tierWeight + (Regions[settlement.RegionIndex].Suitability * 100.0);
                faction.FoundedSettlementCount++;

                double regionalWeight = populationWeight + tierWeight;
                if (regionalWeight > bestRegionWeight[settlement.RegionIndex])
                {
                    bestRegionWeight[settlement.RegionIndex] = regionalWeight;
                    RegionDominantFaction[settlement.RegionIndex] = settlement.FactionIndex;
                }
            }
        }

        public int BorderScore(int factionA, int factionB)
        {
            int score = 0;
            for (int r = 0; r < Regions.Length; r++)
            {
                if (RegionDominantFaction[r] != factionA)
                    continue;

                var neighbors = _neighbors[r];
                for (int n = 0; n < neighbors.Length; n++)
                {
                    if (RegionDominantFaction[neighbors[n]] == factionB)
                        score++;
                }
            }
            return score;
        }

        public int SharedRegionCompetition(int factionA, int factionB)
        {
            int score = 0;
            for (int r = 0; r < Regions.Length; r++)
            {
                bool hasA = false;
                bool hasB = false;
                for (int s = 0; s < Settlements.Length; s++)
                {
                    var settlement = Settlements[s];
                    if (!settlement.Founded || settlement.RegionIndex != r)
                        continue;
                    if (settlement.FactionIndex == factionA) hasA = true;
                    if (settlement.FactionIndex == factionB) hasB = true;
                }
                if (hasA && hasB)
                    score++;
            }
            return score;
        }

        public HistoricalSettlementState FindWarTarget(int attackerFaction, int defenderFaction)
        {
            HistoricalSettlementState best = null;
            int bestScore = int.MaxValue;
            for (int i = 0; i < Settlements.Length; i++)
            {
                var settlement = Settlements[i];
                if (!settlement.Founded || settlement.FactionIndex != defenderFaction)
                    continue;

                int borderDistance = NearestFactionRegionDistance(settlement.RegionIndex, attackerFaction);
                int score = borderDistance * 100 - (int)TierWeight(settlement.CurrentTier);
                if (score < bestScore)
                {
                    best = settlement;
                    bestScore = score;
                }
            }
            return best;
        }

        public int NearestFactionRegionDistance(int regionIndex, int factionIndex)
        {
            int best = int.MaxValue;
            for (int i = 0; i < Settlements.Length; i++)
            {
                var settlement = Settlements[i];
                if (!settlement.Founded || settlement.FactionIndex != factionIndex)
                    continue;

                int distance = ManhattanDistance(regionIndex, settlement.RegionIndex);
                if (distance < best)
                    best = distance;
            }
            return best == int.MaxValue ? GridWidth * 2 : best;
        }

        public int ManhattanDistance(int regionA, int regionB)
        {
            return Math.Abs(Regions[regionA].X - Regions[regionB].X) + Math.Abs(Regions[regionA].Y - Regions[regionB].Y);
        }

        public HistoricalFigureState AddFigure(string name, int factionIndex, int birthYear, int dynastySeed)
        {
            var figure = new HistoricalFigureState(_nextFigureId++, name, factionIndex, birthYear, dynastySeed);
            Figures.Add(figure);
            return figure;
        }

        public HistoricalFigureState CurrentLeader(int factionIndex)
        {
            int leaderId = Factions[factionIndex].LeaderFigureId;
            if (leaderId <= 0)
                return null;

            for (int i = 0; i < Figures.Count; i++)
            {
                if (Figures[i].Id == leaderId && Figures[i].Alive)
                    return Figures[i];
            }
            return null;
        }

        public HistoricalFigureState FindLivingHeir(int factionIndex)
        {
            HistoricalFigureState best = null;
            for (int i = 0; i < Figures.Count; i++)
            {
                var figure = Figures[i];
                if (!figure.Alive || figure.FactionIndex != factionIndex || !figure.IsHeir)
                    continue;
                if (best == null || figure.BirthYear < best.BirthYear || (figure.BirthYear == best.BirthYear && figure.Id < best.Id))
                    best = figure;
            }
            return best;
        }

        public void ForceFoundSettlement(int settlementIndex, int year, SettlementSize tier)
        {
            var settlement = Settlements[settlementIndex];
            settlement.Founded = true;
            settlement.FoundedYear = year;
            settlement.CurrentTier = tier;
            var faction = Factions[settlement.FactionIndex];
            faction.Active = true;
            if (!faction.CivilizationFounded)
                faction.CivilizationFounded = true;
        }

        public static int PairAgent(int a, int b)
        {
            int low = a < b ? a : b;
            int high = a < b ? b : a;
            unchecked
            {
                return ((low + 1) * 397) ^ (high + 1);
            }
        }

        private static int[,] CreateYearMatrix(int size)
        {
            var matrix = new int[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                    matrix[i, j] = int.MinValue;
            }
            return matrix;
        }

        private static int[][] BuildNeighbors(WorldGeography geography, Dictionary<RegionId, int> regionIndexById, int count)
        {
            var neighbors = new List<int>[count];
            for (int i = 0; i < count; i++)
                neighbors[i] = new List<int>(4);

            for (int y = 0; y < geography.Height; y++)
            {
                for (int x = 0; x < geography.Width; x++)
                {
                    int index = (y * geography.Width) + x;
                    if (!geography.LandMask[index])
                        continue;

                    int region = RegionIndexAtTile(geography, regionIndexById, index);
                    if (x + 1 < geography.Width)
                        TryAddGeographyNeighbor(geography, regionIndexById, neighbors, region, index + 1);
                    if (y + 1 < geography.Height)
                        TryAddGeographyNeighbor(geography, regionIndexById, neighbors, region, index + geography.Width);
                }
            }

            AddNearestFallbackNeighbors(geography, regionIndexById, neighbors);

            var result = new int[count][];
            for (int i = 0; i < count; i++)
            {
                neighbors[i].Sort();
                result[i] = neighbors[i].ToArray();
            }

            return result;
        }

        private static void TryAddGeographyNeighbor(
            WorldGeography geography,
            Dictionary<RegionId, int> regionIndexById,
            List<int>[] neighbors,
            int region,
            int neighborTile)
        {
            if (!geography.LandMask[neighborTile])
                return;

            int other = RegionIndexAtTile(geography, regionIndexById, neighborTile);
            if (region == other)
                return;

            AddNeighborPair(neighbors, region, other);
        }

        private static void AddNearestFallbackNeighbors(WorldGeography geography, Dictionary<RegionId, int> regionIndexById, List<int>[] neighbors)
        {
            if (neighbors.Length <= 1)
                return;

            var groups = BuildRegionTileGroups(geography, regionIndexById, neighbors.Length);
            var centers = new int[neighbors.Length];
            for (int i = 0; i < centers.Length; i++)
                centers[i] = ChooseCenterTileFromGroup(geography, groups[i], i, regionIndexById);

            for (int region = 0; region < neighbors.Length; region++)
            {
                if (neighbors[region].Count > 0)
                    continue;

                int nearest = 0;
                int bestDistance = int.MaxValue;
                for (int other = 0; other < centers.Length; other++)
                {
                    if (other == region)
                        continue;

                    int distance = TileDistanceSquared(geography.Width, centers[region], centers[other]);
                    if (distance < bestDistance || (distance == bestDistance && other < nearest))
                    {
                        nearest = other;
                        bestDistance = distance;
                    }
                }

                AddNeighborPair(neighbors, region, nearest);
            }
        }

        private static int ChooseCenterTileFromGroup(WorldGeography geography, IReadOnlyList<int> group, int regionIndex, Dictionary<RegionId, int> regionIndexById)
        {
            if (group.Count > 0)
                return ChooseRegionCenterTile(geography, group, new RegionRecord(new RegionId((ulong)(regionIndex + 1)), "fallback", 0, 0, BiomeKind.TemperatePlain));

            for (int tile = 0; tile < geography.TileCount; tile++)
            {
                if (RegionIndexAtTile(geography, regionIndexById, tile) == regionIndex)
                    return tile;
            }

            return 0;
        }

        private static void AddNeighborPair(List<int>[] neighbors, int a, int b)
        {
            if (!neighbors[a].Contains(b))
                neighbors[a].Add(b);
            if (!neighbors[b].Contains(a))
                neighbors[b].Add(a);
        }

        private static int RegionIndexAtTile(WorldGeography geography, Dictionary<RegionId, int> regionIndexById, int tile)
        {
            int region;
            if (!regionIndexById.TryGetValue(geography.RegionIds[tile], out region))
                throw new ArgumentException("Geography tile points at a region that is not part of this history state.", nameof(geography));
            return region;
        }

        private static int TileDistanceSquared(int width, int a, int b)
        {
            int ax = a % width;
            int ay = a / width;
            int bx = b % width;
            int by = b / width;
            int dx = ax - bx;
            int dy = ay - by;
            return (dx * dx) + (dy * dy);
        }

        private static int CompareRegionRecord(RegionRecord a, RegionRecord b)
        {
            return a.Id.Value.CompareTo(b.Id.Value);
        }

        private static int CompareFactionRecord(FactionRecord a, FactionRecord b)
        {
            return a.Id.Value.CompareTo(b.Id.Value);
        }

        private static int CompareSettlementRecord(SettlementRecord a, SettlementRecord b)
        {
            return a.Id.Value.CompareTo(b.Id.Value);
        }

        private static double BiomeSuitability(BiomeKind biome, WorldStyle style, WorldGenre genre)
        {
            double value;
            switch (biome)
            {
                case BiomeKind.TemperatePlain: value = 0.94; break;
                case BiomeKind.BorealForest: value = 0.72; break;
                case BiomeKind.CoastalMarsh: value = 0.78; break;
                case BiomeKind.AridSteppe: value = 0.54; break;
                case BiomeKind.MountainHighland: value = 0.34; break;
                case BiomeKind.DesertWaste: value = 0.20; break;
                case BiomeKind.TropicalJungle: value = 0.58; break;
                case BiomeKind.FrozenTundra: value = 0.24; break;
                default: value = 0.35; break;
            }

            if (style == WorldStyle.SteampunkRevolution && (biome == BiomeKind.TemperatePlain || biome == BiomeKind.CoastalMarsh))
                value += 0.06;
            if (style == WorldStyle.AncientMythology && (biome == BiomeKind.MountainHighland || biome == BiomeKind.DesertWaste))
                value += 0.04;
            if (genre == WorldGenre.MerchantEmpire && biome == BiomeKind.CoastalMarsh)
                value += 0.05;
            if (genre == WorldGenre.Survival && (biome == BiomeKind.FrozenTundra || biome == BiomeKind.DesertWaste))
                value -= 0.04;

            return value;
        }

        private static double BiomeMovementCost(BiomeKind biome)
        {
            switch (biome)
            {
                case BiomeKind.TemperatePlain: return 1.0;
                case BiomeKind.BorealForest: return 2.1;
                case BiomeKind.CoastalMarsh: return 3.4;
                case BiomeKind.AridSteppe: return 1.8;
                case BiomeKind.MountainHighland: return 24.0;
                case BiomeKind.DesertWaste: return 5.8;
                case BiomeKind.TropicalJungle: return 4.6;
                case BiomeKind.FrozenTundra: return 5.4;
                default: return 3.0;
            }
        }

        private static double InitialRelationBias(WorldStyle style, WorldGenre genre)
        {
            double bias = 0.0;
            if (style == WorldStyle.HighFantasy) bias += 0.04;
            if (style == WorldStyle.DarkFantasyGrim) bias -= 0.06;
            if (style == WorldStyle.SteampunkRevolution) bias += 0.02;
            if (genre == WorldGenre.PoliticalIntrigue) bias -= 0.08;
            if (genre == WorldGenre.MerchantEmpire) bias += 0.03;
            return bias;
        }

        private static double TierWeight(SettlementSize tier)
        {
            switch (tier)
            {
                case SettlementSize.Capital: return 600.0;
                case SettlementSize.City: return 360.0;
                case SettlementSize.Town: return 160.0;
                case SettlementSize.Village: return 60.0;
                case SettlementSize.Hamlet: return 25.0;
                default: return 0.0;
            }
        }

        private static ulong PairKey(int a, int b)
        {
            uint low = (uint)(a < b ? a : b);
            uint high = (uint)(a < b ? b : a);
            return ((ulong)low << 32) | high;
        }

        private static double Clamp01(double value)
        {
            return Clamp(value, 0.0, 1.0);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    public sealed class HistoryRegionState
    {
        public HistoryRegionState(RegionRecord record, int index, int x, int y, double suitability, double baseMovementCost, double carryingCapacity)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
            Index = index;
            X = x;
            Y = y;
            Suitability = suitability;
            BaseMovementCost = baseMovementCost;
            CarryingCapacity = carryingCapacity;
        }

        public RegionRecord Record { get; }
        public int Index { get; }
        public int X { get; }
        public int Y { get; }
        public double Suitability { get; }
        public double BaseMovementCost { get; }
        public double CarryingCapacity { get; }
        public double Population { get; set; }
        public int PopulationMilestone { get; set; }
        public int RoadLevel { get; private set; }

        public double CurrentMovementCost
        {
            get
            {
                double reduction = Math.Min(0.56, RoadLevel * 0.08);
                double cost = BaseMovementCost * (1.0 - reduction);
                if (Record.Biome == BiomeKind.MountainHighland && cost < 9.0)
                    return 9.0;
                return cost;
            }
        }

        public void AddRoadReinforcement()
        {
            if (RoadLevel < 8)
                RoadLevel++;
        }
    }

    public sealed class HistoricalSettlementState
    {
        public HistoricalSettlementState(SettlementRecord record, int index, int regionIndex, int factionIndex)
            : this(record, index, regionIndex, factionIndex, record != null ? record.TileX : -1, record != null ? record.TileY : -1)
        {
        }

        public HistoricalSettlementState(SettlementRecord record, int index, int regionIndex, int factionIndex, int tileX, int tileY)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
            Index = index;
            RegionIndex = regionIndex;
            FactionIndex = factionIndex;
            TileX = tileX;
            TileY = tileY;
            CurrentTier = SettlementSize.None;
            FoundedYear = int.MinValue;
        }

        public SettlementRecord Record { get; }
        public int Index { get; }
        public int RegionIndex { get; }
        public int FactionIndex { get; set; }
        public int TileX { get; }
        public int TileY { get; }
        public bool Founded { get; set; }
        public bool Isolated { get; set; }
        public int FoundedYear { get; set; }
        public SettlementSize CurrentTier { get; set; }
    }

    public sealed class HistoryFactionState
    {
        public HistoryFactionState(FactionRecord record, int index)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
            Index = index;
            Active = true;
            LeaderFigureId = -1;
        }

        public FactionRecord Record { get; }
        public int Index { get; }
        public bool Active { get; set; }
        public bool CivilizationFounded { get; set; }
        public int FoundedSettlementCount { get; set; }
        public double Strength { get; set; }
        public int LeaderFigureId { get; set; }
    }

    public sealed class HistoricalFigureState
    {
        public HistoricalFigureState(int id, string name, int factionIndex, int birthYear, int dynastySeed)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FactionIndex = factionIndex;
            BirthYear = birthYear;
            DynastySeed = dynastySeed;
            DeathYear = int.MinValue;
            Alive = true;
        }

        public int Id { get; }
        public string Name { get; }
        public int FactionIndex { get; }
        public int BirthYear { get; }
        public int DynastySeed { get; }
        public bool IsHeir { get; set; }
        public bool Alive { get; set; }
        public int DeathYear { get; set; }
    }

    public readonly struct HistoryPathResult
    {
        public HistoryPathResult(bool found, double cost, int[] regions)
        {
            Found = found;
            Cost = cost;
            Regions = regions ?? Array.Empty<int>();
        }

        public bool Found { get; }
        public double Cost { get; }
        public int[] Regions { get; }
    }

    public sealed class HistoryEventBuffer : IHistoryEventSink
    {
        private readonly List<WorldHistoryEvent> _events;

        public HistoryEventBuffer(int capacity)
        {
            _events = new List<WorldHistoryEvent>(Math.Max(16, capacity));
        }

        public IReadOnlyList<WorldHistoryEvent> Events { get { return _events; } }

        public void Emit(WorldHistoryEvent historyEvent)
        {
            if (historyEvent == null) throw new ArgumentNullException(nameof(historyEvent));
            _events.Add(historyEvent);
        }
    }

    public static class HistorySystemKeys
    {
        public const int LifeEmergence = 1001;
        public const int PopulationGrowth = 1002;
        public const int Migration = 1003;
        public const int SettlementFounding = 1004;
        public const int RoadNetwork = 1005;
        public const int Diplomacy = 1006;
        public const int War = 1007;
        public const int HistoricalFigure = 1008;
    }

    public static class HistoryRandom
    {
        public static XorShiftRng Create(uint worldSeed, int phase, int agent, int year)
        {
            return new XorShiftRng(Hash(worldSeed, phase, agent, year));
        }

        public static uint Hash(uint worldSeed, int phase, int agent, int year)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = Mix(h, worldSeed);
                h = Mix(h, (uint)phase);
                h = Mix(h, (uint)agent);
                h = Mix(h, (uint)year);
                if (h == 0u)
                    h = 2463534242u;
                return h;
            }
        }

        private static uint Mix(uint h, uint value)
        {
            unchecked
            {
                h ^= value & 0xffu;
                h *= 16777619u;
                h ^= (value >> 8) & 0xffu;
                h *= 16777619u;
                h ^= (value >> 16) & 0xffu;
                h *= 16777619u;
                h ^= (value >> 24) & 0xffu;
                h *= 16777619u;
                return h;
            }
        }
    }
}

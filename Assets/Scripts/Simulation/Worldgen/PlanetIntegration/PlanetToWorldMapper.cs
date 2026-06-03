using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Rng;
using EmberCrpg.Simulation.Worldgen.History;
using EmberCrpg.Simulation.Worldgen.Planet;
using OverlandBiomeKind = EmberCrpg.Domain.Overland.BiomeKind;
using WorldBiomeKind = EmberCrpg.Domain.Worldgen.BiomeKind;

namespace EmberCrpg.Simulation.Worldgen.PlanetIntegration
{
    /// <summary>Maps a generated spherical planet into the existing GeneratedWorld contract.</summary>
    public static class PlanetToWorldMapper
    {
        public const int GeographyWidth = 128;
        public const int GeographyHeight = 64;

        public static GeneratedWorld Map(PlanetField field, WorldgenParameters parameters)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var regionMap = PlanetRegionClusterer.Cluster(field, parameters.RegionCount);
            var projection = PlanetGeographyProjector.Project(field, regionMap, GeographyWidth, GeographyHeight);
            var regions = PlanetWorldRecordMapper.MapRegions(field, parameters, regionMap, projection);
            var settlements = PlanetWorldRecordMapper.MapSettlements(field, regionMap, projection);
            var factions = PlanetWorldRecordMapper.MapFactions(field, parameters, regions, settlements);
            var relations = PlanetWorldRecordMapper.MapFactionRelations(field.Seed, factions);

            var historyResult = new WorldHistorySimulator().Simulate(
                field.Seed,
                parameters,
                projection.Geography,
                regions,
                factions,
                settlements);
            var projectedSettlements = PlanetHistoryProjection.ProjectSettlements(historyResult.State);
            if (projectedSettlements.Count == 0)
                projectedSettlements = settlements;

            var notableFigures = PlanetHistoryProjection.ProjectNotableFigures(historyResult.State, projectedSettlements);
            var npcs = PlanetNpcSeeder.Seed(field.Seed, parameters, projectedSettlements, factions);

            return new GeneratedWorld(
                field.Seed,
                regions,
                projectedSettlements,
                factions,
                relations,
                npcs,
                historyResult.Events,
                notableFigures,
                projection.Geography);
        }
    }

    internal sealed class PlanetRegionMap
    {
        public PlanetRegionMap(int[] regionIndexByTile, int[] seedTileIds, int[] landTileCounts)
        {
            RegionIndexByTile = regionIndexByTile ?? throw new ArgumentNullException(nameof(regionIndexByTile));
            SeedTileIds = seedTileIds ?? throw new ArgumentNullException(nameof(seedTileIds));
            LandTileCounts = landTileCounts ?? throw new ArgumentNullException(nameof(landTileCounts));
        }

        public int RegionCount { get { return SeedTileIds.Length; } }
        public int[] RegionIndexByTile { get; }
        public int[] SeedTileIds { get; }
        public int[] LandTileCounts { get; }
    }

    internal static class PlanetRegionClusterer
    {
        public static PlanetRegionMap Cluster(PlanetField field, int requestedRegionCount)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (requestedRegionCount <= 0) throw new ArgumentOutOfRangeException(nameof(requestedRegionCount));

            var landTiles = new List<int>(field.TileCount);
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (field.TileAt(tileId).IsLand)
                    landTiles.Add(tileId);
            }

            if (landTiles.Count == 0)
                throw new InvalidOperationException("PlanetToWorldMapper requires at least one land tile.");

            int regionCount = Math.Min(requestedRegionCount, landTiles.Count);
            int[] seedTileIds = ChooseSeeds(field, landTiles, regionCount);
            int[] regionIndexByTile = AssignRegions(field, seedTileIds);
            int[] landTileCounts = new int[regionCount];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (field.TileAt(tileId).IsLand)
                    landTileCounts[regionIndexByTile[tileId]]++;
            }

            return new PlanetRegionMap(regionIndexByTile, seedTileIds, landTileCounts);
        }

        private static int[] ChooseSeeds(PlanetField field, IReadOnlyList<int> landTiles, int regionCount)
        {
            int[] seeds = new int[regionCount];
            seeds[0] = BestFirstSeed(field, landTiles);

            for (int region = 1; region < regionCount; region++)
            {
                int bestTile = landTiles[0];
                double bestScore = double.NegativeInfinity;
                for (int i = 0; i < landTiles.Count; i++)
                {
                    int tileId = landTiles[i];
                    if (Contains(seeds, region, tileId))
                        continue;

                    double distance = NearestSeedDistance(field, tileId, seeds, region);
                    double score = distance + (Suitability(field.TileAt(tileId)) * 0.04d) + (StableTie(field.Seed, tileId) * 0.000001d);
                    if (score > bestScore + 0.000000001d ||
                        (Math.Abs(score - bestScore) <= 0.000000001d && tileId < bestTile))
                    {
                        bestScore = score;
                        bestTile = tileId;
                    }
                }

                seeds[region] = bestTile;
            }

            return seeds;
        }

        private static int BestFirstSeed(PlanetField field, IReadOnlyList<int> landTiles)
        {
            int bestTile = landTiles[0];
            double bestScore = double.NegativeInfinity;
            for (int i = 0; i < landTiles.Count; i++)
            {
                int tileId = landTiles[i];
                double score = Suitability(field.TileAt(tileId)) + (StableTie(field.Seed, tileId) * 0.000001d);
                if (score > bestScore + 0.000000001d ||
                    (Math.Abs(score - bestScore) <= 0.000000001d && tileId < bestTile))
                {
                    bestScore = score;
                    bestTile = tileId;
                }
            }

            return bestTile;
        }

        private static int[] AssignRegions(PlanetField field, int[] seedTileIds)
        {
            int[] regionIndexByTile = new int[field.TileCount];
            for (int i = 0; i < regionIndexByTile.Length; i++)
                regionIndexByTile[i] = -1;

            int[] queue = new int[field.TileCount];
            int head = 0;
            int tail = 0;
            for (int region = 0; region < seedTileIds.Length; region++)
            {
                int seed = seedTileIds[region];
                regionIndexByTile[seed] = region;
                queue[tail++] = seed;
            }

            while (head < tail)
            {
                int tileId = queue[head++];
                int region = regionIndexByTile[tileId];
                var neighbors = field.Grid.TileAt(tileId).Neighbors;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    int neighbor = neighbors[i];
                    if (!field.TileAt(neighbor).IsLand || regionIndexByTile[neighbor] >= 0)
                        continue;

                    regionIndexByTile[neighbor] = region;
                    queue[tail++] = neighbor;
                }
            }

            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (regionIndexByTile[tileId] < 0)
                    regionIndexByTile[tileId] = NearestSeedRegion(field, tileId, seedTileIds);
            }

            return regionIndexByTile;
        }

        private static double NearestSeedDistance(PlanetField field, int tileId, int[] seeds, int count)
        {
            PlanetVector position = field.Grid.TileAt(tileId).Position;
            double best = double.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                double dot = PlanetVector.Dot(position, field.Grid.TileAt(seeds[i]).Position);
                double distance = 1d - dot;
                if (distance < best)
                    best = distance;
            }

            return best;
        }

        private static int NearestSeedRegion(PlanetField field, int tileId, int[] seeds)
        {
            PlanetVector position = field.Grid.TileAt(tileId).Position;
            int bestRegion = 0;
            double bestDot = double.NegativeInfinity;
            for (int region = 0; region < seeds.Length; region++)
            {
                double dot = PlanetVector.Dot(position, field.Grid.TileAt(seeds[region]).Position);
                if (dot > bestDot + 0.000000001d ||
                    (Math.Abs(dot - bestDot) <= 0.000000001d && seeds[region] < seeds[bestRegion]))
                {
                    bestDot = dot;
                    bestRegion = region;
                }
            }

            return bestRegion;
        }

        private static bool Contains(int[] values, int count, int value)
        {
            for (int i = 0; i < count; i++)
            {
                if (values[i] == value)
                    return true;
            }

            return false;
        }

        private static double Suitability(PlanetTileField tile)
        {
            double value;
            switch (tile.Biome)
            {
                case PlanetBiome.Grassland: value = 1.16d; break;
                case PlanetBiome.TemperateForest: value = 1.02d; break;
                case PlanetBiome.Savanna: value = 0.88d; break;
                case PlanetBiome.TropicalRainforest: value = 0.80d; break;
                case PlanetBiome.Taiga: value = 0.68d; break;
                case PlanetBiome.Mountain: value = 0.46d; break;
                case PlanetBiome.Desert: value = 0.36d; break;
                case PlanetBiome.Tundra: value = 0.32d; break;
                case PlanetBiome.Ice: value = 0.12d; break;
                default: value = 0.20d; break;
            }

            value += tile.SoilFertility * 0.12d;
            value += tile.FreshWater * 0.10d;
            value += tile.Wood * 0.04d;
            return value;
        }

        private static double StableTie(uint seed, int tileId)
        {
            return PlanetMappingMath.Hash(seed, 0x5245474Eu, (uint)tileId) / (double)uint.MaxValue;
        }
    }

    internal sealed class PlanetGeographyProjection
    {
        private readonly int[] _regionIndexByCell;
        private readonly int[] _tileIdByCell;
        private readonly int[] _cellByTile;

        public PlanetGeographyProjection(WorldGeography geography, int[] regionIndexByCell, int[] tileIdByCell, int[] cellByTile)
        {
            Geography = geography ?? throw new ArgumentNullException(nameof(geography));
            _regionIndexByCell = regionIndexByCell ?? throw new ArgumentNullException(nameof(regionIndexByCell));
            _tileIdByCell = tileIdByCell ?? throw new ArgumentNullException(nameof(tileIdByCell));
            _cellByTile = cellByTile ?? throw new ArgumentNullException(nameof(cellByTile));
        }

        public WorldGeography Geography { get; }

        public int CellForTile(int tileId, int regionIndex)
        {
            int cell = tileId >= 0 && tileId < _cellByTile.Length ? _cellByTile[tileId] : -1;
            if (cell >= 0 && Geography.LandMask[cell] && _regionIndexByCell[cell] == regionIndex)
                return cell;

            PlanetMappingMath.ProjectVectorToCell(Geography.Width, Geography.Height, GeographyCellVector(tileId), out int x, out int y);
            return NearestLandCellForRegion(regionIndex, x, y);
        }

        public int NearestLandCellForRegion(int regionIndex, int targetX, int targetY)
        {
            int best = -1;
            int bestDistance = int.MaxValue;
            for (int cell = 0; cell < _regionIndexByCell.Length; cell++)
            {
                if (!Geography.LandMask[cell] || _regionIndexByCell[cell] != regionIndex)
                    continue;

                int x = cell % Geography.Width;
                int y = cell / Geography.Width;
                int dx = Math.Abs(x - targetX);
                dx = Math.Min(dx, Geography.Width - dx);
                int dy = y - targetY;
                int distance = (dx * dx) + (dy * dy);
                if (distance < bestDistance || (distance == bestDistance && cell < best))
                {
                    best = cell;
                    bestDistance = distance;
                }
            }

            if (best >= 0)
                return best;

            for (int cell = 0; cell < Geography.TileCount; cell++)
            {
                if (Geography.LandMask[cell])
                    return cell;
            }

            return 0;
        }

        public int RegionIndexAtCell(int cell)
        {
            return _regionIndexByCell[cell];
        }

        public int TileIdAtCell(int cell)
        {
            return _tileIdByCell[cell];
        }

        private static PlanetVector GeographyCellVector(int tileId)
        {
            // The fallback path only needs a deterministic valid vector when a tile was not sampled.
            uint hash = PlanetMappingMath.Hash((uint)Math.Max(1, tileId + 1), 0x43454C4Cu, (uint)Math.Max(0, tileId));
            double lon = ((hash / (double)uint.MaxValue) * 2d * Math.PI) - Math.PI;
            double lat = ((((hash >> 8) & 0xffffu) / 65535d) * Math.PI) - (Math.PI / 2d);
            double cosLat = Math.Cos(lat);
            return new PlanetVector(cosLat * Math.Cos(lon), Math.Sin(lat), cosLat * Math.Sin(lon));
        }
    }

    internal static class PlanetGeographyProjector
    {
        private const int BandSearchRadius = 3;

        public static PlanetGeographyProjection Project(PlanetField field, PlanetRegionMap regionMap, int width, int height)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (regionMap == null) throw new ArgumentNullException(nameof(regionMap));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            var lookup = new NearestTileLookup(field.Grid);
            int cellCount = width * height;
            var elevation = new double[cellCount];
            var temperature = new double[cellCount];
            var moisture = new double[cellCount];
            var land = new bool[cellCount];
            var overlandBiomes = new OverlandBiomeKind[cellCount];
            var worldBiomes = new WorldBiomeKind[cellCount];
            var regionIds = new RegionId[cellCount];
            var regionIndexByCell = new int[cellCount];
            var tileIdByCell = new int[cellCount];
            var cellByTile = new int[field.TileCount];
            var cellDotByTile = new double[field.TileCount];
            for (int i = 0; i < cellByTile.Length; i++)
            {
                cellByTile[i] = -1;
                cellDotByTile[i] = double.NegativeInfinity;
            }

            for (int y = 0; y < height; y++)
            {
                double lat = (Math.PI / 2d) - (((y + 0.5d) / height) * Math.PI);
                double cosLat = Math.Cos(lat);
                double sinLat = Math.Sin(lat);
                for (int x = 0; x < width; x++)
                {
                    double lon = (((x + 0.5d) / width) * 2d * Math.PI) - Math.PI;
                    double dx = cosLat * Math.Cos(lon);
                    double dy = sinLat;
                    double dz = cosLat * Math.Sin(lon);
                    int tileId = lookup.Nearest(dx, dy, dz, lat, out double dot);
                    PlanetTileField tile = field.TileAt(tileId);
                    int cell = (y * width) + x;
                    int regionIndex = regionMap.RegionIndexByTile[tileId];

                    elevation[cell] = tile.Elevation;
                    temperature[cell] = tile.Temperature;
                    moisture[cell] = tile.Moisture;
                    land[cell] = tile.IsLand;
                    overlandBiomes[cell] = PlanetBiomeMapper.ToOverland(tile.Biome);
                    worldBiomes[cell] = PlanetBiomeMapper.ToWorld(tile.Biome);
                    regionIds[cell] = new RegionId((ulong)(regionIndex + 1));
                    regionIndexByCell[cell] = regionIndex;
                    tileIdByCell[cell] = tileId;

                    if (dot > cellDotByTile[tileId] + 0.000000001d ||
                        (Math.Abs(dot - cellDotByTile[tileId]) <= 0.000000001d && (cellByTile[tileId] < 0 || cell < cellByTile[tileId])))
                    {
                        cellDotByTile[tileId] = dot;
                        cellByTile[tileId] = cell;
                    }
                }
            }

            var geography = new WorldGeography(
                width,
                height,
                elevation,
                temperature,
                moisture,
                land,
                overlandBiomes,
                worldBiomes,
                regionIds);
            return new PlanetGeographyProjection(geography, regionIndexByCell, tileIdByCell, cellByTile);
        }

        private sealed class NearestTileLookup
        {
            private readonly IcosphereGrid _grid;
            private readonly List<int>[] _bands;
            private readonly double _bandSize;
            private readonly int _bandCount;

            public NearestTileLookup(IcosphereGrid grid)
            {
                _grid = grid ?? throw new ArgumentNullException(nameof(grid));
                _bandCount = Math.Max(8, (int)Math.Round(Math.Sqrt(grid.Count)));
                _bandSize = Math.PI / _bandCount;
                _bands = new List<int>[_bandCount];
                for (int i = 0; i < _bands.Length; i++)
                    _bands[i] = new List<int>();

                for (int tileId = 0; tileId < grid.Count; tileId++)
                {
                    PlanetVector p = grid.TileAt(tileId).Position;
                    int band = LatitudeBand(Math.Asin(PlanetMappingMath.Clamp(p.Y, -1d, 1d)));
                    _bands[band].Add(tileId);
                }
            }

            public int Nearest(double dx, double dy, double dz, double latitude, out double bestDot)
            {
                int centerBand = LatitudeBand(latitude);
                int best = -1;
                bestDot = -2d;
                for (int band = centerBand - BandSearchRadius; band <= centerBand + BandSearchRadius; band++)
                {
                    if (band < 0 || band >= _bandCount)
                        continue;

                    List<int> list = _bands[band];
                    for (int i = 0; i < list.Count; i++)
                    {
                        int tileId = list[i];
                        PlanetVector p = _grid.TileAt(tileId).Position;
                        double dot = (p.X * dx) + (p.Y * dy) + (p.Z * dz);
                        if (dot > bestDot + 0.000000001d ||
                            (Math.Abs(dot - bestDot) <= 0.000000001d && (best < 0 || tileId < best)))
                        {
                            bestDot = dot;
                            best = tileId;
                        }
                    }
                }

                return best;
            }

            private int LatitudeBand(double latitude)
            {
                int band = (int)((latitude + (Math.PI / 2d)) / _bandSize);
                if (band < 0) return 0;
                if (band >= _bandCount) return _bandCount - 1;
                return band;
            }
        }
    }

    internal static class PlanetWorldRecordMapper
    {
        private static readonly string[] RegionSuffixes =
        {
            "Vale", "Reach", "Marches", "Wilds", "Holds", "Steppe", "Coast", "Lowlands",
        };

        private static readonly string[] SettlementSuffixes =
        {
            "ford", "haven", "vale", "stead", "wick", "hollow", "bridge", "cross",
        };

        private static readonly string[] FactionPrefixes =
        {
            "House", "Order", "Circle", "League", "Pact", "Hand",
        };

        public static IReadOnlyList<RegionRecord> MapRegions(
            PlanetField field,
            WorldgenParameters parameters,
            PlanetRegionMap regionMap,
            PlanetGeographyProjection projection)
        {
            var nameBag = new HashSet<string>(StringComparer.Ordinal);
            var rng = new XorShiftRng(PlanetMappingMath.Hash(field.Seed, 0x52474E41u, (uint)regionMap.RegionCount));
            var populationByRegion = SettlementPopulationByRegion(field, regionMap);
            var biomeCounts = BiomeCountsByRegion(field, regionMap);
            var regions = new List<RegionRecord>(regionMap.RegionCount);

            for (int region = 0; region < regionMap.RegionCount; region++)
            {
                PlanetVector centroid = Centroid(field, regionMap, region);
                PlanetMappingMath.ProjectVectorToCell(projection.Geography.Width, projection.Geography.Height, centroid, out int projectedX, out int projectedY);
                int cell = projection.NearestLandCellForRegion(region, projectedX, projectedY);
                int x = cell % projection.Geography.Width;
                int y = cell / projection.Geography.Width;
                int population = populationByRegion[region];
                int low = population <= 0 ? 0 : Math.Max(1, (population * 4) / 5);
                int high = population <= 0 ? 0 : Math.Max(low, (population * 6) / 5);
                WorldBiomeKind biome = DominantWorldBiome(biomeCounts, region, field.TileAt(regionMap.SeedTileIds[region]).Biome);
                string word = SyllableNameForge.ForgeUnique(rng, nameBag);
                string name = word + " " + RegionSuffixes[rng.NextInt(RegionSuffixes.Length)];

                regions.Add(new RegionRecord(
                    new RegionId((ulong)(region + 1)),
                    name,
                    low,
                    high,
                    biome,
                    x,
                    y));
            }

            return regions;
        }

        public static IReadOnlyList<SettlementRecord> MapSettlements(
            PlanetField field,
            PlanetRegionMap regionMap,
            PlanetGeographyProjection projection)
        {
            PlanetSettlement[] settlements = SortedSettlements(field);
            var nameBag = new HashSet<string>(StringComparer.Ordinal);
            var rng = new XorShiftRng(PlanetMappingMath.Hash(field.Seed, 0x53455454u, (uint)settlements.Length));
            var records = new List<SettlementRecord>(settlements.Length);

            for (int i = 0; i < settlements.Length; i++)
            {
                PlanetSettlement settlement = settlements[i];
                int region = regionMap.RegionIndexByTile[settlement.TileId];
                int cell = projection.CellForTile(settlement.TileId, region);
                int population = Math.Max(1, settlement.Population);
                SettlementSize size = SizeFor(settlement.Type, population);
                string word = SyllableNameForge.ForgeUnique(rng, nameBag);
                string name = size == SettlementSize.Capital || size == SettlementSize.City
                    ? word
                    : word + SettlementSuffixes[rng.NextInt(SettlementSuffixes.Length)];

                records.Add(new SettlementRecord(
                    new SettlementId((ulong)(i + 1)),
                    new RegionId((ulong)(region + 1)),
                    name,
                    population,
                    size,
                    cell % projection.Geography.Width,
                    cell / projection.Geography.Width));
            }

            return records;
        }

        public static IReadOnlyList<FactionRecord> MapFactions(
            PlanetField field,
            WorldgenParameters parameters,
            IReadOnlyList<RegionRecord> regions,
            IReadOnlyList<SettlementRecord> settlements)
        {
            int settlementBasis = settlements.Count <= 0 ? regions.Count : Math.Max(1, (settlements.Count + 1) / 2);
            int factionCount = Math.Max(1, Math.Min(parameters.FactionCount, Math.Min(regions.Count, settlementBasis)));
            var factions = new List<FactionRecord>(factionCount);
            var nameBag = new HashSet<string>(StringComparer.Ordinal);
            var rng = new XorShiftRng(PlanetMappingMath.Hash(field.Seed, 0x46414354u, (uint)factionCount));

            for (int i = 0; i < factionCount; i++)
            {
                string prefix = FactionPrefixes[rng.NextInt(FactionPrefixes.Length)];
                string word = SyllableNameForge.ForgeUnique(rng, nameBag);
                factions.Add(new FactionRecord(
                    new FactionId((ulong)(i + 1)),
                    prefix + " " + word,
                    new[] { "planet", "region-" + (i + 1).ToString() }));
            }

            return factions;
        }

        public static IReadOnlyList<FactionRelationSeed> MapFactionRelations(uint seed, IReadOnlyList<FactionRecord> factions)
        {
            var relations = new List<FactionRelationSeed>((factions.Count * (factions.Count - 1)) / 2);
            var rng = new XorShiftRng(PlanetMappingMath.Hash(seed, 0x52454C41u, (uint)factions.Count));
            for (int i = 0; i < factions.Count; i++)
            {
                for (int j = i + 1; j < factions.Count; j++)
                {
                    int a = rng.NextInt(181) - 90;
                    int b = rng.NextInt(181) - 90;
                    int reputation = (a + b) / 2;
                    relations.Add(new FactionRelationSeed(factions[i].Id, factions[j].Id, new FactionReputation(reputation)));
                }
            }

            return relations;
        }

        private static int[] SettlementPopulationByRegion(PlanetField field, PlanetRegionMap regionMap)
        {
            int[] population = new int[regionMap.RegionCount];
            PlanetSettlement[] settlements = SortedSettlements(field);
            for (int i = 0; i < settlements.Length; i++)
            {
                PlanetSettlement settlement = settlements[i];
                population[regionMap.RegionIndexByTile[settlement.TileId]] += Math.Max(1, settlement.Population);
            }

            return population;
        }

        private static int[,] BiomeCountsByRegion(PlanetField field, PlanetRegionMap regionMap)
        {
            var counts = new int[regionMap.RegionCount, (int)PlanetBiome.Mountain + 1];
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (!field.TileAt(tileId).IsLand)
                    continue;

                int region = regionMap.RegionIndexByTile[tileId];
                counts[region, (int)field.TileAt(tileId).Biome]++;
            }

            return counts;
        }

        private static WorldBiomeKind DominantWorldBiome(int[,] biomeCounts, int region, PlanetBiome fallback)
        {
            int best = (int)fallback;
            int bestCount = -1;
            for (int biome = (int)PlanetBiome.Ice; biome <= (int)PlanetBiome.Mountain; biome++)
            {
                int count = biomeCounts[region, biome];
                if (count > bestCount)
                {
                    best = biome;
                    bestCount = count;
                }
            }

            return PlanetBiomeMapper.ToWorld((PlanetBiome)best);
        }

        private static PlanetVector Centroid(PlanetField field, PlanetRegionMap regionMap, int region)
        {
            double x = 0d;
            double y = 0d;
            double z = 0d;
            int count = 0;
            for (int tileId = 0; tileId < field.TileCount; tileId++)
            {
                if (!field.TileAt(tileId).IsLand || regionMap.RegionIndexByTile[tileId] != region)
                    continue;

                PlanetVector p = field.Grid.TileAt(tileId).Position;
                x += p.X;
                y += p.Y;
                z += p.Z;
                count++;
            }

            if (count == 0)
                return field.Grid.TileAt(regionMap.SeedTileIds[region]).Position;

            var centroid = new PlanetVector(x / count, y / count, z / count);
            return centroid.Length <= 0.000000001d
                ? field.Grid.TileAt(regionMap.SeedTileIds[region]).Position
                : centroid.Normalize();
        }

        private static PlanetSettlement[] SortedSettlements(PlanetField field)
        {
            var settlements = new PlanetSettlement[field.Settlements.Count];
            for (int i = 0; i < settlements.Length; i++)
                settlements[i] = field.Settlements[i];

            Array.Sort(settlements, CompareSettlements);
            return settlements;
        }

        private static int CompareSettlements(PlanetSettlement left, PlanetSettlement right)
        {
            int tile = left.TileId.CompareTo(right.TileId);
            if (tile != 0) return tile;
            int type = left.Type.CompareTo(right.Type);
            if (type != 0) return type;
            return left.Population.CompareTo(right.Population);
        }

        private static SettlementSize SizeFor(PlanetSettlementType type, int population)
        {
            if (type == PlanetSettlementType.Capital)
                return SettlementSize.Capital;
            if (population >= 14000)
                return SettlementSize.City;
            if (population >= 5000)
                return SettlementSize.Town;
            if (population >= 600)
                return SettlementSize.Village;
            return SettlementSize.Hamlet;
        }
    }

    internal static class PlanetHistoryProjection
    {
        public static IReadOnlyList<SettlementRecord> ProjectSettlements(HistoryState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var survivingByRegion = new List<int>[state.Regions.Length];
            for (int i = 0; i < state.Settlements.Length; i++)
            {
                HistoricalSettlementState settlement = state.Settlements[i];
                if (!IsSurvivingSettlement(settlement))
                    continue;

                List<int> bucket = survivingByRegion[settlement.RegionIndex];
                if (bucket == null)
                {
                    bucket = new List<int>();
                    survivingByRegion[settlement.RegionIndex] = bucket;
                }

                bucket.Add(i);
            }

            int[] populations = new int[state.Settlements.Length];
            for (int region = 0; region < survivingByRegion.Length; region++)
            {
                List<int> regionSettlements = survivingByRegion[region];
                if (regionSettlements == null || regionSettlements.Count == 0)
                    continue;

                ProjectRegionPopulation(state, region, regionSettlements, populations);
            }

            var projected = new List<SettlementRecord>(state.Settlements.Length);
            for (int i = 0; i < state.Settlements.Length; i++)
            {
                int population = populations[i];
                if (population <= 0)
                    continue;

                HistoricalSettlementState settlement = state.Settlements[i];
                projected.Add(new SettlementRecord(
                    settlement.Record.Id,
                    settlement.Record.Region,
                    settlement.Record.Name,
                    population,
                    settlement.CurrentTier,
                    settlement.TileX,
                    settlement.TileY));
            }

            return projected;
        }

        public static IReadOnlyList<NotableFigureRecord> ProjectNotableFigures(
            HistoryState state,
            IReadOnlyList<SettlementRecord> projectedSettlements)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (projectedSettlements == null) throw new ArgumentNullException(nameof(projectedSettlements));

            var figures = new List<NotableFigureRecord>(state.Figures.Count);
            if (projectedSettlements.Count == 0)
                return figures;

            for (int i = 0; i < state.Figures.Count; i++)
            {
                HistoricalFigureState figure = state.Figures[i];
                if (figure.FactionIndex < 0 || figure.FactionIndex >= state.Factions.Length)
                    continue;

                HistoryFactionState faction = state.Factions[figure.FactionIndex];
                SettlementId home = FindFigureHomeSettlement(state, figure.FactionIndex);
                if (home.IsEmpty)
                    home = projectedSettlements[0].Id;

                int? deathYear = figure.Alive || figure.DeathYear == int.MinValue
                    ? (int?)null
                    : figure.DeathYear;

                figures.Add(new NotableFigureRecord(
                    figure.Id,
                    figure.Name,
                    FigureTitle(state, figure),
                    figure.BirthYear,
                    deathYear,
                    home,
                    faction.Record.Id));
            }

            return figures;
        }

        private static void ProjectRegionPopulation(
            HistoryState state,
            int regionIndex,
            IReadOnlyList<int> regionSettlements,
            int[] populations)
        {
            int targetPopulation = PositivePopulation(state.Regions[regionIndex].Population, regionSettlements.Count);
            var weights = new long[regionSettlements.Count];
            var remainders = new long[regionSettlements.Count];
            long totalWeight = 0L;

            for (int i = 0; i < regionSettlements.Count; i++)
            {
                HistoricalSettlementState settlement = state.Settlements[regionSettlements[i]];
                long weight = TierPopulationWeight(settlement.CurrentTier) + Math.Max(1, settlement.Record.Population);
                weights[i] = weight;
                totalWeight += weight;
            }

            int assigned = 0;
            for (int i = 0; i < regionSettlements.Count; i++)
            {
                long scaled = targetPopulation * weights[i];
                int share = (int)(scaled / totalWeight);
                if (share < 1)
                    share = 1;

                populations[regionSettlements[i]] = share;
                remainders[i] = scaled % totalWeight;
                assigned += share;
            }

            int delta = targetPopulation - assigned;
            while (delta > 0)
            {
                int localIndex = LargestRemainderIndex(remainders, regionSettlements, populations);
                populations[regionSettlements[localIndex]]++;
                remainders[localIndex] = -1L;
                delta--;
            }

            while (delta < 0)
            {
                int localIndex = LargestPopulationIndex(regionSettlements, populations);
                if (localIndex < 0)
                    break;

                populations[regionSettlements[localIndex]]--;
                delta++;
            }
        }

        private static bool IsSurvivingSettlement(HistoricalSettlementState settlement)
        {
            return settlement.Founded && settlement.CurrentTier != SettlementSize.None;
        }

        private static int PositivePopulation(double simulatedPopulation, int minimum)
        {
            if (double.IsNaN(simulatedPopulation) || double.IsInfinity(simulatedPopulation) || simulatedPopulation <= 0.0)
                return minimum;
            if (simulatedPopulation >= int.MaxValue)
                return int.MaxValue;

            int rounded = (int)Math.Round(simulatedPopulation, MidpointRounding.AwayFromZero);
            return rounded < minimum ? minimum : rounded;
        }

        private static long TierPopulationWeight(SettlementSize size)
        {
            switch (size)
            {
                case SettlementSize.Capital: return 180000L;
                case SettlementSize.City: return 75000L;
                case SettlementSize.Town: return 6000L;
                case SettlementSize.Village: return 650L;
                case SettlementSize.Hamlet: return 200L;
                default: return 1L;
            }
        }

        private static int LargestRemainderIndex(long[] remainders, IReadOnlyList<int> regionSettlements, int[] populations)
        {
            int best = 0;
            for (int i = 1; i < remainders.Length; i++)
            {
                if (remainders[i] > remainders[best])
                {
                    best = i;
                    continue;
                }

                if (remainders[i] == remainders[best])
                {
                    int settlementIndex = regionSettlements[i];
                    int bestSettlementIndex = regionSettlements[best];
                    if (populations[settlementIndex] > populations[bestSettlementIndex] ||
                        (populations[settlementIndex] == populations[bestSettlementIndex] && settlementIndex < bestSettlementIndex))
                    {
                        best = i;
                    }
                }
            }

            return best;
        }

        private static int LargestPopulationIndex(IReadOnlyList<int> regionSettlements, int[] populations)
        {
            int best = -1;
            for (int i = 0; i < regionSettlements.Count; i++)
            {
                int settlementIndex = regionSettlements[i];
                if (populations[settlementIndex] <= 1)
                    continue;
                if (best < 0 || populations[settlementIndex] > populations[regionSettlements[best]])
                    best = i;
            }

            return best;
        }

        private static SettlementId FindFigureHomeSettlement(HistoryState state, int factionIndex)
        {
            int best = -1;
            for (int i = 0; i < state.Settlements.Length; i++)
            {
                HistoricalSettlementState settlement = state.Settlements[i];
                if (!IsSurvivingSettlement(settlement) || settlement.FactionIndex != factionIndex)
                    continue;
                if (best < 0 || CompareFigureHome(settlement, state.Settlements[best]) < 0)
                    best = i;
            }

            if (best >= 0)
                return state.Settlements[best].Record.Id;

            for (int i = 0; i < state.Settlements.Length; i++)
            {
                if (IsSurvivingSettlement(state.Settlements[i]))
                    return state.Settlements[i].Record.Id;
            }

            return default;
        }

        private static int CompareFigureHome(HistoricalSettlementState left, HistoricalSettlementState right)
        {
            int tierCompare = ((int)right.CurrentTier).CompareTo((int)left.CurrentTier);
            if (tierCompare != 0)
                return tierCompare;
            return left.Record.Id.Value.CompareTo(right.Record.Id.Value);
        }

        private static string FigureTitle(HistoryState state, HistoricalFigureState figure)
        {
            if (figure.Alive && figure.FactionIndex >= 0 && figure.FactionIndex < state.Factions.Length
                && state.Factions[figure.FactionIndex].LeaderFigureId == figure.Id)
            {
                return "Ruler";
            }

            if (figure.IsHeir)
                return "Heir";
            if (!figure.Alive)
                return "Late noble";
            return "Noble";
        }
    }

    internal static class PlanetNpcSeeder
    {
        public static IReadOnlyList<NpcSeedRecord> Seed(
            uint seed,
            WorldgenParameters parameters,
            IReadOnlyList<SettlementRecord> settlements,
            IReadOnlyList<FactionRecord> factions)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (settlements == null) throw new ArgumentNullException(nameof(settlements));
            if (factions == null) throw new ArgumentNullException(nameof(factions));
            if (settlements.Count == 0 || factions.Count == 0)
                return Array.Empty<NpcSeedRecord>();

            int[] assignment = Allocate(parameters, settlements);
            int total = 0;
            for (int i = 0; i < assignment.Length; i++)
                total += assignment[i];

            var npcs = new List<NpcSeedRecord>(total);
            var rng = new XorShiftRng(PlanetMappingMath.Hash(seed, 0x4E504353u, (uint)total));
            ulong nextId = 1UL;
            for (int settlementIndex = 0; settlementIndex < settlements.Count; settlementIndex++)
            {
                SettlementRecord settlement = settlements[settlementIndex];
                var localNames = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < assignment[settlementIndex]; i++)
                {
                    string given = SyllableNameForge.ForgeUnique(rng, localNames, syllableCount: 2);
                    string family = SyllableNameForge.Forge(rng, syllableCount: 2);
                    FactionRecord faction = factions[(int)((settlement.Id.Value + (ulong)i - 1UL) % (ulong)factions.Count)];
                    npcs.Add(new NpcSeedRecord(
                        new NpcId(nextId++),
                        settlement.Id,
                        faction.Id,
                        given + " " + family,
                        parameters.WorldStartYear - 18 - rng.NextInt(48),
                        RoleFor(rng, settlement.Size, faction.Id)));
                }
            }

            return npcs;
        }

        private static int[] Allocate(WorldgenParameters parameters, IReadOnlyList<SettlementRecord> settlements)
        {
            var assigned = new int[settlements.Count];
            var caps = new int[settlements.Count];
            var weights = new long[settlements.Count];
            int totalCap = 0;
            for (int i = 0; i < settlements.Count; i++)
            {
                caps[i] = Capacity(settlements[i]);
                weights[i] = Math.Max(1, settlements[i].Population) + (TierWeight(settlements[i].Size) * 1000L);
                totalCap += caps[i];
            }

            int target = Math.Min(parameters.NpcCount, totalCap);
            int remaining = target;
            if (target >= settlements.Count)
            {
                for (int i = 0; i < settlements.Count; i++)
                {
                    assigned[i] = 1;
                    remaining--;
                }
            }

            while (remaining > 0)
            {
                int index = BestAssignmentIndex(settlements, weights, caps, assigned);
                if (index < 0)
                    break;

                assigned[index]++;
                remaining--;
            }

            return assigned;
        }

        private static int BestAssignmentIndex(
            IReadOnlyList<SettlementRecord> settlements,
            long[] weights,
            int[] caps,
            int[] assigned)
        {
            int best = -1;
            long bestScore = long.MinValue;
            for (int i = 0; i < settlements.Count; i++)
            {
                if (assigned[i] >= caps[i])
                    continue;

                long score = weights[i] / (assigned[i] + 1L);
                if (score > bestScore ||
                    (score == bestScore && best >= 0 && settlements[i].Id.Value < settlements[best].Id.Value))
                {
                    best = i;
                    bestScore = score;
                }
            }

            return best;
        }

        private static int Capacity(SettlementRecord settlement)
        {
            int byPopulation = Math.Max(1, settlement.Population / 900);
            int baseline;
            int cap;
            switch (settlement.Size)
            {
                case SettlementSize.Capital: baseline = 18; cap = 72; break;
                case SettlementSize.City: baseline = 12; cap = 48; break;
                case SettlementSize.Town: baseline = 5; cap = 20; break;
                case SettlementSize.Village: baseline = 2; cap = 8; break;
                default: baseline = 1; cap = 4; break;
            }

            int capacity = baseline + byPopulation;
            return capacity > cap ? cap : capacity;
        }

        private static int TierWeight(SettlementSize size)
        {
            switch (size)
            {
                case SettlementSize.Capital: return 120;
                case SettlementSize.City: return 70;
                case SettlementSize.Town: return 18;
                case SettlementSize.Village: return 5;
                default: return 2;
            }
        }

        private static NpcRole RoleFor(IDeterministicRng rng, SettlementSize size, FactionId faction)
        {
            int roll = rng.NextInt(100);
            int factionBias = (int)(faction.Value % 5UL);
            if (size == SettlementSize.Capital || size == SettlementSize.City)
            {
                if (roll < 5) return NpcRole.Noble;
                if (roll < 10) return factionBias == 0 ? NpcRole.Knight : NpcRole.Guard;
                if (roll < 25) return NpcRole.Merchant;
                if (roll < 34) return NpcRole.Blacksmith;
                if (roll < 42) return NpcRole.Innkeeper;
                if (roll < 50) return NpcRole.Priest;
                if (roll < 57) return factionBias == 1 ? NpcRole.Mage : NpcRole.Healer;
                if (roll < 65) return NpcRole.Sage;
                if (roll < 72) return NpcRole.Bard;
                if (roll < 80) return NpcRole.Rogue;
                if (roll < 88) return NpcRole.Beggar;
                if (roll < 96) return NpcRole.Farmer;
                return factionBias == 2 ? NpcRole.Bandit : NpcRole.Outlaw;
            }

            if (size == SettlementSize.Town)
            {
                if (roll < 3) return NpcRole.Noble;
                if (roll < 10) return NpcRole.Guard;
                if (roll < 22) return NpcRole.Merchant;
                if (roll < 32) return NpcRole.Blacksmith;
                if (roll < 42) return NpcRole.Innkeeper;
                if (roll < 49) return NpcRole.Priest;
                if (roll < 56) return NpcRole.Healer;
                if (roll < 61) return factionBias == 1 ? NpcRole.Mage : NpcRole.Sage;
                if (roll < 67) return NpcRole.Bard;
                if (roll < 73) return NpcRole.Rogue;
                if (roll < 82) return NpcRole.Beggar;
                if (roll < 96) return NpcRole.Farmer;
                return factionBias == 2 ? NpcRole.Bandit : NpcRole.Outlaw;
            }

            if (roll < 3) return NpcRole.Guard;
            if (roll < 8) return NpcRole.Merchant;
            if (roll < 14) return NpcRole.Blacksmith;
            if (roll < 20) return NpcRole.Innkeeper;
            if (roll < 25) return NpcRole.Healer;
            if (roll < 29) return NpcRole.Priest;
            if (roll < 32) return NpcRole.Bard;
            if (roll < 36) return NpcRole.Beggar;
            if (roll < 94) return NpcRole.Farmer;
            if (roll < 98) return NpcRole.Rogue;
            return factionBias == 2 ? NpcRole.Bandit : NpcRole.Outlaw;
        }
    }

    internal static class PlanetBiomeMapper
    {
        public static OverlandBiomeKind ToOverland(PlanetBiome biome)
        {
            switch (biome)
            {
                case PlanetBiome.Ice:
                case PlanetBiome.Tundra:
                    return OverlandBiomeKind.Tundra;
                case PlanetBiome.Taiga:
                case PlanetBiome.TemperateForest:
                case PlanetBiome.TropicalRainforest:
                    return OverlandBiomeKind.Forest;
                case PlanetBiome.Desert:
                    return OverlandBiomeKind.Desert;
                case PlanetBiome.Mountain:
                    return OverlandBiomeKind.Mountain;
                case PlanetBiome.Ocean:
                    return OverlandBiomeKind.Coast;
                default:
                    return OverlandBiomeKind.Plains;
            }
        }

        public static WorldBiomeKind ToWorld(PlanetBiome biome)
        {
            switch (biome)
            {
                case PlanetBiome.Ice:
                case PlanetBiome.Tundra:
                    return WorldBiomeKind.FrozenTundra;
                case PlanetBiome.Taiga:
                case PlanetBiome.TemperateForest:
                    return WorldBiomeKind.BorealForest;
                case PlanetBiome.TropicalRainforest:
                    return WorldBiomeKind.TropicalJungle;
                case PlanetBiome.Desert:
                    return WorldBiomeKind.DesertWaste;
                case PlanetBiome.Savanna:
                    return WorldBiomeKind.AridSteppe;
                case PlanetBiome.Mountain:
                    return WorldBiomeKind.MountainHighland;
                case PlanetBiome.Ocean:
                    return WorldBiomeKind.CoastalMarsh;
                default:
                    return WorldBiomeKind.TemperatePlain;
            }
        }
    }

    internal static class PlanetMappingMath
    {
        public static uint Hash(uint seed, uint domain, uint value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = Mix(hash, seed);
                hash = Mix(hash, domain);
                hash = Mix(hash, value);
                return hash == 0u ? 2463534242u : hash;
            }
        }

        public static void ProjectVectorToCell(int width, int height, PlanetVector vector, out int x, out int y)
        {
            double lat = Math.Asin(Clamp(vector.Y, -1d, 1d));
            double lon = Math.Atan2(vector.Z, vector.X);
            x = (int)(((lon + Math.PI) / (2d * Math.PI)) * width);
            y = (int)(((Math.PI / 2d - lat) / Math.PI) * height);
            if (x < 0) x = 0;
            if (x >= width) x = width - 1;
            if (y < 0) y = 0;
            if (y >= height) y = height - 1;
        }

        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static uint Mix(uint hash, uint value)
        {
            unchecked
            {
                hash ^= value & 0xffu;
                hash *= 16777619u;
                hash ^= (value >> 8) & 0xffu;
                hash *= 16777619u;
                hash ^= (value >> 16) & 0xffu;
                hash *= 16777619u;
                hash ^= (value >> 24) & 0xffu;
                hash *= 16777619u;
                return hash;
            }
        }
    }
}

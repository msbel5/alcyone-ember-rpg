using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Overland;
using OverlandBiomeKind = EmberCrpg.Domain.Overland.BiomeKind;
using WorldBiomeKind = EmberCrpg.Domain.Worldgen.BiomeKind;

namespace EmberCrpg.Simulation.Worldgen
{
    /// <summary>Immutable geography grid shared by world history and the overland view.</summary>
    public sealed class WorldGeography
    {
        private readonly double[] _elevation;
        private readonly double[] _temperature;
        private readonly double[] _moisture;
        private readonly bool[] _land;
        private readonly OverlandBiomeKind[] _overlandBiomes;
        private readonly WorldBiomeKind[] _worldBiomes;
        private readonly RegionId[] _regionIds;

        public WorldGeography(
            int width,
            int height,
            IReadOnlyList<double> elevation,
            IReadOnlyList<double> temperature,
            IReadOnlyList<double> moisture,
            IReadOnlyList<bool> land,
            IReadOnlyList<OverlandBiomeKind> overlandBiomes,
            IReadOnlyList<WorldBiomeKind> worldBiomes,
            IReadOnlyList<RegionId> regionIds)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
            if (elevation == null) throw new ArgumentNullException(nameof(elevation));
            if (temperature == null) throw new ArgumentNullException(nameof(temperature));
            if (moisture == null) throw new ArgumentNullException(nameof(moisture));
            if (land == null) throw new ArgumentNullException(nameof(land));
            if (overlandBiomes == null) throw new ArgumentNullException(nameof(overlandBiomes));
            if (worldBiomes == null) throw new ArgumentNullException(nameof(worldBiomes));
            if (regionIds == null) throw new ArgumentNullException(nameof(regionIds));

            int expected = width * height;
            if (elevation.Count != expected || temperature.Count != expected || moisture.Count != expected
                || land.Count != expected || overlandBiomes.Count != expected || worldBiomes.Count != expected
                || regionIds.Count != expected)
            {
                throw new ArgumentException("All geography arrays must match width * height.");
            }

            Width = width;
            Height = height;
            _elevation = Copy(elevation);
            _temperature = Copy(temperature);
            _moisture = Copy(moisture);
            _land = Copy(land);
            _overlandBiomes = Copy(overlandBiomes);
            _worldBiomes = Copy(worldBiomes);
            _regionIds = Copy(regionIds);

            for (int i = 0; i < _regionIds.Length; i++)
            {
                if (_regionIds[i].IsEmpty)
                    throw new ArgumentException("Every geography tile must be assigned to a non-empty region.", nameof(regionIds));
            }

            Elevation = new ReadOnlyCollection<double>(_elevation);
            Temperature = new ReadOnlyCollection<double>(_temperature);
            Moisture = new ReadOnlyCollection<double>(_moisture);
            LandMask = new ReadOnlyCollection<bool>(_land);
            OverlandBiomes = new ReadOnlyCollection<OverlandBiomeKind>(_overlandBiomes);
            WorldBiomes = new ReadOnlyCollection<WorldBiomeKind>(_worldBiomes);
            RegionIds = new ReadOnlyCollection<RegionId>(_regionIds);
        }

        public int Width { get; }
        public int Height { get; }
        public int TileCount => Width * Height;
        public IReadOnlyList<double> Elevation { get; }
        public IReadOnlyList<double> Temperature { get; }
        public IReadOnlyList<double> Moisture { get; }
        public IReadOnlyList<bool> LandMask { get; }
        public IReadOnlyList<OverlandBiomeKind> OverlandBiomes { get; }
        public IReadOnlyList<WorldBiomeKind> WorldBiomes { get; }
        public IReadOnlyList<RegionId> RegionIds { get; }

        public bool IsLandAt(int x, int y)
        {
            return _land[Index(x, y)];
        }

        public OverlandBiomeKind OverlandBiomeAt(int x, int y)
        {
            return _overlandBiomes[Index(x, y)];
        }

        public WorldBiomeKind WorldBiomeAt(int x, int y)
        {
            return _worldBiomes[Index(x, y)];
        }

        public RegionId RegionAt(int x, int y)
        {
            return _regionIds[Index(x, y)];
        }

        public double ElevationAt(int x, int y)
        {
            return _elevation[Index(x, y)];
        }

        public bool[] CopyLandMask()
        {
            return (bool[])_land.Clone();
        }

        public OverlandBiomeKind[] CopyOverlandBiomes()
        {
            return (OverlandBiomeKind[])_overlandBiomes.Clone();
        }

        public RegionId[] CopyRegionIds()
        {
            return (RegionId[])_regionIds.Clone();
        }

        private int Index(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                throw new ArgumentOutOfRangeException(nameof(x), $"Tile ({x},{y}) is outside the geography bounds.");
            return (y * Width) + x;
        }

        private static double[] Copy(IReadOnlyList<double> source)
        {
            var copy = new double[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return copy;
        }

        private static bool[] Copy(IReadOnlyList<bool> source)
        {
            var copy = new bool[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return copy;
        }

        private static OverlandBiomeKind[] Copy(IReadOnlyList<OverlandBiomeKind> source)
        {
            var copy = new OverlandBiomeKind[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return copy;
        }

        private static WorldBiomeKind[] Copy(IReadOnlyList<WorldBiomeKind> source)
        {
            var copy = new WorldBiomeKind[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return copy;
        }

        private static RegionId[] Copy(IReadOnlyList<RegionId> source)
        {
            var copy = new RegionId[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return copy;
        }
    }

    public static class WorldGeographyProvider
    {
        public const int DefaultWidth = 16;
        public const int DefaultHeight = 16;

        public static uint DeriveGeographySeed(uint worldSeed)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = Mix(hash, worldSeed);
                hash = Mix(hash, 0xA17C90E3u);
                return hash == 0u ? 2463534242u : hash;
            }
        }

        public static WorldGeography Generate(uint worldSeed, WorldgenParameters parameters, IReadOnlyList<RegionRecord> regions)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (regions == null) throw new ArgumentNullException(nameof(regions));

            return Generate(worldSeed, parameters.RegionCount, regions);
        }

        public static WorldGeography Generate(uint worldSeed, int regionCount, IReadOnlyList<RegionRecord> regions)
        {
            if (regions == null) throw new ArgumentNullException(nameof(regions));
            if (regions.Count != regionCount)
                throw new ArgumentException("Region count must match the supplied region records.", nameof(regionCount));

            return Build(worldSeed, regionCount).Materialize(regions);
        }

        internal static WorldGeographyBuild Build(uint worldSeed, WorldgenParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            return Build(worldSeed, parameters.RegionCount);
        }

        private static WorldGeographyBuild Build(uint worldSeed, int regionCount)
        {
            if (regionCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(regionCount), regionCount, "Region count must be positive.");

            uint geographySeed = DeriveGeographySeed(worldSeed);
            var fields = new WorldGenerationManager().Generate(geographySeed, DefaultWidth, DefaultHeight);
            var elevation = Copy(fields.Elevation);
            var temperature = Copy(fields.Temperature);
            var moisture = Copy(fields.Moisture);
            var land = fields.CopyLandMask();
            var overlandBiomes = fields.CopyBiomes();
            SmoothSingleTileIslands(fields.Width, fields.Height, overlandBiomes, land);
            SmoothRemainingSingleTileBiomes(fields.Width, fields.Height, overlandBiomes);

            var worldBiomes = new WorldBiomeKind[overlandBiomes.Length];
            for (int i = 0; i < worldBiomes.Length; i++)
                worldBiomes[i] = MapToWorldBiome(overlandBiomes[i]);

            var regionIndexByTile = PartitionRegions(fields.Width, fields.Height, geographySeed, land, elevation, overlandBiomes, regionCount, out var regionGeography);
            return new WorldGeographyBuild(
                fields.Width,
                fields.Height,
                elevation,
                temperature,
                moisture,
                land,
                overlandBiomes,
                worldBiomes,
                regionIndexByTile,
                regionGeography);
        }

        public static WorldBiomeKind MapToWorldBiome(OverlandBiomeKind biome)
        {
            switch (biome)
            {
                case OverlandBiomeKind.Forest: return WorldBiomeKind.BorealForest;
                case OverlandBiomeKind.Coast: return WorldBiomeKind.CoastalMarsh;
                case OverlandBiomeKind.Swamp: return WorldBiomeKind.CoastalMarsh;
                case OverlandBiomeKind.Desert: return WorldBiomeKind.DesertWaste;
                case OverlandBiomeKind.Tundra: return WorldBiomeKind.FrozenTundra;
                case OverlandBiomeKind.Mountain: return WorldBiomeKind.MountainHighland;
                case OverlandBiomeKind.Ash: return WorldBiomeKind.AridSteppe;
                default: return WorldBiomeKind.TemperatePlain;
            }
        }

        private static int[] PartitionRegions(
            int width,
            int height,
            uint geographySeed,
            bool[] land,
            double[] elevation,
            OverlandBiomeKind[] biomes,
            int regionCount,
            out WorldGeographyRegion[] regions)
        {
            int tileCount = width * height;
            var assignments = new int[tileCount];
            for (int i = 0; i < assignments.Length; i++)
                assignments[i] = -1;

            var landTiles = new List<int>(tileCount);
            for (int i = 0; i < land.Length; i++)
            {
                if (land[i])
                    landTiles.Add(i);
            }

            if (landTiles.Count == 0)
            {
                for (int i = 0; i < assignments.Length; i++)
                    assignments[i] = 0;
                regions = BuildRegionGeography(width, height, land, elevation, biomes, assignments, regionCount, new int[regionCount]);
                return assignments;
            }

            int activeRegionCount = regionCount < landTiles.Count ? regionCount : landTiles.Count;
            var seedTiles = ChooseRegionSeeds(width, geographySeed, landTiles, elevation, biomes, activeRegionCount, regionCount);
            GrowLandRegions(width, height, land, seedTiles, activeRegionCount, assignments);

            for (int i = 0; i < landTiles.Count; i++)
            {
                int tile = landTiles[i];
                if (assignments[tile] < 0)
                    assignments[tile] = NearestSeedRegion(width, tile, seedTiles, activeRegionCount);
            }

            for (int i = 0; i < assignments.Length; i++)
            {
                if (assignments[i] < 0)
                    assignments[i] = NearestSeedRegion(width, i, seedTiles, activeRegionCount);
            }

            regions = BuildRegionGeography(width, height, land, elevation, biomes, assignments, regionCount, seedTiles);
            return assignments;
        }

        private static int[] ChooseRegionSeeds(
            int width,
            uint geographySeed,
            IReadOnlyList<int> landTiles,
            double[] elevation,
            OverlandBiomeKind[] biomes,
            int activeRegionCount,
            int regionCount)
        {
            var seeds = new int[regionCount];
            for (int i = 0; i < seeds.Length; i++)
                seeds[i] = landTiles[i % landTiles.Count];

            if (activeRegionCount <= 0)
                return seeds;

            seeds[0] = BestFirstSeed(geographySeed, landTiles, elevation, biomes);
            for (int r = 1; r < activeRegionCount; r++)
            {
                long bestScore = long.MinValue;
                int bestTile = landTiles[0];
                for (int i = 0; i < landTiles.Count; i++)
                {
                    int tile = landTiles[i];
                    if (ContainsSeed(seeds, r, tile))
                        continue;

                    int minDistance = int.MaxValue;
                    for (int s = 0; s < r; s++)
                    {
                        int distance = SquaredTileDistance(width, tile, seeds[s]);
                        if (distance < minDistance)
                            minDistance = distance;
                    }

                    long score = (minDistance * 10000L)
                        + (long)(TileSuitability(biomes[tile], elevation[tile]) * 1000.0)
                        + StableTieBreak(geographySeed, tile);
                    if (score > bestScore || (score == bestScore && tile < bestTile))
                    {
                        bestScore = score;
                        bestTile = tile;
                    }
                }

                seeds[r] = bestTile;
            }

            return seeds;
        }

        private static int BestFirstSeed(uint geographySeed, IReadOnlyList<int> landTiles, double[] elevation, OverlandBiomeKind[] biomes)
        {
            int bestTile = landTiles[0];
            long bestScore = long.MinValue;
            for (int i = 0; i < landTiles.Count; i++)
            {
                int tile = landTiles[i];
                long score = (long)(TileSuitability(biomes[tile], elevation[tile]) * 10000.0) + StableTieBreak(geographySeed, tile);
                if (score > bestScore || (score == bestScore && tile < bestTile))
                {
                    bestScore = score;
                    bestTile = tile;
                }
            }

            return bestTile;
        }

        private static void GrowLandRegions(int width, int height, bool[] land, int[] seedTiles, int activeRegionCount, int[] assignments)
        {
            var queue = new int[land.Length];
            int head = 0;
            int tail = 0;

            for (int region = 0; region < activeRegionCount; region++)
            {
                int seed = seedTiles[region];
                if (assignments[seed] >= 0)
                    continue;

                assignments[seed] = region;
                queue[tail++] = seed;
            }

            while (head < tail)
            {
                int tile = queue[head++];
                int x = tile % width;
                int y = tile / width;
                int region = assignments[tile];

                TryGrowNeighbor(width, height, land, assignments, queue, ref tail, x - 1, y, region);
                TryGrowNeighbor(width, height, land, assignments, queue, ref tail, x + 1, y, region);
                TryGrowNeighbor(width, height, land, assignments, queue, ref tail, x, y - 1, region);
                TryGrowNeighbor(width, height, land, assignments, queue, ref tail, x, y + 1, region);
            }
        }

        private static void TryGrowNeighbor(
            int width,
            int height,
            bool[] land,
            int[] assignments,
            int[] queue,
            ref int tail,
            int x,
            int y,
            int region)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;

            int index = (y * width) + x;
            if (!land[index] || assignments[index] >= 0)
                return;

            assignments[index] = region;
            queue[tail++] = index;
        }

        private static WorldGeographyRegion[] BuildRegionGeography(
            int width,
            int height,
            bool[] land,
            double[] elevation,
            OverlandBiomeKind[] biomes,
            int[] assignments,
            int regionCount,
            int[] seedTiles)
        {
            var counts = new int[regionCount];
            var sumX = new long[regionCount];
            var sumY = new long[regionCount];
            var biomeCounts = new int[regionCount, 9];

            for (int index = 0; index < assignments.Length; index++)
            {
                int region = assignments[index];
                if (region < 0 || region >= regionCount || !land[index])
                    continue;

                counts[region]++;
                sumX[region] += index % width;
                sumY[region] += index / width;
                biomeCounts[region, (int)biomes[index]]++;
            }

            var result = new WorldGeographyRegion[regionCount];
            for (int region = 0; region < regionCount; region++)
            {
                int centerTile = counts[region] > 0
                    ? LandTileNearestCentroid(width, land, assignments, region, sumX[region] / (double)counts[region], sumY[region] / (double)counts[region])
                    : seedTiles[region % seedTiles.Length];

                int centerX = centerTile % width;
                int centerY = centerTile / width;
                var biome = DominantBiome(region, biomeCounts, biomes[centerTile]);
                result[region] = new WorldGeographyRegion(
                    region,
                    centerX,
                    centerY,
                    counts[region],
                    biome,
                    MapToWorldBiome(biome),
                    elevation[centerTile]);
            }

            return result;
        }

        private static int LandTileNearestCentroid(int width, bool[] land, int[] assignments, int region, double centerX, double centerY)
        {
            int best = -1;
            double bestDistance = double.MaxValue;
            for (int index = 0; index < assignments.Length; index++)
            {
                if (!land[index] || assignments[index] != region)
                    continue;

                int x = index % width;
                int y = index / width;
                double dx = x - centerX;
                double dy = y - centerY;
                double distance = (dx * dx) + (dy * dy);
                if (distance < bestDistance || (distance == bestDistance && index < best))
                {
                    bestDistance = distance;
                    best = index;
                }
            }

            return best < 0 ? 0 : best;
        }

        private static OverlandBiomeKind DominantBiome(int region, int[,] biomeCounts, OverlandBiomeKind fallback)
        {
            int best = (int)fallback;
            int bestCount = -1;
            for (int biome = 1; biome <= 8; biome++)
            {
                int count = biomeCounts[region, biome];
                if (count > bestCount)
                {
                    best = biome;
                    bestCount = count;
                }
            }

            return (OverlandBiomeKind)best;
        }

        private static int NearestSeedRegion(int width, int tile, int[] seedTiles, int activeRegionCount)
        {
            int bestRegion = 0;
            int bestDistance = int.MaxValue;
            for (int region = 0; region < activeRegionCount; region++)
            {
                int distance = SquaredTileDistance(width, tile, seedTiles[region]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestRegion = region;
                }
            }

            return bestRegion;
        }

        private static bool ContainsSeed(int[] seeds, int count, int tile)
        {
            for (int i = 0; i < count; i++)
            {
                if (seeds[i] == tile)
                    return true;
            }

            return false;
        }

        private static int SquaredTileDistance(int width, int a, int b)
        {
            int ax = a % width;
            int ay = a / width;
            int bx = b % width;
            int by = b / width;
            int dx = ax - bx;
            int dy = ay - by;
            return (dx * dx) + (dy * dy);
        }

        private static double TileSuitability(OverlandBiomeKind biome, double elevation)
        {
            double value;
            switch (biome)
            {
                case OverlandBiomeKind.Plains: value = 1.20; break;
                case OverlandBiomeKind.Coast: value = 1.10; break;
                case OverlandBiomeKind.Forest: value = 0.95; break;
                case OverlandBiomeKind.Swamp: value = 0.74; break;
                case OverlandBiomeKind.Desert: value = 0.48; break;
                case OverlandBiomeKind.Tundra: value = 0.44; break;
                case OverlandBiomeKind.Mountain: value = 0.36; break;
                case OverlandBiomeKind.Ash: value = 0.30; break;
                default: value = 0.60; break;
            }

            if (elevation > 0.74)
                value -= 0.16;
            if (elevation < 0.50)
                value += 0.05;
            return value;
        }

        private static int StableTieBreak(uint seed, int tile)
        {
            unchecked
            {
                uint value = Mix(seed, (uint)tile);
                return (int)(value % 997u);
            }
        }

        private static double[] Copy(IReadOnlyList<double> source)
        {
            var copy = new double[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return copy;
        }

        private static void SmoothSingleTileIslands(int width, int height, OverlandBiomeKind[] biomes, bool[] land)
        {
            var scratch = new OverlandBiomeKind[biomes.Length];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < biomes.Length; i++)
                    scratch[i] = biomes[i];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * width) + x;
                        if (!land[index] || biomes[index] == OverlandBiomeKind.Coast || CountMatchingNeighbors(width, height, biomes, land, x, y, biomes[index]) > 0)
                            continue;

                        scratch[index] = DominantLandNeighbor(width, height, biomes, land, x, y, biomes[index]);
                    }
                }

                for (int i = 0; i < biomes.Length; i++)
                    biomes[i] = scratch[i];
            }
        }

        private static void SmoothRemainingSingleTileBiomes(int width, int height, OverlandBiomeKind[] biomes)
        {
            var scratch = new OverlandBiomeKind[biomes.Length];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < biomes.Length; i++)
                    scratch[i] = biomes[i];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * width) + x;
                        if (biomes[index] == OverlandBiomeKind.Coast || CountMatchingNeighbors(width, height, biomes, x, y, biomes[index]) > 0)
                            continue;

                        scratch[index] = DominantNeighbor(width, height, biomes, x, y, biomes[index]);
                    }
                }

                for (int i = 0; i < biomes.Length; i++)
                    biomes[i] = scratch[i];
            }
        }

        private static OverlandBiomeKind DominantLandNeighbor(int width, int height, OverlandBiomeKind[] biomes, bool[] land, int x, int y, OverlandBiomeKind fallback)
        {
            var dominant = fallback;
            int bestCount = -1;
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    int index = (ny * width) + nx;
                    if (!land[index] || biomes[index] == OverlandBiomeKind.Coast)
                        continue;

                    int count = CountMatchingNeighbors(width, height, biomes, land, x, y, biomes[index]);
                    if (count > bestCount)
                    {
                        dominant = biomes[index];
                        bestCount = count;
                    }
                }
            }

            return bestCount < 0 ? OverlandBiomeKind.Coast : dominant;
        }

        private static int CountMatchingNeighbors(int width, int height, OverlandBiomeKind[] biomes, bool[] land, int x, int y, OverlandBiomeKind biome)
        {
            int count = 0;
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    int index = (ny * width) + nx;
                    if (land[index] && biomes[index] == biome)
                        count++;
                }
            }

            return count;
        }

        private static OverlandBiomeKind DominantNeighbor(int width, int height, OverlandBiomeKind[] biomes, int x, int y, OverlandBiomeKind fallback)
        {
            var bestBiome = fallback;
            int bestCount = -1;
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;

                    int count = CountMatchingNeighbors(width, height, biomes, nx, ny, biomes[(ny * width) + nx]);
                    if (count > bestCount)
                    {
                        bestBiome = biomes[(ny * width) + nx];
                        bestCount = count;
                    }
                }
            }

            return bestBiome;
        }

        private static int CountMatchingNeighbors(int width, int height, OverlandBiomeKind[] biomes, int x, int y, OverlandBiomeKind biome)
        {
            int count = 0;
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    if (biomes[(ny * width) + nx] == biome)
                        count++;
                }
            }

            return count;
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

    internal sealed class WorldGeographyBuild
    {
        private readonly double[] _elevation;
        private readonly double[] _temperature;
        private readonly double[] _moisture;
        private readonly bool[] _land;
        private readonly OverlandBiomeKind[] _overlandBiomes;
        private readonly WorldBiomeKind[] _worldBiomes;
        private readonly int[] _regionIndexByTile;

        public WorldGeographyBuild(
            int width,
            int height,
            double[] elevation,
            double[] temperature,
            double[] moisture,
            bool[] land,
            OverlandBiomeKind[] overlandBiomes,
            WorldBiomeKind[] worldBiomes,
            int[] regionIndexByTile,
            WorldGeographyRegion[] regions)
        {
            Width = width;
            Height = height;
            _elevation = elevation ?? throw new ArgumentNullException(nameof(elevation));
            _temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
            _moisture = moisture ?? throw new ArgumentNullException(nameof(moisture));
            _land = land ?? throw new ArgumentNullException(nameof(land));
            _overlandBiomes = overlandBiomes ?? throw new ArgumentNullException(nameof(overlandBiomes));
            _worldBiomes = worldBiomes ?? throw new ArgumentNullException(nameof(worldBiomes));
            _regionIndexByTile = regionIndexByTile ?? throw new ArgumentNullException(nameof(regionIndexByTile));
            Regions = regions ?? throw new ArgumentNullException(nameof(regions));
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<WorldGeographyRegion> Regions { get; }

        public WorldGeography Materialize(IReadOnlyList<RegionRecord> regions)
        {
            if (regions == null) throw new ArgumentNullException(nameof(regions));
            if (regions.Count != Regions.Count)
                throw new ArgumentException("Region count must match geography partition count.", nameof(regions));

            var regionIds = new RegionId[_regionIndexByTile.Length];
            for (int i = 0; i < regionIds.Length; i++)
                regionIds[i] = regions[_regionIndexByTile[i]].Id;

            return new WorldGeography(
                Width,
                Height,
                _elevation,
                _temperature,
                _moisture,
                _land,
                _overlandBiomes,
                _worldBiomes,
                regionIds);
        }
    }

    internal readonly struct WorldGeographyRegion
    {
        public WorldGeographyRegion(int index, int centerX, int centerY, int landTileCount, OverlandBiomeKind overlandBiome, WorldBiomeKind worldBiome, double elevation)
        {
            Index = index;
            CenterX = centerX;
            CenterY = centerY;
            LandTileCount = landTileCount;
            OverlandBiome = overlandBiome;
            WorldBiome = worldBiome;
            Elevation = elevation;
        }

        public int Index { get; }
        public int CenterX { get; }
        public int CenterY { get; }
        public int LandTileCount { get; }
        public OverlandBiomeKind OverlandBiome { get; }
        public WorldBiomeKind WorldBiome { get; }
        public double Elevation { get; }
    }
}

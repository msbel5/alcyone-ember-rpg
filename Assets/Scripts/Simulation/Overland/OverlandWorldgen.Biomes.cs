using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Overland
{
    /// <summary>Director for DF-style continent fields: elevation, temperature, moisture, then biome classification.</summary>
    public sealed class WorldGenerationManager
    {
        private readonly IFieldStrategy _elevation = new ElevationFieldStrategy();
        private readonly IFieldStrategy _temperature = new TemperatureFieldStrategy();
        private readonly IFieldStrategy _moisture = new MoistureFieldStrategy();
        private readonly BiomeClassifier _classifier = new BiomeClassifier();

        public OverlandWorldFields Generate(uint seed, int width, int height)
        {
            var context = new WorldFieldContext(seed, width, height);
            var elevation = _elevation.Build(context, null);
            var temperature = _temperature.Build(context, elevation);
            var moisture = _moisture.Build(context, elevation);
            return _classifier.Classify(context, elevation, temperature, moisture);
        }

        private interface IFieldStrategy
        {
            double[] Build(WorldFieldContext context, double[] elevation);
        }

        private sealed class ElevationFieldStrategy : IFieldStrategy
        {
            public double[] Build(WorldFieldContext context, double[] elevation)
            {
                var result = new double[context.TileCount];
                for (int y = 0; y < context.Height; y++)
                {
                    for (int x = 0; x < context.Width; x++)
                    {
                        double nx = (context.X01(x) * 2d) - 1d;
                        double ny = ((context.Y01(y) * 2d) - 1d) * 0.92d;
                        double mask = SmoothStep(Clamp01(1d - (Math.Sqrt((nx * nx) + (ny * ny)) / 1.36d)));
                        double continent = FractalNoise(context.ElevationSeed, context.X01(x) + 11d, context.Y01(y) + 17d, 4, 2.15d);
                        double ridge = Math.Abs((FractalNoise(context.RidgeSeed, context.X01(x) + 31d, context.Y01(y) + 43d, 3, 3.6d) * 2d) - 1d);
                        // Lower, broader dome (0.30 + mask*0.56) so the landmass is mostly mid/low elevation
                        // walkable ground, not a high volcanic core; the ridge term carves mountain RANGES.
                        result[context.Index(x, y)] = Clamp01(0.30d + (mask * 0.56d) + ((continent - 0.5d) * 0.40d) + ((ridge - 0.5d) * 0.18d));
                    }
                }

                return result;
            }
        }

        private sealed class TemperatureFieldStrategy : IFieldStrategy
        {
            public double[] Build(WorldFieldContext context, double[] elevation)
            {
                var result = new double[context.TileCount];
                for (int y = 0; y < context.Height; y++)
                {
                    double latitude = Math.Abs((context.Y01(y) * 2d) - 1d);
                    for (int x = 0; x < context.Width; x++)
                    {
                        int index = context.Index(x, y);
                        double weather = (FractalNoise(context.TemperatureSeed, context.X01(x) + 71d, context.Y01(y) + 29d, 2, 2.4d) - 0.5d) * 0.12d;
                        double lapse = Math.Max(0d, elevation[index] - WorldFieldContext.SeaLevel) * 0.45d;
                        // Steeper latitude band (warm equator -> frozen poles) so the now-rounder continent's
                        // higher-latitude land actually gets cold enough to read as tundra.
                        result[index] = Clamp01(1.02d - (latitude * 1.06d) + weather - lapse);
                    }
                }

                return result;
            }
        }

        private sealed class MoistureFieldStrategy : IFieldStrategy
        {
            private const int WaterRange = 3;

            public double[] Build(WorldFieldContext context, double[] elevation)
            {
                var result = new double[context.TileCount];
                for (int y = 0; y < context.Height; y++)
                {
                    for (int x = 0; x < context.Width; x++)
                    {
                        int index = context.Index(x, y);
                        if (elevation[index] < WorldFieldContext.SeaLevel)
                        {
                            result[index] = 1d;
                            continue;
                        }

                        double rainfall = FractalNoise(context.MoistureSeed, context.X01(x) + 47d, context.Y01(y) + 83d, 3, 2.9d);
                        result[index] = Clamp01((rainfall * 0.82d) + WaterBoost(context, elevation, x, y));
                    }
                }

                return result;
            }

            private static double WaterBoost(WorldFieldContext context, double[] elevation, int x, int y)
            {
                int best = WaterRange + 1;
                for (int ny = y - WaterRange; ny <= y + WaterRange; ny++)
                {
                    for (int nx = x - WaterRange; nx <= x + WaterRange; nx++)
                    {
                        if (nx < 0 || ny < 0 || nx >= context.Width || ny >= context.Height)
                            continue;
                        if (elevation[context.Index(nx, ny)] >= WorldFieldContext.SeaLevel)
                            continue;
                        int distance = Math.Abs(nx - x) + Math.Abs(ny - y);
                        if (distance < best)
                            best = distance;
                    }
                }

                return best > WaterRange ? 0d : ((WaterRange + 1 - best) / (double)(WaterRange + 1)) * 0.42d;
            }
        }

        private sealed class BiomeClassifier
        {
            public OverlandWorldFields Classify(WorldFieldContext context, double[] elevation, double[] temperature, double[] moisture)
            {
                var land = new bool[context.TileCount];
                var biomes = new BiomeKind[context.TileCount];

                for (int i = 0; i < elevation.Length; i++)
                    land[i] = elevation[i] >= WorldFieldContext.SeaLevel;

                for (int y = 0; y < context.Height; y++)
                {
                    for (int x = 0; x < context.Width; x++)
                    {
                        int index = context.Index(x, y);
                        biomes[index] = ClassifyTile(context, elevation, temperature, moisture, land, x, y, index);
                    }
                }

                return new OverlandWorldFields(context.Width, context.Height, elevation, temperature, moisture, land, biomes);
            }

            private static BiomeKind ClassifyTile(
                WorldFieldContext context,
                double[] elevation,
                double[] temperature,
                double[] moisture,
                bool[] land,
                int x,
                int y,
                int index)
            {
                if (!land[index] || IsCoastline(context, land, x, y))
                    return BiomeKind.Coast;
                if (elevation[index] >= 0.82d && temperature[index] >= 0.58d)
                    return BiomeKind.Ash;
                if (elevation[index] >= 0.74d)
                    return BiomeKind.Mountain;
                if (temperature[index] <= 0.30d)
                    return BiomeKind.Tundra;
                if (temperature[index] >= 0.70d && moisture[index] <= 0.34d)
                    return BiomeKind.Desert;
                if (elevation[index] <= WorldFieldContext.SeaLevel + 0.10d && moisture[index] >= 0.64d)
                    return BiomeKind.Swamp;
                if (temperature[index] >= 0.30d && moisture[index] >= 0.54d)
                    return BiomeKind.Forest;
                return BiomeKind.Plains;
            }
        }

        private static bool IsCoastline(WorldFieldContext context, bool[] land, int x, int y)
        {
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= context.Width || ny >= context.Height)
                        continue;
                    if (!land[context.Index(nx, ny)])
                        return true;
                }
            }

            return false;
        }

        private static double FractalNoise(uint seed, double x, double y, int octaves, double scale)
        {
            double value = 0d;
            double amplitude = 1d;
            double total = 0d;
            double frequency = scale;

            for (int octave = 0; octave < octaves; octave++)
            {
                value += ValueNoise(seed + (uint)(octave * 0x9E37), x * frequency, y * frequency) * amplitude;
                total += amplitude;
                amplitude *= 0.5d;
                frequency *= 2d;
            }

            return value / total;
        }

        private static double ValueNoise(uint seed, double x, double y)
        {
            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);
            double tx = SmoothStep(x - x0);
            double ty = SmoothStep(y - y0);

            double a = Noise01(seed, x0, y0);
            double b = Noise01(seed, x0 + 1, y0);
            double c = Noise01(seed, x0, y0 + 1);
            double d = Noise01(seed, x0 + 1, y0 + 1);
            return Lerp(Lerp(a, b, tx), Lerp(c, d, tx), ty);
        }

        private static double Noise01(uint seed, int x, int y)
        {
            unchecked
            {
                uint mixed = seed ^ ((uint)x * 374761393u) ^ ((uint)y * 668265263u);
                mixed ^= mixed >> 13;
                mixed *= 1274126177u;
                var rng = new XorShiftRng(mixed);
                return rng.NextInt(1_000_000) / 999_999d;
            }
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + ((b - a) * t);
        }

        private static double SmoothStep(double value)
        {
            value = Clamp01(value);
            return value * value * (3d - (2d * value));
        }

        private static double Clamp01(double value)
        {
            if (value < 0d) return 0d;
            if (value > 1d) return 1d;
            return value;
        }

        private readonly struct WorldFieldContext
        {
            public const double SeaLevel = 0.43d;

            public WorldFieldContext(uint seed, int width, int height)
            {
                if (width <= 0)
                    throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
                if (height <= 0)
                    throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");

                Width = width;
                Height = height;
                TileCount = width * height;

                var rng = new XorShiftRng(seed ^ 0xC01171E7u);
                ElevationSeed = NextSeed(rng);
                RidgeSeed = NextSeed(rng);
                TemperatureSeed = NextSeed(rng);
                MoistureSeed = NextSeed(rng);
            }

            public int Width { get; }
            public int Height { get; }
            public int TileCount { get; }
            public uint ElevationSeed { get; }
            public uint RidgeSeed { get; }
            public uint TemperatureSeed { get; }
            public uint MoistureSeed { get; }

            public int Index(int x, int y)
            {
                return (y * Width) + x;
            }

            public double X01(int x)
            {
                return Width == 1 ? 0.5d : x / (double)(Width - 1);
            }

            public double Y01(int y)
            {
                return Height == 1 ? 0.5d : y / (double)(Height - 1);
            }

            private static uint NextSeed(XorShiftRng rng)
            {
                return (uint)rng.NextInt(int.MaxValue) + 1u;
            }
        }
    }

    public sealed class OverlandWorldFields
    {
        private readonly double[] _elevation;
        private readonly double[] _temperature;
        private readonly double[] _moisture;
        private readonly bool[] _land;
        private readonly BiomeKind[] _biomes;

        public OverlandWorldFields(
            int width,
            int height,
            double[] elevation,
            double[] temperature,
            double[] moisture,
            bool[] land,
            BiomeKind[] biomes)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
            int expected = width * height;
            if (elevation == null || temperature == null || moisture == null || land == null || biomes == null)
                throw new ArgumentNullException(nameof(elevation), "World field arrays cannot be null.");
            if (elevation.Length != expected || temperature.Length != expected || moisture.Length != expected || land.Length != expected || biomes.Length != expected)
                throw new ArgumentException("World field arrays must match width * height.");

            Width = width;
            Height = height;
            _elevation = (double[])elevation.Clone();
            _temperature = (double[])temperature.Clone();
            _moisture = (double[])moisture.Clone();
            _land = (bool[])land.Clone();
            _biomes = (BiomeKind[])biomes.Clone();
            Elevation = new ReadOnlyCollection<double>(_elevation);
            Temperature = new ReadOnlyCollection<double>(_temperature);
            Moisture = new ReadOnlyCollection<double>(_moisture);
            LandMask = new ReadOnlyCollection<bool>(_land);
            Biomes = new ReadOnlyCollection<BiomeKind>(_biomes);
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<double> Elevation { get; }
        public IReadOnlyList<double> Temperature { get; }
        public IReadOnlyList<double> Moisture { get; }
        public IReadOnlyList<bool> LandMask { get; }
        public IReadOnlyList<BiomeKind> Biomes { get; }

        public bool IsLandAt(int x, int y)
        {
            return _land[Index(x, y)];
        }

        public BiomeKind BiomeAt(int x, int y)
        {
            return _biomes[Index(x, y)];
        }

        public BiomeKind[] CopyBiomes()
        {
            return (BiomeKind[])_biomes.Clone();
        }

        public bool[] CopyLandMask()
        {
            return (bool[])_land.Clone();
        }

        private int Index(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                throw new ArgumentOutOfRangeException(nameof(x), $"Tile ({x},{y}) is outside the field bounds.");
            return (y * Width) + x;
        }
    }

    public static partial class OverlandWorldgen
    {
        private static void SmoothSingleTileIslands(int width, int height, BiomeKind[] biomes, bool[] land)
        {
            var scratch = new BiomeKind[biomes.Length];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < biomes.Length; i++)
                    scratch[i] = biomes[i];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = ToIndex(x, y, width);
                        if (!land[index] || biomes[index] == BiomeKind.Coast || CountMatchingNeighbors(width, height, biomes, land, x, y, biomes[index]) > 0)
                            continue;

                        scratch[index] = DominantLandNeighbor(width, height, biomes, land, x, y, biomes[index]);
                    }
                }

                for (int i = 0; i < biomes.Length; i++)
                    biomes[i] = scratch[i];
            }
        }

        private static BiomeKind DominantLandNeighbor(int width, int height, BiomeKind[] biomes, bool[] land, int x, int y, BiomeKind fallback)
        {
            var dominant = fallback;
            int bestCount = -1;
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    int index = ToIndex(nx, ny, width);
                    if (!land[index] || biomes[index] == BiomeKind.Coast)
                        continue;

                    int count = CountMatchingNeighbors(width, height, biomes, land, x, y, biomes[index]);
                    if (count > bestCount)
                    {
                        dominant = biomes[index];
                        bestCount = count;
                    }
                }
            }

            return dominant;
        }

        private static int CountMatchingNeighbors(int width, int height, BiomeKind[] biomes, bool[] land, int x, int y, BiomeKind biome)
        {
            int count = 0;
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    if ((nx == x && ny == y) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    int index = ToIndex(nx, ny, width);
                    if (land[index] && biomes[index] == biome)
                        count++;
                }
            }

            return count;
        }
    }
}

using System;
using System.Runtime.CompilerServices;
using EmberCrpg.Domain.Overland;

namespace EmberCrpg.Simulation.Overland
{
    /// <summary>Engine-free RGBA image payload for the overland map panel.</summary>
    public sealed class OverlandMapImage
    {
        public OverlandMapImage(int width, int height, byte[] rgbaBytes, ulong cacheKey)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Image width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Image height must be positive.");
            if (rgbaBytes == null)
                throw new ArgumentNullException(nameof(rgbaBytes));
            if (rgbaBytes.Length != checked(width * height * 4))
                throw new ArgumentException("RGBA byte count must equal width * height * 4.", nameof(rgbaBytes));

            Width = width;
            Height = height;
            RgbaBytes = rgbaBytes;
            CacheKey = cacheKey;
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] RgbaBytes { get; }
        public ulong CacheKey { get; }
    }

    /// <summary>Deterministically upsamples the 16x16 biome grid into a fine relief-style RGBA map.</summary>
    public static class OverlandMapImageSampler
    {
        public const int DefaultImageSize = 512;

        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        public static OverlandMapImage Sample(
            OverlandMap map,
            int width = DefaultImageSize,
            int height = DefaultImageSize)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Image width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Image height must be positive.");

            ulong cacheKey = ComputeCacheKey(map, width, height);
            uint noiseSeed = unchecked((uint)cacheKey ^ (uint)(cacheKey >> 32));
            var tileColors = BuildTileColors(map);
            var rgba = new byte[checked(width * height * 4)];

            for (int y = 0; y < height; y++)
            {
                double fy = (((double)y + 0.5d) * map.Height / height) - 0.5d;
                int yBase = FloorToInt(fy);
                int y0 = Clamp(yBase, 0, map.Height - 1);
                int y1 = Clamp(yBase + 1, 0, map.Height - 1);
                double ty = Smooth(fy - yBase);

                for (int x = 0; x < width; x++)
                {
                    double fx = (((double)x + 0.5d) * map.Width / width) - 0.5d;
                    int xBase = FloorToInt(fx);
                    int x0 = Clamp(xBase, 0, map.Width - 1);
                    int x1 = Clamp(xBase + 1, 0, map.Width - 1);
                    double tx = Smooth(fx - xBase);

                    var bottom = Lerp(tileColors[ToIndex(x0, y0, map.Width)], tileColors[ToIndex(x1, y0, map.Width)], tx);
                    var top = Lerp(tileColors[ToIndex(x0, y1, map.Width)], tileColors[ToIndex(x1, y1, map.Width)], tx);
                    var color = Lerp(bottom, top, ty);

                    double terrainX = fx + 0.5d;
                    double terrainY = fy + 0.5d;
                    double relief = FractalValueNoise(terrainX, terrainY, noiseSeed);
                    double shade = 0.93d + (relief * 0.18d);

                    int offset = ((y * width) + x) * 4;
                    rgba[offset] = ToByte(color.R * shade);
                    rgba[offset + 1] = ToByte(color.G * shade);
                    rgba[offset + 2] = ToByte(color.B * shade);
                    rgba[offset + 3] = 255;
                }
            }

            return new OverlandMapImage(width, height, rgba, cacheKey);
        }

        public static ulong ComputeCacheKey(
            OverlandMap map,
            int width = DefaultImageSize,
            int height = DefaultImageSize)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            ulong hash = FnvOffset;
            hash = Mix(hash, width);
            hash = Mix(hash, height);
            hash = Mix(hash, map.Width);
            hash = Mix(hash, map.Height);
            hash = Mix(hash, map.Settlements.Count);

            for (int i = 0; i < map.Tiles.Count; i++)
            {
                var tile = map.Tiles[i];
                hash = Mix(hash, tile.X);
                hash = Mix(hash, tile.Y);
                hash = Mix(hash, (int)tile.Biome);
                if (OverlandMapLandMaskStore.TryIsLandAt(map, tile.X, tile.Y, out bool isLand))
                    hash = Mix(hash, isLand ? 1 : 0);
                hash = Mix(hash, tile.PropVariationSeed);
                hash = Mix(hash, tile.SettlementIds.Count);
            }

            for (int i = 0; i < map.Settlements.Count; i++)
            {
                var settlement = map.Settlements[i];
                hash = Mix(hash, settlement.Id.Value);
                hash = Mix(hash, (int)settlement.Kind);
                hash = Mix(hash, settlement.TilePosition.X);
                hash = Mix(hash, settlement.TilePosition.Y);
            }

            return hash;
        }

        private static Rgb[] BuildTileColors(OverlandMap map)
        {
            var colors = new Rgb[map.Width * map.Height];
            for (int i = 0; i < map.Tiles.Count; i++)
            {
                var tile = map.Tiles[i];
                bool hasLandSignal = OverlandMapLandMaskStore.TryIsLandAt(map, tile.X, tile.Y, out bool isLand);
                colors[ToIndex(tile.X, tile.Y, map.Width)] = BiomeColor(tile.Biome, hasLandSignal, isLand);
            }

            return colors;
        }

        // Byte equivalents of the former OverlandMapPanel Unity Color palette.
        private static Rgb BiomeColor(BiomeKind biome, bool hasLandSignal, bool isLand)
        {
            switch (biome)
            {
                case BiomeKind.Plains: return new Rgb(107d, 133d, 71d);
                case BiomeKind.Forest: return new Rgb(46d, 92d, 51d);
                case BiomeKind.Mountain: return new Rgb(122d, 120d, 133d);
                case BiomeKind.Coast:
                    if (!hasLandSignal)
                        return new Rgb(66d, 117d, 158d);
                    return isLand ? new Rgb(188d, 170d, 112d) : new Rgb(32d, 72d, 120d);
                case BiomeKind.Swamp: return new Rgb(69d, 87d, 66d);
                case BiomeKind.Desert: return new Rgb(189d, 168d, 107d);
                case BiomeKind.Tundra: return new Rgb(173d, 184d, 191d);
                case BiomeKind.Ash: return new Rgb(87d, 61d, 61d);
                default: return new Rgb(77d, 77d, 77d);
            }
        }

        private static double FractalValueNoise(double x, double y, uint seed)
        {
            double low = ValueNoise(x * 1.35d, y * 1.35d, seed);
            double mid = ValueNoise((x * 3.25d) + 17.0d, (y * 3.25d) - 29.0d, seed ^ 0x9E3779B9u);
            double high = ValueNoise((x * 8.0d) - 41.0d, (y * 8.0d) + 11.0d, seed ^ 0xA511E9B3u);
            return (low * 0.58d) + (mid * 0.28d) + (high * 0.14d);
        }

        private static double ValueNoise(double x, double y, uint seed)
        {
            int x0 = FloorToInt(x);
            int y0 = FloorToInt(y);
            int x1 = x0 + 1;
            int y1 = y0 + 1;
            double tx = Smooth(x - x0);
            double ty = Smooth(y - y0);

            double bottom = Lerp(Hash01(x0, y0, seed), Hash01(x1, y0, seed), tx);
            double top = Lerp(Hash01(x0, y1, seed), Hash01(x1, y1, seed), tx);
            return (Lerp(bottom, top, ty) * 2.0d) - 1.0d;
        }

        private static double Hash01(int x, int y, uint seed)
        {
            uint hash = Hash2(x, y, seed);
            return (hash & 0x00FFFFFFu) / 16777215.0d;
        }

        private static uint Hash2(int x, int y, uint seed)
        {
            unchecked
            {
                uint hash = seed ^ 0x9E3779B9u;
                hash ^= (uint)x * 0x85EBCA6Bu;
                hash = (hash << 13) | (hash >> 19);
                hash ^= (uint)y * 0xC2B2AE35u;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                hash *= 0x846CA68Bu;
                hash ^= hash >> 16;
                return hash;
            }
        }

        private static ulong Mix(ulong hash, int value)
        {
            return Mix(hash, unchecked((uint)value));
        }

        private static ulong Mix(ulong hash, uint value)
        {
            return Mix(hash, (ulong)value);
        }

        private static ulong Mix(ulong hash, ulong value)
        {
            unchecked
            {
                hash ^= value;
                return hash * FnvPrime;
            }
        }

        private static int ToIndex(int x, int y, int width)
        {
            return (y * width) + x;
        }

        private static int FloorToInt(double value)
        {
            return (int)Math.Floor(value);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }

        private static byte ToByte(double value)
        {
            int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            if (rounded < 0)
                return 0;
            return rounded > 255 ? (byte)255 : (byte)rounded;
        }

        private static double Smooth(double value)
        {
            return value * value * (3.0d - (2.0d * value));
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + ((b - a) * t);
        }

        private static Rgb Lerp(Rgb a, Rgb b, double t)
        {
            return new Rgb(Lerp(a.R, b.R, t), Lerp(a.G, b.G, t), Lerp(a.B, b.B, t));
        }

        private readonly struct Rgb
        {
            public Rgb(double r, double g, double b)
            {
                R = r;
                G = g;
                B = b;
            }

            public double R { get; }
            public double G { get; }
            public double B { get; }
        }
    }

    internal static class OverlandMapLandMaskStore
    {
        private static readonly ConditionalWeakTable<OverlandMap, LandMaskSnapshot> LandMasks = new ConditionalWeakTable<OverlandMap, LandMaskSnapshot>();

        public static void Register(OverlandMap map, bool[] landMask)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (landMask == null)
                throw new ArgumentNullException(nameof(landMask));
            if (landMask.Length != map.Width * map.Height)
                throw new ArgumentException("Land mask length must equal map width * height.", nameof(landMask));

            LandMasks.Remove(map);
            LandMasks.Add(map, new LandMaskSnapshot(map.Width, map.Height, landMask));
        }

        public static bool TryIsLandAt(OverlandMap map, int x, int y, out bool isLand)
        {
            isLand = false;
            if (map == null || !LandMasks.TryGetValue(map, out var snapshot))
                return false;

            return snapshot.TryIsLandAt(x, y, out isLand);
        }

        private sealed class LandMaskSnapshot
        {
            private readonly bool[] _landMask;

            public LandMaskSnapshot(int width, int height, bool[] landMask)
            {
                Width = width;
                Height = height;
                _landMask = (bool[])landMask.Clone();
            }

            public int Width { get; }
            public int Height { get; }

            public bool TryIsLandAt(int x, int y, out bool isLand)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height)
                {
                    isLand = false;
                    return false;
                }

                isLand = _landMask[(y * Width) + x];
                return true;
            }
        }
    }
}

// Why this file is intentionally long: the deterministic overland image sampler co-locates cache-key, land-mask, and pixel sampling helpers so map rendering stays engine-free and reproducible in one unit.
using System;
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

    /// <summary>Deterministically rasterizes the overland map into per-tile RGBA pixels with no smoothing.</summary>
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
            OverlandMapGeographyStore.TryGet(map, out var geography);
            var rgba = new byte[checked(width * height * 4)];

            for (int y = 0; y < height; y++)
            {
                int tileY = SampleTileIndex(y, height, map.Height);
                double geoY = ContinuousTileCoordinate(y, height, map.Height);

                for (int x = 0; x < width; x++)
                {
                    int tileX = SampleTileIndex(x, width, map.Width);
                    double geoX = ContinuousTileCoordinate(x, width, map.Width);
                    var tile = map.Tiles[ToIndex(tileX, tileY, map.Width)];
                    var color = PixelColor(tile, tileX, tileY, geoX, geoY, geography);

                    int offset = ((y * width) + x) * 4;
                    rgba[offset] = ToByte(color.R);
                    rgba[offset + 1] = ToByte(color.G);
                    rgba[offset + 2] = ToByte(color.B);
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
                if (OverlandMapGeographyStore.TryGet(map, out var geography))
                {
                    hash = Mix(hash, geography.IsLand(tile.X, tile.Y) ? 1 : 0);
                    hash = Mix(hash, Quantize(geography.Elevation(tile.X, tile.Y)));
                    hash = Mix(hash, Quantize(geography.Temperature(tile.X, tile.Y)));
                    hash = Mix(hash, Quantize(geography.Moisture(tile.X, tile.Y)));
                }
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

        private static Rgb PixelColor(
            RegionTile tile,
            int tileX,
            int tileY,
            double geoX,
            double geoY,
            OverlandMapGeographySnapshot geography)
        {
            if (geography == null)
                return BiomeColor(tile.Biome, hasLandSignal: false, isLand: false);

            bool isLand = geography.IsLand(tileX, tileY);
            double elevation = Bilinear(geography, geoX, geoY, Channel.Elevation);
            double temperature = Bilinear(geography, geoX, geoY, Channel.Temperature);
            double moisture = Bilinear(geography, geoX, geoY, Channel.Moisture);
            double relief = Relief(geography, geoX, geoY, elevation);

            return isLand
                ? LandColor(tile.Biome, elevation, temperature, moisture, relief)
                : WaterColor(elevation, relief, NeighborLandScore(geography, tileX, tileY));
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

        private static Rgb LandColor(BiomeKind biome, double elevation, double temperature, double moisture, double relief)
        {
            var baseColor = BiomeColor(biome, hasLandSignal: true, isLand: true);
            if (biome == BiomeKind.Mountain)
                baseColor = Lerp(baseColor, new Rgb(196d, 194d, 188d), Clamp01((elevation - 0.58d) * 2.2d));
            else if (biome == BiomeKind.Forest)
                baseColor = Lerp(baseColor, new Rgb(34d, 72d, 38d), Clamp01(moisture * 0.8d));
            else if (biome == BiomeKind.Desert)
                baseColor = Lerp(baseColor, new Rgb(211d, 190d, 122d), Clamp01(temperature * 0.7d));

            double moistureTint = 0.94d + (Clamp01(moisture) * 0.12d);
            return Scale(baseColor, relief * moistureTint);
        }

        private static Rgb WaterColor(double elevation, double relief, double neighborLandScore)
        {
            double shallow = Clamp01((elevation + 0.08d) * 6d);
            shallow = Math.Max(shallow, neighborLandScore * 0.65d);
            var deep = new Rgb(22d, 59d, 111d);
            var shelf = new Rgb(56d, 123d, 183d);
            return Scale(Lerp(deep, shelf, shallow), 0.9d + ((relief - 0.75d) * 0.28d));
        }

        private static double Relief(OverlandMapGeographySnapshot geography, double x, double y, double elevation)
        {
            double east = Bilinear(geography, x + 1d, y, Channel.Elevation);
            double south = Bilinear(geography, x, y + 1d, Channel.Elevation);
            double slope = ((elevation - east) * 0.55d) + ((elevation - south) * 0.35d);
            return Clamp(0.78d + (elevation * 0.34d) + slope, 0.5d, 1.25d);
        }

        private static double NeighborLandScore(OverlandMapGeographySnapshot geography, int x, int y)
        {
            int count = 0;
            if (geography.IsLand(x - 1, y)) count++;
            if (geography.IsLand(x + 1, y)) count++;
            if (geography.IsLand(x, y - 1)) count++;
            if (geography.IsLand(x, y + 1)) count++;
            return count / 4d;
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

        private static int SampleTileIndex(int pixel, int pixelCount, int tileCount)
        {
            return OverlandMapProjection.PixelCenterToTileIndex(pixel, pixelCount, tileCount);
        }

        private static double ContinuousTileCoordinate(int pixel, int pixelCount, int tileCount)
        {
            return (((pixel + 0.5d) / pixelCount) * tileCount) - 0.5d;
        }

        private static double Bilinear(OverlandMapGeographySnapshot geography, double x, double y, Channel channel)
        {
            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);
            double tx = x - x0;
            double ty = y - y0;

            double a = Sample(geography, x0, y0, channel);
            double b = Sample(geography, x0 + 1, y0, channel);
            double c = Sample(geography, x0, y0 + 1, channel);
            double d = Sample(geography, x0 + 1, y0 + 1, channel);
            return Lerp(Lerp(a, b, tx), Lerp(c, d, tx), ty);
        }

        private static double Sample(OverlandMapGeographySnapshot geography, int x, int y, Channel channel)
        {
            switch (channel)
            {
                case Channel.Temperature: return geography.Temperature(x, y);
                case Channel.Moisture: return geography.Moisture(x, y);
                default: return geography.Elevation(x, y);
            }
        }

        private static Rgb Lerp(Rgb a, Rgb b, double t)
        {
            t = Clamp01(t);
            return new Rgb(Lerp(a.R, b.R, t), Lerp(a.G, b.G, t), Lerp(a.B, b.B, t));
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + ((b - a) * t);
        }

        private static Rgb Scale(Rgb color, double scale)
        {
            return new Rgb(color.R * scale, color.G * scale, color.B * scale);
        }

        private static double Clamp01(double value)
        {
            return Clamp(value, 0d, 1d);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }

        private static int Quantize(double value)
        {
            return (int)Math.Round(value * 10000d, MidpointRounding.AwayFromZero);
        }

        private static byte ToByte(double value)
        {
            int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            if (rounded < 0)
                return 0;
            return rounded > 255 ? (byte)255 : (byte)rounded;
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

        private enum Channel
        {
            Elevation,
            Temperature,
            Moisture,
        }
    }
}

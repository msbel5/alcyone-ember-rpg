using System;
using System.Collections.Generic;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Engine-free RGBA image (row-major, 4 bytes/pixel) of a sampled planet.</summary>
    public sealed class PlanetImage
    {
        public PlanetImage(int width, int height, byte[] rgba)
        {
            Width = width;
            Height = height;
            Rgba = rgba;
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] Rgba { get; }
    }

    /// <summary>
    /// Projects a generated <see cref="PlanetField"/> to an equirectangular RGBA image so the planet can be
    /// rendered (proof PNG now, in-game reveal later). Pure + deterministic: each pixel is coloured by the
    /// nearest icosphere tile's elevation/land state. Nearest-tile lookup is accelerated by latitude bands so
    /// it stays fast at high subdivision levels (the true nearest tile is always within a couple of bands of the
    /// pixel's latitude).
    /// </summary>
    public static class PlanetImageSampler
    {
        private const int BandSearchRadius = 3;

        public static PlanetImage Sample(PlanetField field, int width, int height)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));

            var grid = field.Grid;
            int tileCount = grid.Count;
            int bandCount = Math.Max(8, (int)Math.Round(Math.Sqrt(tileCount)));
            double bandSize = Math.PI / bandCount;

            var bands = new List<int>[bandCount];
            for (int b = 0; b < bandCount; b++) bands[b] = new List<int>();
            for (int tileId = 0; tileId < tileCount; tileId++)
            {
                var p = grid.TileAt(tileId).Position;
                int band = LatitudeBand(Math.Asin(Clamp(p.Y, -1d, 1d)), bandSize, bandCount);
                bands[band].Add(tileId);
            }

            double seaLevel = field.Parameters.SeaLevelThreshold;
            var rgba = new byte[width * height * 4];
            for (int py = 0; py < height; py++)
            {
                double lat = (Math.PI / 2d) - (((py + 0.5d) / height) * Math.PI);
                double cosLat = Math.Cos(lat), sinLat = Math.Sin(lat);
                int centerBand = LatitudeBand(lat, bandSize, bandCount);
                for (int px = 0; px < width; px++)
                {
                    double lon = (((px + 0.5d) / width) * 2d * Math.PI) - Math.PI;
                    double dx = cosLat * Math.Cos(lon), dy = sinLat, dz = cosLat * Math.Sin(lon);

                    int best = -1;
                    double bestDot = -2d;
                    for (int b = centerBand - BandSearchRadius; b <= centerBand + BandSearchRadius; b++)
                    {
                        if (b < 0 || b >= bandCount) continue;
                        var list = bands[b];
                        for (int k = 0; k < list.Count; k++)
                        {
                            var p = grid.TileAt(list[k]).Position;
                            double d = (p.X * dx) + (p.Y * dy) + (p.Z * dz);
                            if (d > bestDot) { bestDot = d; best = list[k]; }
                        }
                    }

                    var tile = field.TileAt(best);
                    Color(tile, seaLevel, out byte r, out byte g, out byte bl);
                    int idx = ((py * width) + px) * 4;
                    rgba[idx] = r; rgba[idx + 1] = g; rgba[idx + 2] = bl; rgba[idx + 3] = 255;
                }
            }

            // Overlay emergent settlements as type-coloured markers so the colony seed is visible on the map.
            var settlements = field.Settlements;
            if (settlements != null)
            {
                for (int i = 0; i < settlements.Count; i++)
                {
                    var s = settlements[i];
                    var pos = grid.TileAt(s.TileId).Position;
                    double lat = Math.Asin(Clamp(pos.Y, -1d, 1d));
                    double lon = Math.Atan2(pos.Z, pos.X);
                    int sx = (int)(((lon + Math.PI) / (2d * Math.PI)) * width);
                    int sy = (int)(((Math.PI / 2d - lat) / Math.PI) * height);
                    SettlementColor(s.Type, out byte mr, out byte mg, out byte mb);
                    PlotMarker(rgba, width, height, sx, sy, mr, mg, mb);
                }
            }

            return new PlanetImage(width, height, rgba);
        }

        private static void SettlementColor(PlanetSettlementType type, out byte r, out byte g, out byte b)
        {
            switch (type)
            {
                case PlanetSettlementType.Capital: r = 236; g = 40; b = 40; return;
                case PlanetSettlementType.MiningTown: r = 70; g = 70; b = 76; return;
                case PlanetSettlementType.Port: r = 40; g = 184; b = 224; return;
                case PlanetSettlementType.ForestHamlet: r = 24; g = 132; b = 32; return;
                case PlanetSettlementType.MarketTown: r = 240; g = 146; b = 30; return;
                default: r = 244; g = 214; b = 64; return; // FarmVillage
            }
        }

        // A filled marker (center + plus) in the type colour, ringed by near-black so it reads on any biome.
        private static void PlotMarker(byte[] rgba, int width, int height, int cx, int cy, byte r, byte g, byte b)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || x >= width || y < 0 || y >= height) continue;
                    int manhattan = Math.Abs(dx) + Math.Abs(dy);
                    int idx = ((y * width) + x) * 4;
                    if (manhattan <= 1) { rgba[idx] = r; rgba[idx + 1] = g; rgba[idx + 2] = b; }
                    else if (manhattan == 2) { rgba[idx] = 12; rgba[idx + 1] = 12; rgba[idx + 2] = 14; }
                }
            }
        }

        private static int LatitudeBand(double lat, double bandSize, int bandCount)
        {
            int band = (int)((lat + (Math.PI / 2d)) / bandSize);
            if (band < 0) return 0;
            if (band >= bandCount) return bandCount - 1;
            return band;
        }

        // Ocean by depth; rivers as bright water; land tinted by biome with a little elevation relief shading.
        private static void Color(PlanetTileField tile, double seaLevel, out byte r, out byte g, out byte b)
        {
            if (!tile.IsLand)
            {
                double t = Clamp((tile.Elevation - (seaLevel - 0.5d)) / 0.5d, 0d, 1d);
                r = Lerp(8, 70, t); g = Lerp(22, 130, t); b = Lerp(60, 200, t);
                return;
            }

            if (tile.IsRiver) { r = 70; g = 120; b = 205; return; }
            if (tile.IsLake) { r = 40; g = 90; b = 165; return; }

            BiomeColor(tile.Biome, out int br, out int bg, out int bb);
            double relief = Clamp(0.80d + ((tile.Elevation - seaLevel) * 0.34d), 0.62d, 1.18d);
            r = Mul(br, relief); g = Mul(bg, relief); b = Mul(bb, relief);
        }

        private static void BiomeColor(PlanetBiome biome, out int r, out int g, out int b)
        {
            switch (biome)
            {
                case PlanetBiome.Ice: r = 236; g = 240; b = 246; return;
                case PlanetBiome.Tundra: r = 158; g = 156; b = 138; return;
                case PlanetBiome.Taiga: r = 52; g = 92; b = 64; return;
                case PlanetBiome.TemperateForest: r = 64; g = 122; b = 58; return;
                case PlanetBiome.Grassland: r = 138; g = 168; b = 84; return;
                case PlanetBiome.Desert: r = 212; g = 192; b = 132; return;
                case PlanetBiome.Savanna: r = 178; g = 162; b = 86; return;
                case PlanetBiome.TropicalRainforest: r = 30; g = 104; b = 44; return;
                case PlanetBiome.Mountain: r = 142; g = 138; b = 132; return;
                default: r = 92; g = 122; b = 72; return;
            }
        }

        private static byte Mul(int c, double f)
        {
            int v = (int)Math.Round(c * f);
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)v;
        }

        private static byte Lerp(int from, int to, double t)
        {
            int v = (int)Math.Round(from + ((to - from) * t));
            if (v < 0) v = 0;
            if (v > 255) v = 255;
            return (byte)v;
        }

        private static double Clamp(double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}

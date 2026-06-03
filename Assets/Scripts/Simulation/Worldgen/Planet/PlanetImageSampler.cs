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
                    Color(tile.IsLand, tile.Elevation, seaLevel, out byte r, out byte g, out byte bl);
                    int idx = ((py * width) + px) * 4;
                    rgba[idx] = r; rgba[idx + 1] = g; rgba[idx + 2] = bl; rgba[idx + 3] = 255;
                }
            }

            return new PlanetImage(width, height, rgba);
        }

        private static int LatitudeBand(double lat, double bandSize, int bandCount)
        {
            int band = (int)((lat + (Math.PI / 2d)) / bandSize);
            if (band < 0) return 0;
            if (band >= bandCount) return bandCount - 1;
            return band;
        }

        // Ocean: navy (deep) -> light blue (shallow). Land: green (lowland) -> brown (hill) -> white (peak).
        private static void Color(bool isLand, double elevation, double seaLevel, out byte r, out byte g, out byte b)
        {
            if (!isLand)
            {
                double t = Clamp((elevation - (seaLevel - 0.5d)) / 0.5d, 0d, 1d);
                r = Lerp(8, 70, t); g = Lerp(22, 130, t); b = Lerp(60, 200, t);
                return;
            }

            double h = Clamp((elevation - seaLevel) / 0.9d, 0d, 1d);
            if (h < 0.5d)
            {
                double u = h / 0.5d;
                r = Lerp(58, 140, u); g = Lerp(132, 110, u); b = Lerp(58, 70, u);
            }
            else
            {
                double u = (h - 0.5d) / 0.5d;
                r = Lerp(140, 236, u); g = Lerp(110, 236, u); b = Lerp(70, 240, u);
            }
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

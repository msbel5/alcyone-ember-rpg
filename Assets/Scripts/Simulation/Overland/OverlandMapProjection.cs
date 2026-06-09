using System;

namespace EmberCrpg.Simulation.Overland
{
    /// <summary>
    /// Deterministic map projection math shared by rasterization and UI markers. One overland tile occupies a
    /// rectangular cell in normalized atlas space; no presentation code should guess a second projection.
    /// </summary>
    public static class OverlandMapProjection
    {
        public static double TileCenterNormalized(int coordinate, int tileCount)
        {
            if (tileCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(tileCount), tileCount, "Tile count must be positive.");
            return (coordinate + 0.5d) / tileCount;
        }

        public static float TileCenterPercent(int coordinate, int tileCount)
        {
            return (float)(TileCenterNormalized(coordinate, tileCount) * 100d);
        }

        public static int PixelCenterToTileIndex(int pixel, int pixelCount, int tileCount)
        {
            if (pixelCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(pixelCount), pixelCount, "Pixel count must be positive.");
            if (tileCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(tileCount), tileCount, "Tile count must be positive.");

            double sample = ((pixel + 0.5d) * tileCount) / pixelCount;
            int index = (int)Math.Floor(sample);
            if (index < 0) return 0;
            return index >= tileCount ? tileCount - 1 : index;
        }
    }
}

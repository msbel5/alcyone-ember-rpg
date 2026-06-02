using System;

namespace EmberCrpg.Domain.Overland
{
    /// <summary>Tunable knobs for deterministic overland generation.</summary>
    public sealed class OverlandParameters
    {
        public OverlandParameters(int width = 16, int height = 16, int biomeSeedCount = 12, double settlementDensity = 0.30d)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
            if (biomeSeedCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(biomeSeedCount), biomeSeedCount, "Biome seed count must be positive.");
            if (biomeSeedCount > width * height)
                throw new ArgumentOutOfRangeException(nameof(biomeSeedCount), biomeSeedCount, "Biome seed count cannot exceed tile count.");
            if (settlementDensity <= 0d || settlementDensity > 1d)
                throw new ArgumentOutOfRangeException(nameof(settlementDensity), settlementDensity, "Settlement density must be in (0, 1].");

            Width = width;
            Height = height;
            BiomeSeedCount = biomeSeedCount;
            SettlementDensity = settlementDensity;
        }

        public int Width { get; }
        public int Height { get; }
        public int BiomeSeedCount { get; }
        public double SettlementDensity { get; }
        public static OverlandParameters Default => new OverlandParameters();
    }
}

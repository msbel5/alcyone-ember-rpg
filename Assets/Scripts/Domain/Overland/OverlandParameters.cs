using System;

namespace EmberCrpg.Domain.Overland
{
    /// <summary>Tunable knobs for deterministic overland generation.</summary>
    public sealed class OverlandParameters
    {
        // Each region tile spans this many kilometres per edge. With the default 16x16 grid this makes the
        // playable overland 640 km x 640 km = 409,600 km2 — about double Daggerfall's ~200,000 km2 world,
        // matching the "at least 2x the reference games" scale goal. It is a narrative/travel-scale figure
        // (regions are fast-travelled between), not a per-metre traversal budget.
        public const double DefaultRegionEdgeKm = 40.0d;

        public OverlandParameters(
            int width = 16,
            int height = 16,
            int biomeSeedCount = 12,
            double settlementDensity = 0.30d,
            double regionEdgeKm = DefaultRegionEdgeKm)
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
            if (regionEdgeKm <= 0d)
                throw new ArgumentOutOfRangeException(nameof(regionEdgeKm), regionEdgeKm, "Region edge length (km) must be positive.");

            Width = width;
            Height = height;
            BiomeSeedCount = biomeSeedCount;
            SettlementDensity = settlementDensity;
            RegionEdgeKm = regionEdgeKm;
        }

        public int Width { get; }
        public int Height { get; }
        public int BiomeSeedCount { get; }
        public double SettlementDensity { get; }

        /// <summary>Kilometres spanned by one region tile's edge.</summary>
        public double RegionEdgeKm { get; }

        /// <summary>Area of a single region tile in square kilometres (RegionEdgeKm squared).</summary>
        public double RegionAreaKm2 => RegionEdgeKm * RegionEdgeKm;

        /// <summary>Total playable overland area in square kilometres (Width * Height * RegionAreaKm2).</summary>
        public double TotalAreaKm2 => Width * Height * RegionAreaKm2;

        public static OverlandParameters Default => new OverlandParameters();
    }
}

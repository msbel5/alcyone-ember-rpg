using System;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Phase-1a knobs for deterministic spherical planet substrate generation.</summary>
    public sealed class PlanetParameters
    {
        public PlanetParameters(
            int subdivisionLevel,
            int plateCount,
            double oceanicFraction,
            double seaLevelThreshold,
            double driftScale)
        {
            if (subdivisionLevel < 0 || subdivisionLevel > IcosphereGrid.MaxSubdivisionLevel)
                throw new ArgumentOutOfRangeException(nameof(subdivisionLevel), subdivisionLevel, "Subdivision level is outside the supported range.");
            if (plateCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(plateCount), plateCount, "Plate count must be positive.");
            if (oceanicFraction < 0d || oceanicFraction > 1d)
                throw new ArgumentOutOfRangeException(nameof(oceanicFraction), oceanicFraction, "Oceanic fraction must be between 0 and 1.");
            if (driftScale <= 0d)
                throw new ArgumentOutOfRangeException(nameof(driftScale), driftScale, "Drift scale must be positive.");

            SubdivisionLevel = subdivisionLevel;
            PlateCount = plateCount;
            OceanicFraction = oceanicFraction;
            SeaLevelThreshold = seaLevelThreshold;
            DriftScale = driftScale;
        }

        public static PlanetParameters Default => new PlanetParameters(3, 10, 0.65d, 0d, 0.035d);

        public int SubdivisionLevel { get; }
        public int PlateCount { get; }
        public double OceanicFraction { get; }
        public double SeaLevelThreshold { get; }
        public double DriftScale { get; }
    }
}

using System;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Single facade for phase-1a spherical planet substrate generation.</summary>
    public static class PlanetGenerator
    {
        private const uint PlateStage = 0x70514E45u;

        public static PlanetField Generate(uint seed, PlanetParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var rootRng = new XorShiftRng(seed);
            IcosphereGrid grid = IcosphereGrid.Build(parameters.SubdivisionLevel);
            PlatePartitionResult plates = new PlatePartition().Build(
                grid,
                parameters.PlateCount,
                parameters.OceanicFraction,
                parameters.DriftScale,
                PlanetRng.Fork(rootRng, PlateStage));
            PlateBoundarySet boundaries = new PlateBoundaries().Build(grid, plates);
            return new TectonicElevation().Build(seed, parameters, grid, plates, boundaries);
        }
    }
}

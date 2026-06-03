using System;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Single facade for phase-1a spherical planet substrate generation.</summary>
    public static class PlanetGenerator
    {
        private const uint PlateStage = 0x70514E45u;
        private const uint ElevationNoiseStage = 0x454E4F49u;
        private const uint ClimateStageSeed = 0x434C494Du;
        private const uint HydrologyStageSeed = 0x48594452u;
        private const uint ErosionStageSeed = 0x45524F53u;
        private const uint ResourceStageSeed = 0x5245534Fu;
        private const uint SettlementStageSeed = 0x5345544Cu;

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
            PlanetField field = new TectonicElevation().Build(seed, parameters, grid, plates, boundaries);
            field = new ElevationNoise().Apply(field, PlanetRng.Fork(rootRng, ElevationNoiseStage));
            field = new ClimateStage().Apply(field, PlanetRng.Fork(rootRng, ClimateStageSeed));
            field = new HydrologyStage().Apply(field, PlanetRng.Fork(rootRng, HydrologyStageSeed));
            field = new ErosionStage().Apply(field, PlanetRng.Fork(rootRng, ErosionStageSeed));
            field = new ResourceStage().Apply(field, PlanetRng.Fork(rootRng, ResourceStageSeed));
            return new SettlementStage().Apply(field, PlanetRng.Fork(rootRng, SettlementStageSeed));
        }
    }
}

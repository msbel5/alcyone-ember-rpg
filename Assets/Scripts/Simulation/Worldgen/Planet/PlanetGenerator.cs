using System;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Single facade for phase-1a spherical planet substrate generation.</summary>
    public static class PlanetGenerator
    {
        public static PlanetField Generate(uint seed, PlanetParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var context = new PlanetGenerationContext(seed, parameters);
            var manager = new PlanetGenerationManager(PlanetStageFactory.CreateStages(parameters));
            return manager.Generate(context);
        }
    }
}

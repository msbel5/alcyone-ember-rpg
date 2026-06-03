using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Simulation.Worldgen.Planet;
using EmberCrpg.Simulation.Worldgen.PlanetIntegration;

namespace EmberCrpg.Presentation.Ember.Worldgen
{
    /// <summary>
    /// Builds the live <see cref="GeneratedWorld"/> from the SPHERICAL planet pipeline and caches it in the
    /// <see cref="PlanetWorldContext"/> singleton. One generation per seed: the char-creation reveal can build
    /// it with a streaming observer (the planet forming), and SeedWorld then reuses the cached result so there
    /// is no second generation. Deterministic: same seed -> same planet -> same GeneratedWorld.
    /// </summary>
    public static class PlanetWorldService
    {
        // Level 5 (~10,242 tiles): detailed enough for continents/regions + the 128x64 overland projection,
        // fast enough to build behind the loading screen. Bump to 6 for finer planets once gen is streamed.
        private const int GameSubdivisionLevel = 5;

        public static GeneratedWorld GetOrGenerate(
            uint seed,
            WorldgenParameters parameters,
            IPlanetGenerationObserver observer = null)
        {
            var context = PlanetWorldContext.Instance;
            if (context.Has(seed) && observer == null)
                return context.World;

            PlanetParameters planetParameters = ToPlanetParameters(parameters);
            PlanetField field = Generate(seed, planetParameters, observer);
            GeneratedWorld world = PlanetToWorldMapper.Map(field, parameters);
            context.Set(seed, field, world);
            return world;
        }

        /// <summary>Maps the genesis/worldgen knobs to deterministic planet-physics parameters.</summary>
        public static PlanetParameters ToPlanetParameters(WorldgenParameters parameters)
        {
            // Mostly fixed physics (the seed supplies the variety); slightly more plates for higher region
            // counts so larger worlds fragment into more continents.
            int plateCount = parameters == null ? 24 : System.Math.Max(16, System.Math.Min(32, parameters.RegionCount + 8));
            return new PlanetParameters(GameSubdivisionLevel, plateCount, 0.62d, 0d, 0.04d);
        }

        private static PlanetField Generate(uint seed, PlanetParameters planetParameters, IPlanetGenerationObserver observer)
        {
            if (observer == null)
                return PlanetGenerator.Generate(seed, planetParameters);

            var context = new PlanetGenerationContext(seed, planetParameters);
            var manager = new PlanetGenerationManager(PlanetStageFactory.CreateStages(planetParameters));
            return manager.Generate(context, observer);
        }
    }
}

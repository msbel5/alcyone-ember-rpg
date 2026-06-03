using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Simulation.Worldgen.Planet;

namespace EmberCrpg.Presentation.Ember.Worldgen
{
    /// <summary>
    /// Singleton access point to the ACTIVE generated planet/world (PRD_planetary_worldgen §5 — the World
    /// Director + managers ask "what world are we in" here instead of regenerating). The spherical planet +
    /// its mapped <see cref="GeneratedWorld"/> are deterministic but a few seconds to build, so we generate
    /// ONCE per seed (during the char-creation reveal, or lazily in SeedWorld) and cache the result; SeedWorld,
    /// the overland map, and save/load all read the same instance. Keyed by seed so a different New Game
    /// transparently regenerates.
    /// </summary>
    public sealed class PlanetWorldContext
    {
        private static readonly PlanetWorldContext _instance = new PlanetWorldContext();

        private PlanetWorldContext() { }

        public static PlanetWorldContext Instance => _instance;

        public uint? Seed { get; private set; }
        public PlanetField Field { get; private set; }
        public GeneratedWorld World { get; private set; }

        /// <summary>True when the active world for this exact seed is already built and cached.</summary>
        public bool Has(uint seed) => Seed.HasValue && Seed.Value == seed && World != null;

        public void Set(uint seed, PlanetField field, GeneratedWorld world)
        {
            Seed = seed;
            Field = field;
            World = world;
        }

        public void Clear()
        {
            Seed = null;
            Field = null;
            World = null;
        }
    }
}

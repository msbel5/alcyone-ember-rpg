using EmberCrpg.Domain.Overland;

namespace EmberCrpg.Simulation.WorldDirector
{
    /// <summary>
    /// The deterministic inputs a layout strategy needs to plan one settlement. Pure data (no Unity), so
    /// the same world always yields the same plan and the layout can be unit-tested without a scene.
    /// </summary>
    public readonly struct SettlementContext
    {
        public SettlementContext(string name, SettlementKind kind, BiomeKind biome, uint seed)
        {
            Name = name;
            Kind = kind;
            Biome = biome;
            Seed = seed;
        }

        /// <summary>Display name of the settlement (for logs only — not used by the deterministic plan).</summary>
        public string Name { get; }

        public SettlementKind Kind { get; }
        public BiomeKind Biome { get; }

        /// <summary>Deterministic seed (the home region tile's PropVariationSeed). Drives all layout choices.</summary>
        public uint Seed { get; }
    }
}

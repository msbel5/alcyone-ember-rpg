using System;
using System.Collections.Generic;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    public enum PlanetResourceKind
    {
        IronOre = 0,
        PreciousMetal = 1,
        Coal = 2,
        OilGas = 3,
        Stone = 4,
        Clay = 5,
        Wood = 6,
        SoilFertility = 7,
        FreshWater = 8,
    }

    public enum PlanetSettlementType
    {
        FarmVillage = 0,
        MiningTown = 1,
        Port = 2,
        ForestHamlet = 3,
        MarketTown = 4,
        Capital = 5,
    }

    public sealed class PlanetSettlement
    {
        private readonly PlanetResourceKind[] _dominantResources;

        public PlanetSettlement(int tileId, PlanetSettlementType type, int population, PlanetResourceKind[] dominantResources)
        {
            if (tileId < 0)
                throw new ArgumentOutOfRangeException(nameof(tileId), tileId, "Settlement tile id must be non-negative.");
            if (population < 0)
                throw new ArgumentOutOfRangeException(nameof(population), population, "Settlement population must be non-negative.");

            TileId = tileId;
            Type = type;
            Population = population;
            _dominantResources = dominantResources == null ? Array.Empty<PlanetResourceKind>() : (PlanetResourceKind[])dominantResources.Clone();
            DominantResources = Array.AsReadOnly(_dominantResources);
        }

        public int TileId { get; }
        public PlanetSettlementType Type { get; }
        public int Population { get; }
        public IReadOnlyList<PlanetResourceKind> DominantResources { get; }
    }

    public sealed class PlanetImpactSite
    {
        public PlanetImpactSite(int tileId, int radius, double ironAbundance, double preciousMetalAbundance)
        {
            if (tileId < 0)
                throw new ArgumentOutOfRangeException(nameof(tileId), tileId, "Impact tile id must be non-negative.");
            if (radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "Impact radius must be non-negative.");

            TileId = tileId;
            Radius = radius;
            IronAbundance = Clamp01(ironAbundance);
            PreciousMetalAbundance = Clamp01(preciousMetalAbundance);
        }

        public int TileId { get; }
        public int Radius { get; }
        public double IronAbundance { get; }
        public double PreciousMetalAbundance { get; }

        private static double Clamp01(double value)
        {
            if (value < 0d)
                return 0d;
            if (value > 1d)
                return 1d;
            return value;
        }
    }
}

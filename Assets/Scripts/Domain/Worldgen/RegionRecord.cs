using System;

// Design note:
// RegionRecord is the worldgen FOUNDATION's per-region payload. Mirrors
// SiteRecord / FactionRecord's defensive-constructor pattern so invariants
// are pinned at construction: id rejects the empty sentinel, biome rejects
// None, name rejects blank, population bounds reject inversion and negatives.
// Population bounds are stored as the design range supplied by the generator
// — the realized total comes from summing the settlements that land inside.
namespace EmberCrpg.Domain.Worldgen
{
    /// <summary>Pure record describing a procedurally generated region by id, name, population band, and biome.</summary>
    public sealed class RegionRecord
    {
        public RegionRecord(RegionId id, string name, int populationLow, int populationHigh, BiomeKind biome)
        {
            if (id.IsEmpty)
                throw new ArgumentException("RegionId.Empty cannot back a RegionRecord.", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Region name is required.", nameof(name));
            if (biome == BiomeKind.None)
                throw new ArgumentException("BiomeKind.None is reserved as the empty sentinel.", nameof(biome));
            if (populationLow < 0)
                throw new ArgumentOutOfRangeException(nameof(populationLow), populationLow, "Region population bounds must be non-negative.");
            if (populationHigh < populationLow)
                throw new ArgumentOutOfRangeException(nameof(populationHigh), populationHigh, "populationHigh must be greater than or equal to populationLow.");

            Id = id;
            Name = name;
            PopulationLow = populationLow;
            PopulationHigh = populationHigh;
            Biome = biome;
        }

        public RegionId Id { get; }
        public string Name { get; }
        public int PopulationLow { get; }
        public int PopulationHigh { get; }
        public BiomeKind Biome { get; }
    }
}

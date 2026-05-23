using System;

// Design note:
// SettlementRecord is the worldgen FOUNDATION's per-settlement payload —
// one of the ~200 settlements that populate the ~50 regions. Mirrors the
// defensive-constructor pattern: id rejects the empty sentinel, region must
// be non-empty (every settlement lives inside a region), name rejects
// blank, size rejects None, and population must be positive (a settlement
// with zero population is meaningless).
namespace EmberCrpg.Domain.Worldgen
{
    /// <summary>Pure record describing a procedurally generated settlement by id, parent region, name, population, and size bucket.</summary>
    public sealed class SettlementRecord
    {
        public SettlementRecord(SettlementId id, RegionId region, string name, int population, SettlementSize size)
        {
            if (id.IsEmpty)
                throw new ArgumentException("SettlementId.Empty cannot back a SettlementRecord.", nameof(id));
            if (region.IsEmpty)
                throw new ArgumentException("RegionId.Empty cannot back a SettlementRecord — every settlement lives inside a region.", nameof(region));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Settlement name is required.", nameof(name));
            if (size == SettlementSize.None)
                throw new ArgumentException("SettlementSize.None is reserved as the empty sentinel.", nameof(size));
            if (population <= 0)
                throw new ArgumentOutOfRangeException(nameof(population), population, "Settlement population must be positive.");

            Id = id;
            Region = region;
            Name = name;
            Population = population;
            Size = size;
        }

        public SettlementId Id { get; }
        public RegionId Region { get; }
        public string Name { get; }
        public int Population { get; }
        public SettlementSize Size { get; }
    }
}

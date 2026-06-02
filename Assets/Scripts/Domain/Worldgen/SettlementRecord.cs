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
            : this(id, region, name, population, size, -1, -1)
        {
        }

        public SettlementRecord(SettlementId id, RegionId region, string name, int population, SettlementSize size, int tileX, int tileY)
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
            if ((tileX < 0 || tileY < 0) && !(tileX == -1 && tileY == -1))
                throw new ArgumentOutOfRangeException(nameof(tileX), "Settlement tile coordinates must both be non-negative, or both be -1 for legacy positionless records.");

            Id = id;
            Region = region;
            Name = name;
            Population = population;
            Size = size;
            TileX = tileX;
            TileY = tileY;
        }

        public SettlementId Id { get; }
        public RegionId Region { get; }
        public string Name { get; }
        public int Population { get; }
        public SettlementSize Size { get; }
        public int TileX { get; }
        public int TileY { get; }
        public int X => TileX;
        public int Y => TileY;
        public bool HasTilePosition => TileX >= 0 && TileY >= 0;
    }
}

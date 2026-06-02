using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Domain.Overland
{
    /// <summary>Immutable overland tile carrying region ownership, biome, and settlement occupancy.</summary>
    public sealed class RegionTile
    {
        public RegionTile(
            int x,
            int y,
            RegionId regionId,
            BiomeKind biome,
            IReadOnlyList<SettlementId> settlementIds,
            uint propVariationSeed,
            ClimateKind? climate = null)
        {
            if (x < 0)
                throw new ArgumentOutOfRangeException(nameof(x), x, "Tile x must be non-negative.");
            if (y < 0)
                throw new ArgumentOutOfRangeException(nameof(y), y, "Tile y must be non-negative.");
            if (regionId.IsEmpty)
                throw new ArgumentException("RegionId.Empty cannot back a RegionTile.", nameof(regionId));
            if (!Enum.IsDefined(typeof(BiomeKind), biome))
                throw new ArgumentOutOfRangeException(nameof(biome), biome, "Biome must be a defined overland biome.");
            if (settlementIds == null)
                throw new ArgumentNullException(nameof(settlementIds));

            X = x;
            Y = y;
            RegionId = regionId;
            Biome = biome;
            SettlementIds = CopySettlements(settlementIds);
            PropVariationSeed = propVariationSeed;
            Climate = climate;
        }

        public int X { get; }
        public int Y { get; }
        public GridPosition Position => new GridPosition(X, Y);
        public RegionId RegionId { get; }
        public BiomeKind Biome { get; }
        public IReadOnlyList<SettlementId> SettlementIds { get; }
        public uint PropVariationSeed { get; }
        public ClimateKind? Climate { get; }

        private static IReadOnlyList<SettlementId> CopySettlements(IReadOnlyList<SettlementId> source)
        {
            var copy = new List<SettlementId>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].IsEmpty)
                    throw new ArgumentException("SettlementId.Empty cannot be stored on a RegionTile.", nameof(source));
                copy.Add(source[i]);
            }

            return new ReadOnlyCollection<SettlementId>(copy);
        }
    }
}

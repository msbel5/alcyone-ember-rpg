using System;
using System.Collections.Generic;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Tile-to-plate assignment plus seeded per-plate motion.</summary>
    public sealed class PlatePartitionResult
    {
        private readonly int[] _tilePlateIds;

        internal PlatePartitionResult(int[] tilePlateIds, PlateMotion[] plates)
        {
            _tilePlateIds = tilePlateIds;
            Plates = Array.AsReadOnly(plates);
            TilePlateIds = Array.AsReadOnly(_tilePlateIds);
        }

        public IReadOnlyList<int> TilePlateIds { get; }
        public IReadOnlyList<PlateMotion> Plates { get; }
        public int PlateCount => Plates.Count;

        public int PlateIdForTile(int tileId)
        {
            if (tileId < 0 || tileId >= _tilePlateIds.Length)
                throw new ArgumentOutOfRangeException(nameof(tileId), tileId, "Tile id is outside the partition.");
            return _tilePlateIds[tileId];
        }
    }
}

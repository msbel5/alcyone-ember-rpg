using System;
using System.Collections.Generic;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Generated spherical substrate consumed by later projection and reveal stages.</summary>
    public sealed class PlanetField
    {
        private readonly PlanetTileField[] _tiles;
        private readonly PlanetSettlement[] _settlements;
        private readonly PlanetImpactSite[] _resourceImpacts;

        public PlanetField(
            uint seed,
            PlanetParameters parameters,
            IcosphereGrid grid,
            PlatePartitionResult plates,
            PlateBoundarySet boundaries,
            PlanetTileField[] tiles)
            : this(seed, parameters, grid, plates, boundaries, tiles, null, null)
        {
        }

        public PlanetField(
            uint seed,
            PlanetParameters parameters,
            IcosphereGrid grid,
            PlatePartitionResult plates,
            PlateBoundarySet boundaries,
            PlanetTileField[] tiles,
            PlanetSettlement[] settlements,
            PlanetImpactSite[] resourceImpacts)
        {
            Seed = seed;
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            Plates = plates ?? throw new ArgumentNullException(nameof(plates));
            Boundaries = boundaries ?? throw new ArgumentNullException(nameof(boundaries));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _settlements = settlements == null ? Array.Empty<PlanetSettlement>() : (PlanetSettlement[])settlements.Clone();
            _resourceImpacts = resourceImpacts == null ? Array.Empty<PlanetImpactSite>() : (PlanetImpactSite[])resourceImpacts.Clone();
            Tiles = Array.AsReadOnly(_tiles);
            Settlements = Array.AsReadOnly(_settlements);
            ResourceImpacts = Array.AsReadOnly(_resourceImpacts);
        }

        public uint Seed { get; }
        public PlanetParameters Parameters { get; }
        public IcosphereGrid Grid { get; }
        public PlatePartitionResult Plates { get; }
        public PlateBoundarySet Boundaries { get; }
        public IReadOnlyList<PlanetTileField> Tiles { get; }
        public IReadOnlyList<PlanetSettlement> Settlements { get; }
        public IReadOnlyList<PlanetImpactSite> ResourceImpacts { get; }
        public int TileCount => _tiles.Length;

        public PlanetTileField TileAt(int tileId)
        {
            if (tileId < 0 || tileId >= _tiles.Length)
                throw new ArgumentOutOfRangeException(nameof(tileId), tileId, "Tile id is outside the planet field.");
            return _tiles[tileId];
        }

        internal PlanetTileField[] CopyTiles()
        {
            return (PlanetTileField[])_tiles.Clone();
        }

        internal PlanetSettlement[] CopySettlements()
        {
            return (PlanetSettlement[])_settlements.Clone();
        }

        internal PlanetImpactSite[] CopyResourceImpacts()
        {
            return (PlanetImpactSite[])_resourceImpacts.Clone();
        }
    }
}

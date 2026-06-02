using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Domain.Overland
{
    /// <summary>Immutable deterministic overland grid plus the settlement roster indexed on top of it.</summary>
    public sealed class OverlandMap
    {
        private readonly RegionTile[] _tiles;
        private readonly Dictionary<SettlementId, OverlandSettlement> _settlementsById;

        public OverlandMap(int width, int height, IReadOnlyList<RegionTile> tiles, IReadOnlyList<OverlandSettlement> settlements)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
            if (tiles == null)
                throw new ArgumentNullException(nameof(tiles));
            if (settlements == null)
                throw new ArgumentNullException(nameof(settlements));
            if (tiles.Count != width * height)
                throw new ArgumentException("Tile count must equal width * height.", nameof(tiles));

            Width = width;
            Height = height;
            _tiles = new RegionTile[tiles.Count];
            _settlementsById = new Dictionary<SettlementId, OverlandSettlement>(settlements.Count);

            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i] ?? throw new ArgumentException("Tiles cannot contain null.", nameof(tiles));
                if (tile.X >= width || tile.Y >= height)
                    throw new ArgumentException("Tile coordinates must fit inside the map bounds.", nameof(tiles));
                int index = ToIndex(tile.X, tile.Y);
                if (_tiles[index] != null)
                    throw new ArgumentException($"Duplicate tile coordinate ({tile.X},{tile.Y}).", nameof(tiles));
                _tiles[index] = tile;
            }

            var settlementCopy = new List<OverlandSettlement>(settlements.Count);
            for (int i = 0; i < settlements.Count; i++)
            {
                var settlement = settlements[i] ?? throw new ArgumentException("Settlements cannot contain null.", nameof(settlements));
                if (settlement.TilePosition.X >= width || settlement.TilePosition.Y >= height)
                    throw new ArgumentException("Settlement tile position must be inside the map bounds.", nameof(settlements));
                if (_settlementsById.ContainsKey(settlement.Id))
                    throw new ArgumentException($"Duplicate settlement id {settlement.Id}.", nameof(settlements));
                _settlementsById.Add(settlement.Id, settlement);
                settlementCopy.Add(settlement);
            }

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i] == null)
                    throw new ArgumentException("Every tile coordinate in the map bounds must be populated.", nameof(tiles));

                var tile = _tiles[i];
                for (int settlementIndex = 0; settlementIndex < tile.SettlementIds.Count; settlementIndex++)
                {
                    if (!_settlementsById.ContainsKey(tile.SettlementIds[settlementIndex]))
                        throw new ArgumentException($"Tile ({tile.X},{tile.Y}) references missing settlement {tile.SettlementIds[settlementIndex]}.", nameof(tiles));
                }
            }

            Tiles = new ReadOnlyCollection<RegionTile>(_tiles);
            Settlements = new ReadOnlyCollection<OverlandSettlement>(settlementCopy);
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<RegionTile> Tiles { get; }
        public IReadOnlyList<OverlandSettlement> Settlements { get; }

        public RegionTile TileAt(int x, int y)
        {
            if (!TryGetTile(x, y, out var tile))
                throw new ArgumentOutOfRangeException(nameof(x), $"Tile ({x},{y}) is outside the map bounds.");
            return tile;
        }

        public bool TryGetTile(int x, int y, out RegionTile tile)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                tile = null;
                return false;
            }

            tile = _tiles[ToIndex(x, y)];
            return true;
        }

        public bool TryGetSettlement(SettlementId id, out OverlandSettlement settlement)
        {
            if (id.IsEmpty)
            {
                settlement = null;
                return false;
            }

            return _settlementsById.TryGetValue(id, out settlement);
        }

        public int DistanceBetween(SettlementId from, SettlementId to)
        {
            if (!TryGetSettlement(from, out var fromSettlement))
                throw new KeyNotFoundException($"Unknown settlement {from}.");
            if (!TryGetSettlement(to, out var toSettlement))
                throw new KeyNotFoundException($"Unknown settlement {to}.");
            return ChebyshevDistance(fromSettlement.TilePosition, toSettlement.TilePosition);
        }

        public bool TryGetNearestSettlement(GridPosition position, out OverlandSettlement settlement, out int distance)
        {
            settlement = null;
            distance = 0;
            if (Settlements.Count == 0)
                return false;

            int bestDistance = int.MaxValue;
            OverlandSettlement best = null;
            for (int i = 0; i < Settlements.Count; i++)
            {
                var candidate = Settlements[i];
                int candidateDistance = ChebyshevDistance(position, candidate.TilePosition);
                if (candidateDistance < bestDistance || (candidateDistance == bestDistance && candidate.Id.Value < best.Id.Value))
                {
                    bestDistance = candidateDistance;
                    best = candidate;
                }
            }

            settlement = best;
            distance = bestDistance;
            return true;
        }

        public static int ChebyshevDistance(GridPosition a, GridPosition b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            return dx > dy ? dx : dy;
        }

        public static double EuclideanDistance(GridPosition a, GridPosition b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private int ToIndex(int x, int y)
        {
            return (y * Width) + x;
        }
    }
}

using System.Collections.Generic;
using EmberCrpg.Domain.Actors;

// Design note:
// B10 §A2: sim-blocked-cell store. Backing = HashSet<long> keyed by ((long)y * PackStride) + x,
// mirroring GridPathfinder's PackStride shape so a future Stage-B wire-up shares the encoding.
// DERIVED state — never serialized. Rebuilt after any load via a HydrateBlockedCells re-call on
// the same seam that runs EnsureInvariants (see WorldState). Revision bumps on any mutation so
// a future path cache can invalidate for free when buildings/blockers change.
namespace EmberCrpg.Domain.World
{
    /// <summary>Cheap O(1) blocker probe over packed (x,y) cells. Zero per-tick allocation.</summary>
    public sealed class BlockedCellSet
    {
        // 1e6 stride keeps a long key collision-free for any grid up to a million cells per axis
        // (settlement grids are ~30 cells wide; overland is single-tile). Long, not int, so packs
        // never wrap on the site-metre coords hydrated from settlement.TileX * 40000.
        private const long PackStride = 1_000_000L;

        private readonly HashSet<long> _cells = new HashSet<long>();
        private long _revision;

        public long Revision => _revision;

        public int Count => _cells.Count;

        public bool Contains(GridPosition cell) => _cells.Contains(Pack(cell.X, cell.Y));

        public void Add(GridPosition cell)
        {
            if (_cells.Add(Pack(cell.X, cell.Y)))
                _revision++;
        }

        public void Clear()
        {
            if (_cells.Count == 0) return;
            _cells.Clear();
            _revision++;
        }

        public IEnumerable<long> PackedCells => _cells; // read-only enumeration for digest/inspection

        private static long Pack(int x, int y) => ((long)y * PackStride) + x;
    }
}

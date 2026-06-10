using EmberCrpg.Domain.Actors;   // GridPosition
using EmberCrpg.Domain.Overland;  // OverlandMap, OverlandSettlement

namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>
    /// Overland read-model surface (part of <see cref="IWorldViewReadModel"/>): projects the already-
    /// generated overland map, the player's region tile, and the starting settlement name for the M-key
    /// world map and the HUD. Pure projection of existing WorldState / GeneratedWorld data — it runs no
    /// simulation and allocates nothing per frame beyond the resolved value.
    /// </summary>
    public sealed partial class DomainSimulationAdapter
    {
        public OverlandMap Overland => _world?.Overland;

        public GridPosition PlayerOverlandTile => ResolvePlayerOverlandTile();

        public string StartingSettlementName => ResolveStartingSettlementName();

        // The player's overland tile is their CURRENT settlement's tile (fast travel moves it; defaults to
        // the starting settlement); otherwise the settlement nearest the map centre; otherwise the centre.
        private GridPosition ResolvePlayerOverlandTile()
        {
            var map = _world?.Overland;
            if (map == null) return default;

            var current = CurrentSettlementOrStart;
            if (!current.IsEmpty && map.TryGetSettlement(current, out var home))
                return home.TilePosition;

            var centre = new GridPosition(map.Width / 2, map.Height / 2);
            if (map.TryGetNearestSettlement(centre, out var nearest, out _))
                return nearest.TilePosition;
            return centre;
        }

        // Prefer the CURRENT settlement's name (fast travel moves it; defaults to the starting one); fall
        // back to the GeneratedWorld record, which always contains the starting id even when that settlement
        // was not selected for the overland subset.
        private string ResolveStartingSettlementName()
        {
            var current = CurrentSettlementOrStart;
            if (current.IsEmpty) return null;

            var map = _world?.Overland;
            if (map != null && map.TryGetSettlement(current, out var settlement)
                && !string.IsNullOrEmpty(settlement.Name))
                return settlement.Name;

            var records = GeneratedWorld?.Settlements;
            if (records != null)
            {
                for (int i = 0; i < records.Count; i++)
                    if (records[i].Id.Equals(current))
                        return records[i].Name;
            }
            return null;
        }
    }
}

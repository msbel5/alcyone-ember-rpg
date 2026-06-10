using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>Narrow command port for overland fast travel (world map → settlement).</summary>
    public interface IWorldTravelSink
    {
        bool TryTravelToSettlement(string settlementName, out string message);
    }

    public sealed partial class DomainSimulationAdapter : IWorldTravelSink
    {
        // Travel state. Deliberately NOT a new save field: when empty (fresh adapter, or after load) the
        // current settlement is DERIVED from the saved player actor's position (nearest settlement site),
        // so the domain actor position stays the single source of truth for "where the player is".
        private SettlementId _currentSettlement;

        internal SettlementId CurrentSettlementOrStart
        {
            get
            {
                if (!_currentSettlement.IsEmpty) return _currentSettlement;
                var derived = NearestSettlementToPlayer();
                return derived.IsEmpty ? StartingSettlement : derived;
            }
        }

        public bool TryTravelToSettlement(string settlementName, out string message)
        {
            var map = _world?.Overland;
            if (map == null || string.IsNullOrWhiteSpace(settlementName))
            {
                message = "There is no overland world to travel.";
                return false;
            }

            for (int i = 0; i < map.Settlements.Count; i++)
            {
                var settlement = map.Settlements[i];
                if (!string.Equals(settlement.Name, settlementName, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                // The REAL move: the domain player actor relocates to the destination settlement's site, so
                // schedules/quests/save all see the new position. Everything else (overland tile, billboard
                // origin, HUD town name) re-derives from it.
                if (_world.Actors != null && _world.Actors.TryFirstByRole(ActorRole.Player, out var player) && player != null)
                    player.MoveTo(CenterOfSite(SettlementSiteId(settlement.Id)));

                _currentSettlement = settlement.Id;
                _billboardOriginResolved = false; // NPC grid→world re-bases on the new settlement centre
                message = "Travelled to " + settlement.Name + ".";
                return true;
            }

            message = "Unknown settlement: " + settlementName;
            return false;
        }

        private SettlementId NearestSettlementToPlayer()
        {
            var map = _world?.Overland;
            if (map == null || _world.Actors == null) return default;
            if (!_world.Actors.TryFirstByRole(ActorRole.Player, out var player) || player == null) return default;

            SettlementId best = default;
            long bestSq = long.MaxValue;
            for (int i = 0; i < map.Settlements.Count; i++)
            {
                var centre = CenterOfSite(SettlementSiteId(map.Settlements[i].Id));
                long dx = player.Position.X - centre.X, dy = player.Position.Y - centre.Y;
                long sq = (dx * dx) + (dy * dy);
                if (sq < bestSq) { bestSq = sq; best = map.Settlements[i].Id; }
            }
            return best;
        }
    }
}

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

        public bool TryBeginTravelToSettlement(string settlementName, out int travelDays, out string message)
        {
            travelDays = 0;
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

                // Honest travel time: one overland tile edge is 40 km — call it a day's journey. Capture the
                // origin tile BEFORE the move so the distance is real.
                var fromTile = ResolvePlayerOverlandTile();
                int tiles = EmberCrpg.Domain.Overland.OverlandMap.ChebyshevDistance(fromTile, settlement.TilePosition);
                int days = System.Math.Min(14, System.Math.Max(1, tiles));

                // The REAL move: the domain player actor relocates to the destination settlement's site, so
                // schedules/quests/save all see the new position. Everything else (overland tile, billboard
                // origin, HUD town name) re-derives from it.
                if (_world.Actors != null && _world.Actors.TryFirstByRole(ActorRole.Player, out var player) && player != null)
                    player.MoveTo(CenterOfSite(SettlementSiteId(settlement.Id)));

                _currentSettlement = settlement.Id;
                _billboardOriginResolved = false; // NPC grid→world re-bases on the new settlement centre

                // F2/quest variety: arriving at a Shrine completes the VISIT pilgrimage (one-shot, paid in gold).
                if (settlement.Kind == EmberCrpg.Domain.Overland.SettlementKind.Shrine)
                    CompleteWorldQuest(ShrinePilgrimageQuestId, 40, "Pilgrimage complete");

                // F3/loading screen: the clock does NOT advance here any more. The UI ticks the journey
                // day-by-day behind the travel overlay via AdvanceTravelDay (one sim-day per frame), so a
                // cross-continent hop no longer freezes the scene cut — and the 14-day cap is GONE: the
                // world lives through the WHOLE journey.
                travelDays = System.Math.Max(1, tiles);
                message = "Travelling to " + settlement.Name + " — " + travelDays + (travelDays == 1 ? " day" : " days") + " on the road.";
                return true;
            }

            travelDays = 0;
            message = "Unknown settlement: " + settlementName;
            return false;
        }

        /// <summary>One sim-day of the journey — called per frame by the travel overlay coroutine.</summary>
        public void AdvanceTravelDay()
            => AdvanceTick(_tick + EmberCrpg.Simulation.Composition.WorldTickComposer.TicksPerGameDay);

        /// <summary>
        /// Legacy SYNC travel (proof drivers + non-coroutine callers): begin + tick the days inline, capped
        /// at 14 so a headless caller can't stall for minutes. The game UI uses the chunked path above.
        /// </summary>
        public bool TryTravelToSettlement(string settlementName, out string message)
        {
            if (!TryBeginTravelToSettlement(settlementName, out int days, out message))
                return false;
            int capped = System.Math.Min(14, days);
            for (int d = 0; d < capped; d++)
                AdvanceTravelDay();
            return true;
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

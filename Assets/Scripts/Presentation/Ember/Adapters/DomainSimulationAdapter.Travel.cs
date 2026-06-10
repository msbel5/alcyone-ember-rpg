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

                // The world LIVES through the journey: advance the real clock (AdvanceTick takes an ABSOLUTE
                // tick index) so schedules/needs/prices tick along. PARTIAL (honest): capped at 14 days so a
                // cross-continent hop cannot freeze the scene cut for minutes — the cap trades sim-honesty
                // for UX until ticking is chunked behind a loading screen.
                AdvanceTick(_tick + (days * EmberCrpg.Simulation.Composition.WorldTickComposer.TicksPerGameDay));

                message = "Travelled to " + settlement.Name + " — " + days + (days == 1 ? " day" : " days") + " on the road.";
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

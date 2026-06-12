using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.Quest;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter : IQuestGuidanceSource, IQuestGuidanceTracker
    {
        private GridPosition? _questGuidancePlayerPosition;

        public void UpdateQuestGuidancePlayerLocalPosition(GridPosition localPosition)
        {
            var origin = BillboardOrigin();
            _questGuidancePlayerPosition = new GridPosition(
                origin.X + localPosition.X,
                origin.Y + localPosition.Y);
        }

        public QuestGuidanceRow ReadQuestGuidance()
        {
            if (_world?.Actors == null || _world.Quests == null)
                return QuestGuidanceRow.None;

            var state = _world.Quests.TryGet(QuestCatalog.ForgeIronIngotId, out var forgeState)
                ? forgeState
                : null;
            if (state != null && state.IsComplete)
                return NearestDungeonRow(); // F5: the errand is done — guide the explorer to the dark
            if (!TryFindForgeQuestGiver(out var actor, out var npc))
                return NearestDungeonRow();

            // Cross-settlement quests speak OVERLAND (tiles between home settlements). Domain site placement
            // is compact — every town's local grid overlaps — so raw grid distance toward a giver who lives
            // 11 tiles away read "80m east" and pointed at empty ground. Local metres only when the giver
            // actually lives where the player stands.
            int distance;
            string direction;
            string unit;
            var here = CurrentSettlementOrStart;
            if (npc != null && !npc.Home.Equals(here)
                && TryGetSettlementTile(npc.Home, out int gtx, out int gty)
                && TryGetSettlementTile(here, out int htx, out int hty))
            {
                distance = System.Math.Max(System.Math.Abs(gtx - htx), System.Math.Abs(gty - hty));
                direction = OverlandDirection(gtx - htx, gty - hty);
                unit = "tiles";
            }
            else
            {
                distance = DistanceFromPlayer(actor);
                direction = DirectionFromPlayer(actor);
                unit = "m";
            }

            var title = state == null ? "Quest Lead" : "Quest Marker";
            var line = BuildForgeGuidanceLine(state, actor.Name, distance, direction, unit);
            return new QuestGuidanceRow(true, title, line, actor.Name, distance, direction, unit);
        }

        // F9: the delve row is its own HUD line now — quest state no longer gates dungeon discovery.
        public QuestGuidanceRow ReadDelveGuidance() => NearestDungeonRow();

        // F5/dungeon discovery ("dungeon bulamadım"): when the forge errand is complete or has no giver,
        // the compass row guides the EXPLORER — nearest Dungeon-kind settlement by overland tiles.
        private QuestGuidanceRow NearestDungeonRow()
        {
            var map = _world?.Overland;
            if (map == null) return QuestGuidanceRow.None;
            if (!TryGetSettlementTile(CurrentSettlementOrStart, out int htx, out int hty))
                return QuestGuidanceRow.None;

            string bestName = null;
            int bestDist = int.MaxValue, bdx = 0, bdy = 0;
            for (int i = 0; i < map.Settlements.Count; i++)
            {
                var s = map.Settlements[i];
                if (s.Kind != EmberCrpg.Domain.Overland.SettlementKind.Dungeon) continue;
                int dx = s.TilePosition.X - htx, dy = s.TilePosition.Y - hty;
                int d = System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
                if (d < bestDist) { bestDist = d; bestName = s.Name; bdx = dx; bdy = dy; }
            }
            if (bestName == null) return QuestGuidanceRow.None;

            string direction = OverlandDirection(bdx, bdy);
            string line = bestDist == 0
                ? "You stand at the delve of " + bestName + " — its dark mouth waits by the plaza."
                : "Nearest delve: " + bestName + " — " + bestDist + " tiles " + direction + " (fast travel via the map).";
            return new QuestGuidanceRow(true, "Delve Lead", line, bestName, bestDist, direction, "tiles");
        }

        private bool TryFindForgeQuestGiver(out ActorRecord actor, out NpcSeedRecord npc)
        {
            foreach (var candidate in _world.Actors.Records)
            {
                if (candidate.Role == ActorRole.Player)
                    continue;

                var candidateNpc = ResolveNpcForActor(candidate);
                if (!QuestInteractionService.IsForgeQuestGiver(_world, candidate.Id, candidateNpc))
                    continue;

                actor = candidate;
                npc = candidateNpc;
                return true;
            }

            actor = null;
            npc = null;
            return false;
        }

        private NpcSeedRecord ResolveNpcForActor(ActorRecord actor)
        {
            if (actor == null || _world?.NpcSeeds == null)
                return null;

            if (actor.Id.Value >= GeneratedNpcActorOffset)
            {
                var npcId = new NpcId(actor.Id.Value - GeneratedNpcActorOffset);
                for (int i = 0; i < _world.NpcSeeds.Count; i++)
                    if (_world.NpcSeeds[i].Id.Equals(npcId))
                        return _world.NpcSeeds[i];
            }

            for (int i = 0; i < _world.NpcSeeds.Count; i++)
                if (string.Equals(_world.NpcSeeds[i].Name, actor.Name, System.StringComparison.Ordinal))
                    return _world.NpcSeeds[i];
            return null;
        }

        private bool TryGetSettlementTile<TId>(TId id, out int tileX, out int tileY)
        {
            tileX = 0;
            tileY = 0;
            var map = _world?.Overland;
            if (map == null) return false;
            for (int i = 0; i < map.Settlements.Count; i++)
            {
                if (!map.Settlements[i].Id.Equals(id)) continue;
                var p = map.Settlements[i].TilePosition;
                tileX = p.X;
                tileY = p.Y;
                return true;
            }
            return false;
        }

        // Same compass convention as the overland map: tileY shrinks northward (atlas row 0 = north).
        private static string OverlandDirection(int dx, int dy)
        {
            if (dx == 0 && dy == 0) return "here";
            string vertical = dy < 0 ? "north" : (dy > 0 ? "south" : string.Empty);
            string horizontal = dx < 0 ? "west" : (dx > 0 ? "east" : string.Empty);
            if (vertical.Length == 0) return horizontal;
            if (horizontal.Length == 0) return vertical;
            return vertical + "-" + horizontal;
        }

        private int DistanceFromPlayer(ActorRecord target)
        {
            return TryGetGuidancePlayerPosition(out var playerPosition)
                ? Chebyshev(playerPosition, target.Position)
                : 0;
        }

        private string DirectionFromPlayer(ActorRecord target)
        {
            if (!TryGetGuidancePlayerPosition(out var playerPosition))
                return "nearby";

            int dx = target.Position.X - playerPosition.X;
            int dy = target.Position.Y - playerPosition.Y;
            if (dx == 0 && dy == 0) return "nearby";

            // ACTOR-grid convention: ProjectActor maps grid +Y onto Unity +Z, and +Z is north (the
            // test-locked WorldSpaceProjection chain), so a LARGER target Y is NORTH of the player.
            // The previous dy<0=north line copied the OVERLAND row rule (tileY shrinks northward) into the
            // wrong space — the user proved it: walking compass-N grew a "5m north" quest distance.
            string vertical = dy > 0 ? "north" : (dy < 0 ? "south" : string.Empty);
            string horizontal = dx < 0 ? "west" : (dx > 0 ? "east" : string.Empty);
            if (vertical.Length == 0) return horizontal;
            if (horizontal.Length == 0) return vertical;
            return vertical + "-" + horizontal;
        }

        private bool TryGetPlayer(out ActorRecord player)
        {
            return _world.Actors.TryFirstByRole(ActorRole.Player, out player);
        }

        private bool TryGetGuidancePlayerPosition(out GridPosition position)
        {
            if (_questGuidancePlayerPosition.HasValue)
            {
                position = _questGuidancePlayerPosition.Value;
                return true;
            }

            if (TryGetPlayer(out var player))
            {
                position = player.Position;
                return true;
            }

            position = default;
            return false;
        }

        // F14: combat distance math reads the LIVE first-person body (the tracker-fed guidance position),
        // not the parked deterministic player actor — in a delve the actor sits at the plaza centre while
        // the rig walks the chamber; chase/reach/strike must follow the body. Headless proofs and tests
        // never push the tracker, so this falls back to the actor position there — both contexts honest.
        private GridPosition PlayerCombatPosition(ActorRecord player)
        {
            return TryGetGuidancePlayerPosition(out var live) ? live : player.Position;
        }

        private static int Chebyshev(GridPosition a, GridPosition b)
        {
            int dx = System.Math.Abs(a.X - b.X);
            int dy = System.Math.Abs(a.Y - b.Y);
            return dx > dy ? dx : dy;
        }

        private static string BuildForgeGuidanceLine(QuestState state, string actorName, int distance, string direction, string unit)
        {
            var range = distance <= 0 ? "nearby" : distance + unit + " " + direction;
            if (state == null)
                return actorName + " has forge work (" + range + "). Press E nearby, then ask about forge work.";
            if (state.IsTaskTriggered(0))
                return "Return the iron ingot to " + actorName + " (" + range + ").";
            return "Craft one iron ingot, then return to " + actorName + " (" + range + ").";
        }
    }
}

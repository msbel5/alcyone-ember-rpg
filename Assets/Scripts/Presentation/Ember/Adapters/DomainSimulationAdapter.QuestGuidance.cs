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
                return QuestGuidanceRow.None;
            if (!TryFindForgeQuestGiver(out var actor, out var npc))
                return QuestGuidanceRow.None;

            var distance = DistanceFromPlayer(actor);
            var direction = DirectionFromPlayer(actor);
            var title = state == null ? "Quest Lead" : "Quest Marker";
            var line = BuildForgeGuidanceLine(state, actor.Name, distance, direction);
            return new QuestGuidanceRow(true, title, line, actor.Name, distance, direction);
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

            string vertical = dy < 0 ? "north" : (dy > 0 ? "south" : string.Empty);
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

        private static int Chebyshev(GridPosition a, GridPosition b)
        {
            int dx = System.Math.Abs(a.X - b.X);
            int dy = System.Math.Abs(a.Y - b.Y);
            return dx > dy ? dx : dy;
        }

        private static string BuildForgeGuidanceLine(QuestState state, string actorName, int distance, string direction)
        {
            var range = distance <= 0 ? "nearby" : distance + " tiles " + direction;
            if (state == null)
                return actorName + " has forge work (" + range + "). Press E nearby, then ask about forge work.";
            if (state.IsTaskTriggered(0))
                return "Return the iron ingot to " + actorName + " (" + range + ").";
            return "Craft one iron ingot, then return to " + actorName + " (" + range + ").";
        }
    }
}

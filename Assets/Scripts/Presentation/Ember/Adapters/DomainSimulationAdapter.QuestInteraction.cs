using System.Collections.Generic;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Quest;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        private readonly QuestInteractionService _questInteractions = new QuestInteractionService();

        private IReadOnlyList<AskAboutTopic> AddQuestInteractionTopics(
            NpcSeedRecord npc,
            IReadOnlyList<AskAboutTopic> baseTopics)
        {
            var questTopics = _questInteractions.BuildTopics(_world, npc);
            if (questTopics.Count == 0)
                return baseTopics;

            var merged = new List<AskAboutTopic>(questTopics.Count + (baseTopics?.Count ?? 0));
            merged.AddRange(questTopics);
            if (baseTopics != null)
                merged.AddRange(baseTopics);
            return merged;
        }

        private bool TryHandleQuestInteractionTopic(string topicId)
        {
            if (string.IsNullOrEmpty(topicId) || _conversation == null)
                return false;

            var npc = ResolveActiveQuestNpc();
            if (!_questInteractions.TrySelectTopic(_world, _activeDialogActorId, npc, topicId, out var line))
                return false;

            _currentDialogLine = line;
            _isDialogThinking = false;
            return true;
        }

        private NpcSeedRecord ResolveActiveQuestNpc()
        {
            if (_conversation != null && !_conversation.NpcId.IsEmpty)
            {
                foreach (var npc in _world.NpcSeeds)
                {
                    if (npc.Id.Equals(_conversation.NpcId))
                        return npc;
                }
            }

            foreach (var npc in _world.NpcSeeds)
            {
                if (string.Equals(npc.Name, _activeDialogActor, System.StringComparison.Ordinal))
                    return npc;
            }

            return null;
        }
    }
}

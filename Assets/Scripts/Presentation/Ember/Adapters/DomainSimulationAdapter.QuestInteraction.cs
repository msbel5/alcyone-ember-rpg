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
            EmberCrpg.Domain.Core.ActorId actorId,
            NpcSeedRecord npc,
            IReadOnlyList<AskAboutTopic> baseTopics)
        {
            var questTopics = _questInteractions.BuildTopics(_world, actorId, npc);
            // W31 ('ikinci questi alamadim'): the F21 contract machine existed but ONLY the proof
            // driver ever called it - work-giving NPCs now offer it as a real topic.
            bool givesContracts = npc != null && WorldQuestGenerator.GivesWork(npc.Role);
            if (questTopics.Count == 0 && !givesContracts)
                return baseTopics;

            var merged = new List<AskAboutTopic>(questTopics.Count + 1 + (baseTopics?.Count ?? 0));
            merged.AddRange(questTopics);
            if (givesContracts)
                merged.Add(new AskAboutTopic(ContractWorkTopicId, "work for pay",
                    (npc?.Name ?? "They") + " may have a contract for reliable hands."));
            if (baseTopics != null)
                merged.AddRange(baseTopics);
            return merged;
        }

        private bool TryHandleQuestInteractionTopic(string topicId)
        {
            if (string.IsNullOrEmpty(topicId) || _conversation == null)
                return false;

            var npc = ResolveActiveQuestNpc();
            if (string.Equals(topicId, ContractWorkTopicId, System.StringComparison.Ordinal))
            {
                _currentDialogLine = HandleContractWork(npc);
                _isDialogThinking = false;
                return true;
            }
            if (!_questInteractions.TrySelectTopic(_world, _activeDialogActorId, npc, topicId, out var line))
                return false;

            _currentDialogLine = line;
            _isDialogThinking = false;
            return true;
        }

        private const string ContractWorkTopicId = "contract_work";

        // W31: turn in whatever can conclude HERE; otherwise report the exact blocking step
        // (the refusal lines name it); otherwise mint fresh work. Seed derives only from
        // deterministic world state - replays and saves stay bit-identical.
        private string HandleContractWork(NpcSeedRecord npc)
        {
            var contracts = ReadGeneratedQuests();
            if (contracts != null)
            {
                string blocked = null;
                foreach (var contract in contracts)
                {
                    if (contract == null || contract.Completed || contract.Failed) continue;
                    var outcome = TryTurnInGeneratedQuest(contract.Id);
                    if (string.IsNullOrEmpty(outcome)) continue;
                    if (outcome.Contains("Paid") || outcome.Contains("concluded")
                        || outcome.Contains("done") || outcome.Contains("complete"))
                        return outcome;
                    blocked = blocked ?? outcome; // honest path: "you are not there", "still draws breath"...
                }
                if (blocked != null) return blocked;
            }
            ulong seed = (ulong)_world.Time.TotalMinutes * 1000003UL
                + (npc?.Id.Value ?? 0UL) * 8191UL
                + (ulong)(contracts?.Count ?? 0);
            var minted = AcceptGeneratedQuest(seed);
            return minted != null
                ? $"\"There is work.\" {minted.Title} - {minted.RewardGold} gold, day {minted.DeadlineDay} latest. It is in your journal."
                : "\"Nothing today. Ask again when the roads change.\"";
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

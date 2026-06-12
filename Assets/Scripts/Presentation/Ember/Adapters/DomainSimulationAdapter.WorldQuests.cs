using EmberCrpg.Domain.Quest;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        // F2/quest variety: two WORLD quests join the forge errand, completing the fetch/kill/visit trio.
        // Ids live in a reserved high band well clear of QuestCatalog's authored ids.
        private static readonly QuestId OutlawBountyQuestId = new QuestId(9001UL);
        private static readonly QuestId ShrinePilgrimageQuestId = new QuestId(9002UL);

        // F22: the stores live on the WORLD ROOT now (WorldState.WorldQuestStates / WorldContracts)
        // so WorldSaveMapper carries them — the adapter-local dictionary died here. Keyed by raw
        // QuestId.Value because the kernel QuestStore stays catalog-only (the F2 lesson).
        private System.Collections.Generic.Dictionary<ulong, QuestState> WorldQuestStates
            => _world?.WorldQuestStates;

        /// <summary>Seeded at hydration: a KILL bounty (fell any outlaw) and a VISIT pilgrimage (reach a
        /// Shrine). Idempotent — a RESTORED world keeps its saved states.</summary>
        private void SeedWorldQuests()
        {
            if (_world == null || WorldQuestStates == null) return;
            if (!WorldQuestStates.ContainsKey(OutlawBountyQuestId.Value))
                WorldQuestStates[OutlawBountyQuestId.Value] = new QuestState(1, _world.Time);
            if (!WorldQuestStates.ContainsKey(ShrinePilgrimageQuestId.Value))
                WorldQuestStates[ShrinePilgrimageQuestId.Value] = new QuestState(1, _world.Time);
            UnityEngine.Debug.Log("[Quest] world quests seeded: outlaw bounty (kill) + shrine pilgrimage (visit).");
        }

        // F21 GENERATED QUESTS ("görev makinesi") — F22: backed by WorldState.WorldContracts so
        // saves carry every contract; the serial derives from what already exists (restore-safe).
        private System.Collections.Generic.List<EmberCrpg.Domain.Quest.WorldQuestRecord> _generatedQuests
            => _world?.WorldContracts;

        private ulong NextGeneratedQuestSerial()
        {
            ulong max = 9099UL;
            var contracts = _generatedQuests;
            if (contracts != null)
                for (int i = 0; i < contracts.Count; i++)
                    if (contracts[i] != null && contracts[i].Id.Value > max)
                        max = contracts[i].Id.Value;
            return max + 1UL;
        }

        /// <summary>F21: mint + accept one generated quest for the CURRENT settlement. Deterministic
        /// in the seed; the optional template force keeps proofs reproducible.</summary>
        public EmberCrpg.Domain.Quest.WorldQuestRecord AcceptGeneratedQuest(
            ulong seed, EmberCrpg.Domain.Quest.WorldQuestTemplate? force = null)
        {
            if (_world?.NpcSeeds == null || _world.Overland == null) return null;
            var quest = EmberCrpg.Simulation.Quest.WorldQuestGenerator.Generate(
                _world.NpcSeeds, _world.Overland.Settlements, CurrentSettlementOrStart,
                CurrentWorldDay(), seed, force);
            if (quest == null || _generatedQuests == null) return null;
            quest.Id = new QuestId(NextGeneratedQuestSerial());
            _generatedQuests.Add(quest);
            _lastCombatLine = $"Accepted: {quest.Title} ({quest.RewardGold}g, day {quest.DeadlineDay} latest).";
            UnityEngine.Debug.Log($"[QuestGen] accepted #{quest.Id.Value}: {quest.Title} from {quest.GiverName} " +
                                  $"(reward {quest.RewardGold}g, deadline day {quest.DeadlineDay}).");
            return quest;
        }

        /// <summary>F21: live rows for the journal screen (deadline lazily fails overdue quests).</summary>
        public System.Collections.Generic.IReadOnlyList<EmberCrpg.Domain.Quest.WorldQuestRecord> ReadGeneratedQuests()
        {
            int today = CurrentWorldDay();
            for (int i = 0; i < _generatedQuests.Count; i++)
            {
                var q = _generatedQuests[i];
                if (q != null && !q.Completed && !q.Failed && today > q.DeadlineDay)
                {
                    q.Failed = true;
                    UnityEngine.Debug.Log($"[QuestGen] failed #{q.Id.Value}: {q.Title} — the deadline (day {q.DeadlineDay}) passed.");
                }
            }
            return _generatedQuests;
        }

        /// <summary>F21: turn in a generated quest — each template has its own honest check.</summary>
        public string TryTurnInGeneratedQuest(QuestId id)
        {
            EmberCrpg.Domain.Quest.WorldQuestRecord quest = null;
            for (int i = 0; i < _generatedQuests.Count; i++)
                if (_generatedQuests[i] != null && _generatedQuests[i].Id.Equals(id)) { quest = _generatedQuests[i]; break; }
            if (quest == null) return "No such contract.";
            if (quest.Completed) return "That work is already done.";
            if (CurrentWorldDay() > quest.DeadlineDay) { quest.Failed = true; return $"Too late — day {quest.DeadlineDay} has passed."; }

            switch (quest.Template)
            {
                case EmberCrpg.Domain.Quest.WorldQuestTemplate.Fetch:
                case EmberCrpg.Domain.Quest.WorldQuestTemplate.Deliver:
                    if (!CurrentSettlementOrStart.Equals(quest.TargetSettlementId))
                        return $"The contract concludes at {quest.TargetSettlementName} — you are not there.";
                    if (_world.PlayerInventory == null
                        || !_world.PlayerInventory.TryRemove(quest.ItemTemplateId, 1, _world.PlayerEquipment))
                        return $"You do not carry the {quest.ItemTemplateId}.";
                    break;
                case EmberCrpg.Domain.Quest.WorldQuestTemplate.Kill:
                {
                    var actorId = new EmberCrpg.Domain.Core.ActorId(GeneratedNpcActorOffset + quest.TargetNpcId.Value);
                    if (_world.Actors.TryGet(actorId, out var mark) && mark != null && mark.IsAlive)
                        return $"{quest.TargetNpcName} still draws breath.";
                    break;
                }
                case EmberCrpg.Domain.Quest.WorldQuestTemplate.Visit:
                    if (!CurrentSettlementOrStart.Equals(quest.TargetSettlementId))
                        return $"You have not reached {quest.TargetSettlementName} yet.";
                    break;
            }

            quest.Completed = true;
            _world.PlayerGold += quest.RewardGold;
            GrantXp(60, "quest");
            _world.PlayerReputation += 1; // F23: finished work builds a name
            UnityEngine.Debug.Log($"[Rep] +1 (contract) → {_world.PlayerReputation}.");
            _lastCombatLine = $"Contract complete: {quest.Title} — +{quest.RewardGold} gold.";
            UnityEngine.Debug.Log($"[QuestGen] completed #{quest.Id.Value}: {quest.Title} — +{quest.RewardGold} gold (purse {_world.PlayerGold}).");
            return _lastCombatLine;
        }

        /// <summary>F21-DoD proof: mint a FETCH contract, buy its cargo through the LIVE economy,
        /// turn it in — the full loop in three log lines.</summary>
        public string ProofRunGeneratedQuestLeg()
        {
            var quest = AcceptGeneratedQuest(7100UL, EmberCrpg.Domain.Quest.WorldQuestTemplate.Fetch);
            if (quest == null) return "LOOP-PROOF: no generated quest (no eligible giver here).";
            var buy = ExecuteTrade(new EmberCrpg.Presentation.Ember.UI.TradeActionRequest(
                EmberCrpg.Presentation.Ember.UI.TradeActionKind.Buy, quest.ItemTemplateId));
            if (!buy.Success)
                return $"LOOP-PROOF: cargo '{quest.ItemTemplateId}' not buyable here — fetch leg honest-failed.";
            var result = TryTurnInGeneratedQuest(quest.Id);
            return $"LOOP-PROOF generated-quest: '{quest.Title}' cargo bought, turn-in => {result}";
        }

        private int CurrentWorldDay()
        {
            return _world != null ? (int)(_world.Time.TotalMinutes / (24L * 60L)) + 1 : 0;
        }

        /// <summary>One-shot completion: marks the task, closes the quest, pays the reward into the purse.</summary>
        private void CompleteWorldQuest(QuestId id, int goldReward, string label)
        {
            if (_world == null || WorldQuestStates == null
                || !WorldQuestStates.TryGetValue(id.Value, out var state) || state == null || state.IsComplete) return;
            state.MarkTaskTriggered(0);
            state.SetCompleted(success: true);
            _world.PlayerGold += goldReward;
            GrantXp(60, "quest"); // F17: finished work teaches more than the kill itself
            _world.PlayerReputation += 1; // F23: finished work builds a name
            UnityEngine.Debug.Log($"[Rep] +1 (world quest) → {_world.PlayerReputation}.");
            _lastCombatLine = $"{label}: +{goldReward} gold.";
            UnityEngine.Debug.Log($"[Quest] {label} — quest complete, +{goldReward} gold (purse {_world.PlayerGold}).");
        }
    }
}

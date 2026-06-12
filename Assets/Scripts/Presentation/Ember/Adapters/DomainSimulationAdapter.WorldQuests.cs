using EmberCrpg.Domain.Quest;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        // F2/quest variety: two WORLD quests join the forge errand, completing the fetch/kill/visit trio.
        // Ids live in a reserved high band well clear of QuestCatalog's authored ids.
        private static readonly QuestId OutlawBountyQuestId = new QuestId(9001UL);
        private static readonly QuestId ShrinePilgrimageQuestId = new QuestId(9002UL);

        // ADAPTER-LOCAL store (probe-run finding): parking these in WorldState.Quests made QuestSystem.Tick
        // and the Journal call QuestCatalog.Resolve on ids the catalog doesn't know — KeyNotFoundException
        // EVERY TICK (it killed the screen tour). World quests are presentation-flow state; the kernel's
        // store stays catalog-only. PARTIAL (honest): not yet persisted in saves — F4 save work picks it up.
        private readonly System.Collections.Generic.Dictionary<QuestId, QuestState> _worldQuests
            = new System.Collections.Generic.Dictionary<QuestId, QuestState>();

        /// <summary>Seeded at hydration: a KILL bounty (fell any outlaw) and a VISIT pilgrimage (reach a Shrine).</summary>
        private void SeedWorldQuests()
        {
            if (_world == null) return;
            if (!_worldQuests.ContainsKey(OutlawBountyQuestId))
                _worldQuests[OutlawBountyQuestId] = new QuestState(1, _world.Time);
            if (!_worldQuests.ContainsKey(ShrinePilgrimageQuestId))
                _worldQuests[ShrinePilgrimageQuestId] = new QuestState(1, _world.Time);
            UnityEngine.Debug.Log("[Quest] world quests seeded: outlaw bounty (kill) + shrine pilgrimage (visit).");
        }

        // F21 GENERATED QUESTS ("görev makinesi"): minted on demand by the deterministic
        // WorldQuestGenerator, held adapter-local like the fixed pair above. PARTIAL (honest):
        // save persistence is F22's explicit job (the adapter-local dictionary dies there).
        private readonly System.Collections.Generic.List<EmberCrpg.Domain.Quest.WorldQuestRecord> _generatedQuests
            = new System.Collections.Generic.List<EmberCrpg.Domain.Quest.WorldQuestRecord>();
        private ulong _nextGeneratedQuestSerial = 9100UL;

        /// <summary>F21: mint + accept one generated quest for the CURRENT settlement. Deterministic
        /// in the seed; the optional template force keeps proofs reproducible.</summary>
        public EmberCrpg.Domain.Quest.WorldQuestRecord AcceptGeneratedQuest(
            ulong seed, EmberCrpg.Domain.Quest.WorldQuestTemplate? force = null)
        {
            if (_world?.NpcSeeds == null || _world.Overland == null) return null;
            var quest = EmberCrpg.Simulation.Quest.WorldQuestGenerator.Generate(
                _world.NpcSeeds, _world.Overland.Settlements, CurrentSettlementOrStart,
                CurrentWorldDay(), seed, force);
            if (quest == null) return null;
            quest.Id = new QuestId(_nextGeneratedQuestSerial++);
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
            if (_world == null || !_worldQuests.TryGetValue(id, out var state) || state == null || state.IsComplete) return;
            state.MarkTaskTriggered(0);
            state.SetCompleted(success: true);
            _world.PlayerGold += goldReward;
            GrantXp(60, "quest"); // F17: finished work teaches more than the kill itself
            _lastCombatLine = $"{label}: +{goldReward} gold.";
            UnityEngine.Debug.Log($"[Quest] {label} — quest complete, +{goldReward} gold (purse {_world.PlayerGold}).");
        }
    }
}

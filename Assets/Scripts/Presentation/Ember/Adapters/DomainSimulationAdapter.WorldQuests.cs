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

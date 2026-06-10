using EmberCrpg.Domain.Quest;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        // F2/quest variety: two WORLD quests join the forge errand, completing the fetch/kill/visit trio.
        // Ids live in a reserved high band well clear of QuestCatalog's authored ids.
        private static readonly QuestId OutlawBountyQuestId = new QuestId(9001UL);
        private static readonly QuestId ShrinePilgrimageQuestId = new QuestId(9002UL);

        /// <summary>Seeded at hydration: a KILL bounty (fell any outlaw) and a VISIT pilgrimage (reach a Shrine).</summary>
        private void SeedWorldQuests()
        {
            if (_world?.Quests == null) return;
            if (!_world.Quests.Contains(OutlawBountyQuestId))
                _world.Quests.Add(OutlawBountyQuestId, new QuestState(1, _world.Time));
            if (!_world.Quests.Contains(ShrinePilgrimageQuestId))
                _world.Quests.Add(ShrinePilgrimageQuestId, new QuestState(1, _world.Time));
            UnityEngine.Debug.Log("[Quest] world quests seeded: outlaw bounty (kill) + shrine pilgrimage (visit).");
        }

        /// <summary>One-shot completion: marks the task, closes the quest, pays the reward into the purse.</summary>
        private void CompleteWorldQuest(QuestId id, int goldReward, string label)
        {
            if (_world?.Quests == null || !_world.Quests.TryGet(id, out var state) || state.IsComplete) return;
            state.MarkTaskTriggered(0);
            state.SetCompleted(success: true);
            _world.PlayerGold += goldReward;
            _lastCombatLine = $"{label}: +{goldReward} gold.";
            UnityEngine.Debug.Log($"[Quest] {label} — quest complete, +{goldReward} gold (purse {_world.PlayerGold}).");
        }
    }
}

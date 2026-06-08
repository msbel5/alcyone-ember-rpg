using EmberCrpg.Simulation.Quest;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        private readonly QuestSystem _immediateQuestSystem = new QuestSystem();

        private void ReevaluateQuestProgress()
        {
            if (_world?.Quests == null || _world.Quests.Count == 0)
                return;

            _immediateQuestSystem.Tick(_world);
        }
    }
}

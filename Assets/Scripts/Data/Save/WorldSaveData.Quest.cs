using System;

namespace EmberCrpg.Data.Save
{
    public sealed partial class WorldSaveData
    {
        public QuestStateSaveData[] quests;
        // F22: world-quest persistence — the fixed bounty/pilgrimage pair reuses the quest-state
        // DTO; generated contracts (F21) get their own row shape below.
        public QuestStateSaveData[] worldQuestStates;
        public WorldContractSaveData[] worldContracts;
    }

    [Serializable]
    public sealed class WorldContractSaveData
    {
        public long id;
        public int template;
        public long giverNpcId;
        public string giverName;
        public long targetSettlementId;
        public string targetSettlementName;
        public long targetNpcId;
        public string targetNpcName;
        public string itemTemplateId;
        public int rewardGold;
        public int deadlineDay;
        public bool completed;
        public bool failed;
        public string title;
    }

    [Serializable]
    public sealed class QuestStateSaveData
    {
        public long questId;
        public long startTickMinutes;
        public bool isComplete;
        public bool isSuccess;
        public bool[] triggeredTasks;
    }
}

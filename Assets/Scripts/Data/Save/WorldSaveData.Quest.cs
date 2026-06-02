using System;

namespace EmberCrpg.Data.Save
{
    public sealed partial class WorldSaveData
    {
        public QuestStateSaveData[] quests;
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

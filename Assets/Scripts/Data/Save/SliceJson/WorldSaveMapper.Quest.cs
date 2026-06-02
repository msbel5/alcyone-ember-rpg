using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Data.Save
{
    public static partial class WorldSaveMapper
    {
        private static QuestStateSaveData[] ToQuestStoreData(QuestStore store)
        {
            if (store == null)
                return Array.Empty<QuestStateSaveData>();

            var rows = new List<QuestStateSaveData>();
            foreach (var entry in store.Active)
            {
                if (entry.Value == null)
                    continue;

                rows.Add(new QuestStateSaveData
                {
                    questId = (long)entry.Key.Value,
                    startTickMinutes = entry.Value.StartTick.TotalMinutes,
                    isComplete = entry.Value.IsComplete,
                    isSuccess = entry.Value.IsSuccess,
                    triggeredTasks = (bool[])entry.Value.TriggeredTasks.Clone(),
                });
            }

            return rows.ToArray();
        }

        private static QuestStore ToQuestStore(QuestStateSaveData[] data)
        {
            var store = new QuestStore();
            foreach (var row in data ?? Array.Empty<QuestStateSaveData>())
            {
                if (row == null || row.questId <= 0)
                    continue;

                var state = new QuestState(row.triggeredTasks?.Length ?? 0, new GameTime(row.startTickMinutes));
                foreach (var index in TriggeredIndexes(row.triggeredTasks))
                    state.MarkTaskTriggered(index);
                if (row.isComplete)
                    state.SetCompleted(row.isSuccess);

                store.Add(new QuestId((ulong)row.questId), state);
            }

            return store;
        }

        private static IEnumerable<int> TriggeredIndexes(bool[] flags)
        {
            for (var i = 0; i < (flags?.Length ?? 0); i++)
            {
                if (flags[i])
                    yield return i;
            }
        }
    }
}

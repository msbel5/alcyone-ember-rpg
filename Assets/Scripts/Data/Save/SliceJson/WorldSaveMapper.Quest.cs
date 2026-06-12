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

        // F22: the fixed world-quest pair's states — same DTO as the kernel store, keyed raw.
        private static QuestStateSaveData[] ToWorldQuestStatesData(Dictionary<ulong, QuestState> states)
        {
            if (states == null || states.Count == 0)
                return Array.Empty<QuestStateSaveData>();
            var rows = new List<QuestStateSaveData>();
            foreach (var entry in states)
            {
                if (entry.Value == null) continue;
                rows.Add(new QuestStateSaveData
                {
                    questId = (long)entry.Key,
                    startTickMinutes = entry.Value.StartTick.TotalMinutes,
                    isComplete = entry.Value.IsComplete,
                    isSuccess = entry.Value.IsSuccess,
                    triggeredTasks = (bool[])entry.Value.TriggeredTasks.Clone(),
                });
            }
            return rows.ToArray();
        }

        private static Dictionary<ulong, QuestState> ToWorldQuestStates(QuestStateSaveData[] data)
        {
            var states = new Dictionary<ulong, QuestState>();
            foreach (var row in data ?? Array.Empty<QuestStateSaveData>())
            {
                if (row == null || row.questId <= 0) continue;
                var state = new QuestState(row.triggeredTasks?.Length ?? 0, new GameTime(row.startTickMinutes));
                foreach (var index in TriggeredIndexes(row.triggeredTasks))
                    state.MarkTaskTriggered(index);
                if (row.isComplete)
                    state.SetCompleted(row.isSuccess);
                states[(ulong)row.questId] = state;
            }
            return states;
        }

        // F22: generated contracts (F21) — straight field mapping, both directions.
        private static WorldContractSaveData[] ToWorldContractsData(List<WorldQuestRecord> contracts)
        {
            if (contracts == null || contracts.Count == 0)
                return Array.Empty<WorldContractSaveData>();
            var rows = new List<WorldContractSaveData>();
            foreach (var q in contracts)
            {
                if (q == null) continue;
                rows.Add(new WorldContractSaveData
                {
                    id = (long)q.Id.Value,
                    template = (int)q.Template,
                    giverNpcId = (long)q.GiverNpcId.Value,
                    giverName = q.GiverName,
                    targetSettlementId = (long)q.TargetSettlementId.Value,
                    targetSettlementName = q.TargetSettlementName,
                    targetNpcId = (long)q.TargetNpcId.Value,
                    targetNpcName = q.TargetNpcName,
                    itemTemplateId = q.ItemTemplateId,
                    rewardGold = q.RewardGold,
                    deadlineDay = q.DeadlineDay,
                    completed = q.Completed,
                    failed = q.Failed,
                    title = q.Title,
                });
            }
            return rows.ToArray();
        }

        private static List<WorldQuestRecord> ToWorldContracts(WorldContractSaveData[] data)
        {
            var contracts = new List<WorldQuestRecord>();
            foreach (var row in data ?? Array.Empty<WorldContractSaveData>())
            {
                if (row == null || row.id <= 0) continue;
                contracts.Add(new WorldQuestRecord
                {
                    Id = new QuestId((ulong)row.id),
                    Template = (WorldQuestTemplate)row.template,
                    GiverNpcId = new EmberCrpg.Domain.Worldgen.NpcId((ulong)row.giverNpcId),
                    GiverName = row.giverName ?? string.Empty,
                    TargetSettlementId = new EmberCrpg.Domain.Worldgen.SettlementId((ulong)row.targetSettlementId),
                    TargetSettlementName = row.targetSettlementName ?? string.Empty,
                    TargetNpcId = new EmberCrpg.Domain.Worldgen.NpcId((ulong)row.targetNpcId),
                    TargetNpcName = row.targetNpcName ?? string.Empty,
                    ItemTemplateId = row.itemTemplateId ?? string.Empty,
                    RewardGold = row.rewardGold,
                    DeadlineDay = row.deadlineDay,
                    Completed = row.completed,
                    Failed = row.failed,
                    Title = row.title ?? string.Empty,
                });
            }
            return contracts;
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

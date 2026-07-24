// WorldSaveMapper partial — W32 EAT action phase-trace ring (docs/ruh/w32/04-action-log.md §4):
// parallel arrays oldest-to-newest plus the monotone TotalPushed counter. The reflection golden
// roundtrip proves every column; a dropped column fails there forever.
using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;

namespace EmberCrpg.Data.Save
{
    public static partial class WorldSaveMapper
    {
        private static void ToActionLogData(ActionLogRing ring, WorldSaveData data)
        {
            var count = ring?.Count ?? 0;
            data.actionLogTickMinutes = new long[count];
            data.actionLogActorIds = new ulong[count];
            data.actionLogIntents = new int[count];
            data.actionLogFromActions = new int[count];
            data.actionLogFromPhases = new int[count];
            data.actionLogToActions = new int[count];
            data.actionLogToPhases = new int[count];
            data.actionLogTargetIds = new ulong[count];
            data.actionLogReasons = new int[count];
            data.actionLogTotalPushed = ring?.TotalPushed ?? 0L;
            for (var i = 0; i < count; i++)
            {
                var entry = ring.At(i);
                data.actionLogTickMinutes[i] = entry.TickMinutes;
                data.actionLogActorIds[i] = entry.ActorId;
                data.actionLogIntents[i] = (int)entry.Intent;
                data.actionLogFromActions[i] = (int)entry.FromAction;
                data.actionLogFromPhases[i] = (int)entry.FromPhase;
                data.actionLogToActions[i] = (int)entry.ToAction;
                data.actionLogToPhases[i] = (int)entry.ToPhase;
                data.actionLogTargetIds[i] = entry.TargetId;
                data.actionLogReasons[i] = (int)entry.Reason;
            }
        }

        private static ActionLogRing ToActionLogRing(WorldSaveData data)
        {
            var ring = new ActionLogRing();
            var ticks = data.actionLogTickMinutes;
            if (ticks == null || data.actionLogActorIds == null || data.actionLogIntents == null
                || data.actionLogFromActions == null || data.actionLogFromPhases == null
                || data.actionLogToActions == null || data.actionLogToPhases == null
                || data.actionLogTargetIds == null || data.actionLogReasons == null)
                return ring; // pre-W32 save: empty ring

            var count = ticks.Length;
            count = Math.Min(count, data.actionLogActorIds.Length);
            count = Math.Min(count, data.actionLogIntents.Length);
            count = Math.Min(count, data.actionLogFromActions.Length);
            count = Math.Min(count, data.actionLogFromPhases.Length);
            count = Math.Min(count, data.actionLogToActions.Length);
            count = Math.Min(count, data.actionLogToPhases.Length);
            count = Math.Min(count, data.actionLogTargetIds.Length);
            count = Math.Min(count, data.actionLogReasons.Length);

            var entries = new List<ActionLogEntry>(count);
            for (var i = 0; i < count; i++)
                entries.Add(new ActionLogEntry(
                    ticks[i],
                    data.actionLogActorIds[i],
                    (ActorIntent)data.actionLogIntents[i],
                    (ActorActionType)data.actionLogFromActions[i],
                    (ActionPhase)data.actionLogFromPhases[i],
                    (ActorActionType)data.actionLogToActions[i],
                    (ActionPhase)data.actionLogToPhases[i],
                    data.actionLogTargetIds[i],
                    (ActionLogReason)data.actionLogReasons[i]));
            ring.Restore(entries, data.actionLogTotalPushed);
            return ring;
        }
    }
}

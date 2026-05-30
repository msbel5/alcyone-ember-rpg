// SliceSaveMapper partial — narrative/aidm: world events, tool-call trace, world profile, llm proposals, npc seeds (split from the 961-line monolith, NAME/LOC-split).
using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Data.Save
{
    public static partial class SliceSaveMapper
    {
        private static WorldEventSaveData[] ToWorldEventLogData(WorldEventLog log)
        {
            return (log?.Events ?? Array.Empty<WorldEvent>()).Select(ToWorldEventData).ToArray();
        }

        private static WorldEventSaveData ToWorldEventData(WorldEvent worldEvent)
        {
            return new WorldEventSaveData
            {
                tickMinutes = worldEvent.Tick.TotalMinutes,
                kind = (int)worldEvent.Kind,
                actorId = (long)worldEvent.ActorId.Value,
                siteId = (long)worldEvent.SiteId.Value,
                reason = worldEvent.Reason,
                reasonTrace = worldEvent.ReasonTrace?.Causes.ToArray(),
            };
        }

        private static WorldEventLog ToWorldEventLog(WorldEventSaveData[] data)
        {
            var log = new WorldEventLog();
            foreach (var worldEvent in data ?? Array.Empty<WorldEventSaveData>())
            {
                if (worldEvent != null)
                {
                    log.Append(new WorldEvent(
                        new GameTime(worldEvent.tickMinutes),
                        (WorldEventKind)worldEvent.kind,
                        new ActorId((ulong)worldEvent.actorId),
                        new SiteId((ulong)worldEvent.siteId),
                        worldEvent.reason,
                        ToReasonTrace(worldEvent.reasonTrace)));
                }
            }
            return log;
        }

        private static ReasonTrace ToReasonTrace(string[] causes)
        {
            return causes == null || causes.Length == 0 ? null : new ReasonTrace(causes);
        }

        private static ToolCallTraceSaveData[] ToToolCallTraceData(IEnumerable<ToolCallTraceRecord> entries)
        {
            return (entries ?? Array.Empty<ToolCallTraceRecord>())
                .Where(entry => entry != null)
                .Select(entry => ToToolCallTraceData(entry.Tick, entry.SiteId, entry.Request, entry.Result))
                .ToArray();
        }

        private static WorldProfileSaveData ToWorldProfileData(WorldProfile profile)
        {
            if (profile == null) return null;
            return new WorldProfileSaveData
            {
                style = (int)profile.Style,
                genre = (int)profile.Genre,
                seed = profile.Seed,
                targetPopulation = profile.TargetPopulation,
                regionCount = profile.RegionCount,
                factionCount = profile.FactionCount,
                historyYears = profile.HistoryYears,
                moodKeyword = profile.MoodKeyword,
                playerCallingKeyword = profile.PlayerCallingKeyword,
                startLocationKeyword = profile.StartLocationKeyword,
            };
        }

        private static WorldProfile ToWorldProfile(WorldProfileSaveData data)
        {
            if (data == null || data.targetPopulation <= 0) return null;
            return new WorldProfile(
(WorldStyle)data.style,
                (WorldGenre)data.genre,
                (uint)data.seed,
                data.targetPopulation,
data.regionCount,
                data.factionCount,
                data.historyYears,
                data.moodKeyword,
                data.playerCallingKeyword,
                data.startLocationKeyword);
        }

        private static ToolCallTraceSaveData ToToolCallTraceData(GameTime tick, SiteId siteId, ToolCallRequest request, ToolCallResult result)
        {
            return new ToolCallTraceSaveData
            {
                tickMinutes = (long)tick.TotalMinutes,
                siteId = (long)siteId.Value,
                surfaceCode = request?.Surface.Code,
                toolCode = request?.ToolId.Code,
                parameters = ToToolCallParameterData(request?.Parameters),
                accepted = result?.Accepted ?? false,
                payload = result?.Payload,
                rejectionReason = result?.RejectionReason,
            };
        }

        private static ToolCallParameterSaveData[] ToToolCallParameterData(IReadOnlyDictionary<string, string> parameters)
        {
            // Codex audit (second pass A-P3): the dictionary's enumeration order
            // is implementation-defined, so the on-disk parameter list could
            // shift between Save() calls for the same in-memory state. Sort
            // by parameter name (ordinal) for deterministic JSON output.
            return (parameters ?? new Dictionary<string, string>())
                .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                .Select(parameter => new ToolCallParameterSaveData { name = parameter.Key, value = parameter.Value })
                .ToArray();
        }

        private static List<ToolCallTraceRecord> ToToolCallTrace(ToolCallTraceSaveData[] data)
        {
            return (data ?? Array.Empty<ToolCallTraceSaveData>())
                .Where(row => row != null)
                .Select(row => new ToolCallTraceRecord(
                    new GameTime(row.tickMinutes < 0 ? 0 : row.tickMinutes),
                    new SiteId((ulong)row.siteId),
                    ToToolCallRequest(row),
                    new ToolCallResult(row.accepted, row.payload, row.rejectionReason)))
                .ToList();
        }

        private static ToolCallRequest ToToolCallRequest(ToolCallTraceSaveData row)
        {
            return new ToolCallRequest(
                new ToolId(string.IsNullOrWhiteSpace(row.toolCode) ? "unknown" : row.toolCode),
                ToolSurfaceKind.FromCode(row.surfaceCode),
                ToToolCallParameterDictionary(row.parameters));
        }

        private static Dictionary<string, string> ToToolCallParameterDictionary(ToolCallParameterSaveData[] parameters)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var parameter in parameters ?? Array.Empty<ToolCallParameterSaveData>())
            {
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.name))
                    continue;
                dictionary[parameter.name] = parameter.value ?? string.Empty;
            }

            return dictionary;
        }

        private static LlmProposalLogSaveData[] ToLlmProposalLogData(IEnumerable<LlmProposalLogEntry> entries)
        {
            return (entries ?? Array.Empty<LlmProposalLogEntry>())
                .Where(entry => entry != null)
                .Select(entry => new LlmProposalLogSaveData
                {
                    tickMinutes = entry.Tick.TotalMinutes,
                    providerCode = entry.Provider.Code,
                    conversationId = entry.ConversationId,
                    responseText = entry.ResponseText,
                    acceptedToolCalls = entry.AcceptedToolCalls
                        .Select(call => ToToolCallTraceData(entry.Tick, default, call, ToolCallResult.AcceptedWith("accepted")))
                        .ToArray(),
                    rejectedToolCalls = entry.RejectedToolCalls
                        .Select(rejection => new LlmRejectedToolCallSaveData
                        {
                            request = ToToolCallTraceData(entry.Tick, default, rejection.Request, ToolCallResult.Rejected(rejection.Reason)),
                            reason = rejection.Reason,
                        })
                        .ToArray(),
                })
                .ToArray();
        }

        private static List<LlmProposalLogEntry> ToLlmProposalLog(LlmProposalLogSaveData[] data)
        {
            return (data ?? Array.Empty<LlmProposalLogSaveData>())
                .Where(row => row != null)
                .Select(row => new LlmProposalLogEntry(
                    new GameTime(row.tickMinutes < 0 ? 0 : row.tickMinutes),
                    LlmProviderKind.FromCode(row.providerCode),
                    row.conversationId,
                    row.responseText,
                    (row.acceptedToolCalls ?? Array.Empty<ToolCallTraceSaveData>()).Select(ToToolCallRequest),
                    (row.rejectedToolCalls ?? Array.Empty<LlmRejectedToolCallSaveData>())
                        .Where(rejection => rejection != null && rejection.request != null)
                        .Select(rejection => new ToolCallRejection(ToToolCallRequest(rejection.request), rejection.reason))))
                .ToList();
        }

        private static NpcSeedSaveData[] ToNpcSeedData(IEnumerable<NpcSeedRecord> npcs)
        {
            return (npcs ?? Array.Empty<NpcSeedRecord>())
                .Where(npc => npc != null)
                .OrderBy(npc => (long)npc.Id.Value)
                .Select(npc => new NpcSeedSaveData
                {
                    id = (long)npc.Id.Value,
                    home = (long)npc.Home.Value,
                    faction = (long)npc.Faction.Value,
                    name = npc.Name,
                    birthYear = npc.BirthYear,
                    role = (int)npc.Role,
                    portraitAssetPath = npc.PortraitAssetPath,
                })
                .ToArray();
        }

        private static List<NpcSeedRecord> ToNpcSeeds(NpcSeedSaveData[] data)
        {
            return (data ?? Array.Empty<NpcSeedSaveData>())
                .Where(row => row != null
                    && row.id != 0L
                    && row.home != 0L
                    && row.faction != 0L
                    && !string.IsNullOrWhiteSpace(row.name)
                    && row.role != (int)NpcRole.None)
                .OrderBy(row => row.id)
                .Select(row => new NpcSeedRecord(
                    new NpcId((ulong)row.id),
                    new SettlementId((ulong)row.home),
                    new FactionId((ulong)row.faction),
                    row.name,
                    row.birthYear,
                    (NpcRole)row.role,
                    row.portraitAssetPath))
                .ToList();
        }
    }
}

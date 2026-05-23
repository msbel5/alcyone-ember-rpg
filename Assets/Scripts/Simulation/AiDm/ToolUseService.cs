using System;
using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.AiDm
{
    public sealed class ToolUseService
    {
        private readonly ToolRegistry _registry;
        private readonly Dictionary<string, Func<ToolCallRequest, SliceWorldState, GameTime, ToolCallResult>> _handlers;
        private readonly ToolCallValidator _validator = new ToolCallValidator();

        public ToolUseService(ToolRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _handlers = new Dictionary<string, Func<ToolCallRequest, SliceWorldState, GameTime, ToolCallResult>>(StringComparer.Ordinal)
            {
                { "gift_item", GiftItem },
                { "propose_quest", ProposeQuest },
                { "flavor_bark", FlavorBark },
                { "recall_memory", RecallMemory },
                { "consult_fate", ConsultFate },
            };
        }

        public ToolCallResult Execute(ToolCallRequest request, SliceWorldState world, GameTime now, SiteId siteId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var validation = _validator.Validate(request, _registry);
            if (!validation.Accepted)
            {
                AppendTrace(world, now, siteId, request, validation);
                return validation;
            }

            var key = request.ToolId.Code;
            var result = _handlers.TryGetValue(key, out var handler)
                ? handler(request, world, now)
                : ToolCallResult.Rejected("no_handler");
            AppendTrace(world, now, siteId, request, result);
            if (result.Accepted && request.Surface == ToolSurfaceKind.Npc)
                AppendMemory(world, request, now, result.Payload);
            return result;
        }

        private static void AppendTrace(SliceWorldState world, GameTime now, SiteId siteId, ToolCallRequest request, ToolCallResult result)
        {
            if (request == null) return;
            if (world.ToolCallTrace == null) world.ToolCallTrace = new List<ToolCallTraceRecord>();
            world.ToolCallTrace.Add(new ToolCallTraceRecord(now, siteId, request, result));
        }

        private static void AppendMemory(SliceWorldState world, ToolCallRequest request, GameTime now, string summary)
        {
            if (!request.Parameters.TryGetValue("actor", out var raw) || !ulong.TryParse(raw, out var actor) || actor == 0UL)
                return;
            if (world.NpcMemory == null) world.NpcMemory = new NpcMemoryStore();
            world.NpcMemory.GetOrCreate(new ActorId(actor)).RecordEvent(new InteractionEvent(now, "AiToolUse", default, request.ToolId.Code, summary ?? string.Empty, 0, default));
        }

        private static ToolCallResult GiftItem(ToolCallRequest request, SliceWorldState world, GameTime now)
        {
            return ToolCallResult.AcceptedWith("gift_item:" + request.Parameters["item"]);
        }

        private static ToolCallResult ProposeQuest(ToolCallRequest request, SliceWorldState world, GameTime now)
        {
            world.Events?.Append(new WorldEvent(now, WorldEventKind.DmConsultFate, default, default, request.Parameters["summary"]));
            return ToolCallResult.AcceptedWith("quest:" + request.Parameters["summary"]);
        }

        private static ToolCallResult FlavorBark(ToolCallRequest request, SliceWorldState world, GameTime now)
        {
            return ToolCallResult.AcceptedWith(request.Parameters["text"]);
        }

        private static ToolCallResult RecallMemory(ToolCallRequest request, SliceWorldState world, GameTime now)
        {
            if (!ulong.TryParse(request.Parameters["actor"], out var actor) || world.NpcMemory == null || !world.NpcMemory.TryGet(new ActorId(actor), out var memory))
                return ToolCallResult.AcceptedWith(string.Empty);
            return ToolCallResult.AcceptedWith(string.Join("|", new EmberCrpg.Simulation.Memory.MemoryRecallService().LastN(memory, 8)));
        }

        private static ToolCallResult ConsultFate(ToolCallRequest request, SliceWorldState world, GameTime now)
        {
            var roll = (int)(StableHash(request.Parameters["query"]) % 100UL) + 1;
            return ToolCallResult.AcceptedWith(ConsultFateOutcomeBucket.FromRoll(roll).Code);
        }

        private static ulong StableHash(string value)
        {
            unchecked
            {
                ulong hash = 1469598103934665603UL;
                foreach (var c in value ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 1099511628211UL;
                }
                return hash;
            }
        }
    }
}

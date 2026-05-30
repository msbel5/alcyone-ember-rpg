using System;
using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>Deterministic DM escalation handlers backed by world snapshot data.</summary>
    public sealed class DmAgentEscalationService
    {
        public void RegisterHandlers(ToolCallRouter router, WorldState world)
        {
            if (router == null) throw new ArgumentNullException(nameof(router));
            if (world == null) throw new ArgumentNullException(nameof(world));

            router.RegisterHandler(ToolSurfaceKind.Dm, new ToolId("query_world_snapshot"), _ =>
                ToolCallResult.AcceptedWith(BuildSnapshot(world)));
            router.RegisterHandler(ToolSurfaceKind.Dm, new ToolId("escalate_resolve_or_pass"), request =>
            {
                request.TryGetParameter("npc_request_id", out var id);
                return ToolCallResult.AcceptedWith(string.IsNullOrWhiteSpace(id) ? "pass" : "resolved:" + id);
            });
        }

        public ToolCallResult EscalateNpcToDm(
            string npcRequestId,
            ToolRegistry registry,
            ToolCallRouter router,
            GameTime now,
            SiteId siteId,
            WorldEventLog events,
            ToolCallTracer tracer)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (router == null) throw new ArgumentNullException(nameof(router));

            var request = new ToolCallRequest(
                new ToolId("escalate_resolve_or_pass"),
                ToolSurfaceKind.Dm,
                new Dictionary<string, string> { { "npc_request_id", npcRequestId ?? string.Empty } });
            return router.Invoke(request, registry, now, siteId, events, tracer);
        }

        private static string BuildSnapshot(WorldState world)
        {
            return $"actors:{world.Actors.Count} sites:{world.Sites.Count} factions:{world.Factions.Count} stockpiles:{world.Stockpiles.Count} events:{world.Events.Count}";
        }
    }
}

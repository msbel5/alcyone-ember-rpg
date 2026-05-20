using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>
    /// Builds the DM-side tool descriptors. Faz 10 Atom 10.
    /// </summary>
    public static class DmAgentToolSurface
    {
        public static IReadOnlyList<ToolDescriptor> Descriptors()
        {
            return new[]
            {
                new ToolDescriptor(
                    new ToolId("query_world_snapshot"),
                    ToolSurfaceKind.Dm,
                    new ToolParameter[0],
                    "world_snapshot",
                    ToolSideEffect.Read),
                new ToolDescriptor(
                    new ToolId("propose_event"),
                    ToolSurfaceKind.Dm,
                    new[]
                    {
                        new ToolParameter("event_kind", "string", required: true),
                        new ToolParameter("subject_actor", "actor_id", required: false),
                        new ToolParameter("site", "site_id", required: false),
                        new ToolParameter("reason", "string", required: true),
                    },
                    "proposed_event_id",
                    ToolSideEffect.Mutate),
                new ToolDescriptor(
                    new ToolId("consult_fate"),
                    ToolSurfaceKind.Dm,
                    new[] { new ToolParameter("query", "string", required: true) },
                    "fate_bucket",
                    ToolSideEffect.Read),
                new ToolDescriptor(
                    new ToolId("escalate_resolve_or_pass"),
                    ToolSurfaceKind.Dm,
                    new[] { new ToolParameter("npc_request_id", "string", required: true) },
                    "dm_resolution",
                    ToolSideEffect.Read),
            };
        }

        public static void RegisterAll(ToolRegistry registry)
        {
            foreach (var descriptor in Descriptors())
                registry.Register(descriptor);
        }
    }
}

using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>
    /// Builds the NPC-side tool descriptors. Faz 10 Atom 8.
    /// </summary>
    public static class NpcAgentToolSurface
    {
        public static IReadOnlyList<ToolDescriptor> Descriptors()
        {
            return new[]
            {
                new ToolDescriptor(
                    new ToolId("ask_about"),
                    ToolSurfaceKind.Npc,
                    new[] { new ToolParameter("topic", "topic_id", required: true) },
                    "dialogue_response",
                    ToolSideEffect.Read),
                new ToolDescriptor(
                    new ToolId("remember"),
                    ToolSurfaceKind.Npc,
                    new[]
                    {
                        new ToolParameter("topic", "topic_id", required: true),
                        new ToolParameter("about", "actor_id", required: false),
                    },
                    "void",
                    ToolSideEffect.Mutate),
                new ToolDescriptor(
                    new ToolId("query_relation"),
                    ToolSurfaceKind.Npc,
                    new[] { new ToolParameter("other_faction", "faction_id", required: true) },
                    "relation_code",
                    ToolSideEffect.Read),
                new ToolDescriptor(
                    new ToolId("escalate_to_dm"),
                    ToolSurfaceKind.Npc,
                    new[] { new ToolParameter("reason", "string", required: true) },
                    "void",
                    ToolSideEffect.Mutate),
            };
        }

        public static void RegisterAll(ToolRegistry registry)
        {
            foreach (var descriptor in Descriptors())
                registry.Register(descriptor);
        }
    }
}

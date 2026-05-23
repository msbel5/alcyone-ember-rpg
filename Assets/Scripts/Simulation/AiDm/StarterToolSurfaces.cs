using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
{
    public static class StarterToolSurfaces
    {
        public static IReadOnlyList<ToolDescriptor> Descriptors()
        {
            return new[]
            {
                new ToolDescriptor(new ToolId("gift_item"), ToolSurfaceKind.Npc,
                    new[] { new ToolParameter("actor", "actor_id", true), new ToolParameter("item", "string", true) },
                    "gift_result", ToolSideEffect.Mutate, "gift_item"),
                new ToolDescriptor(new ToolId("propose_quest"), ToolSurfaceKind.Dm,
                    new[] { new ToolParameter("summary", "string", true) },
                    "quest_proposal", ToolSideEffect.Mutate, "propose_quest"),
                new ToolDescriptor(new ToolId("flavor_bark"), ToolSurfaceKind.Npc,
                    new[] { new ToolParameter("text", "string", true) },
                    "bark", ToolSideEffect.Read, "flavor_bark"),
                new ToolDescriptor(new ToolId("recall_memory"), ToolSurfaceKind.Npc,
                    new[] { new ToolParameter("actor", "actor_id", true) },
                    "memory_window", ToolSideEffect.Read, "recall_memory"),
                new ToolDescriptor(new ToolId("consult_fate"), ToolSurfaceKind.Dm,
                    new[] { new ToolParameter("query", "string", true) },
                    "fate_bucket", ToolSideEffect.Read, "consult_fate"),
            };
        }

        public static ToolRegistry Registry()
        {
            var registry = new ToolRegistry();
            foreach (var descriptor in Descriptors())
                registry.Register(descriptor);
            return registry;
        }
    }
}

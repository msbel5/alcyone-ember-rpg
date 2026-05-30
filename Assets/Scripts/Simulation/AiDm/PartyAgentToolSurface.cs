using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>
    /// Builds the party-member-side tool descriptors. Phase 10 Atom 9.
    /// </summary>
    public static class PartyAgentToolSurface
    {
        public static IReadOnlyList<ToolDescriptor> Descriptors()
        {
            return new[]
            {
                new ToolDescriptor(
                    new ToolId("request_item"),
                    ToolSurfaceKind.Party,
                    new[]
                    {
                        new ToolParameter("item_tag", "string", required: true),
                        new ToolParameter("quantity", "int", required: true),
                    },
                    "void",
                    ToolSideEffect.Mutate),
                new ToolDescriptor(
                    new ToolId("report_status"),
                    ToolSurfaceKind.Party,
                    new ToolParameter[0],
                    "party_status_block",
                    ToolSideEffect.Read),
                new ToolDescriptor(
                    new ToolId("vote_on_action"),
                    ToolSurfaceKind.Party,
                    new[] { new ToolParameter("proposed_action_id", "string", required: true) },
                    "vote_outcome",
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

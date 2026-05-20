using System;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>
    /// Validates a ToolCallRequest against the registry. Rejects unknown tool
    /// id, wrong surface, missing required parameter. Faz 10 Atom 6.
    /// </summary>
    public sealed class ToolCallValidator
    {
        public ToolCallResult Validate(ToolCallRequest request, ToolRegistry registry)
        {
            if (request == null) return ToolCallResult.Rejected("null_request");
            if (registry == null) return ToolCallResult.Rejected("null_registry");

            if (!registry.TryGet(request.Surface, request.ToolId, out var descriptor))
                return ToolCallResult.Rejected("unknown_tool");

            if (!descriptor.Surface.Equals(request.Surface))
                return ToolCallResult.Rejected("surface_mismatch");

            foreach (var parameter in descriptor.Parameters)
            {
                if (parameter.Required && !request.Parameters.ContainsKey(parameter.Name))
                    return ToolCallResult.Rejected("missing_required:" + parameter.Name);
            }

            return ToolCallResult.AcceptedWith("validated:" + descriptor.Id.Code);
        }
    }
}

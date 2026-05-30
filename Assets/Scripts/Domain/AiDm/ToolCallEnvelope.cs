using System;
using System.Collections.Generic;

namespace EmberCrpg.Domain.AiDm
{
    /// <summary>
    /// Immutable request envelope for an AI/DM tool invocation. Carries the
    /// tool id, the calling surface, and a typed-by-name parameter set.
    /// Phase 10 Atom 4.
    /// </summary>
    public sealed class ToolCallRequest
    {
        private readonly Dictionary<string, string> _parameters;

        public ToolCallRequest(ToolId toolId, ToolSurfaceKind surface, IReadOnlyDictionary<string, string> parameters)
        {
            if (toolId.IsEmpty) throw new ArgumentException("ToolCallRequest.ToolId must be non-empty.", nameof(toolId));
            if (surface.IsEmpty) throw new ArgumentException("ToolCallRequest.Surface must be set.", nameof(surface));

            ToolId = toolId;
            Surface = surface;
            _parameters = parameters == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(parameters);
        }

        public ToolId ToolId { get; }
        public ToolSurfaceKind Surface { get; }
        public IReadOnlyDictionary<string, string> Parameters => _parameters;

        public bool TryGetParameter(string name, out string value)
        {
            return _parameters.TryGetValue(name, out value);
        }
    }

    /// <summary>
    /// Immutable result envelope for a tool invocation. Carries the resulting
    /// payload, a boolean accepted/rejected verdict, and a stable reason code
    /// for rejection. Pure data; routing happens elsewhere.
    /// </summary>
    public sealed class ToolCallResult
    {
        public ToolCallResult(bool accepted, string payload, string rejectionReason)
        {
            Accepted = accepted;
            Payload = payload ?? string.Empty;
            RejectionReason = rejectionReason ?? string.Empty;
        }

        public bool Accepted { get; }
        public string Payload { get; }
        public string RejectionReason { get; }

        public static ToolCallResult AcceptedWith(string payload) => new ToolCallResult(true, payload, string.Empty);
        public static ToolCallResult Rejected(string reasonCode) => new ToolCallResult(false, string.Empty, reasonCode);
    }
}

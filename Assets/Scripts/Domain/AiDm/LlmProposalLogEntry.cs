using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Domain.AiDm
{
    /// <summary>Persistable LLM proposal audit row used for save/replay.</summary>
    public sealed class LlmProposalLogEntry
    {
        public LlmProposalLogEntry(
            GameTime tick,
            LlmProviderKind provider,
            string conversationId,
            string responseText,
            IEnumerable<ToolCallRequest> acceptedToolCalls,
            IEnumerable<ToolCallRejection> rejectedToolCalls)
        {
            Tick = tick;
            Provider = provider.IsEmpty ? LlmProviderKind.Mock : provider;
            ConversationId = conversationId ?? string.Empty;
            ResponseText = responseText ?? string.Empty;
            AcceptedToolCalls = acceptedToolCalls == null
                ? new ToolCallRequest[0]
                : new List<ToolCallRequest>(acceptedToolCalls).AsReadOnly();
            RejectedToolCalls = rejectedToolCalls == null
                ? new ToolCallRejection[0]
                : new List<ToolCallRejection>(rejectedToolCalls).AsReadOnly();
        }

        public GameTime Tick { get; }
        public LlmProviderKind Provider { get; }
        public string ConversationId { get; }
        public string ResponseText { get; }
        public IReadOnlyList<ToolCallRequest> AcceptedToolCalls { get; }
        public IReadOnlyList<ToolCallRejection> RejectedToolCalls { get; }
    }

    public readonly struct ToolCallRejection
    {
        public ToolCallRejection(ToolCallRequest request, string reason)
        {
            Request = request;
            Reason = reason ?? string.Empty;
        }

        public ToolCallRequest Request { get; }
        public string Reason { get; }
    }
}

using System;
using System.Collections.Generic;

namespace EmberCrpg.Domain.AiDm
{
    /// <summary>
    /// Immutable LLM request envelope. Carries system prompt id, conversation
    /// id, available tool descriptors, max tokens, deterministic seed.
    /// Faz 12 Atom 2.
    /// </summary>
    public sealed class LlmRequest
    {
        public LlmRequest(
            string systemPromptId,
            string conversationId,
            IEnumerable<ToolDescriptor> availableTools,
            int maxTokens,
            ulong seed)
        {
            if (string.IsNullOrWhiteSpace(systemPromptId))
                throw new ArgumentException("SystemPromptId must be non-blank.", nameof(systemPromptId));
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("ConversationId must be non-blank.", nameof(conversationId));
            if (maxTokens <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTokens), "MaxTokens must be positive.");

            SystemPromptId = systemPromptId.Trim();
            ConversationId = conversationId.Trim();
            AvailableTools = availableTools == null
                ? new ToolDescriptor[0]
                : new List<ToolDescriptor>(availableTools).AsReadOnly();
            MaxTokens = maxTokens;
            Seed = seed;
        }

        public string SystemPromptId { get; }
        public string ConversationId { get; }
        public IReadOnlyList<ToolDescriptor> AvailableTools { get; }
        public int MaxTokens { get; }
        public ulong Seed { get; }
    }

    /// <summary>
    /// Immutable LLM response envelope. Text + proposed tool calls + tokens used.
    /// Faz 12 Atom 2.
    /// </summary>
    public sealed class LlmResponse
    {
        public LlmResponse(string text, IEnumerable<ToolCallRequest> proposedToolCalls, int tokensUsed)
        {
            if (tokensUsed < 0)
                throw new ArgumentOutOfRangeException(nameof(tokensUsed), "TokensUsed must be non-negative.");

            Text = text ?? string.Empty;
            ProposedToolCalls = proposedToolCalls == null
                ? new ToolCallRequest[0]
                : new List<ToolCallRequest>(proposedToolCalls).AsReadOnly();
            TokensUsed = tokensUsed;
        }

        public string Text { get; }
        public IReadOnlyList<ToolCallRequest> ProposedToolCalls { get; }
        public int TokensUsed { get; }
    }
}

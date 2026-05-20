using System;
using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>
    /// Deterministic in-process mock LLM client. Returns scripted responses
    /// keyed by (system_prompt_id + conversation_id + seed). Used ONLY by tests
    /// — never wired as a default execution path. Faz 12 Atom 12.
    /// </summary>
    public sealed class MockLlmClient
    {
        private readonly Dictionary<string, LlmResponse> _scripts = new Dictionary<string, LlmResponse>();

        public LlmProviderKind Kind => LlmProviderKind.Mock;

        public void Script(string systemPromptId, string conversationId, ulong seed, LlmResponse response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            _scripts[Key(systemPromptId, conversationId, seed)] = response;
        }

        public LlmResponse Complete(LlmRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var key = Key(request.SystemPromptId, request.ConversationId, request.Seed);
            if (_scripts.TryGetValue(key, out var response))
                return response;
            return new LlmResponse(string.Empty, null, 0);
        }

        private static string Key(string sysId, string convId, ulong seed)
        {
            // PR#170 bot review fix: joining unescaped strings with "|" lets
            // distinct request tuples collide whenever an id contains the
            // separator (e.g. ("a|b","c",1) and ("a","b|c",1) collapse to the
            // same key). Length-prefix each field so the encoding is injective.
            var s = sysId ?? string.Empty;
            var c = convId ?? string.Empty;
            return s.Length.ToString() + ":" + s + "|" + c.Length.ToString() + ":" + c + "|" + seed;
        }
    }
}

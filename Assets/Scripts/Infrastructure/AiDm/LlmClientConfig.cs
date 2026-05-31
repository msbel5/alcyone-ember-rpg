// EMB-019/ARCH-05: this non-deterministic LLM provider lives in the EmberCrpg.Infrastructure
// assembly AND namespace (EmberCrpg.Infrastructure.AiDm), so the deterministic, headless Simulation
// core can never reference HTTP/native inference at compile time.
// LEFT-021: split out of the former LlmClients.cs (one public type per file).
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Infrastructure.AiDm
{
    /// <summary>
    /// Explicit endpoint config for sync LLM clients. Disabled by default.
    /// Codex audit (D-P3): currently consumed only by LocalQwenClient /
    /// CloudLlmClient (also experimental) and the AiDm test suite. Kept
    /// public so external setup code and integration tests can build a
    /// real-provider client; do not depend on this type in production
    /// pathways until the routing surface is wired.
    /// </summary>
    public sealed class LlmClientConfig
    {
        public LlmClientConfig(LlmProviderKind provider, string endpointUrl, string apiKey, bool enabled)
        {
            Provider = provider.IsEmpty ? LlmProviderKind.Mock : provider;
            EndpointUrl = endpointUrl ?? string.Empty;
            ApiKey = apiKey ?? string.Empty;
            Enabled = enabled;
        }

        public LlmProviderKind Provider { get; }
        public string EndpointUrl { get; }
        public string ApiKey { get; }
        public bool Enabled { get; }
    }
}

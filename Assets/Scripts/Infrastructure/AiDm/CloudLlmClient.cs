// EMB-019/ARCH-05: lives in EmberCrpg.Infrastructure.AiDm (see LlmClientConfig.cs header).
// LEFT-021: split out of the former LlmClients.cs (one public type per file).
using System;
using System.Net.Http;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Infrastructure.AiDm
{
    /// <summary>
    /// HTTP client targeting a cloud provider (Anthropic/OpenAI/etc.). Provider
    /// label is derived from <see cref="LlmClientConfig.Provider"/>.
    /// Codex audit (D-P3, restated in seventh-pass #14): no production caller
    /// is wired today; experimental alongside <see cref="LocalQwenClient"/>.
    /// Same Phase 12 gate applies: do not couple gameplay to this class until
    /// LlmRoutingService picks it up from configuration. The integration
    /// tests under Assets/Tests/EditMode/Net/* are the only consumers right
    /// now and they bypass the routing seam entirely.
    /// </summary>
    public sealed class CloudLlmClient
    {
        private readonly LlmClientConfig _config;
        private readonly HttpClient _http;

        public CloudLlmClient(LlmClientConfig config, HttpClient http = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _http = http ?? new HttpClient();
        }

        public LlmProviderKind Kind => _config.Provider;

        public LlmResponse Complete(LlmRequest request)
        {
            return LlmHttpClientCore.CompleteHttp(_config, _http, request);
        }
    }
}

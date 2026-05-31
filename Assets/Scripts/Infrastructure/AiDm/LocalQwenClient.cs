// EMB-019/ARCH-05: lives in EmberCrpg.Infrastructure.AiDm (see LlmClientConfig.cs header).
// LEFT-021: split out of the former LlmClients.cs (one public type per file).
using System;
using System.Net.Http;
using System.Threading; // EMB-018: CancellationTokenSource for bounded HTTP timeouts
using System.Threading.Tasks;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Infrastructure.AiDm
{
    /// <summary>
    /// HTTP client targeting a local Qwen-compatible endpoint.
    /// Codex audit (D-P3, restated in seventh-pass #14): no production caller
    /// wires this in any of the live Phase scenes; the only callers are
    /// integration tests and a manual smoke harness. Production adoption is
    /// gated on the Phase 12 LLM tool-calling sprint where LlmRoutingService
    /// will be wired against this contract. Until then, treat this class as
    /// experimental — do not couple gameplay code to it.
    /// </summary>
    public sealed class LocalQwenClient
    {
        public const string DefaultOllamaGenerateEndpoint = "http://localhost:11434/api/generate";

        private readonly LlmClientConfig _config;
        private readonly HttpClient _http;

        public LocalQwenClient(LlmClientConfig config, HttpClient http = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _http = http ?? new HttpClient();
        }

        public LlmProviderKind Kind => LlmProviderKind.LocalQwen;

        public LlmResponse Complete(LlmRequest request)
        {
            return SyncTaskBridge.Run(() => CompleteAsync(request, CancellationToken.None));
        }

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
        {
            return LlmHttpClientCore.CompleteHttpAsync(_config, _http, request, cancellationToken);
        }

        /// <summary>
        /// Codex review (PR #203 P2): a non-null `Complete().Text` is not a
        /// reliable availability signal because the response constructor
        /// normalises null → string.Empty. Probe the explicit HTTP status of
        /// a small HEAD/GET against the configured endpoint instead, so
        /// callers get a true/false grounded in network reachability + 2xx.
        /// </summary>
        public bool IsAvailable()
        {
            return SyncTaskBridge.Run(() => IsAvailableAsync(CancellationToken.None));
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            if (!_config.Enabled || string.IsNullOrWhiteSpace(_config.EndpointUrl))
                return false;
            if (cancellationToken.IsCancellationRequested)
                return false;
            try
            {
                using (var probe = new HttpRequestMessage(HttpMethod.Get, _config.EndpointUrl))
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) // EMB-018: probe must not hang
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(5));
                    var resp = await _http.SendAsync(probe, timeout.Token).ConfigureAwait(false);
                    // Ollama responds 200 OK on GET to /api/generate even
                    // without a model selected; any 2xx-3xx is "service up".
                    return (int)resp.StatusCode >= 200 && (int)resp.StatusCode < 400;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

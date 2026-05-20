using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>Explicit endpoint config for sync LLM clients. Disabled by default.</summary>
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

    public sealed class LocalQwenClient
    {
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
            return LlmHttpClientCore.CompleteHttp(_config, _http, request);
        }
    }

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

    internal static class LlmHttpClientCore
    {
        public static LlmResponse CompleteHttp(LlmClientConfig config, HttpClient http, LlmRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!config.Enabled || string.IsNullOrWhiteSpace(config.EndpointUrl))
                return new LlmResponse(string.Empty, null, 0);

            var payload = JsonSerializer.Serialize(new
            {
                system_prompt_id = request.SystemPromptId,
                conversation_id = request.ConversationId,
                max_tokens = request.MaxTokens,
                seed = request.Seed,
            });

            using (var message = new HttpRequestMessage(HttpMethod.Post, config.EndpointUrl))
            {
                message.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(config.ApiKey))
                    message.Headers.TryAddWithoutValidation("Authorization", "Bearer " + config.ApiKey);

                var response = http.SendAsync(message).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                    return new LlmResponse(string.Empty, null, 0);

                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                using (var document = JsonDocument.Parse(json))
                {
                    var root = document.RootElement;
                    var text = root.TryGetProperty("text", out var textElement) ? textElement.GetString() : string.Empty;
                    var tokens = root.TryGetProperty("tokens_used", out var tokensElement) && tokensElement.TryGetInt32(out var parsed)
                        ? parsed
                        : 0;
                    return new LlmResponse(text, null, tokens);
                }
            }
        }
    }
}

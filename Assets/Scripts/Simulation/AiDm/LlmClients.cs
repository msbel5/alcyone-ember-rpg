using System;
using System.Net.Http;
using System.Text;
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

            // Hand-rolled JSON; Unity's default .NET Standard 2.1 profile does not
            // ship System.Text.Json. Newtonsoft is gated behind an extra package
            // and the payload shape here is small + stable, so the simplest fix is
            // to format four well-known fields with proper escaping.
            var payload = BuildRequestJson(request);

            using (var message = new HttpRequestMessage(HttpMethod.Post, config.EndpointUrl))
            {
                message.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(config.ApiKey))
                    message.Headers.TryAddWithoutValidation("Authorization", "Bearer " + config.ApiKey);

                var response = http.SendAsync(message).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                    return new LlmResponse(string.Empty, null, 0);

                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var text = ExtractStringField(json, "text") ?? string.Empty;
                var tokens = ExtractIntField(json, "tokens_used") ?? 0;
                return new LlmResponse(text, null, tokens);
            }
        }

        private static string BuildRequestJson(LlmRequest request)
        {
            var sb = new StringBuilder(128);
            sb.Append('{');
            sb.Append("\"system_prompt_id\":");   AppendJsonString(sb, request.SystemPromptId);
            sb.Append(",\"conversation_id\":");   AppendJsonString(sb, request.ConversationId);
            sb.Append(",\"max_tokens\":").Append(request.MaxTokens);
            sb.Append(",\"seed\":").Append(request.Seed);
            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendJsonString(StringBuilder sb, string value)
        {
            sb.Append('"');
            if (value != null)
            {
                foreach (var c in value)
                {
                    switch (c)
                    {
                        case '"':  sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20) sb.AppendFormat("\\u{0:x4}", (int)c);
                            else sb.Append(c);
                            break;
                    }
                }
            }
            sb.Append('"');
        }

        private static string ExtractStringField(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var marker = "\"" + key + "\"";
            var idx = json.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return null;
            var colon = json.IndexOf(':', idx + marker.Length);
            if (colon < 0) return null;
            var start = json.IndexOf('"', colon + 1);
            if (start < 0) return null;
            var sb = new StringBuilder();
            for (int i = start + 1; i < json.Length; i++)
            {
                var c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    var esc = json[i + 1];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        default:  sb.Append(esc); break;
                    }
                    i++;
                    continue;
                }
                if (c == '"') return sb.ToString();
                sb.Append(c);
            }
            return null;
        }

        private static int? ExtractIntField(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var marker = "\"" + key + "\"";
            var idx = json.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return null;
            var colon = json.IndexOf(':', idx + marker.Length);
            if (colon < 0) return null;
            var i = colon + 1;
            while (i < json.Length && (json[i] == ' ' || json[i] == '\t' || json[i] == '\n' || json[i] == '\r')) i++;
            var start = i;
            if (i < json.Length && (json[i] == '-' || json[i] == '+')) i++;
            while (i < json.Length && char.IsDigit(json[i])) i++;
            if (start == i) return null;
            return int.TryParse(json.Substring(start, i - start), out var parsed) ? parsed : (int?)null;
        }
    }
}

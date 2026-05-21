using System;
using System.Net.Http;
using System.Text;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
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

    /// <summary>
    /// HTTP client targeting a local Qwen-compatible endpoint.
    /// Codex audit (D-P3): no production caller is wired today; this class is
    /// experimental — set up by integration tests and a manual smoke harness.
    /// Routing service can adopt it once the local inference contract is
    /// frozen. Do not lock production behaviour on this until then.
    /// </summary>
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

    /// <summary>
    /// HTTP client targeting a cloud provider (Anthropic/OpenAI/etc.). Provider
    /// label is derived from <see cref="LlmClientConfig.Provider"/>.
    /// Codex audit (D-P3): no production caller is wired today; experimental
    /// alongside <see cref="LocalQwenClient"/>. Use through integration tests
    /// only until LlmRoutingService is wired to construct one from
    /// configuration.
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
                // Codex audit (second pass A-P1): the HTTP path previously
                // hardcoded `null` for the tool-call list, so every Consult-Fate
                // proposal from a cloud / local-HTTP provider was discarded
                // before the validator could see it — the model's tool decisions
                // never reached the simulation. Parse `proposed_tool_calls` as
                // an array of { tool_id, surface, parameters: {...} } objects.
                var calls = ExtractProposedToolCalls(json);
                return new LlmResponse(text, calls, tokens);
            }
        }

        /// <summary>
        /// Extracts a JSON array shaped as
        /// `"proposed_tool_calls":[{"tool_id":"...","surface":"...","parameters":{...}}, ...]`
        /// from the provider response. Tolerant of missing fields; any malformed
        /// row is skipped rather than throwing. Hand-rolled to avoid a JSON
        /// dependency in Domain-adjacent code.
        /// </summary>
        private static System.Collections.Generic.List<ToolCallRequest> ExtractProposedToolCalls(string json)
        {
            var result = new System.Collections.Generic.List<ToolCallRequest>();
            if (string.IsNullOrEmpty(json)) return result;
            const string key = "\"proposed_tool_calls\"";
            var idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return result;
            var colon = json.IndexOf(':', idx + key.Length);
            if (colon < 0) return result;
            var arrStart = json.IndexOf('[', colon + 1);
            if (arrStart < 0) return result;
            // Walk forward, balancing brackets to find array end
            int depth = 1;
            int arrEnd = -1;
            for (int i = arrStart + 1; i < json.Length; i++)
            {
                var c = json[i];
                if (c == '[') depth++;
                else if (c == ']')
                {
                    depth--;
                    if (depth == 0) { arrEnd = i; break; }
                }
            }
            if (arrEnd < 0) return result;
            // Iterate top-level objects inside [arrStart+1, arrEnd)
            int p = arrStart + 1;
            while (p < arrEnd)
            {
                while (p < arrEnd && (json[p] == ' ' || json[p] == ',' || json[p] == '\n' || json[p] == '\r' || json[p] == '\t')) p++;
                if (p >= arrEnd) break;
                if (json[p] != '{') { p++; continue; }
                int objStart = p;
                int objDepth = 1;
                int objEnd = -1;
                for (int j = p + 1; j < arrEnd; j++)
                {
                    var c = json[j];
                    if (c == '{') objDepth++;
                    else if (c == '}')
                    {
                        objDepth--;
                        if (objDepth == 0) { objEnd = j; break; }
                    }
                }
                if (objEnd < 0) break;
                var obj = json.Substring(objStart, objEnd - objStart + 1);
                p = objEnd + 1;
                var call = TryParseToolCallObject(obj);
                if (call != null) result.Add(call);
            }
            return result;
        }

        private static ToolCallRequest TryParseToolCallObject(string obj)
        {
            try
            {
                var toolIdRaw = ExtractStringField(obj, "tool_id");
                var surfaceRaw = ExtractStringField(obj, "surface");
                if (string.IsNullOrEmpty(toolIdRaw) || string.IsNullOrEmpty(surfaceRaw))
                    return null;
                // Surface code → ToolSurfaceKind via FromCode (returns Empty on miss).
                var surface = ToolSurfaceKind.FromCode(surfaceRaw);
                if (surface.IsEmpty) return null;
                var parameters = ExtractParametersObject(obj);
                return new ToolCallRequest(new ToolId(toolIdRaw), surface, parameters);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private static System.Collections.Generic.IReadOnlyDictionary<string, string> ExtractParametersObject(string obj)
        {
            var result = new System.Collections.Generic.Dictionary<string, string>();
            const string key = "\"parameters\"";
            var idx = obj.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return result;
            var colon = obj.IndexOf(':', idx + key.Length);
            if (colon < 0) return result;
            var braceStart = obj.IndexOf('{', colon + 1);
            if (braceStart < 0) return result;
            int depth = 1;
            int braceEnd = -1;
            for (int i = braceStart + 1; i < obj.Length; i++)
            {
                var c = obj[i];
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) { braceEnd = i; break; }
                }
            }
            if (braceEnd < 0) return result;
            // Inside parameter object, expect "key":"value" pairs; capture each
            int p = braceStart + 1;
            while (p < braceEnd)
            {
                while (p < braceEnd && obj[p] != '"') p++;
                if (p >= braceEnd) break;
                int keyStart = p + 1;
                int keyEnd = obj.IndexOf('"', keyStart);
                if (keyEnd < 0 || keyEnd >= braceEnd) break;
                var k = obj.Substring(keyStart, keyEnd - keyStart);
                int afterKey = obj.IndexOf(':', keyEnd + 1);
                if (afterKey < 0 || afterKey >= braceEnd) break;
                // Value: only support string values for now (parameters dict is <string,string>)
                int valStart = obj.IndexOf('"', afterKey + 1);
                if (valStart < 0 || valStart >= braceEnd) { p = afterKey + 1; continue; }
                int valEnd = valStart + 1;
                while (valEnd < braceEnd)
                {
                    if (obj[valEnd] == '\\' && valEnd + 1 < braceEnd) { valEnd += 2; continue; }
                    if (obj[valEnd] == '"') break;
                    valEnd++;
                }
                if (valEnd >= braceEnd) break;
                var v = obj.Substring(valStart + 1, valEnd - valStart - 1);
                result[k] = v;
                p = valEnd + 1;
            }
            return result;
        }

        private static string BuildRequestJson(LlmRequest request)
        {
            var sb = new StringBuilder(256);
            sb.Append('{');
            sb.Append("\"system_prompt_id\":");   AppendJsonString(sb, request.SystemPromptId);
            sb.Append(",\"conversation_id\":");   AppendJsonString(sb, request.ConversationId);
            sb.Append(",\"max_tokens\":").Append(request.MaxTokens);
            sb.Append(",\"seed\":").Append(request.Seed);

            // Codex audit (A/P2): the request envelope previously dropped the
            // available_tools list entirely, so any cloud-routed conversation
            // arrived at the provider with no tool schemas — every tool call
            // would be a hallucination. Serialize the descriptor rows
            // deterministically (stable id order from LlmRequest) so the
            // provider sees the exact contract.
            sb.Append(",\"available_tools\":[");
            var tools = request.AvailableTools;
            if (tools != null)
            {
                for (int i = 0; i < tools.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    var tool = tools[i];
                    if (tool == null) { sb.Append("null"); continue; }
                    sb.Append('{');
                    sb.Append("\"id\":");           AppendJsonString(sb, tool.Id.Code);
                    sb.Append(",\"surface\":");     AppendJsonString(sb, tool.Surface.Code);
                    sb.Append(",\"output_schema\":"); AppendJsonString(sb, tool.OutputSchemaKey);
                    sb.Append(",\"side_effect\":"); AppendJsonString(sb, tool.SideEffect.Code);
                    sb.Append(",\"parameters\":[");
                    var pars = tool.Parameters;
                    for (int p = 0; p < pars.Count; p++)
                    {
                        if (p > 0) sb.Append(',');
                        var par = pars[p];
                        sb.Append('{');
                        sb.Append("\"name\":");      AppendJsonString(sb, par.Name);
                        sb.Append(",\"schema\":");   AppendJsonString(sb, par.SchemaKey);
                        sb.Append(",\"required\":").Append(par.Required ? "true" : "false");
                        sb.Append('}');
                    }
                    sb.Append("]}");
                }
            }
            sb.Append(']');

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
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            // Codex audit (A/P3): previously the parser fell into
                            // the `default` branch on `\uXXXX`, emitting the literal
                            // 'u' followed by four raw chars instead of the encoded
                            // code point — string fields with international content
                            // or quoted glyphs (e.g. é) round-tripped as
                            // garbage. Decode the four hex digits to a UTF-16 code
                            // unit; high surrogates pair naturally if the next
                            // escape is another \u. Fall back to literal `\u` on
                            // malformed escape so we never throw mid-parse.
                            if (i + 5 < json.Length
                                && int.TryParse(json.Substring(i + 2, 4),
                                    System.Globalization.NumberStyles.HexNumber,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out var code))
                            {
                                sb.Append((char)code);
                                i += 4; // i++ below advances past 'u'
                            }
                            else
                            {
                                sb.Append('\\');
                                sb.Append('u');
                            }
                            break;
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

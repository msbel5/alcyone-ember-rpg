// EMB-019/ARCH-05: this non-deterministic LLM provider lives in the EmberCrpg.Infrastructure
// assembly AND namespace (EmberCrpg.Infrastructure.AiDm), so the deterministic, headless Simulation
// core can never reference HTTP/native inference at compile time and the namespace matches the
// assembly that actually owns the type.
using System;
using System.Net.Http;
using System.Text;
using System.Threading; // EMB-018: CancellationTokenSource for bounded HTTP timeouts
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
    // Codex audit (seventh pass J-P3 #32): this file deliberately folds three
    // related public types — LlmClientConfig, LocalQwenClient, CloudLlmClient
    // — into a single file because they share the same HTTP envelope and
    // config struct. The fold is documented in docs/sprint-phase-12-atom-map.md
    // rows 3 + 4. Splitting them across three files would only multiply
    // boilerplate (each would import the same envelope types and config). If
    // a future code style sweep mandates one-public-type-per-file, the split
    // points are obvious; do not split now.
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
            return LlmHttpClientCore.CompleteHttp(_config, _http, request);
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
            if (!_config.Enabled || string.IsNullOrWhiteSpace(_config.EndpointUrl))
                return false;
            try
            {
                using (var probe = new HttpRequestMessage(HttpMethod.Get, _config.EndpointUrl))
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5))) // EMB-018: probe must not hang
                {
                    var resp = _http.SendAsync(probe, timeout.Token).GetAwaiter().GetResult();
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

    internal static class LlmHttpClientCore
    {
        // EMB-018: hard ceiling on the sync-over-async request so a hung endpoint can't pin the thread.
        private const int HttpTimeoutSeconds = 30;

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

                // EMB-018: bound the sync-over-async HTTP call with a timeout so a hung/slow endpoint
                // can't block the (worker) thread indefinitely; on timeout or transport error, degrade
                // to an empty response instead of throwing into the caller's awaited Task.Run.
                string json;
                try
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(HttpTimeoutSeconds)))
                    {
                        var response = http.SendAsync(message, timeout.Token).GetAwaiter().GetResult();
                        if (!response.IsSuccessStatusCode)
                            return new LlmResponse(string.Empty, null, 0);
                        json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }
                }
                catch (OperationCanceledException) { return new LlmResponse(string.Empty, null, 0); }
                catch (HttpRequestException) { return new LlmResponse(string.Empty, null, 0); }
                var text = ExtractStringField(json, "text") ?? ExtractStringField(json, "response") ?? string.Empty;
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
            // Codex audit (third pass A-P2): the bracket scanner used to ignore
            // string contents, so a `]`, `{`, or `}` inside a JSON string value
            // (e.g. `"reason":"loaded ] of payload"`) corrupted depth tracking
            // and silently dropped subsequent tool calls. Track string state
            // (with escape awareness) so brackets inside strings don't count.
            int arrEnd = FindMatchingBracket(json, arrStart, '[', ']');
            if (arrEnd < 0) return result;
            // Iterate top-level objects inside [arrStart+1, arrEnd)
            int p = arrStart + 1;
            while (p < arrEnd)
            {
                while (p < arrEnd && (json[p] == ' ' || json[p] == ',' || json[p] == '\n' || json[p] == '\r' || json[p] == '\t')) p++;
                if (p >= arrEnd) break;
                if (json[p] != '{') { p++; continue; }
                int objStart = p;
                int objEnd = FindMatchingBracket(json, objStart, '{', '}');
                if (objEnd < 0 || objEnd > arrEnd) break;
                var obj = json.Substring(objStart, objEnd - objStart + 1);
                p = objEnd + 1;
                var call = TryParseToolCallObject(obj);
                if (call != null) result.Add(call);
            }
            return result;
        }

        /// <summary>
        /// Codex audit (third pass A-P2): string-aware bracket walker. Starts
        /// at <paramref name="openIndex"/> (which must hold <paramref name="open"/>),
        /// returns the index of the matching <paramref name="close"/>, or -1 if
        /// not found. Skips brackets that occur inside JSON string literals,
        /// honoring `\"` and `\\` escapes.
        /// </summary>
        private static int FindMatchingBracket(string text, int openIndex, char open, char close)
        {
            int depth = 1;
            bool inString = false;
            for (int i = openIndex + 1; i < text.Length; i++)
            {
                var c = text[i];
                if (inString)
                {
                    if (c == '\\') { i++; continue; } // skip escaped char
                    if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') { inString = true; continue; }
                if (c == open) depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
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
            int braceEnd = FindMatchingBracket(obj, braceStart, '{', '}');
            if (braceEnd < 0) return result;
            // Inside parameter object, expect "key": <scalar> pairs. Codex audit
            // (third pass A-P2): the previous parser only captured quoted string
            // values, silently dropping numeric `actor_id: 1234`, `int site_id`,
            // bool, and null scalars. The validator downstream coerces strings,
            // so we render every scalar to its invariant string form.
            int p = braceStart + 1;
            while (p < braceEnd)
            {
                while (p < braceEnd && (obj[p] == ' ' || obj[p] == ',' || obj[p] == '\n' || obj[p] == '\r' || obj[p] == '\t')) p++;
                if (p >= braceEnd) break;
                if (obj[p] != '"') { p++; continue; }
                int keyStart = p + 1;
                int keyEnd = obj.IndexOf('"', keyStart);
                if (keyEnd < 0 || keyEnd >= braceEnd) break;
                var k = obj.Substring(keyStart, keyEnd - keyStart);
                int afterKey = obj.IndexOf(':', keyEnd + 1);
                if (afterKey < 0 || afterKey >= braceEnd) break;
                int valCursor = afterKey + 1;
                while (valCursor < braceEnd && (obj[valCursor] == ' ' || obj[valCursor] == '\n' || obj[valCursor] == '\r' || obj[valCursor] == '\t')) valCursor++;
                if (valCursor >= braceEnd) break;
                var ch = obj[valCursor];
                string v;
                int next;
                if (ch == '"')
                {
                    // Quoted string value — same scan as before, with escape awareness.
                    int valEnd = valCursor + 1;
                    while (valEnd < braceEnd)
                    {
                        if (obj[valEnd] == '\\' && valEnd + 1 < braceEnd) { valEnd += 2; continue; }
                        if (obj[valEnd] == '"') break;
                        valEnd++;
                    }
                    if (valEnd >= braceEnd) break;
                    v = obj.Substring(valCursor + 1, valEnd - valCursor - 1);
                    next = valEnd + 1;
                }
                else
                {
                    // Scalar token until comma / closing brace / whitespace.
                    int tokenStart = valCursor;
                    int tokenEnd = valCursor;
                    while (tokenEnd < braceEnd && obj[tokenEnd] != ',' && obj[tokenEnd] != '}' && obj[tokenEnd] != ' ' && obj[tokenEnd] != '\n' && obj[tokenEnd] != '\r' && obj[tokenEnd] != '\t')
                        tokenEnd++;
                    v = obj.Substring(tokenStart, tokenEnd - tokenStart);
                    next = tokenEnd;
                }
                result[k] = v;
                p = next;
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
            if (!string.IsNullOrEmpty(request.SystemPrompt) || request.RecentTurns.Count > 0)
            {
                sb.Append(",\"prompt\":");
                AppendJsonString(sb, BuildOllamaPrompt(request));
                sb.Append(",\"stream\":false");
                sb.Append(",\"model\":\"qwen2.5:3b-instruct-q4_K_M\"");
            }

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

        private static string BuildOllamaPrompt(LlmRequest request)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(request.SystemPrompt))
                sb.Append(request.SystemPrompt).Append('\n');
            for (int i = 0; i < request.RecentTurns.Count; i++)
                sb.Append("Memory ").Append(i + 1).Append(": ").Append(request.RecentTurns[i]).Append('\n');
            sb.Append("Respond in character.");
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

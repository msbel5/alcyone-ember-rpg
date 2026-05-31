using System;
using System.Net.Http;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Simulation.AiDm;
using EmberCrpg.Infrastructure.AiDm; // ARCH-05: LLM provider impls
using EmberCrpg.Presentation.Ember.Forge; // BUG-3: reach the already-wired native Qwen

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public static class DefaultNpcPortraitJsonProvider
    {
        private const string SystemPrompt =
            "You create Ember CRPG NPC portrait JSON only. Respond with one JSON object matching the NpcPromptJson schema. No prose, no markdown fences.";

        public static string Request(uint seed, string correctionReason)
        {
            // BUG-3 root cause: this used to spin up a fresh LocalQwenClient pointed at the OLLAMA HTTP
            // endpoint, which is NOT running in the default offline build — so every portrait request
            // failed and fell back to "invalid_json", leaving the deterministic placeholder forever.
            // Prefer the SAME wired native Qwen the rest of the game uses (greetings / topics / fate),
            // exposed via ForgeLocator.NativeLlm. Only fall back to the env/Ollama dev path if it's absent.
            var wired = ForgeLocator.NativeLlm;
            if (wired != null && wired.IsAvailable)
            {
                try
                {
                    var routing = new LlmRoutingService(request => wired.Complete(request), null);
                    var response = routing.Complete(BuildRequest(seed, correctionReason), out _);
                    return ExtractJsonObject(response?.Text);
                }
                catch
                {
                    return string.Empty;
                }
            }

            // Dev fallback: an env-pointed native GGUF, else the Ollama HTTP endpoint.
            var endpoint = Environment.GetEnvironmentVariable("EMBER_LOCAL_LLM_ENDPOINT");
            if (string.IsNullOrWhiteSpace(endpoint))
                endpoint = LocalQwenClient.DefaultOllamaGenerateEndpoint;

            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) })
            {
                var local = new LocalQwenClient(
                    new LlmClientConfig(LlmProviderKind.LocalQwen, endpoint, string.Empty, true),
                    http);
                LlmDispatch dispatch = request => local.Complete(request);

                var modelPath = Environment.GetEnvironmentVariable("EMBER_NATIVE_LLM_MODEL");
                NativeLlmClient native = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(modelPath))
                    {
                        native = NativeLlmClient.FromModelFile(modelPath, local);
                        dispatch = request => native.Complete(request);
                    }

                    var routing = new LlmRoutingService(dispatch, null);
                    var response = routing.Complete(BuildRequest(seed, correctionReason), out _);
                    return ExtractJsonObject(response?.Text);
                }
                catch
                {
                    return string.Empty;
                }
                finally
                {
                    native?.Dispose();
                }
            }
        }

        private static LlmRequest BuildRequest(uint seed, string correctionReason) =>
            new LlmRequest(
                "npc_prompt_json",
                "character_creation_portrait",
                Array.Empty<ToolDescriptor>(),
                240,
                seed,
                SystemPrompt,
                new[] { BuildPrompt(seed, correctionReason) });

        // BUG-3: a small local model frequently wraps the object in prose or ```json fences. Pull out the
        // first balanced {...} block so the (strict) validator sees clean JSON instead of rejecting prose.
        private static string ExtractJsonObject(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');
            if (start < 0 || end <= start) return raw.Trim();
            return raw.Substring(start, end - start + 1);
        }

        private static string BuildPrompt(uint seed, string correctionReason)
        {
            var schema = "{\"archetype_id\":\"humanoid_male\",\"primary_hue_degrees\":28,\"secondary_hue_degrees\":215,\"mood_keywords\":[\"wary\"],\"distinctive_features\":[\"scar\"],\"clothing_style\":\"leather jerkin\",\"accessory\":\"talisman pendant\",\"world_style_anchor\":\"ember-warm\"}";
            if (!string.IsNullOrWhiteSpace(correctionReason))
                return "The previous response was invalid because " + correctionReason + ". Respond ONLY with valid JSON matching this shape: " + schema;
            return "Seed " + seed + ". Produce a grounded Ember dark-fantasy NPC portrait JSON matching this shape: " + schema;
        }
    }
}

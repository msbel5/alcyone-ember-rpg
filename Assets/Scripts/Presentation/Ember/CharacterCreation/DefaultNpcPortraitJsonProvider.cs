using System;
using System.Net.Http;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Simulation.AiDm;
using EmberCrpg.Infrastructure.AiDm; // ARCH-05: LLM provider impls

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public static class DefaultNpcPortraitJsonProvider
    {
        private const string SystemPrompt =
            "You create Ember CRPG NPC portrait JSON only. Respond with one JSON object matching the NpcPromptJson schema. No prose.";

        public static string Request(uint seed, string correctionReason)
        {
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
                    var prompt = BuildPrompt(seed, correctionReason);
                    var request = new LlmRequest(
                        "npc_prompt_json",
                        "character_creation_portrait",
                        Array.Empty<ToolDescriptor>(),
                        240,
                        seed,
                        SystemPrompt,
                        new[] { prompt });
                    var response = routing.Complete(request, out _);
                    return response.Text;
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

        private static string BuildPrompt(uint seed, string correctionReason)
        {
            var schema = "{\"archetype_id\":\"humanoid_male\",\"primary_hue_degrees\":28,\"secondary_hue_degrees\":215,\"mood_keywords\":[\"wary\"],\"distinctive_features\":[\"scar\"],\"clothing_style\":\"leather jerkin\",\"accessory\":\"talisman pendant\",\"world_style_anchor\":\"ember-warm\"}";
            if (!string.IsNullOrWhiteSpace(correctionReason))
                return "The previous response was invalid because " + correctionReason + ". Respond ONLY with valid JSON matching this shape: " + schema;
            return "Seed " + seed + ". Produce a grounded Ember dark-fantasy NPC portrait JSON matching this shape: " + schema;
        }
    }
}

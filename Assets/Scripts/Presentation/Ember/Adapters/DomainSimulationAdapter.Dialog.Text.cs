using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        /// <summary>
        /// BUG-DIALOG-EMPTY: the deterministic, offline-safe opening line for a conversation. NEVER
        /// returns null/empty/whitespace. Prefers the leading per-actor AskAbout answer (the same
        /// deterministic copy NpcTopicCatalog / the WorldFactory seed produce, e.g. "Sentinel Rook
        /// keeps count of every footstep, including yours."); otherwise composes a role+name greeting;
        /// otherwise a generic safe fallback. Used as the synchronous line the async LLM may replace.
        /// </summary>
        private static string DeterministicGreeting(
            string actorName, NpcSeedRecord npc, IReadOnlyList<AskAboutTopic> topics)
        {
            // 1) Lead with the first topic answer that carries real copy — this is the persona/AskAbout
            //    line the player reads as the NPC speaking in character.
            if (topics != null)
            {
                foreach (var t in topics)
                {
                    if (t != null && !string.IsNullOrWhiteSpace(t.Answer))
                        return t.Answer.Trim();
                }
            }

            // 2) No topic copy available: greet from role + name when we have a seed.
            bool hasName = !string.IsNullOrWhiteSpace(actorName);
            if (npc != null)
            {
                return hasName
                    ? $"{actorName} the {npc.Role} regards you. \"Speak your piece.\""
                    : $"The {npc.Role} regards you. \"Speak your piece.\"";
            }

            // 3) Last-resort generic line (still never empty).
            return hasName
                ? $"{actorName} waits for you to speak."
                : "Someone waits for you to speak.";
        }

        // BUG-DIALOG-BRAND: WorldProfile.Style is an INTERNAL codename enum (e.g. "LowFantasy").
        // Interpolating it verbatim into an NPC prompt makes the local model narrate the real brand
        // ("Welcome to the world of Morrowind"). Produce a brand-safe, human descriptor instead: drop any
        // brand/codename token, split the CamelCase enum into spaced lowercase words, and fall back to a
        // generic phrase when nothing usable remains.
        private string StyleDescriptor()
        {
            var raw = _world.WorldProfile?.Style.ToString();
            if (string.IsNullOrWhiteSpace(raw)) raw = "low-fantasy";

            // Strip known IP brand/codenames (case-insensitive). "elder ?scrolls" matches "elderscrolls"
            // and "elder scrolls".
            raw = System.Text.RegularExpressions.Regex.Replace(
                raw,
                "morrowind|daggerfall|tamriel|skyrim|oblivion|elder ?scrolls",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Split CamelCase / PascalCase boundaries into spaces, then lowercase. "LowFantasy" -> "Low Fantasy".
            raw = System.Text.RegularExpressions.Regex.Replace(raw, "(?<=[a-z0-9])(?=[A-Z])", " ");
            // Collapse runs of whitespace that the brand strip may have left behind.
            raw = System.Text.RegularExpressions.Regex.Replace(raw, "\\s+", " ").Trim().ToLowerInvariant();

            return string.IsNullOrWhiteSpace(raw) ? "low-fantasy" : raw;
        }

        // BUG-DIALOG-TURNLEAK: the local model sometimes echoes the prompt's chat-turn scaffolding back
        // into its completion (e.g. "...What brings you here?\nUser:"). Cut the response at the FIRST
        // leaked turn/role/memory marker so only the in-character head survives. Returns "" when there is
        // nothing usable, so callers keep their deterministic line.
        private static string SanitizeNpcLine(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            if (LooksLikeLlmProviderFailure(raw)) return string.Empty;

            string[] markers =
            {
                "User:", "Assistant:", "System:", "<|im", "Memory:", "\nUser", "\nMemory"
            };

            int cut = -1;
            foreach (var marker in markers)
            {
                int idx = raw.IndexOf(marker, System.StringComparison.OrdinalIgnoreCase);
                if (idx >= 0 && (cut < 0 || idx < cut))
                    cut = idx;
            }

            var head = cut >= 0 ? raw.Substring(0, cut) : raw;
            return head.Trim();
        }

        private static bool LooksLikeLlmProviderFailure(string raw)
        {
            var lower = raw.Trim().ToLowerInvariant();
            return lower.StartsWith("native error:", System.StringComparison.Ordinal)
                || lower.Contains("llama_decode failed")
                || lower.Contains("invalidinputbatch")
                || lower.Contains("native model missing")
                || lower.Contains("llamasharp) not enabled");
        }

        private static LlmResponse CompleteLlmOrEmpty(EmberCrpg.Simulation.AiDm.ILlmRouter router, LlmRequest request)
        {
            try { return router.Complete(request, out _); }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("[DialogLLM] provider failed; keeping deterministic line. " + ex.GetType().Name + ": " + ex.Message);
                return new LlmResponse(string.Empty, null, 0);
            }
        }

    }
}

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
        private string DeterministicGreeting(
            string actorName, NpcSeedRecord npc, IReadOnlyList<AskAboutTopic> topics)
        {
            // F6/dialog variety ("npc benzer konuşma seçenekleri sunuyor"): the old line was the FIRST
            // topic answer — every same-role NPC opened identically. DFU recipe: a greeting matrix of
            // SOCIAL GROUP × TIME OF DAY, picked deterministically per (npc, day); then ~35% of the time a
            // REAL rumor rides along — a recent world event retold, or the nearest delve revealed (the DFU
            // 35% map-reveal made conversational).
            if (npc == null)
            {
                // PLAYTEST FIX: AUTHORED actors (no worldgen seed) exited here with a topic
                // answer before any acquaintance check — the second meeting sounded identical.
                var authored = _world?.Actors?.Records?.FirstOrDefault(
                    a => a != null && string.Equals(a.Name, actorName, System.StringComparison.Ordinal));
                if (authored != null && _world.NpcMemory != null
                    && _world.NpcMemory.TryGet(authored.Id, out var authoredMemory))
                    foreach (var known in authoredMemory.Events)
                        if (known.EventType == "met_player" && known.Timestamp.TotalMinutes < _world.Time.TotalMinutes)
                            return string.Format("{0} again — I remember you. Ask, and I will answer.",
                                string.IsNullOrWhiteSpace(known.SubjectId) ? "Traveller" : known.SubjectId);

                if (topics != null)
                    foreach (var t in topics)
                        if (t != null && !string.IsNullOrWhiteSpace(t.Answer))
                            return t.Answer.Trim();
                return string.IsNullOrWhiteSpace(actorName)
                    ? "Someone waits for you to speak."
                    : actorName + " waits for you to speak.";
            }

            int hour = (int)((_world.Time.TotalMinutes / GameTime.MinutesPerHour) % 24);
            int slot = hour < 6 ? 3 : hour < 12 ? 0 : hour < 18 ? 1 : 2; // day/evening/night/morning indexing below
            string[] pool = GreetingPool(SocialGroupFor(npc.Role), slot);
            long day = _world.Time.TotalMinutes / (24L * GameTime.MinutesPerHour);
            uint h = unchecked(((uint)npc.Id.Value * 2654435761u) ^ ((uint)day * 40503u) ^ 0x9E37u);
            string town = ResolveStartingSettlementName() ?? "this holding";
            string line = string.Format(pool[h % (uint)pool.Length], actorName, town);

            // PLAYTEST FIX ("benimle tanistigini hatirlamiyor"): the deterministic layer ALSO
            // greets acquaintances by NAME — with a slow or offline LLM this is the line the
            // player actually reads, and it must not sound like a stranger twice.
            if (_world?.NpcMemory != null && _world.NpcMemory.TryGet(new ActorId(npc.Id.Value), out var greetMemory))
                foreach (var known in greetMemory.Events)
                    if (known.EventType == "met_player" && known.Timestamp.TotalMinutes < _world.Time.TotalMinutes)
                    {
                        string[] againPool =
                        {
                            "{0} again! The road keeps bringing you back.",
                            "Well met once more, {0}.",
                            "Back so soon, {0}? {1} keeps its faces.",
                            "{0}! I remember you.",
                        };
                        line = string.Format(againPool[h % (uint)againPool.Length],
                            string.IsNullOrWhiteSpace(known.SubjectId) ? "traveller" : known.SubjectId, town);
                        break;
                    }

            if (((h >> 8) % 100) < 35)
            {
                string rumor = ComposeRumor(h);
                if (!string.IsNullOrEmpty(rumor)) line = line + " " + rumor;
            }
            return line;
        }

        // DFU social groups — tone per group, one line per time slot (day/evening/night/morning).
        private static int SocialGroupFor(EmberCrpg.Domain.Worldgen.NpcRole role)
        {
            switch (role)
            {
                case EmberCrpg.Domain.Worldgen.NpcRole.Merchant:
                case EmberCrpg.Domain.Worldgen.NpcRole.Innkeeper: return 1;
                case EmberCrpg.Domain.Worldgen.NpcRole.Priest:
                case EmberCrpg.Domain.Worldgen.NpcRole.Scholar: return 2;
                case EmberCrpg.Domain.Worldgen.NpcRole.Noble: return 3;
                case EmberCrpg.Domain.Worldgen.NpcRole.Outlaw: return 4;
                default: return 0; // farmers, guards, smiths, artisans — commoners
            }
        }

        private static readonly string[][] GreetingMatrix =
        {
            new[] { "\"Fair day to you, stranger. {1} keeps us busy.\"", "\"Long day behind me. Say what you need.\"", "\"You walk late. Honest folk are abed.\"", "\"Up with the sun, same as every day in {1}.\"" },
            new[] { "\"Welcome, welcome! Coin opens every door in {1}.\"", "\"Closing soon — but for you, I'll linger.\"", "\"Trade at this hour? Desperate... or rich.\"", "\"First customer of the morning brings luck!\"" },
            new[] { "\"The Flame keeps {1}, traveler. Peace upon you.\"", "\"Evening rites soon. Speak, but be brief.\"", "\"The night watch belongs to the faithful. Why are YOU awake?\"", "\"A blessed morning. The embers burned clean.\"" },
            new[] { "\"{0} acknowledges you. State your business in {1}.\"", "\"You interrupt my evening. Make it worth my while.\"", "\"Audiences at this hour are... irregular.\"", "\"Mornings are for petitions. You have one, I assume?\"" },
            new[] { "\"Keep your voice down. Daylight has eyes in {1}.\"", "\"Dusk's good for business. Yours or mine?\"", "\"Night work, is it? Now you speak my tongue.\"", "\"Mornings are for marks. Don't be one.\"" },
        };

        private static string[] GreetingPool(int group, int slot)
        {
            var row = GreetingMatrix[group];
            // lead with the slot's own line; keep two neighbours reachable so the same NPC still varies by day
            return new[] { row[slot], row[(slot + 1) % 4], row[(slot + 2) % 4] };
        }

        // A REAL rumor. F9 ("zindanı bulamadım"): the delve reveal now rides EVERY rumor roll — the old
        // coin-flip halved the DFU 35% map-reveal to ~17% effective. The dungeon name is SHOUTED so it
        // sticks; a world-event retelling rides along when one reads as a sentence.
        private string ComposeRumor(uint h)
        {
            // OYNANABILIRLIK: rumors used to quote the raw log verbatim ("They say actor_stepped
            // from:12,4...") — the living world spoke in debug strings. Now the last 32 events are
            // filtered to STORY-worthy kinds and narrated as sentences; the pick stays hash-deterministic.
            string eventLine = null;
            if ((h & 1) == 0)
            {
                var events = _world.Events?.Events;
                if (events != null && events.Count > 0)
                {
                    var narratable = new List<string>();
                    for (int i = events.Count - 1; i >= 0 && i >= events.Count - 32; i--)
                    {
                        var line = NarrateEvent(events[i]);
                        if (line != null) narratable.Add(line);
                    }
                    if (narratable.Count > 0)
                        eventLine = narratable[(int)(h % (uint)narratable.Count)];
                }
            }

            var row = NearestDungeonRow();
            // (NarrateEvent below maps event kinds to spoken lines.)
            if (!row.HasTarget) return eventLine;
            string delve = "And mind yourself — dark things stir at " + row.TargetName.ToUpperInvariant()
                + ", " + row.DistanceTiles + " tiles " + row.Direction + ".";
            return eventLine == null ? delve : eventLine + " " + delve;
        }

        private static string NarrateEvent(WorldEvent e)
        {
            if (e == null) return null;
            switch (e.Kind)
            {
                case WorldEventKind.NeedChanged:
                    return e.Reason != null && e.Reason.StartsWith("meal_eaten", System.StringComparison.Ordinal)
                        ? "The tavern table has been busy of late — folk are eating well."
                        : null;
                case WorldEventKind.WitnessRecorded:
                    return "Someone was set upon in the open street — people SAW it happen.";
                case WorldEventKind.GuardResponded:
                    return "The watch crossed steel with a prowler; there is blood on the cobbles.";
                case WorldEventKind.ShortageDetected:
                    return "Grain runs thin — the fields are being planted in a hurry.";
                case WorldEventKind.CaravanArrived:
                    return "A caravan rolled in off the trade road, wheels heavy.";
                case WorldEventKind.PriceChanged:
                    return "Market prices have shifted again; the merchants are muttering.";
                case WorldEventKind.FactionReputationChanged:
                    return "Word is the guilds' tempers have turned — friendships strain and mend.";
                case WorldEventKind.CombatResolved:
                    return "Blood was spilled not far from here, they say.";
                case WorldEventKind.PlantHarvested:
                    return "The harvest came in from the fields.";
                case WorldEventKind.ChronicleEvent:
                    if (e.Reason != null && e.Reason.Contains("festival"))
                        return "The festival is still on everyone's lips.";
                    if (e.Reason != null && e.Reason.Contains("caravan_surge"))
                        return "A grain caravan came through — the larders are full for once.";
                    if (e.Reason != null && e.Reason.Contains("border_dispute"))
                        return "The law and the merchants are at each other's throats again.";
                    return "The chroniclers wrote a new page this month, so folk whisper.";
                default:
                    return null;
            }
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

        // M3a: streaming twin - routes through the streaming overload when the router has one.
        private static LlmResponse CompleteLlmOrEmpty(
            EmberCrpg.Simulation.AiDm.ILlmRouter router, LlmRequest request, System.Action<string> onPartial)
        {
            try
            {
                if (onPartial != null && router is EmberCrpg.Simulation.AiDm.LlmRoutingService streamingRouter)
                    return streamingRouter.Complete(request, onPartial, out _);
                return router.Complete(request, out _);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("[DialogLLM] provider failed; keeping deterministic line. " + ex.GetType().Name + ": " + ex.Message);
                return new LlmResponse(string.Empty, null, 0);
            }
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

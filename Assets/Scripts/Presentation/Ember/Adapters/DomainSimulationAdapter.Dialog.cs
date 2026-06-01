// Why this file is intentionally long: dialog, Ask-About, and LLM reply routing remain a legacy partial awaiting staged extraction.
// REF-a (LEFT-019): the dialog / conversation / Ask-About concern of the DomainSimulationAdapter
// god-class, extracted to a partial. GetDialogSource, BeginConversation, greeting/topic LLM helpers,
// IDialogSource (GetCurrentLine/Topics/Portrait/SelectTopic), StyleDescriptor, SanitizeNpcLine.
// Zero behaviour change — methods moved verbatim; the partial shares all fields with the core file.
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
        public IDialogSource GetDialogSource(string actorName)
        {
            // Legacy/fallback path: callers that only have a display-name string. Try to upgrade the
            // name to the actor's STABLE id so we get identical resolution to the id overload; only
            // when no actor carries that name do we bind by raw name (ad-hoc / scene-authored label).
            var byName = _world.Actors?.Records.FirstOrDefault(
                a => string.Equals(a.Name, actorName, System.StringComparison.Ordinal));
            if (byName != null)
                return GetDialogSource(byName.Id);

            var npc = _world.NpcSeeds.FirstOrDefault(
                n => string.Equals(n.Name, actorName, System.StringComparison.Ordinal));
            BeginConversation(default, npc != null ? npc.Id : default, actorName, npc);
            return this;
        }

        // ----- IPlayerCommandSink (DLG-01) -----
        public IDialogSource GetDialogSource(ActorId id)
        {
            // DLG-01: resolve by the stable ActorId rather than by display-name string.
            if (id.IsEmpty || _world.Actors == null || !_world.Actors.TryGet(id, out var actor) || actor == null)
            {
                // A mismatch must be LOUD and must NOT silently drop the player into the shared
                // global topic menu (the old name-resolution bug). Surface an explicit empty state.
                _suppressGlobalTopicFallback = true;
                _activeDialogActor = string.Empty;
                _activeDialogActorId = default;
                _activeDialogNpcId = default;
                _currentDialogLine = "There is no one here to talk to.";
                _currentPortrait = "portrait_npc_placeholder";
                _isDialogThinking = false;
                _conversation = ConversationState.None;
                UnityEngine.Debug.LogWarning(
                    $"DLG-01: GetDialogSource({id}) found no actor in WorldState.Actors; " +
                    "refusing to fall back to global world topics.");
                return this;
            }

            // Recover the matching NpcSeed for generated NPCs. HydrateNpcs mints ActorIds as
            // GeneratedNpcActorOffset + NpcId.Value, so the seed is recoverable by subtracting the
            // offset. Authored slice actors (ids 1..5) have no seed and fall through to name match.
            NpcSeedRecord npc = null;
            if (id.Value >= GeneratedNpcActorOffset)
            {
                var npcId = new EmberCrpg.Domain.Worldgen.NpcId(id.Value - GeneratedNpcActorOffset);
                npc = _world.NpcSeeds.FirstOrDefault(n => n.Id.Equals(npcId));
            }
            npc ??= _world.NpcSeeds.FirstOrDefault(
                n => string.Equals(n.Name, actor.Name, System.StringComparison.Ordinal));

            BeginConversation(actor.Id, npc != null ? npc.Id : default, actor.Name, npc);
            return this;
        }

        /// <summary>
        /// DLG-01/EMB-020/EMB-045: single funnel that binds the active speaker, portrait, and the
        /// per-actor topic set into the one <see cref="ConversationState"/> the dialog surface reads.
        /// When <paramref name="npc"/> is non-null the topics come from THEIR role + faction (not the
        /// global menu) and a persona greeting is fired; otherwise the shared world topics back the
        /// conversation. Both <see cref="GetDialogSource(string)"/> and
        /// <see cref="GetDialogSource(ActorId)"/> route through here so the two paths can never drift.
        /// </summary>
        private void BeginConversation(ActorId actorId, NpcId npcId, string actorName, NpcSeedRecord npc)
        {
            _suppressGlobalTopicFallback = false;
            _activeDialogActorId = actorId;
            _activeDialogNpcId = npcId;
            _activeDialogActor = actorName ?? string.Empty;
            _currentPortrait = ResolveConversationPortraitKey(npc, _activeDialogActor);

            if (npc != null)
            {
                var perActorTopics = NpcTopicCatalog.For(npc.Role, npc.Faction.Value, _world.Topics);
                _conversation = new ConversationState(
                    _activeDialogActorId,
                    _activeDialogNpcId,
                    _activeDialogActor,
                    _currentPortrait,
                    perActorTopics);

                // BUG-DIALOG-EMPTY: seed a DETERMINISTIC opening line synchronously, up front, so the
                // panel always renders a real sentence even when native inference returns nothing (no
                // model bytes). Prefer the per-actor AskAbout/persona answer (e.g. a Guard's "watch"
                // topic => "Sentinel Rook keeps count of every footstep, including yours."); only fall
                // back to a role+name greeting when no topic answer is available. The async LLM call
                // below REPLACES this line only if it yields a non-empty (non-whitespace) response.
                _currentDialogLine = DeterministicGreeting(_activeDialogActor, npc, perActorTopics);

                _ = GenerateNpcGreetingAsync(npc);
            }
            else
            {
                // No seed record (ad-hoc / authored slice actor): fall back to the shared world topics,
                // still funneled through the one ConversationState model so GetTopics/SelectTopic have
                // a single source of truth.
                _conversation = new ConversationState(
                    _activeDialogActorId,
                    _activeDialogNpcId,
                    _activeDialogActor,
                    _currentPortrait,
                    _world.Topics ?? new List<AskAboutTopic>());

                // BUG-DIALOG-EMPTY: seed a deterministic, non-empty opening line first so the panel
                // is never blank. Lead with a shared world topic answer when present.
                _currentDialogLine = DeterministicGreeting(_activeDialogActor, npc, _world.Topics);

                // LLM-NOT-FIRING fix: the playable scenes use authored actors with no NpcSeed, so the
                // seeded greeting path above never ran for them and the player ALWAYS saw the
                // deterministic line. Fire a name-based LLM greeting here too; it replaces the
                // deterministic line only if the local model returns real text (else the line stays).
                _ = GenerateAdHocGreetingAsync(_activeDialogActor);
            }
        }

        private static string ResolveConversationPortraitKey(NpcSeedRecord npc, string actorName)
        {
            if (npc != null && !string.IsNullOrWhiteSpace(npc.PortraitAssetPath))
                return npc.PortraitAssetPath;

            if (npc != null)
            {
                switch (npc.Role)
                {
                    case NpcRole.Merchant: return "merchant";
                    case NpcRole.Scholar: return "sage";
                    case NpcRole.Priest: return "sage";
                    case NpcRole.Guard: return "knight";
                    case NpcRole.Noble: return "knight";
                    case NpcRole.Outlaw: return "warrior";
                    case NpcRole.Artisan: return "blacksmith";
                    case NpcRole.Farmer: return "innkeeper";
                }
            }

            var lower = (actorName ?? string.Empty).ToLowerInvariant();
            if (lower.Contains("merchant")) return "merchant";
            if (lower.Contains("sage") || lower.Contains("priest")) return "sage";
            if (lower.Contains("guard") || lower.Contains("warden") || lower.Contains("knight")) return "knight";
            if (lower.Contains("blacksmith") || lower.Contains("smith") || lower.Contains("artisan")) return "blacksmith";
            if (lower.Contains("innkeeper") || lower.Contains("farmer")) return "innkeeper";
            if (lower.Contains("warrior") || lower.Contains("outlaw")) return "warrior";
            return "blacksmith";
        }

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

        private async Task GenerateNpcGreetingAsync(NpcSeedRecord npc)
        {
            var router = ForgeLocator.LlmRouter;
            if (router == null) return;

            _isDialogThinking = true;
            var request = new LlmRequest(
                "npc_greeting",
                "npc:" + npc.Id.Value,
                null,
                100,
                npc.Id.Value,
                $"You are {npc.Name}, a {npc.Role} in a {StyleDescriptor()} world. Greet the player character briefly in character.",
                new List<string>()
            );

            // EMB-007: only the blocking LLM call runs off the main thread. The shared-state
            // mutations are applied AFTER the await, which resumes on Unity's main-thread
            // SynchronizationContext — previously _currentDialogLine / _isDialogThinking were
            // written from the worker thread, racing the main-thread dialog reader.
            var response = await Task.Run(() => router.Complete(request, out _));
            // DET-02: apply the result on the main-thread tick, not on the await's resumption thread.
            _mainThreadApply.Enqueue(() =>
            {
                // DIAG (LLM-not-firing): surface exactly what the native model returned so a runtime
                // log reveals whether inference is empty (len<=0 -> llama.cpp produced no tokens) or
                // working. Remove once the local LLM is confirmed generating in-game.
                UnityEngine.Debug.Log($"[NpcGreeting] npc={npc.Name} llm-len={(response?.Text?.Length ?? -1)} " +
                    $"used={(response != null && !string.IsNullOrWhiteSpace(response.Text))}");
                // BUG-DIALOG-EMPTY: a whitespace-only inference result (the native model returning no
                // real bytes) used to pass the IsNullOrEmpty guard, Trim() to "", and BLANK the good
                // deterministic greeting. Require non-whitespace before replacing; otherwise keep the
                // deterministic line that BeginConversation already set.
                // BUG-DIALOG-TURNLEAK: also strip any echoed chat-turn scaffolding; only a non-empty
                // cleaned line replaces the deterministic greeting.
                var greeting = SanitizeNpcLine(response?.Text);
                if (!string.IsNullOrEmpty(greeting))
                    _currentDialogLine = greeting;
                _isDialogThinking = false;
            });
        }

        // LLM-NOT-FIRING fix: name-based greeting for authored/ad-hoc actors (no NpcSeed), so the
        // playable scene cast also gets a local-LLM line instead of only the deterministic one. Same
        // off-main-thread + main-thread-apply pattern as GenerateNpcGreetingAsync.
        private async Task GenerateAdHocGreetingAsync(string actorName)
        {
            var router = ForgeLocator.LlmRouter;
            if (router == null || string.IsNullOrEmpty(actorName)) return;

            _isDialogThinking = true;
            // Stable per-name seed (string.GetHashCode is process-randomised, so fold the chars via FNV).
            ulong seed = 1469598103934665603UL;
            foreach (var ch in actorName) { seed ^= ch; seed *= 1099511628211UL; }

            var request = new LlmRequest(
                "npc_greeting",
                "npc:" + actorName,
                null,
                100,
                seed,
                $"You are {actorName}, a character in a {StyleDescriptor()} world. Greet the player character briefly, in character.",
                new List<string>()
            );

            var response = await Task.Run(() => router.Complete(request, out _));
            _mainThreadApply.Enqueue(() =>
            {
                UnityEngine.Debug.Log($"[NpcGreeting-adhoc] actor={actorName} llm-len={(response?.Text?.Length ?? -1)} " +
                    $"used={(response != null && !string.IsNullOrWhiteSpace(response.Text))}");
                // BUG-DIALOG-TURNLEAK: strip echoed chat-turn scaffolding; only a non-empty cleaned line
                // replaces the deterministic greeting.
                var greeting = SanitizeNpcLine(response?.Text);
                if (!string.IsNullOrEmpty(greeting))
                    _currentDialogLine = greeting;
                _isDialogThinking = false;
            });
        }

        // Ninth-pass FOUNDATION worldgen: SeedWorld now runs the deterministic
// WorldgenService (Assets/Scripts/Simulation/Worldgen/) so the
        // mood/calling/start tuple from the main-menu wizard actually
        // produces a ~50-region, ~200-settlement, ~750-NPC world instead
        // of vanishing into a log line. The generated bundle is held on
        // the adapter so subsequent reads (UI panels, save/load) can
        // inspect it through the IDomainSimulationAdapter handle.
        public EmberCrpg.Simulation.Worldgen.GeneratedWorld GeneratedWorld { get; private set; }

        /// <summary>The starting region selected from the wizard's start-location string. Empty when no world has been seeded.</summary>
        public RegionId StartingRegion { get; private set; }
        public SettlementId StartingSettlement { get; private set; }
        public FactionId StartingFaction { get; private set; }


        // ----- IDialogSource -----
        // Mami: _isDialogThinking surfaces a "thinking …" placeholder while
        // the NPC LLM (or DM ConsultFate) is still generating, so the panel
        // never shows a stale or empty line during background inference.
        public string GetCurrentLine()
        {
            if (_isDialogThinking)
                return string.IsNullOrEmpty(_activeDialogActor) ? "Thinking…" : _activeDialogActor + " thinks…";
            // BUG-DIALOG-EMPTY: final guarantee — the dialog panel renders this string verbatim into a
            // label, so it must NEVER be null/empty/whitespace. BeginConversation/SelectTopic always
            // set a deterministic line, but guard here too so no future caller (or a read before any
            // conversation begins) can surface a blank box.
            return string.IsNullOrWhiteSpace(_currentDialogLine) ? "..." : _currentDialogLine;
        }
        public bool IsThinking => _isDialogThinking;
        public string GetPortraitName() => _currentPortrait;
        // EMB-045: surface THIS actor's topics (role/faction-derived), not the global menu. Falls back
        // to the world list only when no conversation is active.
        public IReadOnlyList<string> GetTopics()
        {
            if (_conversation != null && _conversation.Topics.Count > 0)
                return _conversation.Topics.Select(t => t.Id).ToList();
            // DLG-01: when an id-keyed lookup missed, do NOT leak the global world topics — the
            // player is looking at "no one"; an empty topic list is the honest answer.
            if (_suppressGlobalTopicFallback)
                return new List<string>();
            return _world.Topics?.Select(t => t.Id).ToList() ?? new List<string>();
        }

        public void SelectTopic(string topicId)
        {
            // Audit (Phase 12 production wire, 2026-05-27): previously only set a
            // deterministic placeholder. Now (a) sets the deterministic fallback
            // from the seeded AskAboutTopic so the panel always renders SOMETHING
            // useful even with the LLM offline, (b) appends the WorldEvent
            // (unchanged), and (c) fires the async LLM topic-answer via
            // ForgeLocator.LlmRouter. The async path replaces _currentDialogLine
            // on success; _isDialogThinking gates the "thinking…" indicator.
            if (string.IsNullOrEmpty(topicId)) return;

            // EMB-045: answer from THIS actor's topic set first; only fall back to the world list. An
            // actor never answers for a topic they did not offer.
            var topic = _conversation?.FindTopic(topicId)
                ?? _world.Topics?.FirstOrDefault(t => string.Equals(t.Id, topicId, System.StringComparison.Ordinal));
            _currentDialogLine = !string.IsNullOrEmpty(topic?.Answer)
                ? topic.Answer
                : $"{_activeDialogActor} considers \"{topicId}\".";

            ActorRecord actor = null;
            if (_conversation != null && !_conversation.ActorId.IsEmpty)
                _world.Actors.TryGet(_conversation.ActorId, out actor);
            actor ??= _world.Actors.Records.FirstOrDefault(
                a => string.Equals(a.Name, _activeDialogActor, System.StringComparison.Ordinal));
            if (actor != null && _world.Events != null)
            {
                _world.Events.Append(new WorldEvent(
                    _world.Time,
                    WorldEventKind.ActorTalked,
                    actor.Id,
                    default,
                    $"topic_selected id:{topicId}"));
            }

            // Live LLM topic answer (Phase 12 production wire). Authored scene actors have no
            // NpcSeed, so route them through the ad-hoc (name-based) path — otherwise selecting a
            // topic ALWAYS showed the deterministic answer (the LLM-NOT-FIRING bug, same as greetings).
            // E7-005: IDENTITY is id-first — resolve the NPC by the conversation's stable NpcId. The
            // name match below is an explicit LEGACY fallback that only fires when no stable id is present
            // (pre-E7-004 authored actors). It can mis-resolve duplicate display-names, so it is being
            // eliminated by the E7-004 authored-scene actor-ID migration, after which every conversation
            // carries a real NpcId/ActorId and this branch is dead.
            NpcSeedRecord npc = null;
            if (_conversation != null && !_conversation.NpcId.IsEmpty)
                npc = _world.NpcSeeds.FirstOrDefault(n => n.Id.Equals(_conversation.NpcId));
            npc ??= _world.NpcSeeds.FirstOrDefault(
                n => string.Equals(n.Name, _activeDialogActor, System.StringComparison.Ordinal)); // LEGACY name fallback
            if (npc != null)
                _ = GenerateNpcTopicAnswerAsync(npc, topicId, topic);
            else
                _ = GenerateAdHocTopicAnswerAsync(_activeDialogActor, topicId, topic);
        }

        private async Task GenerateNpcTopicAnswerAsync(NpcSeedRecord npc, string topicId, AskAboutTopic topic)
        {
            var router = ForgeLocator.LlmRouter;
            if (router == null) return;

            _isDialogThinking = true;
            var topicLabel = !string.IsNullOrEmpty(topic?.Label) ? topic.Label : topicId;

            var request = new LlmRequest(
                "npc_topic_answer",
                "npc:" + npc.Id.Value + ":topic:" + topicId,
                null,
                180,
                npc.Id.Value,
                $"You are {npc.Name}, a {npc.Role} in a {StyleDescriptor()} world. The player asks you about \"{topicLabel}\". Answer briefly in character (1-2 sentences). Reference what you know; do not invent new quests.",
                new List<string>());

            // EMB-007/DET-02: blocking LLM call off-thread; shared-state mutations are enqueued and
            // applied on the deterministic main-thread tick (not on the await's resumption thread).
            var response = await Task.Run(() => router.Complete(request, out _));
            _mainThreadApply.Enqueue(() =>
            {
                // BUG-DIALOG-EMPTY: same whitespace guard as the greeting path — never overwrite the
                // deterministic topic answer with an empty/whitespace inference result.
                // BUG-DIALOG-TURNLEAK: also strip echoed chat-turn scaffolding; only a non-empty cleaned
                // line replaces the deterministic topic answer.
                var answer = SanitizeNpcLine(response?.Text);
                if (!string.IsNullOrEmpty(answer))
                    _currentDialogLine = answer;
                _isDialogThinking = false;
            });
        }

        // LLM-NOT-FIRING fix (topic answers): name-based answer for authored/ad-hoc actors with no
        // NpcSeed, so picking an Ask-About topic yields a real local-LLM answer instead of only the
        // deterministic one. Mirrors GenerateNpcTopicAnswerAsync.
        private async Task GenerateAdHocTopicAnswerAsync(string actorName, string topicId, AskAboutTopic topic)
        {
            var router = ForgeLocator.LlmRouter;
            if (router == null || string.IsNullOrEmpty(actorName)) return;

            _isDialogThinking = true;
            var topicLabel = !string.IsNullOrEmpty(topic?.Label) ? topic.Label : topicId;
            ulong seed = 1469598103934665603UL;
            foreach (var ch in actorName) { seed ^= ch; seed *= 1099511628211UL; }
            foreach (var ch in topicId ?? string.Empty) { seed ^= ch; seed *= 1099511628211UL; }

            var request = new LlmRequest(
                "npc_topic_answer",
                "npc:" + actorName + ":topic:" + topicId,
                null,
                180,
                seed,
                $"You are {actorName}, a character in a {StyleDescriptor()} world. The player asks you about \"{topicLabel}\". Answer briefly in character (1-2 sentences). Reference what you know; do not invent new quests.",
                new List<string>());

            var response = await Task.Run(() => router.Complete(request, out _));
            _mainThreadApply.Enqueue(() =>
            {
                UnityEngine.Debug.Log($"[NpcTopic-adhoc] actor={actorName} topic={topicId} " +
                    $"llm-len={(response?.Text?.Length ?? -1)} used={(response != null && !string.IsNullOrWhiteSpace(response.Text))}");
                // BUG-DIALOG-TURNLEAK: strip echoed chat-turn scaffolding; only a non-empty cleaned line
                // replaces the deterministic topic answer.
                var answer = SanitizeNpcLine(response?.Text);
                if (!string.IsNullOrEmpty(answer))
                    _currentDialogLine = answer;
                _isDialogThinking = false;
            });
        }
    }
}

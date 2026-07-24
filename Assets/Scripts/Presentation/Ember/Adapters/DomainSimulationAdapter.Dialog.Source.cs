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
        // ----- IDialogSource -----
        // Mami: _isDialogThinking surfaces a "thinking …" placeholder while
        // the NPC LLM (or DM ConsultFate) is still generating, so the panel
        // never shows a stale or empty line during background inference.
        /// <summary>M3b: actor id when we have one; FNV of the name for authored speakers.</summary>
        public ulong VoiceKey
            => !_activeDialogActorId.IsEmpty
                ? _activeDialogActorId.Value
                : EmberCrpg.Simulation.AiDm.NpcVoiceSignatureService.VoiceKeyFor(_activeDialogActor);

        public string GetCurrentLine()
        {
            if (_isDialogThinking)
            {
                // M3a: once tokens start flowing the placeholder yields to the LIVE text.
                if (!string.IsNullOrWhiteSpace(_streamingPartialLine))
                    return _streamingPartialLine + " …";
                return string.IsNullOrEmpty(_activeDialogActor) ? "Thinking…" : _activeDialogActor + " thinks…";
            }
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
            var live = ActiveOptions;
            if (live != null)
            {
                var topics = new List<string>(live);
                AppendCompanionTopics(topics);
                return topics; // W23: consumed options are gone, followups have grown in
            }
            if (_conversation != null && _conversation.Topics.Count > 0)
            {
                var topics = _conversation.Topics.Select(t => t.Id).ToList();
                AppendCompanionTopics(topics);
                return topics;
            }
            // DLG-01: when an id-keyed lookup missed, do NOT leak the global world topics — the
            // player is looking at "no one"; an empty topic list is the honest answer.
            if (_suppressGlobalTopicFallback)
                return new List<string>();
            return _world.Topics?.Select(t => t.Id).ToList() ?? new List<string>();
        }

        // REVIEW FIX (stale-reply race): _conversationSerial only changes per CONVERSATION, so
        // two requests in one conversation both passed the guard and the SLOWER inference won,
        // overwriting the newer answer and lying with the thinking flag. Each request now takes
        // a serial; only the LATEST may publish its line or clear the indicator.
        private int _dialogRequestSerial;

        // CAN SUYU V2.1: the memory the NPC carries into the LLM prompt. Same canonical
        // source Gate9 proves (NpcMemoryLlmEnvelope.RecallLines) - witnessed attacks, past
        // conversations, trades: all of it reaches the tongue.
        private List<string> RecallDialogMemory(ulong npcId)
            => EmberCrpg.Simulation.AiDm.NpcMemoryLlmEnvelope.RecallLines(_world, new ActorId(npcId), 8);

        private List<string> RecallDialogMemoryByName(string actorName)
        {
            var actor = string.IsNullOrEmpty(actorName) ? null : _world?.Actors?.Records?.FirstOrDefault(
                a => a != null && string.Equals(a.Name, actorName, System.StringComparison.Ordinal));
            return actor == null
                ? new List<string>()
                : EmberCrpg.Simulation.AiDm.NpcMemoryLlmEnvelope.RecallLines(_world, actor.Id, 8);
        }

        // V2.1: conversations are EXPERIENCES - they write real memory the next reply recalls.
        private void RecordConversationMemory(ActorRecord actor, string topicId)
        {
            if (actor == null || _world == null || string.IsNullOrEmpty(topicId)) return;
            _world.NpcMemory ??= new EmberCrpg.Domain.Memory.NpcMemoryStore();
            var memory = _world.NpcMemory.GetOrCreate(actor.Id);
            memory.MarkDialogueSeen(topicId);
            var player = _world.Actors?.Records?.FirstOrDefault(a => a != null && a.Role == ActorRole.Player);
            memory.RecordEvent(new EmberCrpg.Domain.Memory.InteractionEvent(
                _world.Time, "player_asked", player?.Id ?? default, topicId, string.Empty, 0, actor.Position));
        }

        // PLAYTEST FIX: the LLM is TOLD it knows this traveller — name included — so replies
        // stop being stranger-generic ("hep adini soyleyip generic cevaplar").
        internal string AcquaintanceSuffix(ulong npcId)
        {
            if (_world?.NpcMemory == null || !_world.NpcMemory.TryGet(new ActorId(npcId), out var memory))
                return string.Empty;
            foreach (var known in memory.Events)
                if (known.EventType == "met_player" && known.Timestamp.TotalMinutes < _world.Time.TotalMinutes)
                    return $" You have spoken with this traveller before — their name is {known.SubjectId}; greet them as an acquaintance, never introduce yourself again.";
            return string.Empty;
        }

        // M3b.3: YOUR voice - derived from creation (name + class) like a forge portrait -
        // reads the question you clicked or typed; the NPC answers in theirs.
        internal void SpeakPlayerQuestion(string questionText)
        {
            if (string.IsNullOrWhiteSpace(questionText)) return;
            questionText = NaturalQuestion(questionText);
            var player = EmberCrpg.Simulation.Living.CompanionService.FindPlayer(_world);
            if (player == null) return;
            EmberCrpg.Presentation.Ember.Audio.SpeechDirector.FeedFinal(
                EmberCrpg.Simulation.AiDm.PlayerVoiceService.PlayerVoiceKey(player.Name, _world.PlayerClassName),
                questionText);
        }

        // W23 DIALOG STATE MACHINE v1 (GemRB DLG skeleton + LLM content): per-NPC LIVE option
        // list - picked options are CONSUMED, the answer's FOLLOWUPS grow new ones, and the
        // memo SURVIVES farewell so reopening resumes where you left off.
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> _liveOptions
            = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();

        private string ActiveMemoKey
            => !_activeDialogActorId.IsEmpty ? "id:" + _activeDialogActorId.Value : "name:" + _activeDialogActor;

        private System.Collections.Generic.List<string> ActiveOptions
        {
            get { _liveOptions.TryGetValue(ActiveMemoKey, out var options); return options; }
        }

        internal void EnsureLiveOptions(System.Collections.Generic.IEnumerable<string> seedTopicIds)
        {
            if (_liveOptions.ContainsKey(ActiveMemoKey)) return; // resume: keep the lived state
            _liveOptions[ActiveMemoKey] = new System.Collections.Generic.List<string>(seedTopicIds
                ?? System.Linq.Enumerable.Empty<string>());
        }

        private void ConsumeOption(string picked)
        {
            var options = ActiveOptions;
            options?.RemoveAll(o => string.Equals(o, picked, System.StringComparison.Ordinal));
        }

        private void AbsorbFollowups(System.Collections.Generic.List<string> followups)
        {
            var options = ActiveOptions;
            if (options == null || followups == null) return;
            foreach (var question in followups)
                if (!options.Contains(question) && options.Count < 6)
                    options.Add(question);
        }

        internal const string FollowupsInstruction =
            " End with one final line exactly like: FOLLOWUPS: first question | second question | third question" +
            " - three short in-character questions the traveller might naturally ask you NEXT.";

        /// <summary>Split "answer ... FOLLOWUPS: q1 | q2 | q3" into (answer, questions).</summary>
        internal static (string Body, System.Collections.Generic.List<string> Followups) SplitFollowups(string answer)
        {
            var none = (answer, (System.Collections.Generic.List<string>)null);
            if (string.IsNullOrEmpty(answer)) return none;
            int at = answer.LastIndexOf("FOLLOWUPS:", System.StringComparison.OrdinalIgnoreCase);
            if (at < 0) return none;
            var body = answer.Substring(0, at).TrimEnd();
            var list = new System.Collections.Generic.List<string>();
            foreach (var raw in answer.Substring(at + 10).Split('|'))
            {
                var q = raw.Trim().TrimStart('-', '*', ' ').Trim();
                if (q.Length > 4 && list.Count < 3) list.Add(q);
            }
            return (body.Length > 0 ? body : answer, list.Count > 0 ? list : null);
        }

        // TTS ('sadece gate diyor'): menu labels become sentences a person would actually say.
        internal static string NaturalQuestion(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return label;
            var trimmed = label.Trim();
            if (trimmed.StartsWith("Ask about Companion", System.StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("companion_join", System.StringComparison.OrdinalIgnoreCase))
                return "Will you travel with me?";
            if (trimmed.StartsWith("companion_leave", System.StringComparison.OrdinalIgnoreCase))
                return "It is time we parted ways.";
            if (trimmed.StartsWith("Ask about ", System.StringComparison.OrdinalIgnoreCase))
                return "What can you tell me about " + trimmed.Substring(10).TrimEnd('.', '…') + "?";
            return trimmed;
        }

        // PLAYTEST FIX: deterministic seeds made every repeat of a question word-identical.
        private int NextAskCount(string askKey)
        {
            _topicAskCounts.TryGetValue(askKey, out var asked);
            _topicAskCounts[askKey] = asked + 1;
            return asked;
        }

        private static string RepeatAskSuffix(int askedBefore)
            => askedBefore <= 0
                ? string.Empty
                : " You have answered this exact question before - vary the phrasing and add one NEW detail this time.";

        // PLAYTEST FIX ("ismimizi soruyor ama bilmiyor"): the model is told who it talks to,
        // so a typed 'I am X' lands on ground it already shares with the simulation.
        internal string PlayerContextSuffix()
        {
            var player = EmberCrpg.Simulation.Living.CompanionService.FindPlayer(_world);
            return player == null ? string.Empty : $" The player character's name is {player.Name}.";
        }

        // M3a: a mid-stream partial is USER-VISIBLE - cut at anti-prompt echoes before they flash.
        private static string TrimStreamPartial(string partial)
        {
            if (string.IsNullOrEmpty(partial)) return string.Empty;
            int cut = partial.IndexOf("User:", System.StringComparison.Ordinal);
            int cutMemory = partial.IndexOf("Memory", System.StringComparison.Ordinal);
            if (cutMemory >= 0 && (cut < 0 || cutMemory < cut)) cut = cutMemory;
            if (cut >= 0) partial = partial.Substring(0, cut);
            return partial.TrimStart();
        }

        // M2: a companion SPEAKS like one — the persona suffix reframes the LLM voice, and
        // the recalled shared memories (already in the prompt turns) gain their meaning.
        internal string CompanionPersonaSuffix(ulong npcId)
            => EmberCrpg.Simulation.Living.CompanionService.IsCompanion(_world, new ActorId(npcId))
                ? " You are travelling WITH the player as their trusted companion — speak with the familiarity of the shared road."
                : string.Empty;

        // V3 YOLDAŞ: recruiting happens IN conversation — a civilian you are talking to can
        // be asked to travel with you; a companion can be released. Topic ids double as the
        // player-facing labels (the panel renders topic ids verbatim).
        private const string CompanionJoinTopic = "companion_join: Travel with me";
        private const string CompanionLeaveTopic = "companion_leave: Part ways";

        private void AppendCompanionTopics(List<string> topics)
        {
            if (_conversation == null || _conversation.ActorId.IsEmpty) return;
            if (!_world.Actors.TryGet(_conversation.ActorId, out var actor) || actor == null) return;
            if (actor.Role == ActorRole.Enemy || actor.Role == ActorRole.Player) return;
            if (EmberCrpg.Simulation.Living.CompanionService.IsCompanion(_world, actor.Id))
                topics.Add(CompanionLeaveTopic);
            else if (_world.CompanionIds.Count < EmberCrpg.Simulation.Living.CompanionService.MaxCompanions)
                topics.Add(CompanionJoinTopic);
        }

        private bool TryHandleCompanionTopic(string topicId)
        {
            if (topicId != CompanionJoinTopic && topicId != CompanionLeaveTopic) return false;
            if (_conversation == null || _conversation.ActorId.IsEmpty) return true;
            if (!_world.Actors.TryGet(_conversation.ActorId, out var actor) || actor == null) return true;

            if (topicId == CompanionJoinTopic)
                _currentDialogLine = EmberCrpg.Simulation.Living.CompanionService.TryRecruit(_world, actor.Id)
                    ? $"{actor.Name} shoulders their pack. \"Lead on, then.\""
                    : $"{actor.Name} shakes their head. \"Not now — you are too far, or your company is full.\"";
            else
                _currentDialogLine = EmberCrpg.Simulation.Living.CompanionService.TryDismiss(_world, actor.Id)
                    ? $"{actor.Name} nods slowly. \"It was a good road. Find me if you need me.\""
                    : $"{actor.Name} looks puzzled — they were not travelling with you.";
            return true;
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
            if (TryHandleCompanionTopic(topicId)) return;
            if (TryHandleQuestInteractionTopic(topicId)) return;
            ConsumeOption(topicId); // W23: a picked bubble pops

            // W23: a grown FOLLOWUP is a QUESTION, not a catalog id - route it through the
            // free-text path (same LLM, same memory) instead of pretending it is a topic.
            bool isCatalogTopic = _conversation?.FindTopic(topicId) != null
                || (_world.Topics?.Any(t => string.Equals(t.Id, topicId, System.StringComparison.Ordinal)) ?? false);
            if (!isCatalogTopic && topicId.Contains("?"))
            {
                AskFreeText(topicId);
                return;
            }

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
                RecordConversationMemory(actor, topicId);
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

        public void AskFreeText(string question)
        {
            var trimmed = question == null ? string.Empty : question.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            var topicId = "free_text:" + trimmed;
            {
                ActorRecord freeTextActor = null;
                if (_conversation != null && !_conversation.ActorId.IsEmpty)
                    _world.Actors.TryGet(_conversation.ActorId, out freeTextActor);
                freeTextActor ??= _world.Actors.Records.FirstOrDefault(
                    a => a != null && string.Equals(a.Name, _activeDialogActor, System.StringComparison.Ordinal));
                RecordConversationMemory(freeTextActor, topicId);
            }
            var floor = string.IsNullOrWhiteSpace(_activeDialogActor)
                ? $"Someone considers your question: \"{trimmed}\"."
                : $"{_activeDialogActor} considers your question: \"{trimmed}\".";
            var syntheticTopic = new AskAboutTopic(topicId, trimmed, floor);
            _currentDialogLine = floor;

            NpcSeedRecord npc = null;
            if (_conversation != null && !_conversation.NpcId.IsEmpty)
                npc = _world.NpcSeeds.FirstOrDefault(n => n.Id.Equals(_conversation.NpcId));
            npc ??= _world.NpcSeeds.FirstOrDefault(
                n => string.Equals(n.Name, _activeDialogActor, System.StringComparison.Ordinal));

            if (npc != null)
                _ = GenerateNpcTopicAnswerAsync(npc, topicId, syntheticTopic);
            else
                _ = GenerateAdHocTopicAnswerAsync(_activeDialogActor, topicId, syntheticTopic);
        }

    }
}

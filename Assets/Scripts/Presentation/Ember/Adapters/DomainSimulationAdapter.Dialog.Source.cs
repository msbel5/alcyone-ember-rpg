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

        public void AskFreeText(string question)
        {
            var trimmed = question == null ? string.Empty : question.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            var topicId = "free_text:" + trimmed;
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

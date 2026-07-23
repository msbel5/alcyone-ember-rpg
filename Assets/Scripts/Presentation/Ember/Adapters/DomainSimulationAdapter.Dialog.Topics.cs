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
        private async Task GenerateNpcTopicAnswerAsync(NpcSeedRecord npc, string topicId, AskAboutTopic topic)
        {
            var router = ForgeLocator.LlmRouter;
            if (router == null) return;

            _isDialogThinking = true;
            _streamingPartialLine = string.Empty; // M3a: never leak the previous answer's stream
            int gen = _conversationSerial;
            int req = ++_dialogRequestSerial; // REVIEW FIX: latest-request-wins ordering
            var topicLabel = !string.IsNullOrEmpty(topic?.Label) ? topic.Label : topicId;

            int asked = NextAskCount("id:" + npc.Id.Value + "|" + topicId);
            var request = new LlmRequest(
                "npc_topic_answer",
                "npc:" + npc.Id.Value + ":topic:" + topicId,
                null,
                180, // PLAYTEST REVERT ("96da sadece considers your question diyor"): quality beats latency; STREAMING is the real latency fix

                npc.Id.Value + ((ulong)asked * 0x9E3779B97F4A7C15UL),
                $"You are {npc.Name}, a {npc.Role} in a {StyleDescriptor()} world. The player asks you about \"{topicLabel}\". Answer briefly in character (1-2 sentences). Reference what you know; do not invent new quests. The conversation log below is what you personally remember - witnessed events, past talks, trades - use it when it bears on the question." + CompanionPersonaSuffix(npc.Id.Value) + AcquaintanceSuffix(npc.Id.Value) + PlayerContextSuffix() + RepeatAskSuffix(asked),
                RecallDialogMemory(npc.Id.Value));

            // EMB-007/DET-02: blocking LLM call off-thread; shared-state mutations are enqueued and
            // applied on the deterministic main-thread tick (not on the await's resumption thread).
            // M3a: partials arrive on the WORKER thread - marshal through the apply queue and
            // re-check the serials so a superseded request can never paint the screen.
            System.Action<string> onPartial = partialText => _mainThreadApply.Enqueue(() =>
            {
                if (gen != _conversationSerial || req != _dialogRequestSerial) return;
                _streamingPartialLine = TrimStreamPartial(partialText);
            });
            var response = await Task.Run(() => CompleteLlmOrEmpty(router, request, onPartial));
            _mainThreadApply.Enqueue(() =>
            {
                if (gen != _conversationSerial) return;   // a newer conversation superseded this — drop the stale reply
                if (req != _dialogRequestSerial) return;  // a newer REQUEST superseded this — drop the stale reply
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
            _streamingPartialLine = string.Empty; // M3a: never leak the previous answer's stream
            int gen = _conversationSerial;
            int req = ++_dialogRequestSerial; // REVIEW FIX: latest-request-wins ordering
            var topicLabel = !string.IsNullOrEmpty(topic?.Label) ? topic.Label : topicId;
            int asked = NextAskCount("name:" + actorName + "|" + topicId);
            ulong seed = 1469598103934665603UL;
            foreach (var ch in actorName) { seed ^= ch; seed *= 1099511628211UL; }
            foreach (var ch in topicId ?? string.Empty) { seed ^= ch; seed *= 1099511628211UL; }
            seed += (ulong)asked * 2654435761UL; // repeats deserve fresh dice

            var request = new LlmRequest(
                "npc_topic_answer",
                "npc:" + actorName + ":topic:" + topicId,
                null,
                180, // PLAYTEST REVERT ("96da sadece considers your question diyor"): quality beats latency; STREAMING is the real latency fix

                seed,
                $"You are {actorName}, a character in a {StyleDescriptor()} world. The player asks you about \"{topicLabel}\". Answer briefly in character (1-2 sentences). Reference what you know; do not invent new quests." + PlayerContextSuffix() + RepeatAskSuffix(asked),
                RecallDialogMemoryByName(actorName));

            // M3a: partials arrive on the WORKER thread - marshal through the apply queue and
            // re-check the serials so a superseded request can never paint the screen.
            System.Action<string> onPartial = partialText => _mainThreadApply.Enqueue(() =>
            {
                if (gen != _conversationSerial || req != _dialogRequestSerial) return;
                _streamingPartialLine = TrimStreamPartial(partialText);
            });
            var response = await Task.Run(() => CompleteLlmOrEmpty(router, request, onPartial));
            _mainThreadApply.Enqueue(() =>
            {
                if (gen != _conversationSerial) return;   // a newer conversation superseded this — drop the stale reply
                if (req != _dialogRequestSerial) return;  // a newer REQUEST superseded this — drop the stale reply
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

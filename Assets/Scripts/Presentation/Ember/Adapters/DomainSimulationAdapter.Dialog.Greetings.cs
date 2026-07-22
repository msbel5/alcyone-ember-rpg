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
        private async Task GenerateNpcGreetingAsync(NpcSeedRecord npc)
        {
            var router = ForgeLocator.LlmRouter;
            if (router == null) return;

            _isDialogThinking = true;
            int gen = _conversationSerial;
            var request = new LlmRequest(
                "npc_greeting",
                "npc:" + npc.Id.Value,
                null,
                100,
                npc.Id.Value,
                $"You are {npc.Name}, a {npc.Role} in a {StyleDescriptor()} world. Greet the player character briefly in character. The conversation log below is what you personally remember (witnessed events, past talks); let it colour your greeting when relevant.",
                RecallDialogMemory(npc.Id.Value)
            );

            // EMB-007: only the blocking LLM call runs off the main thread. The shared-state
            // mutations are applied AFTER the await, which resumes on Unity's main-thread
            // SynchronizationContext — previously _currentDialogLine / _isDialogThinking were
            // written from the worker thread, racing the main-thread dialog reader.
            var response = await Task.Run(() => CompleteLlmOrEmpty(router, request));
            // DET-02: apply the result on the main-thread tick, not on the await's resumption thread.
            _mainThreadApply.Enqueue(() =>
            {
                if (gen != _conversationSerial) return;   // a newer conversation superseded this — drop the stale reply
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
            int gen = _conversationSerial;
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
                RecallDialogMemoryByName(actorName)
            );

            var response = await Task.Run(() => CompleteLlmOrEmpty(router, request));
            _mainThreadApply.Enqueue(() =>
            {
                if (gen != _conversationSerial) return;   // a newer conversation superseded this — drop the stale reply
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

    }
}

// EMB-010: DomainSimulationAdapter IConsultFateOracle (partial-class split).
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
        // ----- IConsultFateOracle -----
        public string ConsultFate()
        {
            if (_isFateThinking) return "The oracle is gazing into the void...";
            
            // Fire async consult
            _ = ConsultFateAsync();
            
            return "The oracle consults the fates...";
        }

        private async Task ConsultFateAsync()
        {
            var router = ForgeLocator.LlmRouter;
            if (router == null) return;

            _isFateThinking = true;

            // FNV-1a-32 roll as baseline
            uint salted = (uint)_tick * 2654435761u;
            int roll = (int)(salted % 100u) + 1;
            var bucket = EmberCrpg.Domain.AiDm.ConsultFateOutcomeBucket.FromRoll(roll);

            var tools = new List<ToolDescriptor>
            {
                new ToolDescriptor(
                    new ToolId("consult_fate"),
                    ToolSurfaceKind.Dm,
                    new[] { new ToolParameter("query", "string", true) },
                    "string",
                    ToolSideEffect.Read
                )
            };

            var request = new LlmRequest(
                "consult_fate",
                "oracle_fate",
                tools,
                150,
                (ulong)_tick,
                $"You are the Oracle of Ember. The dice have rolled a {bucket.Code} outcome ({roll}/100). Provide a brief, cryptic prophecy reflecting this result for the player. The world is {_world.WorldProfile?.Style}.",
                new List<string>()
            );

            // EMB-007: only the blocking LLM call runs off the main thread. The response — including
            // the AUTHORITATIVE _world.ToolCallTrace mutation — is applied after the await on the
            // main thread (Unity SynchronizationContext). Previously _pendingFate and, worse,
            // _world.ToolCallTrace.Add (a List on the authoritative world) were written from the
            // worker thread, racing the main-thread tick that reads the trace.
            var response = await Task.Run(() => router.Complete(request, out _));
            if (response != null)
            {
                _pendingFate = string.IsNullOrEmpty(response.Text) ? $"THE FATES DECREE: {bucket.Code.ToUpper()} ({roll}/100)" : response.Text.Trim();

                // EMB-008: route the consult_fate tool call through the governed Simulation tool-
                // authority layer (ToolRegistry + ToolCallValidator) instead of hand-synthesising an
                // accepted trace. Even this read-only/benign tool is gated, so the trace is produced
                // ONLY via the validated path — an unregistered tool, a wrong surface, or a missing
                // required arg yields a rejected verdict that is recorded + logged, never blind-trusted.
                var registry = new EmberCrpg.Simulation.AiDm.ToolRegistry();
                registry.Register(tools[0]);
                var toolReq = new ToolCallRequest(new ToolId("consult_fate"), ToolSurfaceKind.Dm, new Dictionary<string, string> { { "query", "oracle_consult" } });
                var validation = new EmberCrpg.Simulation.AiDm.ToolCallValidator().Validate(toolReq, registry);
                // On accept, surface the actual oracle outcome as the trace payload; on reject, keep the
                // validator's rejection result (and reason) so the trace reflects the refusal.
                var toolRes = validation.Accepted ? ToolCallResult.AcceptedWith(bucket.Code) : validation;
                _world.ToolCallTrace.Add(new ToolCallTraceRecord(_world.Time, default, toolReq, toolRes));
                if (!validation.Accepted)
                    LogCombat($"[fate] tool call rejected: {validation.RejectionReason}");
            }
            else
            {
                _pendingFate = $"THE FATES DECREE: {bucket.Code.ToUpper()} ({roll}/100)";
            }
            _isFateThinking = false;

            // Update combat log once finished
            LogCombat(_pendingFate);
        }

    }
}

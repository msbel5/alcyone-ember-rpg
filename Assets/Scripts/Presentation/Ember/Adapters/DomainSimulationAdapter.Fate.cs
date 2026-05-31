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
            int roll = ConsultFateOutcomeBucket.D100(salted); // DET-08: shared deterministic d100 roll
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
                $"You are the Oracle of Ember. The dice have rolled a {bucket.Code} outcome ({roll}/100). Provide a brief, cryptic prophecy reflecting this result for the player. The world is {StyleDescriptor()}.",
                new List<string>()
            );

            // EMB-007 / DET-02: only the blocking LLM call runs off the main thread; the result is
            // applied via the adapter's main-thread apply queue (drained in AdvanceTick), NOT via the
            // implicit SynchronizationContext — so the AUTHORITATIVE _world.ToolCallTrace write can
            // never land on a worker thread (the EMB-007 race) even in a headless run with no context.
            var response = await Task.Run(() => router.Complete(request, out _));
            _mainThreadApply.Enqueue(() =>
            {
                // The OUTCOME (bucket/roll) stays deterministic; only the FLAVOUR text becomes the LLM line.
                // BUG-DIALOG-TURNLEAK: strip any echoed chat-turn scaffolding the local model leaked into
                // its completion, and only adopt the cleaned line when it is non-empty — otherwise keep the
                // deterministic prophecy.
                var deterministicFate = $"THE FATES DECREE: {bucket.Code.ToUpper()} ({roll}/100)";
                var oracleLine = SanitizeNpcLine(response?.Text);
                _pendingFate = !string.IsNullOrEmpty(oracleLine) ? oracleLine : deterministicFate;

                // DET-03: the LLM's tool authority is REAL, not cosmetic. Route the model's ACTUAL
                // response.ProposedToolCalls through the governed gate — LlmProposalValidator over the
                // ToolRegistry, then ToolCallRouter for the accepted calls. Rejected proposals never
                // mutate the world and are logged; consult_fate is ToolSideEffect.Read so its handler
                // just echoes the deterministic fate bucket. The fate OUTCOME stays deterministic (the
                // roll), so the LLM only decorates it and can never conjure a tool the game never
                // declared. (Replaces the EMB-008 self-built synthetic request that validated nothing.)
                if (response != null)
                {
                    var toolValidator = new EmberCrpg.Simulation.AiDm.ToolCallValidator();
                    var fateRegistry = new EmberCrpg.Simulation.AiDm.ToolRegistry();
                    fateRegistry.Register(tools[0]);
                    var fateRouter = new EmberCrpg.Simulation.AiDm.ToolCallRouter(toolValidator);
                    fateRouter.RegisterHandler(ToolSurfaceKind.Dm, new ToolId("consult_fate"),
                        _ => ToolCallResult.AcceptedWith(bucket.Code));
                    var tracer = new EmberCrpg.Simulation.AiDm.ToolCallTracer();

                    var proposals = new EmberCrpg.Simulation.AiDm.LlmProposalValidator(toolValidator)
                        .Validate(response, fateRegistry);
                    foreach (var accepted in proposals.Accepted)
                        fateRouter.Invoke(accepted, fateRegistry, _world.Time, default, _world.Events, tracer);
                    foreach (var rejected in proposals.Rejected)
                        LogCombat($"[fate] rejected LLM tool call {rejected.request.ToolId.Code}: {rejected.reason}");
                    foreach (var rec in tracer.Entries)
                        _world.ToolCallTrace.Add(rec);
                }

                _isFateThinking = false;
                LogCombat(_pendingFate); // surface the prophecy once finished
            });
        }

    }
}

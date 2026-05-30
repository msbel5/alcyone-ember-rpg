using System;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Codex ninth-pass D-P3: confirmed test-wired only — no production caller exists at HEAD. Backend-only by design until the AI/DM scene host attaches it in Phase 12 (per docs/sprint-phase-12-atom-map.md row 11).
namespace EmberCrpg.Simulation.AiDm
{
    // Codex audit (seventh pass J-P3 #33): this file deliberately folds the
    // five Phase-12 LLM narration types — NpcFlavourService, DmNarrationService,
    // ConsultFateService, StorytellerCheckpointSystem, and the supporting
    // FlavourBudget — into a single file because they all share the same
    // LlmRoutingService + FlavourBudget dependency surface and the same
    // proposal-log append pattern. The fold is documented in
    // docs/sprint-phase-12-atom-map.md rows 8, 9, 11, 12. The previous version
    // of the doc named separate .cs files for each — that drift was the
    // sibling H29 audit finding and is fixed in the same pass. If a future
    // code style sweep mandates one-public-type-per-file, split along the
    // obvious type boundaries below; do not split now.
    public sealed class NpcFlavourService
    {
        private readonly LlmRoutingService _routing;
        private readonly FlavourBudget _budget;

        public NpcFlavourService(LlmRoutingService routing, FlavourBudget budget)
        {
            _routing = routing ?? throw new ArgumentNullException(nameof(routing));
            _budget = budget ?? throw new ArgumentNullException(nameof(budget));
        }

        public LlmResponse Generate(LlmRequest request, GameTime now, SliceWorldState world, string fallbackText)
        {
            if (!_budget.TryReserve())
                return new LlmResponse(fallbackText ?? string.Empty, null, 0);

            var response = _routing.Complete(request, out var provider);
            world?.LlmProposalLog.Add(new LlmProposalLogEntry(now, provider, request.ConversationId, response.Text, null, null));
            return string.IsNullOrEmpty(response.Text) ? new LlmResponse(fallbackText ?? string.Empty, null, 0) : response;
        }
    }

    /// <summary>
    /// DM-side narrator. Wraps LlmRoutingService and appends a structured
    /// proposal-log entry that carries the proposed tool calls.
    /// Codex audit (D-P3, restated in seventh-pass #15): no production host
    /// calls this service today — it is exercised by EditMode tests only,
    /// not by the live EmberWorldHost tick chain. Runtime wiring lands in
    /// the Phase 12 DM/LLM sprint when the AI/DM scene host attaches it
    /// (alongside <see cref="DmCheckpointService"/> below). Until then,
    /// treat as test-only — runtime callers should not assume this fires.
    /// </summary>
    public sealed class DmNarrationService
    {
        private readonly LlmRoutingService _routing;

        public DmNarrationService(LlmRoutingService routing)
        {
            _routing = routing ?? throw new ArgumentNullException(nameof(routing));
        }

        public LlmResponse Narrate(LlmRequest request, GameTime now, SliceWorldState world)
        {
            var response = _routing.Complete(request, out var provider);
            world?.LlmProposalLog.Add(new LlmProposalLogEntry(now, provider, request.ConversationId, response.Text, response.ProposedToolCalls, null));
            return response;
        }
    }

    public sealed class ConsultFateService
    {
        private readonly LlmProposalValidator _validator;
        private readonly ToolCallRouter _router;

        public ConsultFateService(LlmProposalValidator validator, ToolCallRouter router)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _router = router ?? throw new ArgumentNullException(nameof(router));
        }

        public ConsultFateResult Resolve(
            LlmRequest request,
            LlmResponse response,
            ToolRegistry registry,
            GameTime now,
            SiteId siteId,
            WorldEventLog events,
            ToolCallTracer tracer,
            ulong seed)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (response == null) throw new ArgumentNullException(nameof(response));

            var validation = _validator.Validate(response, registry);
            var applied = 0;
            foreach (var accepted in validation.Accepted)
            {
                var result = _router.Invoke(accepted, registry, now, siteId, events, tracer);
                if (result.Accepted)
                    applied++;
            }

            // Codex audit Batch 2 / Finding 1: `seed % 100` yields 0..99, but
            // `FromRoll` requires 1..100 and throws on 0. The off-by-one made any
            // seed that was a multiple of 100 (including the obvious initial `0`)
            // crash deterministic narration. Add 1 to land in the canonical 1..100
            // bucket window without changing the 35/35/30 distribution.
            return new ConsultFateResult(
                ConsultFateOutcomeBucket.FromSeed(seed), // DET-08: shared deterministic d100 roll
                validation.Accepted.Count,
                validation.Rejected.Count,
                applied);
        }
    }

    public sealed class ConsultFateResult
    {
        public ConsultFateResult(ConsultFateOutcomeBucket bucket, int acceptedProposals, int rejectedProposals, int appliedToolCalls)
        {
            Bucket = bucket;
            AcceptedProposals = acceptedProposals;
            RejectedProposals = rejectedProposals;
            AppliedToolCalls = appliedToolCalls;
        }

        public ConsultFateOutcomeBucket Bucket { get; }
        public int AcceptedProposals { get; }
        public int RejectedProposals { get; }
        public int AppliedToolCalls { get; }
    }

    /// <summary>
    /// Stores narrator-side checkpoints into the deterministic world-event log.
    /// Codex audit (D-P3): no production host invokes RecordCheckpoint today —
    /// experimental until the AI/DM session host hooks it into save/replay.
    /// Integration tests demonstrate the intended pattern.
    /// </summary>
    public sealed class StorytellerCheckpointSystem
    {
        public void RecordCheckpoint(SliceWorldState world, GameTime now, string label)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            SiteId siteId = default;
            foreach (var site in world.Sites.Records)
            {
                siteId = site.Id;
                break;
            }

            if (siteId.IsEmpty)
                return;

            world.Events.Append(new WorldEvent(
                now,
                WorldEventKind.StorytellerCheckpoint,
                default,
                siteId,
                string.IsNullOrWhiteSpace(label) ? "storyteller_checkpoint" : label.Trim()));
        }
    }
}

using System;
using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>
    /// Validates LLM proposed tool calls through ToolCallValidator. Returns
    /// only the accepted ones; rejected proposals never mutate the world.
    /// Faz 12 Atom 6.
    /// </summary>
    public sealed class LlmProposalValidator
    {
        private readonly ToolCallValidator _toolValidator;

        public LlmProposalValidator(ToolCallValidator toolValidator)
        {
            _toolValidator = toolValidator ?? throw new ArgumentNullException(nameof(toolValidator));
        }

        public LlmProposalValidationResult Validate(LlmResponse response, ToolRegistry registry)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            var accepted = new List<ToolCallRequest>();
            var rejected = new List<(ToolCallRequest request, string reason)>();

            foreach (var proposal in response.ProposedToolCalls)
            {
                var validation = _toolValidator.Validate(proposal, registry);
                if (validation.Accepted)
                    accepted.Add(proposal);
                else
                    rejected.Add((proposal, validation.RejectionReason));
            }

            return new LlmProposalValidationResult(accepted, rejected);
        }
    }

    public sealed class LlmProposalValidationResult
    {
        public LlmProposalValidationResult(
            IReadOnlyList<ToolCallRequest> accepted,
            IReadOnlyList<(ToolCallRequest request, string reason)> rejected)
        {
            Accepted = accepted ?? new ToolCallRequest[0];
            Rejected = rejected ?? new (ToolCallRequest, string)[0];
        }

        public IReadOnlyList<ToolCallRequest> Accepted { get; }
        public IReadOnlyList<(ToolCallRequest request, string reason)> Rejected { get; }
    }
}

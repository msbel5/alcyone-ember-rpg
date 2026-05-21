using System;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
{
    public delegate LlmResponse LlmDispatch(LlmRequest request);

    /// <summary>
    /// Routes an LLM request to the local provider first; falls back to cloud
    /// when local returns null/empty. Faz 12 Atom 5.
    /// </summary>
    public sealed class LlmRoutingService
    {
        private readonly LlmDispatch _local;
        private readonly LlmDispatch _cloud;
        private readonly LlmProviderKind _cloudKind;

        public LlmRoutingService(LlmDispatch local, LlmDispatch cloud)
            : this(local, cloud, LlmProviderKind.CloudAnthropic)
        {
        }

        /// <summary>
        /// Codex audit (second pass A-P2): the original 2-arg constructor
        /// hardcoded `CloudAnthropic` for every cloud dispatch, mis-labelling
        /// every CloudOpenAI/CloudOther provider in telemetry. The 3-arg form
        /// preserves the actual configured provider through `chosen`.
        /// </summary>
        public LlmRoutingService(LlmDispatch local, LlmDispatch cloud, LlmProviderKind cloudKind)
        {
            _local = local;
            _cloud = cloud;
            _cloudKind = cloudKind.IsEmpty ? LlmProviderKind.CloudAnthropic : cloudKind;
        }

        public LlmResponse Complete(LlmRequest request, out LlmProviderKind chosen)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (_local != null)
            {
                var response = _local(request);
                // PR#175 bot review fix: a valid local response may carry no text and
                // only ProposedToolCalls (the model decided to call a tool instead of
                // narrating). Treat that as success — falling back to cloud would
                // duplicate the tool decision and burn tokens.
                if (response != null && HasUsefulPayload(response))
                {
                    chosen = LlmProviderKind.LocalQwen;
                    return response;
                }
            }

            if (_cloud != null)
            {
                var response = _cloud(request);
                if (response != null)
                {
                    chosen = _cloudKind;
                    return response;
                }
            }

            chosen = LlmProviderKind.Mock;
            return new LlmResponse(string.Empty, null, 0);
        }

        private static bool HasUsefulPayload(LlmResponse response)
        {
            if (!string.IsNullOrEmpty(response.Text)) return true;
            return response.ProposedToolCalls != null && response.ProposedToolCalls.Count > 0;
        }
    }
}

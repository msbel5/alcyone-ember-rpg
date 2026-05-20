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

        public LlmRoutingService(LlmDispatch local, LlmDispatch cloud)
        {
            _local = local;
            _cloud = cloud;
        }

        public LlmResponse Complete(LlmRequest request, out LlmProviderKind chosen)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (_local != null)
            {
                var response = _local(request);
                if (response != null && !string.IsNullOrEmpty(response.Text))
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
                    chosen = LlmProviderKind.CloudAnthropic;
                    return response;
                }
            }

            chosen = LlmProviderKind.Mock;
            return new LlmResponse(string.Empty, null, 0);
        }
    }
}

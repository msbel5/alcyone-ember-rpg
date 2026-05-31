using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
{
    public interface ILlmRouter
    {
        LlmResponse Complete(LlmRequest req, out string chosen);
    }
}

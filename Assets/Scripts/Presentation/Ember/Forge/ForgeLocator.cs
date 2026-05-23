using EmberCrpg.Domain.Forge;
using EmberCrpg.Simulation.AiDm;

namespace EmberCrpg.Presentation.Ember.Forge
{
    public static class ForgeLocator
    {
        public static IAssetForge AssetForge { get; private set; }
        public static NativeLlmClient NativeLlm { get; private set; }
        public static LlmRoutingService LlmRouter { get; private set; }

        public static void Register(IAssetForge forge, NativeLlmClient llm, LlmRoutingService router)
        {
            AssetForge = forge;
            NativeLlm = llm;
            LlmRouter = router;
        }

        public static void Clear()
        {
            AssetForge = null;
            NativeLlm = null;
            LlmRouter = null;
        }
    }
}

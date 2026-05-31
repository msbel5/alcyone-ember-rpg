using EmberCrpg.Domain.Forge;
using EmberCrpg.Simulation.AiDm;
using EmberCrpg.Infrastructure.AiDm; // ARCH-05: LLM provider impls

namespace EmberCrpg.Presentation.Ember.Forge
{
    public static class ForgeLocator
    {
        public static IAssetForge AssetForge { get; private set; }
        public static NativeLlmClient NativeLlm { get; private set; }
        public static ILlmRouter LlmRouter { get; private set; }
        public static EmbeddingClient Embedding { get; private set; }

        public static void Register(IAssetForge forge, NativeLlmClient llm, ILlmRouter router)
        {
            AssetForge = forge;
            NativeLlm = llm;
            LlmRouter = router;
        }

        // ModelBootstrap rebinds the asset forge once it has verified model paths
        // on disk — it must NOT clobber the LLM router that ForgeBootstrap already
        // wired. Hence these targeted setters.
        public static void SetAssetForge(IAssetForge forge)
        {
            AssetForge = forge;
        }

        public static void SetEmbeddingClient(EmbeddingClient client)
        {
            Embedding = client;
        }

        public static void Clear()
        {
            AssetForge = null;
            NativeLlm = null;
            LlmRouter = null;
            Embedding = null;
        }
    }
}

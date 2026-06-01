using System;
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
            SetAssetForge(forge);
            NativeLlm = llm;
            LlmRouter = router;
        }

        // ModelBootstrap rebinds the asset forge once it has verified model paths
        // on disk — it must NOT clobber the LLM router that ForgeBootstrap already
        // wired. Hence these targeted setters.
        public static void SetAssetForge(IAssetForge forge)
        {
            // Just store the forge + dispose the previous one. Serialization (one-at-a-time + the RAM guard)
            // is applied by the BOOTSTRAP callers, which wrap the real forge in a SerializedAssetForge. That
            // keeps this locator engine-free (no UnityResourceProbe reference) so the fallback harness — which
            // compiles ForgeLocator — does not pull in UnityEngine.
            if (object.ReferenceEquals(AssetForge, forge)) return;
            (AssetForge as IDisposable)?.Dispose();
            AssetForge = forge;
        }

        public static void SetEmbeddingClient(EmbeddingClient client)
        {
            Embedding = client;
        }

        public static void Clear()
        {
            (AssetForge as IDisposable)?.Dispose();
            AssetForge = null;
            NativeLlm = null;
            LlmRouter = null;
            Embedding = null;
        }
    }
}

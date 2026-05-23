using System.Threading;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Simulation.AiDm;
using EmberCrpg.Simulation.Forge;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Presentation.Ember.Forge
{
    public sealed class ForgeBootstrap : MonoBehaviour
    {
        [SerializeField] private string _comfyUiUrl = "http://localhost:8188";
        [SerializeField] private string _ollamaUrl = LocalQwenClient.DefaultOllamaGenerateEndpoint;

        public bool ComfyUiAvailable { get; private set; }
        public bool OllamaAvailable { get; private set; }
        public bool NativeLlmAvailable => _nativeLlm?.IsAvailable ?? false;
        public bool OnnxForgeAvailable => _onnxForge?.IsAvailable() ?? false;

        private NativeLlmClient _nativeLlm;
        private OnnxAssetForge _onnxForge;
        private IAssetForge _activeForge;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            _cts = new CancellationTokenSource();

            // Initialize clients. Models live under persistentDataPath after the
            // ModelBootstrap download step. Until ModelBootstrap completes we
            // operate in placeholder mode — the forge returns deterministic stub
            // PNGs and the LLM falls back to the HTTP Ollama client.
            var httpLlm = new LocalQwenClient(new LlmClientConfig(LlmProviderKind.LocalQwen, _ollamaUrl, string.Empty, true));
            var modelDir = ResolveModelDirectory();
            _nativeLlm = new NativeLlmClient(modelDir, httpLlm);

            var httpForge = new ComfyUiAssetForge(_comfyUiUrl);
            _onnxForge = BuildOnnxForge(modelDir);

            // Primary path is OnnxAssetForge; if it's in placeholder mode the
            // game still functions, but ComfyUI remains the high-quality
            // network fallback for dev builds with a running ComfyUI server.
            _activeForge = _onnxForge.IsAvailable() ? (IAssetForge)_onnxForge : httpForge;

            // Register in locator
            var router = new LlmRoutingService(
                req => _nativeLlm.Complete(req),
                req => httpLlm.Complete(req),
                LlmProviderKind.LocalQwen
            );
            ForgeLocator.Register(_activeForge, _nativeLlm, router);

            _ = DetectAsync(_cts.Token);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _nativeLlm?.Dispose();
            _onnxForge?.Dispose();
            ForgeLocator.Clear();
        }

        private string ResolveModelDirectory()
        {
            // Bundled models ship under streamingAssetsPath/Models. Downloaded /
            // user-provided overrides live under persistentDataPath/Models. We
            // prefer the latter when it exists so the runtime always sees the
            // freshest copy.
            var persistent = Path.Combine(Application.persistentDataPath, "Models");
            if (Directory.Exists(persistent)) return persistent;
            return Path.Combine(Application.streamingAssetsPath, "Models");
        }

        private OnnxAssetForge BuildOnnxForge(string modelDir)
        {
            // Default to the SDXL-Turbo bundle. ModelBootstrap may rewrite these
            // paths when it resolves the manifest. We hand four paths in the
            // order OnnxAssetForge expects: text encoder, U-Net, VAE decoder,
            // tokenizer JSON.
            // SDXL Turbo HuggingFace export layout: each component lives in
            // its own subdirectory (text_encoder/model.onnx, unet/model.onnx,
            // vae_decoder/model.onnx) and the tokenizer ships as
            // vocab.json + merges.txt + tokenizer_config.json (no single
            // tokenizer.json). Path file probe uses vocab.json.
            var paths = new[]
            {
                Path.Combine(modelDir, "sdxl-turbo", "text_encoder", "model.onnx"),
                Path.Combine(modelDir, "sdxl-turbo", "unet", "model.onnx"),
                Path.Combine(modelDir, "sdxl-turbo", "vae_decoder", "model.onnx"),
                Path.Combine(modelDir, "sdxl-turbo", "tokenizer", "vocab.json"),
            };
            return new OnnxAssetForge(paths, OnnxDiffusionFlavor.SdxlTurbo);
        }

        private async Task DetectAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                ComfyUiAvailable = new ComfyUiAssetForge(_comfyUiUrl).IsAvailable();
                OllamaAvailable = new LocalQwenClient(
                    new LlmClientConfig(LlmProviderKind.LocalQwen, _ollamaUrl, string.Empty, true))
                    .IsAvailable();

                Debug.Log($"Forge Connectivity: ComfyUI={ComfyUiAvailable}, Ollama={OllamaAvailable}, NativeLLM={NativeLlmAvailable}, OnnxForge={OnnxForgeAvailable}");
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task EnsureNativeLlmReady(System.Action<float> progress)
        {
            if (_nativeLlm != null)
                await _nativeLlm.EnsureModelReady(progress);
        }
    }
}

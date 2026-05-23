using System.Threading;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using EmberCrpg.Simulation.AiDm;
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
        public bool SentisAvailable => _sentisForge?.IsAvailable() ?? false;

        private NativeLlmClient _nativeLlm;
        private SentisAssetForge _sentisForge;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            _cts = new CancellationTokenSource();
            
            // Initialize clients
            var httpLlm = new LocalQwenClient(new LlmClientConfig(LlmProviderKind.LocalQwen, _ollamaUrl, string.Empty, true));
            var modelDir = Path.Combine(Application.streamingAssetsPath, "Models");
            _nativeLlm = new NativeLlmClient(modelDir, httpLlm);

            var httpForge = new ComfyUiAssetForge(_comfyUiUrl);
            _sentisForge = new SentisAssetForge(httpForge);

            // Register in locator
            var router = new LlmRoutingService(
                req => _nativeLlm.Complete(req),
                req => httpLlm.Complete(req),
                LlmProviderKind.LocalQwen
            );
            ForgeLocator.Register(_sentisForge, _nativeLlm, router);

            _ = DetectAsync(_cts.Token);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _nativeLlm?.Dispose();
            _sentisForge?.Dispose();
            ForgeLocator.Clear();
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
                
                Debug.Log($"Forge Connectivity: ComfyUI={ComfyUiAvailable}, Ollama={OllamaAvailable}, NativeLLM={NativeLlmAvailable}, Sentis={SentisAvailable}");
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task EnsureNativeLlmReady(System.Action<float> progress)
        {
            if (_nativeLlm != null)
                await _nativeLlm.EnsureModelReady(progress);
        }
    }
}

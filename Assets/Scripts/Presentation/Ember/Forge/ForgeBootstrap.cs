using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Forge
{
    public sealed class ForgeBootstrap : MonoBehaviour
    {
        [SerializeField] private string _comfyUiUrl = "http://localhost:8188";
        [SerializeField] private string _ollamaUrl = EmberCrpg.Simulation.AiDm.LocalQwenClient.DefaultOllamaGenerateEndpoint;

        public bool ComfyUiAvailable { get; private set; }
        public bool OllamaAvailable { get; private set; }

        private CancellationTokenSource _cts;

        private void Awake()
        {
            _cts = new CancellationTokenSource();
            _ = DetectAsync(_cts.Token);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async Task DetectAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                ComfyUiAvailable = new ComfyUiAssetForge(_comfyUiUrl).IsAvailable();
                // Codex review (PR #203 P2): use the explicit IsAvailable()
                // probe instead of (.Complete().Text != null) — the latter is
                // always true because LlmResponse normalises null Text to
                // string.Empty, which falsely reports Ollama as up when the
                // HTTP call actually failed.
                OllamaAvailable = new EmberCrpg.Simulation.AiDm.LocalQwenClient(
                    new EmberCrpg.Simulation.AiDm.LlmClientConfig(EmberCrpg.Domain.AiDm.LlmProviderKind.LocalQwen, _ollamaUrl, string.Empty, true))
                    .IsAvailable();
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}

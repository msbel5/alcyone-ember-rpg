// Why this file is intentionally long: it wires local LLM, ONNX forge selection, CUDA probing, and locator registration at Unity runtime startup.
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Simulation.AiDm;
using EmberCrpg.Infrastructure.AiDm; // ARCH-05: LLM provider impls
using EmberCrpg.Simulation.Forge;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Presentation.Ember.Forge
{
    public sealed class ForgeBootstrap : MonoBehaviour
    {
        public bool ComfyUiAvailable { get; private set; }
        public bool OllamaAvailable { get; private set; }
        public bool NativeLlmAvailable => _nativeLlm?.IsAvailable ?? false;
        public bool OnnxForgeAvailable => _onnxForge != null && _onnxForge.IsAvailable() && !_onnxForge.PlaceholderMode;

        private NativeLlmClient _nativeLlm;
        private OnnxAssetForge _onnxForge;
        private SerializedAssetForge _serializedForge;
        private SingleFigureRefiningAssetForge _refiningForge;
        private IAssetForge _activeForge;
        private CancellationTokenSource _cts;
        private string _forgeInitFailure = string.Empty;

        private void Awake()
        {
            _cts = new CancellationTokenSource();

            var modelRoot = ResolveModelDirectory();

            // PROOF-RUN ISOLATION (--ember-forge-off): the SDXL portrait forge shares the GPU with the
            // game — during proof captures it inflated the 16ms-budget probe to 24.7ms avg / 537ms worst
            // and starved a lookaround for ~20 minutes. Proofs measure the GAME; the forge has its own
            // tests. Redirecting the model root to a non-existent folder rides the EXISTING offline
            // degrade path: serialized-cache hits still load, new generation returns placeholders fast.
            foreach (var arg in System.Environment.GetCommandLineArgs())
                if (string.Equals(arg, "--ember-forge-off", System.StringComparison.Ordinal))
                {
                    modelRoot = Path.Combine(Application.temporaryCachePath, "forge-disabled-proof");
                    Debug.Log("[Forge] ONNX generation DISABLED for this run (--ember-forge-off) — cache-only.");
                    break;
                }

            _nativeLlm = new NativeLlmClient(modelRoot, fallback: null);

            var realForge = EmberForgeFactory.BuildForge(modelRoot, out _onnxForge, out _forgeInitFailure);
            _serializedForge = new SerializedAssetForge(realForge, new UnityResourceProbe());
            _refiningForge = RuntimeSingleFigureForgeFactory.WrapNpcBillboards(_serializedForge, modelRoot);
            _activeForge = _refiningForge;

            ILlmRouter router = new LlmRoutingService(
                req => _nativeLlm.Complete(req),
                cloud: null,
                cloudKind: LlmProviderKind.Mock);

            ForgeLocator.Register(_activeForge, _nativeLlm, router);
            _ = DetectAsync(_cts.Token);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            ForgeLocator.Clear();
            _refiningForge = null;
            _serializedForge = null;
            _nativeLlm?.Dispose();
            _onnxForge?.Dispose();
        }

        private string ResolveModelDirectory()
        {
            var persistent = Path.Combine(Application.persistentDataPath, "Models");
            if (Directory.Exists(persistent)) return persistent;
            return Path.Combine(Application.streamingAssetsPath, "Models");
        }

        private async Task DetectAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                // Runtime inference path is pure native C# only.
                ComfyUiAvailable = false;
                OllamaAvailable = false;
                Debug.Log(
                    $"Forge Connectivity: ComfyUI={ComfyUiAvailable}, Ollama={OllamaAvailable}, " +
                    $"NativeLLM={NativeLlmAvailable}, OnnxForge={OnnxForgeAvailable}, Failure='{_forgeInitFailure}'");
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task EnsureNativeLlmReady(System.Action<float> progress)
        {
            if (_nativeLlm != null)
                await _nativeLlm.EnsureModelReady(progress);
        }

        private sealed class ExplicitFailureAssetForge : IAssetForge
        {
            private readonly string _reason;

            public ExplicitFailureAssetForge(string reason)
            {
                _reason = string.IsNullOrWhiteSpace(reason) ? "forge_initialization_failed" : reason;
            }

            public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(AssetGenerationResult.Failed(
                    request != null ? request.RequestId : "unknown_request",
                    _reason));
            }

            public bool IsAvailable() => false;
        }
    }
}

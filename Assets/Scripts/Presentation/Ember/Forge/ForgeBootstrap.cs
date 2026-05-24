using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Simulation.AiDm;
using EmberCrpg.Simulation.Forge;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Presentation.Ember.Forge
{
    public sealed class ForgeBootstrap : MonoBehaviour
    {
        public bool ComfyUiAvailable { get; private set; }
        public bool OllamaAvailable { get; private set; }
        public bool NativeLlmAvailable => _nativeLlm?.IsAvailable ?? false;
        public bool OnnxForgeAvailable => _activeForge is OnnxAssetForge onnx && onnx.IsAvailable() && !onnx.PlaceholderMode;

        private NativeLlmClient _nativeLlm;
        private OnnxAssetForge _onnxForge;
        private IAssetForge _activeForge;
        private CancellationTokenSource _cts;
        private string _forgeInitFailure = string.Empty;

        private void Awake()
        {
            _cts = new CancellationTokenSource();

            var modelRoot = ResolveModelDirectory();
            _nativeLlm = new NativeLlmClient(modelRoot, fallback: null);

            _activeForge = BuildForge(modelRoot, out _onnxForge, out _forgeInitFailure);

            var router = new LlmRoutingService(
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
            _nativeLlm?.Dispose();
            _onnxForge?.Dispose();
            ForgeLocator.Clear();
        }

        private string ResolveModelDirectory()
        {
            var persistent = Path.Combine(Application.persistentDataPath, "Models");
            if (Directory.Exists(persistent)) return persistent;
            return Path.Combine(Application.streamingAssetsPath, "Models");
        }

        private static IAssetForge BuildForge(string modelRoot, out OnnxAssetForge selectedOnnx, out string failureReason)
        {
            selectedOnnx = null;
            failureReason = string.Empty;

            bool cudaArtifacts = HasCudaRuntimeArtifacts();

            if (cudaArtifacts)
            {
                var sdxl = BuildSdxlForge(modelRoot, OnnxExecutionProviderPreference.PreferCuda);
                string sdxlError = string.Empty;
                bool sdxlReady = sdxl.IsAvailable() && sdxl.TryWarmup(out sdxlError);
                if (sdxlReady)
                {
                    selectedOnnx = sdxl;
                    return sdxl;
                }

                failureReason = "sdxl_init_failed:" + (string.IsNullOrEmpty(sdxlError) ? "unknown" : sdxlError);
                sdxl.Dispose();
            }
            else
            {
                failureReason = "cuda_runtime_missing";
            }

            var sd15 = BuildSd15Forge(modelRoot);
            string sd15Error = string.Empty;
            bool sd15Ready = sd15.IsAvailable() && sd15.TryWarmup(out sd15Error);
            if (sd15Ready)
            {
                selectedOnnx = sd15;
                return sd15;
            }

            if (!string.IsNullOrEmpty(sd15Error))
                failureReason = string.IsNullOrEmpty(failureReason)
                    ? "sd15_init_failed:" + sd15Error
                    : failureReason + "|sd15_init_failed:" + sd15Error;
            else if (!sd15.IsAvailable())
                failureReason = string.IsNullOrEmpty(failureReason)
                    ? "sd15_model_files_missing"
                    : failureReason + "|sd15_model_files_missing";

            sd15.Dispose();
            return new ExplicitFailureAssetForge(failureReason);
        }

        private static OnnxAssetForge BuildSdxlForge(string modelRoot, OnnxExecutionProviderPreference providerPreference)
        {
            var modelDir = Path.Combine(modelRoot, "sdxl-turbo");
            var paths = new[]
            {
                Path.Combine(modelDir, "text_encoder", "model.onnx"),
                Path.Combine(modelDir, "text_encoder_2", "model.onnx"),
                Path.Combine(modelDir, "unet", "model.onnx"),
                Path.Combine(modelDir, "vae_decoder", "model.onnx"),
                Path.Combine(modelDir, "tokenizer", "vocab.json"),
                Path.Combine(modelDir, "tokenizer", "merges.txt"),
                Path.Combine(modelDir, "tokenizer", "tokenizer_config.json"),
            };

            return new OnnxAssetForge(paths, OnnxDiffusionFlavor.SdxlTurbo, providerPreference);
        }

        private static OnnxAssetForge BuildSd15Forge(string modelRoot)
        {
            var modelDir = Path.Combine(modelRoot, "sd-1.5");
            var paths = new[]
            {
                Path.Combine(modelDir, "text_encoder", "model.onnx"),
                Path.Combine(modelDir, "unet", "model.onnx"),
                Path.Combine(modelDir, "vae_decoder", "model.onnx"),
                Path.Combine(modelDir, "tokenizer", "vocab.json"),
                Path.Combine(modelDir, "tokenizer", "merges.txt"),
                Path.Combine(modelDir, "tokenizer", "tokenizer_config.json"),
            };

            return new OnnxAssetForge(paths, OnnxDiffusionFlavor.Sd15Lcm, OnnxExecutionProviderPreference.CpuOnly);
        }

        private static bool HasCudaRuntimeArtifacts()
        {
            var basePath = Path.Combine(Application.dataPath, "Plugins", "x86_64", "cuda");
            return File.Exists(Path.Combine(basePath, "onnxruntime.dll"))
                && File.Exists(Path.Combine(basePath, "onnxruntime_providers_cuda.dll"))
                && File.Exists(Path.Combine(basePath, "onnxruntime_providers_shared.dll"))
                && File.Exists(Path.Combine(basePath, "onnxruntime_providers_tensorrt.dll"));
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

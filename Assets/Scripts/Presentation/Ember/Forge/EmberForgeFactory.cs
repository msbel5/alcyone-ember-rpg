using System.IO;
using UnityEngine;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Simulation.Forge;

namespace EmberCrpg.Presentation.Ember.Forge
{
    /// <summary>
    /// EMB-041: the single source of truth for constructing the runtime asset forge. Previously the
    /// identical CUDA-probe → SDXL-Turbo → SD1.5-LCM selection logic was copy-pasted into BOTH
    /// <c>ForgeBootstrap</c> and <c>ModelBootstrap</c> (a "split-brain" where the two could silently
    /// drift, and a fix to one — e.g. the SDXL model-path layout — could miss the other). Both
    /// bootstraps now call <see cref="BuildForge"/> here, so the SDXL/SD15 provider selection lives in
    /// exactly one place.
    ///
    /// Selection order (unchanged): if the CUDA runtime artifacts are present, prepend their directory
    /// to PATH and try SDXL-Turbo on the CUDA provider; on warmup failure fall back to SD1.5-LCM (CPU).
    /// The returned <see cref="OnnxAssetForge"/> degrades to its own deterministic placeholder mode when
    /// model files / the native runtime are absent, so the bootstrap never hard-fails generation.
    /// </summary>
    public static class EmberForgeFactory
    {
        /// <summary>Build the best available asset forge for <paramref name="modelRoot"/>.
        /// <paramref name="selectedOnnx"/> is the concrete forge (for Dispose/diagnostics);
        /// <paramref name="failureReason"/> accumulates why higher-quality providers were skipped.</summary>
        public static IAssetForge BuildForge(string modelRoot, out OnnxAssetForge selectedOnnx, out string failureReason)
        {
            selectedOnnx = null;
            failureReason = string.Empty;

            bool cudaArtifacts = HasCudaRuntimeArtifacts();
            if (cudaArtifacts) AddCudaProviderDirectoryToPath();

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

            // P1 fix (Codex review on PR #212): keep the SD15 instance rather than returning a hard-
            // failure forge — OnnxAssetForge.GenerateAsync degrades to its deterministic placeholder PNG
            // (when USE_ONNX_RUNTIME is undefined or model files are missing) or a structured failure
            // result, neither of which throws, so CI/offline setups still get a working forge.
            selectedOnnx = sd15;
            return sd15;
        }

        public static OnnxAssetForge BuildSdxlForge(string modelRoot, OnnxExecutionProviderPreference providerPreference)
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

        public static OnnxAssetForge BuildSd15Forge(string modelRoot)
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

        public static bool HasCudaRuntimeArtifacts()
        {
            var basePath = FindCudaProviderDirectory();
            if (string.IsNullOrEmpty(basePath)) return false;
            return File.Exists(Path.Combine(basePath, "onnxruntime.dll"))
                && File.Exists(Path.Combine(basePath, "onnxruntime_providers_cuda.dll"))
                && File.Exists(Path.Combine(basePath, "onnxruntime_providers_shared.dll"));
        }

        public static void AddCudaProviderDirectoryToPath()
        {
            var basePath = FindCudaProviderDirectory();
            if (string.IsNullOrEmpty(basePath)) return;
            var current = System.Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (current.IndexOf(basePath, System.StringComparison.OrdinalIgnoreCase) >= 0) return;
            System.Environment.SetEnvironmentVariable("PATH", basePath + Path.PathSeparator + current);
        }

        private static string FindCudaProviderDirectory()
        {
            var candidates = new[]
            {
                Path.Combine(Application.dataPath, "Plugins", "x86_64", "cuda"),
                Path.Combine(Application.dataPath, "Plugins", "x86_64"),
                Path.Combine(Application.dataPath, "Plugins"),
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                var path = candidates[i];
                if (File.Exists(Path.Combine(path, "onnxruntime.dll"))
                    && File.Exists(Path.Combine(path, "onnxruntime_providers_cuda.dll"))
                    && File.Exists(Path.Combine(path, "onnxruntime_providers_shared.dll")))
                    return path;
            }

            return string.Empty;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Simulation.Forge;
using EmberCrpg.Presentation.Ember.Forge;

namespace EmberCrpg.Editor.Forge
{
    public static class WorldgenSmokeTest
    {
        [MenuItem("Ember/Forge/Generate Fresh World Assets")]
        public static void GenerateWorldAssets()
        {
            // Run on a background thread to not block the Editor UI
            _ = RunSmokeTestAsync();
        }

        public static void GenerateWorldAssetsBatch()
        {
            RunSmokeTestAsync().GetAwaiter().GetResult();
            AssetDatabase.Refresh();
        }

        private static async Task RunSmokeTestAsync()
        {
            Debug.Log("Starting Phase 3: Fresh World Asset Generation (Seed 42)...");
            long peakManaged = LogManagedMemory("before");

            var profile = new WorldProfile(
                WorldStyle.DarkFantasyGrim,
                WorldGenre.PoliticalIntrigue,
                42,
                1000000,
                50,
                20,
                100,
                "Grim",
                "None",
                "None"
            );

            // 1. Generate World
            var parameters = EmberCrpg.Simulation.Worldgen.WorldgenParameters.For(profile.Style, profile.Genre);
            // WorldgenParameters are immutable, so we use the factory method\'s results.
            // If we needed to override them, we\'d use the constructor directly, but For() is tuned correctly.

            var world = WorldgenService.Generate(profile.Seed, parameters);

            Debug.Log($"Generated World: {world.Npcs.Count} NPCs.");

            var modelRoot = Path.Combine(Application.streamingAssetsPath, "Models");
            var npc = world.Npcs.FirstOrDefault();
            if (npc == null)
            {
                Debug.LogError("Generated world has no NPCs. Cannot generate sample portrait.");
                return;
            }

            var request = BuildPortraitRequest(npc, profile);
            Debug.Log($"Generating portrait for {npc.Name} (Seed: {request.Seed})...");

            var result = await GenerateWithFallbackAsync(modelRoot, request).ConfigureAwait(false);
            peakManaged = Math.Max(peakManaged, LogManagedMemory("after_inference"));
            if (!result.Success || result.ImageBytes == null || result.ImageBytes.Length == 0)
            {
                Debug.LogError($"Native forge failed. reason={result.FailureReason}");
                return;
            }

            SaveSinglePortrait(result.ImageBytes, "Docs/forge-samples/grid.png");
            peakManaged = Math.Max(peakManaged, LogManagedMemory("after_save"));
            Debug.Log($"[ForgeSmoke] managed_ram_peak_bytes={peakManaged}");
            Debug.Log("Phase 3 Smoke Test Complete.");
        }

        private static AssetGenerationRequest BuildPortraitRequest(NpcSeedRecord npc, WorldProfile profile)
        {
            var baseRequest = PromptComposers.NpcPortrait(npc, profile);
            return new AssetGenerationRequest(
                baseRequest.RequestId,
                baseRequest.Subject,
                baseRequest.Style,
                baseRequest.Genre,
                baseRequest.MoodKeyword,
                baseRequest.PromptHash,
                width: 256,
                height: 256,
                seed: baseRequest.Seed,
                prompt: baseRequest.Prompt,
                negativePrompt: baseRequest.NegativePrompt);
        }

        private static async Task<AssetGenerationResult> GenerateWithFallbackAsync(string modelRoot, AssetGenerationRequest request)
        {
            if (HasCudaRuntimeArtifacts())
            {
                using (var sdxl = BuildSdxlForge(modelRoot))
                {
                    Debug.Log("[ForgeSmoke] trying_pipeline=SDXL_Turbo provider=CUDA");
                    var result = await sdxl.GenerateAsync(request, CancellationToken.None).ConfigureAwait(false);
                    if (result.Success && string.IsNullOrEmpty(result.FailureReason))
                    {
                        Debug.Log("[ForgeSmoke] chosen_pipeline=SDXL_Turbo provider=CUDA");
                        return result;
                    }

                    Debug.LogWarning($"[ForgeSmoke] SDXL_Turbo failed; falling back to SD15_LCM_CPU. reason={result.FailureReason}");
                }
            }
            else
            {
                Debug.LogWarning("[ForgeSmoke] SDXL_Turbo skipped; reason=sdxl_requires_cuda");
            }

            using (var sd15 = BuildSd15Forge(modelRoot))
            {
                Debug.Log("[ForgeSmoke] trying_pipeline=SD15_LCM provider=CPU");
                var result = await sd15.GenerateAsync(request, CancellationToken.None).ConfigureAwait(false);
                if (result.Success && string.IsNullOrEmpty(result.FailureReason))
                    Debug.Log("[ForgeSmoke] chosen_pipeline=SD15_LCM provider=CPU");
                return result;
            }
        }

        private static OnnxAssetForge BuildSdxlForge(string modelRoot)
        {
            var modelDir = Path.Combine(modelRoot, "sdxl-turbo");
            return new OnnxAssetForge(
                new[]
                {
                    Path.Combine(modelDir, "text_encoder", "model.onnx"),
                    Path.Combine(modelDir, "text_encoder_2", "model.onnx"),
                    Path.Combine(modelDir, "unet", "model.onnx"),
                    Path.Combine(modelDir, "vae_decoder", "model.onnx"),
                    Path.Combine(modelDir, "tokenizer", "vocab.json"),
                    Path.Combine(modelDir, "tokenizer", "merges.txt"),
                    Path.Combine(modelDir, "tokenizer", "tokenizer_config.json"),
                },
                OnnxDiffusionFlavor.SdxlTurbo,
                OnnxExecutionProviderPreference.PreferCuda);
        }

        private static OnnxAssetForge BuildSd15Forge(string modelRoot)
        {
            var modelDir = Path.Combine(modelRoot, "sd-1.5");
            var sd15 = new OnnxAssetForge(
                new[]
                {
                    Path.Combine(modelDir, "text_encoder", "model.onnx"),
                    Path.Combine(modelDir, "unet", "model.onnx"),
                    Path.Combine(modelDir, "vae_decoder", "model.onnx"),
                    Path.Combine(modelDir, "tokenizer", "vocab.json"),
                    Path.Combine(modelDir, "tokenizer", "merges.txt"),
                    Path.Combine(modelDir, "tokenizer", "tokenizer_config.json"),
                },
                OnnxDiffusionFlavor.Sd15Lcm,
                OnnxExecutionProviderPreference.CpuOnly);
            return sd15;
        }

        private static bool HasCudaRuntimeArtifacts()
        {
            var basePath = Path.Combine(Application.dataPath, "Plugins", "x86_64", "cuda");
            return File.Exists(Path.Combine(basePath, "onnxruntime.dll"))
                && File.Exists(Path.Combine(basePath, "onnxruntime_providers_cuda.dll"))
                && File.Exists(Path.Combine(basePath, "onnxruntime_providers_shared.dll"))
                && File.Exists(Path.Combine(basePath, "onnxruntime_providers_tensorrt.dll"));
        }

        private static void SaveSinglePortrait(byte[] pngBytes, string relativePath)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, pngBytes);
            
            Debug.Log($"Saved single portrait PNG to: {fullPath} bytes={pngBytes.Length}");
        }

        private static long LogManagedMemory(string label)
        {
            long bytes = GC.GetTotalMemory(false);
            Debug.Log($"[ForgeSmoke] managed_ram_{label}_bytes={bytes}");
            return bytes;
        }

        private sealed class NativeFailureForge : IAssetForge
        {
            private readonly string _reason;

            public NativeFailureForge(string reason)
            {
                _reason = string.IsNullOrWhiteSpace(reason) ? "native_forge_failed" : reason;
            }

            public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(AssetGenerationResult.Failed(request?.RequestId ?? "worldgen_smoke", _reason));
            }

            public bool IsAvailable() => false;
        }
    }
    
    // Simple dummy for WorldgenParameters if it's not exactly this structure
    public class WorldgenParameters
    {
        public int TargetPopulation;
        public int RegionCount;
        public int FactionCount;
        public int HistoryYears;
    }
}

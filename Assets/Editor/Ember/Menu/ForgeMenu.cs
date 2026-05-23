using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Simulation.Forge;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.Menu
{
    public static class ForgeMenu
    {
        [MenuItem("Ember/Forge/Generate world assets")]
        public static async void GenerateWorldAssets()
        {
            var adapter = EmberDomainAdapterLocator.Current as DomainSimulationAdapter;
            var world = adapter?.World;
            var generated = adapter?.GeneratedWorld;
            IReadOnlyList<NpcSeedRecord> sourceNpcs = generated?.Npcs ?? world?.NpcSeeds;
            if (world?.WorldProfile == null || sourceNpcs == null || sourceNpcs.Count == 0)
            {
                Debug.Log("WorldProfile{ missing } -> forge queue 0 NPC portraits -> no active generated world");
                return;
            }

            var modelRoot = Path.Combine(Application.streamingAssetsPath, "Models");
            var forge = BuildNativeForge(modelRoot);
            var cache = new AssetForgeCache(Application.persistentDataPath);
            var nativeAvailable = forge.IsAvailable();

            int processed = 0;
            int cached = 0;
            var started = DateTime.UtcNow;
            var npcSeeds = new List<NpcSeedRecord>(sourceNpcs.Count);
            foreach (var npc in sourceNpcs)
            {
                var request = PromptComposers.NpcPortrait(npc, world.WorldProfile);
                var portraitAssetPath = PromptComposers.CacheKey(request);
                if (cache.TryRead(request, out _))
                {
                    cached++;
                    processed++;
                    npcSeeds.Add(WithPortrait(npc, portraitAssetPath));
                    continue;
                }

                if (!nativeAvailable)
                {
                    npcSeeds.Add(npc);
                    processed++;
                    continue;
                }

                var result = await forge.GenerateAsync(request, CancellationToken.None);
                if (result.Success)
                {
                    cache.Write(request, result);
                    cached++;
                    npcSeeds.Add(WithPortrait(npc, portraitAssetPath));
                }
                else
                {
                    npcSeeds.Add(npc);
                }
                processed++;
            }

            world.NpcSeeds = npcSeeds;
            var elapsed = DateTime.UtcNow - started;
            Directory.CreateDirectory("docs/forge-samples");
            var sample = npcSeeds.Count > 0 ? npcSeeds[0].Name : "none";
            var suffix = nativeAvailable ? string.Empty : " -> native_forge_unavailable";
            Debug.Log($"WorldProfile{{ Style={world.WorldProfile.Style}, Seed={world.WorldProfile.Seed} }} -> forge queue {sourceNpcs.Count} NPC portraits -> Native ONNX processed {processed}/{sourceNpcs.Count} in {(int)elapsed.TotalMinutes}m{elapsed.Seconds:00}s -> cache populated {cached} entries -> sample portrait for NpcSeedRecord{{ Name='{sample}' }}: docs/forge-samples/sample.png{suffix}");
        }

        private static IAssetForge BuildNativeForge(string modelRoot)
        {
            var sdxl = new OnnxAssetForge(
                new[]
                {
                    Path.Combine(modelRoot, "sdxl-turbo", "text_encoder", "model.onnx"),
                    Path.Combine(modelRoot, "sdxl-turbo", "text_encoder_2", "model.onnx"),
                    Path.Combine(modelRoot, "sdxl-turbo", "unet", "model.onnx"),
                    Path.Combine(modelRoot, "sdxl-turbo", "vae_decoder", "model.onnx"),
                    Path.Combine(modelRoot, "sdxl-turbo", "tokenizer", "vocab.json"),
                    Path.Combine(modelRoot, "sdxl-turbo", "tokenizer", "merges.txt"),
                    Path.Combine(modelRoot, "sdxl-turbo", "tokenizer", "tokenizer_config.json"),
                },
                OnnxDiffusionFlavor.SdxlTurbo,
                HasCudaRuntimeArtifacts()
                    ? OnnxExecutionProviderPreference.PreferCuda
                    : OnnxExecutionProviderPreference.CpuOnly);

            if (sdxl.IsAvailable() && sdxl.TryWarmup(out _))
                return sdxl;

            sdxl.Dispose();

            var sd15 = new OnnxAssetForge(
                new[]
                {
                    Path.Combine(modelRoot, "sd-1.5", "text_encoder", "model.onnx"),
                    Path.Combine(modelRoot, "sd-1.5", "unet", "model.onnx"),
                    Path.Combine(modelRoot, "sd-1.5", "vae_decoder", "model.onnx"),
                    Path.Combine(modelRoot, "sd-1.5", "tokenizer", "vocab.json"),
                    Path.Combine(modelRoot, "sd-1.5", "tokenizer", "merges.txt"),
                    Path.Combine(modelRoot, "sd-1.5", "tokenizer", "tokenizer_config.json"),
                },
                OnnxDiffusionFlavor.Sd15Lcm,
                OnnxExecutionProviderPreference.CpuOnly);

            if (sd15.IsAvailable() && sd15.TryWarmup(out _))
                return sd15;

            sd15.Dispose();
            return new NativeFailureForge("sdxl_and_sd15_unavailable");
        }

        private static bool HasCudaRuntimeArtifacts()
        {
            var basePath = Path.Combine(Application.dataPath, "Plugins", "x86_64", "cuda");
            return File.Exists(Path.Combine(basePath, "onnxruntime.dll"))
                && File.Exists(Path.Combine(basePath, "onnxruntime_providers_cuda.dll"))
                && File.Exists(Path.Combine(basePath, "onnxruntime_providers_shared.dll"))
                && File.Exists(Path.Combine(basePath, "onnxruntime_providers_tensorrt.dll"));
        }

        private static NpcSeedRecord WithPortrait(NpcSeedRecord npc, string portraitAssetPath)
        {
            return new NpcSeedRecord(
                npc.Id,
                npc.Home,
                npc.Faction,
                npc.Name,
                npc.BirthYear,
                npc.Role,
                portraitAssetPath);
        }

        private sealed class NativeFailureForge : IAssetForge
        {
            private readonly string _reason;

            public NativeFailureForge(string reason)
            {
                _reason = string.IsNullOrWhiteSpace(reason) ? "native_forge_failed" : reason;
            }

            public System.Threading.Tasks.Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return System.Threading.Tasks.Task.FromResult(AssetGenerationResult.Failed(request?.RequestId ?? "forge_menu", _reason));
            }

            public bool IsAvailable() => false;
        }
    }
}

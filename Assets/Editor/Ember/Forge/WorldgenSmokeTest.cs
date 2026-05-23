using System;
using System.Collections.Generic;
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

        private static async Task RunSmokeTestAsync()
        {
            Debug.Log("Starting Phase 3: Fresh World Asset Generation (Seed 42)...");

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

            // 2. Initialize Forge
            var cache = new AssetForgeCache(Application.persistentDataPath);
            var modelDir = Path.Combine(Application.streamingAssetsPath, "Models", "sdxl-turbo");
            var onnxPaths = new[]
            {
                Path.Combine(modelDir, "text_encoder.onnx"),
                Path.Combine(modelDir, "unet.onnx"),
                Path.Combine(modelDir, "vae_decoder.onnx"),
                Path.Combine(modelDir, "tokenizer.json"),
            };
            IAssetForge forge = new OnnxAssetForge(onnxPaths, OnnxDiffusionFlavor.SdxlTurbo);
            if (!forge.IsAvailable()) forge = new ComfyUiAssetForge();

            var portraits = new List<Texture2D>();
            int maxCount = 10; // Generate first 10 for the grid and testing

            foreach (var npc in world.Npcs.Take(maxCount))
            {
                var request = PromptComposers.NpcPortrait(npc, profile);
                Debug.Log($"Generating portrait for {npc.Name} (Seed: {request.Seed})...");

                AssetGenerationResult result;
                if (!cache.TryRead(request, out result))
                {
                    result = await forge.GenerateAsync(request, CancellationToken.None);
                    if (result.Success)
                    {
                        cache.Write(request, result);
                    }
                }

                if (result.Success)
                {
                    var tex = new Texture2D(request.Width, request.Height);
                    tex.LoadImage(result.ImageBytes);
                    portraits.Add(tex);
                }
                else
                {
                    Debug.LogWarning($"Failed to generate portrait for {npc.Name}: {result.FailureReason}");
                }
            }

            // 3. Create Grid
            if (portraits.Count > 0)
            {
                SaveGrid(portraits, "docs/forge-samples/grid.png");
            }

            Debug.Log("Phase 3 Smoke Test Complete.");
        }

        private static void SaveGrid(List<Texture2D> textures, string relativePath)
        {
            int cols = 5;
            int rows = (textures.Count + cols - 1) / cols;
            int w = textures[0].width;
            int h = textures[0].height;

            var grid = new Texture2D(w * cols, h * rows);
            for (int i = 0; i < textures.Count; i++)
            {
                int x = (i % cols) * w;
                int y = (rows - 1 - (i / cols)) * h;
                grid.SetPixels(x, y, w, h, textures[i].GetPixels());
            }
            grid.Apply();

            byte[] png = grid.EncodeToPNG();
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, png);
            
            Debug.Log($"Saved 10-portrait grid to: {fullPath}");
            
            // Clean up textures
            foreach (var t in textures) UnityEngine.Object.DestroyImmediate(t);
            UnityEngine.Object.DestroyImmediate(grid);
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

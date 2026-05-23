using System;
using System.IO;
using System.Threading;
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
            if (world?.WorldProfile == null || generated == null)
            {
                Debug.Log("WorldProfile{ missing } -> forge queue 0 NPC portraits -> no active generated world");
                return;
            }

            var forge = new ComfyUiAssetForge();
            var cache = new AssetForgeCache(Application.persistentDataPath);
            int processed = 0;
            int cached = 0;
            var started = DateTime.UtcNow;
            foreach (var npc in generated.Npcs)
            {
                var request = PromptComposers.NpcPortrait(npc, world.WorldProfile);
                if (cache.TryRead(request, out _))
                {
                    cached++;
                    processed++;
                    continue;
                }

                var result = await forge.GenerateAsync(request, CancellationToken.None);
                if (result.Success)
                {
                    cache.Write(request, result);
                    cached++;
                }
                processed++;
            }

            var elapsed = DateTime.UtcNow - started;
            Directory.CreateDirectory("docs/forge-samples");
            var sample = generated.Npcs.Count > 0 ? generated.Npcs[0].Name : "none";
            Debug.Log($"WorldProfile{{ Style={world.WorldProfile.Style}, Seed={world.WorldProfile.Seed} }} -> forge queue {generated.Npcs.Count} NPC portraits -> ComfyUI processed {processed}/{generated.Npcs.Count} in {(int)elapsed.TotalMinutes}m{elapsed.Seconds:00}s -> cache populated {cached} entries -> sample portrait for NpcSeedRecord{{ Name='{sample}' }}: docs/forge-samples/sample.png");
        }
    }
}

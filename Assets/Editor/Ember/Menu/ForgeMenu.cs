using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

            var forge = new ComfyUiAssetForge();
            var cache = new AssetForgeCache(Application.persistentDataPath);
            var comfyAvailable = await forge.IsAvailableAsync(CancellationToken.None);

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

                if (!comfyAvailable)
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
            var suffix = comfyAvailable ? string.Empty : " -> comfyui_unavailable";
            Debug.Log($"WorldProfile{{ Style={world.WorldProfile.Style}, Seed={world.WorldProfile.Seed} }} -> forge queue {sourceNpcs.Count} NPC portraits -> ComfyUI processed {processed}/{sourceNpcs.Count} in {(int)elapsed.TotalMinutes}m{elapsed.Seconds:00}s -> cache populated {cached} entries -> sample portrait for NpcSeedRecord{{ Name='{sample}' }}: docs/forge-samples/sample.png{suffix}");
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
    }
}

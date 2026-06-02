using System;
using System.Collections.Generic;
using System.IO;
using EmberCrpg.Simulation.Generation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Sprites
{
    /// <summary>Loads generated Core portrait PNGs into runtime sprites, with a tiny timestamp cache.</summary>
    public static class GeneratedCoreSpriteLoader
    {
        private sealed class CachedSprite
        {
            public string Path;
            public long LastWriteTicks;
            public Sprite Sprite;
        }

        private static readonly Dictionary<string, CachedSprite> Cache = new Dictionary<string, CachedSprite>(StringComparer.Ordinal);

        public static Sprite TryLoadPortrait(string id)
        {
            var key = NormalizePortraitId(id);
            if (string.IsNullOrEmpty(key)) return null;

            var path = ResolveExistingPath(key);
            if (string.IsNullOrEmpty(path)) return null;
            if (!GeneratedAssetProvenance.IsFreshCoreAsset(key, path)) return null;

            var ticks = File.GetLastWriteTimeUtc(path).Ticks;
            if (Cache.TryGetValue(key, out var cached)
                && cached.Sprite != null
                && string.Equals(cached.Path, path, StringComparison.Ordinal)
                && cached.LastWriteTicks == ticks)
                return cached.Sprite;

            var sprite = LoadSprite(path, key);
            if (sprite != null)
                Cache[key] = new CachedSprite { Path = path, LastWriteTicks = ticks, Sprite = sprite };
            return sprite;
        }

        private static string NormalizePortraitId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            var key = id.Trim().ToLowerInvariant();
            if (key == "dm_portrait") return key;
            if (key.StartsWith("portrait_npc_", StringComparison.Ordinal)
                && key != "portrait_npc_placeholder")
                return key;
            return string.Empty;
        }

        private static string ResolveExistingPath(string key)
        {
            var fileName = key + ".png";
            string[] candidates =
            {
                Path.Combine(Application.persistentDataPath, "Generated", "Core", fileName),
                Path.Combine(ProjectRoot(), "Assets", "Generated", "Core", fileName),
                Path.Combine(Application.streamingAssetsPath, "Generated", "Core", fileName),
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                try
                {
                    if (File.Exists(candidates[i])) return candidates[i];
                }
                catch
                {
                    // Ignore unreadable candidates; missing generated art must degrade to the placeholder.
                }
            }
            return string.Empty;
        }

        private static Sprite LoadSprite(string path, string key)
        {
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(path)))
                {
                    UnityEngine.Object.Destroy(tex);
                    return null;
                }

                tex.name = key;
                var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = key;
                return sprite;
            }
            catch
            {
                return null;
            }
        }

        private static string ProjectRoot()
        {
            var parent = Directory.GetParent(Application.dataPath);
            return parent != null ? parent.FullName : Directory.GetCurrentDirectory();
        }
    }
}

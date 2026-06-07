using System;
using System.Collections.Generic;
using System.IO;
using EmberCrpg.Simulation.Generation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Sprites
{
    /// <summary>Loads generated Core PNGs into runtime sprites, with a tiny timestamp cache.</summary>
    public static class GeneratedCoreSpriteLoader
    {
        private sealed class CachedSprite
        {
            public string Path;
            public long LastWriteTicks;
            public Sprite Sprite;
        }

        private static readonly Dictionary<string, CachedSprite> Cache = new Dictionary<string, CachedSprite>(StringComparer.Ordinal);

        public static Sprite TryLoadByName(string name)
        {
            return GeneratedCoreSpriteNameMapper.TryMap(name, out var coreId) ? TryLoadCoreId(coreId) : null;
        }

        public static Sprite TryLoadPortrait(string id)
        {
            var key = NormalizePortraitCoreId(id);
            if (string.IsNullOrEmpty(key)) return null;
            return TryLoadCoreId(key);
        }

        private static Sprite TryLoadCoreId(string id)
        {
            var key = NormalizeCoreKey(id);
            if (string.IsNullOrEmpty(key)) return null;

            // Walk every candidate root in priority order and return the FIRST copy that exists AND passes the
            // provenance freshness gate. Falling through a stale candidate prevents persistentDataPath junk from
            // shadowing fresh build/project generated Core assets.
            foreach (var path in CoreCandidatePaths(key))
            {
                bool exists;
                try { exists = File.Exists(path); }
                catch { exists = false; }
                if (!exists) continue;
                if (!GeneratedAssetProvenance.IsFreshCoreAsset(key, path)) continue;
                return LoadCachedSprite(key, path);
            }
            return null;
        }

        public static Sprite TryLoadRelativeSprite(string assetsRelativePath, string cacheKey = null)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath)) return null;
            var path = ResolveExistingAssetPath(assetsRelativePath);
            if (string.IsNullOrEmpty(path)) return null;
            return LoadCachedSprite(string.IsNullOrWhiteSpace(cacheKey) ? assetsRelativePath : cacheKey, path);
        }

        private static Sprite LoadCachedSprite(string cacheKey, string path)
        {
            var ticks = File.GetLastWriteTimeUtc(path).Ticks;
            if (Cache.TryGetValue(cacheKey, out var cached)
                && cached.Sprite != null
                && string.Equals(cached.Path, path, StringComparison.Ordinal)
                && cached.LastWriteTicks == ticks)
                return cached.Sprite;

            var sprite = LoadSprite(path, cacheKey);
            if (sprite != null)
                Cache[cacheKey] = new CachedSprite { Path = path, LastWriteTicks = ticks, Sprite = sprite };
            return sprite;
        }

        private static string NormalizePortraitCoreId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            var key = NormalizeCoreKey(id);
            if (key == "dm_portrait") return key;
            if (key.StartsWith("npc_", StringComparison.Ordinal))
                return key;
            if (key.StartsWith("portrait_npc_", StringComparison.Ordinal)
                && key != "portrait_npc_placeholder")
                return key;
            return string.Empty;
        }

        private static string NormalizeCoreKey(string id)
        {
            return string.IsNullOrWhiteSpace(id)
                ? string.Empty
                : id.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        }

        private static IEnumerable<string> CoreCandidatePaths(string key)
        {
            var fileName = key + ".png";
            yield return Path.Combine(Application.persistentDataPath, "Generated", "Core", fileName);
            yield return Path.Combine(ProjectRoot(), "Assets", "Generated", "Core", fileName);
            yield return Path.Combine(Application.streamingAssetsPath, "Generated", "Core", fileName);
        }

        private static string ResolveFreshCorePath(string key)
        {
            key = NormalizeCoreKey(key);
            if (string.IsNullOrEmpty(key)) return string.Empty;

            foreach (var candidate in CoreCandidatePaths(key))
            {
                try
                {
                    if (File.Exists(candidate) && GeneratedAssetProvenance.IsFreshCoreAsset(key, candidate)) return candidate;
                }
                catch
                {
                    // Ignore unreadable candidates; missing generated art must degrade to the placeholder.
                }
            }
            return string.Empty;
        }

        private static string ResolveExistingAssetPath(string assetsRelativePath)
        {
            var normalized = (assetsRelativePath ?? string.Empty).Replace('\\', '/').Trim();
            if (string.IsNullOrEmpty(normalized)) return string.Empty;

            if (normalized.StartsWith("Assets/Generated/Core/", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(normalized);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var generatedCore = ResolveFreshCorePath(Path.GetFileNameWithoutExtension(fileName));
                    if (!string.IsNullOrEmpty(generatedCore))
                        return generatedCore;
                }
            }

            var projectRelative = normalized.Replace('/', Path.DirectorySeparatorChar);
            var projectPath = Path.Combine(ProjectRoot(), projectRelative);
            return File.Exists(projectPath) ? projectPath : string.Empty;
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

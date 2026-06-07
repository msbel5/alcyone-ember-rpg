using System;
using System.Collections.Generic;
using System.IO;
using EmberCrpg.Simulation.Generation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Sprites
{
    /// <summary>
    /// Shared runtime loader for generated Core textures.
    /// Pattern: Facade. It keeps path priority and provenance freshness in one place.
    /// </summary>
    public static class GeneratedCoreTextureLoader
    {
        private sealed class CachedTexture
        {
            public string Path;
            public long LastWriteTicks;
            public Texture2D Texture;
        }

        private static readonly Dictionary<string, CachedTexture> Cache =
            new Dictionary<string, CachedTexture>(StringComparer.Ordinal);

        public static Texture2D TryLoad(
            string entryId,
            TextureWrapMode wrapMode,
            FilterMode filterMode,
            bool mipChain)
        {
            var key = Normalize(entryId);
            if (string.IsNullOrEmpty(key)) return null;

            foreach (var path in CandidatePaths(key))
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    if (!GeneratedAssetProvenance.IsFreshCoreAsset(key, path)) continue;
                    return LoadCached(key, path, wrapMode, filterMode, mipChain);
                }
                catch
                {
                    // Unreadable/stale generated assets must not break scene realization.
                }
            }
            return null;
        }

        private static Texture2D LoadCached(
            string key,
            string path,
            TextureWrapMode wrapMode,
            FilterMode filterMode,
            bool mipChain)
        {
            var ticks = File.GetLastWriteTimeUtc(path).Ticks;
            var cacheKey = key + "|" + wrapMode + "|" + filterMode + "|" + mipChain;
            if (Cache.TryGetValue(cacheKey, out var cached)
                && cached.Texture != null
                && string.Equals(cached.Path, path, StringComparison.Ordinal)
                && cached.LastWriteTicks == ticks)
                return cached.Texture;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain);
            if (!tex.LoadImage(File.ReadAllBytes(path)))
            {
                UnityEngine.Object.Destroy(tex);
                return null;
            }

            tex.name = key;
            tex.wrapMode = wrapMode;
            tex.filterMode = filterMode;
            Cache[cacheKey] = new CachedTexture { Path = path, LastWriteTicks = ticks, Texture = tex };
            return tex;
        }

        private static IEnumerable<string> CandidatePaths(string key)
        {
            var fileName = key + ".png";
            yield return Path.Combine(Application.persistentDataPath, "Generated", "Core", fileName);
            yield return Path.Combine(ProjectRoot(), "Assets", "Generated", "Core", fileName);
            yield return Path.Combine(Application.streamingAssetsPath, "Generated", "Core", fileName);
        }

        private static string Normalize(string entryId)
        {
            return string.IsNullOrWhiteSpace(entryId)
                ? string.Empty
                : entryId.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        }

        private static string ProjectRoot()
        {
            var parent = Directory.GetParent(Application.dataPath);
            return parent != null ? parent.FullName : Directory.GetCurrentDirectory();
        }
    }
}

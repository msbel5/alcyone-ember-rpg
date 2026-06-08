using System;
using System.Collections.Generic;
using System.IO;
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
            var key = GeneratedCoreAssetStore.NormalizeKey(entryId);
            if (string.IsNullOrEmpty(key)) return null;

            return GeneratedCoreAssetStore.TryResolveFreshCorePath(key, out var path)
                ? LoadCached(key, path, wrapMode, filterMode, mipChain)
                : null;
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
    }
}

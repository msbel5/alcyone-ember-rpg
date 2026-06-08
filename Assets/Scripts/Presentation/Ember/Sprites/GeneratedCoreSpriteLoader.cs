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
            return GeneratedCoreAssetStore.TryResolveFreshCorePath(key, out var path)
                ? LoadCachedSprite(key, path)
                : null;
        }

        public static Sprite TryLoadRelativeSprite(string assetsRelativePath, string cacheKey = null)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath)) return null;
            return GeneratedCoreAssetStore.TryResolveAssetPath(assetsRelativePath, out var path)
                ? LoadCachedSprite(string.IsNullOrWhiteSpace(cacheKey) ? assetsRelativePath : cacheKey, path)
                : null;
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
                : GeneratedCoreAssetStore.NormalizeKey(id);
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
    }
}

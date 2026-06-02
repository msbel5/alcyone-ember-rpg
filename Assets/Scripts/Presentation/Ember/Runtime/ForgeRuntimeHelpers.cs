using System.IO;
using System.Threading.Tasks;
using EmberCrpg.Presentation.Ember.Forge;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Runtime
{
    public static class ForgeRuntimeHelpers
    {
        public static void EnsureForgeBootstrap()
        {
            if (ForgeLocator.AssetForge != null) return;
            if (Object.FindFirstObjectByType<ForgeBootstrap>() != null) return;
            var go = new GameObject("ForgeBootstrap");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ForgeBootstrap>();
        }

        public static async Task<bool> WaitForForgeAsync(int maxFrames)
        {
            var frames = maxFrames < 1 ? 1 : maxFrames;
            for (var waited = 0; waited < frames && ForgeLocator.AssetForge == null; waited++)
                await Task.Yield();
            return ForgeLocator.AssetForge != null;
        }

        public static string ResolveRuntimeRoot()
        {
            var parent = Directory.GetParent(Application.dataPath);
            return parent != null ? parent.FullName : Application.dataPath;
        }

        public static Texture2D TryDecodeTexture(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return texture.LoadImage(bytes) ? texture : null;
        }
    }
}

using System;
using System.IO;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Simulation.Forge
{
    public sealed class AssetForgeCache
    {
        private readonly string _root;

        public AssetForgeCache(string persistentDataPath)
        {
            if (string.IsNullOrWhiteSpace(persistentDataPath)) throw new ArgumentException("Cache root is required.", nameof(persistentDataPath));
            _root = Path.Combine(persistentDataPath, "forge-cache");
        }

        public string Root => _root;

        public string PathFor(AssetGenerationRequest request)
        {
            return Path.Combine(_root, PromptComposers.CacheKey(request) + ".png");
        }

        public bool TryRead(AssetGenerationRequest request, out AssetGenerationResult result)
        {
            var path = PathFor(request);
            if (!File.Exists(path))
            {
                result = null;
                return false;
            }

            result = new AssetGenerationResult(request.RequestId, File.ReadAllBytes(path), "image/png", 0, true, string.Empty);
            return true;
        }

        public string Write(AssetGenerationRequest request, AssetGenerationResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (!result.Success || result.ImageBytes.Length == 0) throw new ArgumentException("Only successful image results can be cached.", nameof(result));
            Directory.CreateDirectory(_root);
            var path = PathFor(request);
            File.WriteAllBytes(path, result.ImageBytes);
            return path;
        }
    }
}

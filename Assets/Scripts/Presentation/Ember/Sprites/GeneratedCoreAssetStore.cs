using System;
using System.Collections.Generic;
using System.IO;
using EmberCrpg.Simulation.Generation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Sprites
{
    /// <summary>
    /// Pattern: Policy + Facade. Owns generated Core path order and provenance
    /// freshness so sprite/texture loaders cannot drift apart.
    /// </summary>
    public static class GeneratedCoreAssetStore
    {
        public static string NormalizeKey(string id)
        {
            return string.IsNullOrWhiteSpace(id)
                ? string.Empty
                : id.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        }

        public static bool TryResolveFreshCorePath(string entryId, out string path)
        {
            var key = NormalizeKey(entryId);
            path = string.Empty;
            if (string.IsNullOrEmpty(key)) return false;

            foreach (var candidate in CoreCandidatePaths(key))
            {
                try
                {
                    if (!File.Exists(candidate)) continue;
                    if (!GeneratedAssetProvenance.IsFreshCoreAsset(key, candidate)) continue;
                    path = candidate;
                    return true;
                }
                catch
                {
                    // Unreadable generated files degrade to the next source.
                }
            }

            return false;
        }

        public static bool TryResolveAssetPath(string assetsRelativePath, out string path)
        {
            var normalized = (assetsRelativePath ?? string.Empty).Replace('\\', '/').Trim();
            path = string.Empty;
            if (string.IsNullOrEmpty(normalized)) return false;

            if (normalized.StartsWith("Assets/Generated/Core/", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(normalized);
                if (!string.IsNullOrEmpty(fileName)
                    && TryResolveFreshCorePath(Path.GetFileNameWithoutExtension(fileName), out path))
                    return true;
            }

            var projectPath = Path.Combine(ProjectRoot(), normalized.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(projectPath)) return false;
            path = projectPath;
            return true;
        }

        public static IEnumerable<string> CoreCandidatePaths(string key)
        {
            var normalized = NormalizeKey(key);
            if (string.IsNullOrEmpty(normalized)) yield break;

            var fileName = normalized + ".png";
            yield return Path.Combine(Application.persistentDataPath, "Generated", "Core", fileName);
            yield return Path.Combine(ProjectRoot(), "Assets", "Generated", "Core", fileName);
            yield return Path.Combine(Application.streamingAssetsPath, "Generated", "Core", fileName);
        }

        private static string ProjectRoot()
        {
            var parent = Directory.GetParent(Application.dataPath);
            return parent != null ? parent.FullName : Directory.GetCurrentDirectory();
        }
    }
}

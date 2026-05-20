using System.IO;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.Common
{
    /// <summary>
    /// Decides where Ember scenes are saved on disk and ensures the target folder exists.
    /// Pure path policy — does not touch <see cref="UnityEngine.SceneManagement.Scene"/> state.
    /// </summary>
    public static class EmberSceneSavePolicy
    {
        /// <summary>Returns the asset-relative .unity path the recipe of <paramref name="sceneName"/> should save to.</summary>
        public static string ResolveScenePath(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new System.ArgumentException("Scene name is required", nameof(sceneName));

            EnsureFolderExists(EmberAssetPaths.EmberScenesDir);
            var fileName = sceneName.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase)
                ? sceneName
                : sceneName + ".unity";
            return Path.Combine(EmberAssetPaths.EmberScenesDir, fileName).Replace('\\', '/');
        }

        /// <summary>Creates the asset folder chain if any segment is missing.</summary>
        public static void EnsureFolderExists(string folderAssetPath)
        {
            if (AssetDatabase.IsValidFolder(folderAssetPath))
                return;

            var parent = Path.GetDirectoryName(folderAssetPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(folderAssetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
                return;

            EnsureFolderExists(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}

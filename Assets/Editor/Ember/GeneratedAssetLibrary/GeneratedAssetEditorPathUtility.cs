using System.IO;
using EmberCrpg.Editor.Ember.Common;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedAssetEditorPathUtility
    {
        public static string AssetToAbsolute(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        public static void EnsureAssetFolder(string assetFolderPath)
        {
            EmberSceneSavePolicy.EnsureFolderExists(assetFolderPath.Replace('\\', '/'));
        }

        public static void WriteAssetBytes(string assetPath, byte[] bytes)
        {
            EnsureAssetFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            File.WriteAllBytes(AssetToAbsolute(assetPath), bytes);
        }
    }
}

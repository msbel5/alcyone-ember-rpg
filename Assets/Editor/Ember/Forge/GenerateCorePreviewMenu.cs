using EmberCrpg.Simulation.Generation;
using UnityEditor;

namespace EmberCrpg.Editor.Ember.Forge
{
    public static class GenerateCorePreviewMenu
    {
        [MenuItem("Ember/Forge/Generate Core (Editor preview)")]
        public static void GenerateCorePreview()
        {
            var manifest = CoreAssetManifest.CreateDefault();
            UnityEngine.Debug.Log("[GenerateCorePreview] Core preview entries=" + manifest.Entries.Count + ". Run Boot scene for visible generation.");
        }
    }
}

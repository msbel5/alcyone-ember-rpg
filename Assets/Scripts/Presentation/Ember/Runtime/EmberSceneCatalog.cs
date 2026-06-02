using System.IO;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Runtime
{
    public static class EmberSceneCatalog
    {
        public static bool IsKnownBuildScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return false;
#if UNITY_EDITOR
            foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
            {
                if (scene == null || string.IsNullOrEmpty(scene.path)) continue;
                var stem = Path.GetFileNameWithoutExtension(scene.path);
                if (string.Equals(stem, sceneName, System.StringComparison.Ordinal))
                    return true;
            }
            return false;
#else
            return Application.CanStreamedLevelBeLoaded(sceneName);
#endif
        }
    }
}

using UnityEditor;
using EmberCrpg.Editor.Ember.Build;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EmberCrpg.Editor.Ember.Menu
{
    public static class EmberBuildSettingsMenu
    {
        [MenuItem("Ember/Build/Add All Scenes To Build Settings")]
        public static void AddAllScenes()
        {
            BuildSettingsSceneRegistrar.AddAllScenesToBuildSettings();
        }

        [MenuItem("Ember/Build/Build Windows64 Player")]
        public static void BuildWindows64()
        {
            Windows64BuildMenu.Build();
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace EmberCrpg.Editor.Ember.Build
{
    public static class BuildSettingsSceneRegistrar
    {
        public const string BootScenePath = "Assets/Scenes/Ember/Boot.unity";

        public static void AddAllScenesToBuildSettings()
        {
            var paths = new List<string>();
            if (File.Exists(BootScenePath)) paths.Add(BootScenePath);
            AddIfExists(paths, "Assets/Scenes/Ember/MainMenu.unity");
            AddIfExists(paths, "Assets/Scenes/Ember/CharacterCreation.unity");
            AddIfExists(paths, "Assets/Scenes/Ember/GeneratedWorld.unity"); // runtime-directed world (New Game default)
            EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            UnityEngine.Debug.Log("[BuildSettingsSceneRegistrar] Registered " + paths.Count + " scenes; Boot index=" + paths.IndexOf(BootScenePath));
        }

        public static bool WouldKeepBootAtIndexZero(IReadOnlyList<string> current)
        {
            return current != null && current.Count > 0 && current[0] == BootScenePath;
        }

        private static void AddIfExists(List<string> paths, string path)
        {
            if (File.Exists(path) && !paths.Contains(path)) paths.Add(path);
        }
    }
}

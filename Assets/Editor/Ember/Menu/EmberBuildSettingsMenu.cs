using UnityEditor;
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
            var scenePaths = Directory.GetFiles("Assets/Scenes/Ember", "*.unity", SearchOption.AllDirectories)
                .Select(p => p.Replace("\\", "/"))
                .ToList();

            // Sort to ensure Faz3 is early, etc. if needed, but the main thing is they are all there.
            // Actually, let's put MainMenu first if it exists.
            var mainMenu = scenePaths.FirstOrDefault(p => p.Contains("MainMenu"));
            if (mainMenu != null)
            {
                scenePaths.Remove(mainMenu);
                scenePaths.Insert(0, mainMenu);
            }

            var scenes = scenePaths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            EditorBuildSettings.scenes = scenes;
            
            UnityEngine.Debug.Log($"Added {scenes.Length} scenes to build settings.");
        }

        [MenuItem("Ember/Build/Build Windows64 Player")]
        public static void BuildWindows64()
        {
            AddAllScenes();
            
            string buildPath = "Builds/Ember-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmm") + "/Ember.exe";
            string buildDir = System.IO.Path.GetDirectoryName(buildPath);
            if (!System.IO.Directory.Exists(buildDir))
                System.IO.Directory.CreateDirectory(buildDir);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            buildPlayerOptions.locationPathName = buildPath;
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.None;

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                UnityEngine.Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            }
            else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
            {
                UnityEngine.Debug.LogError("Build failed");
            }
        }
    }
}

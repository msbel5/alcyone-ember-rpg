using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace EmberCrpg.Editor.Ember.Build
{
    public static class Windows64BuildMenu
    {
        [MenuItem("Ember/Build/Windows64")]
        public static void Build()
        {
            BuildSettingsSceneRegistrar.AddAllScenesToBuildSettings();
            var output = "Builds/Windows64/alcyone-ember-rpg.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("Windows64 build failed: " + report.summary.result);
            UnityEngine.Debug.Log("[Windows64BuildMenu] Build succeeded: " + output + " bytes=" + report.summary.totalSize);
        }
    }
}

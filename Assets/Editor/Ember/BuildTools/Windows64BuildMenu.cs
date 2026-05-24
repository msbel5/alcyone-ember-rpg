using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace EmberCrpg.Editor.Ember.Build
{
    public static class Windows64BuildMenu
    {
        [MenuItem("Ember/Build/Windows64")]
        public static void Build()
        {
            BuildSettingsSceneRegistrar.AddAllScenesToBuildSettings();
            ConfigureOnnxNativePlugins();
            var output = "Builds/Windows64/alcyone-ember-rpg.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var oldBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            var oldStripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Standalone);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Disabled);
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                    extraScriptingDefines = new[] { "EXCLUDE_BCL_MEMORY", "EXCLUDE_BCL_NUMERICS" },
                };
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new System.InvalidOperationException("Windows64 build failed: " + report.summary.result);
                UnityEngine.Debug.Log("[Windows64BuildMenu] Build succeeded: " + output + " bytes=" + report.summary.totalSize);
            }
            finally
            {
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, oldStripping);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, oldBackend);
            }
        }

        private static void ConfigureOnnxNativePlugins()
        {
            ConfigureNativePlugin("Assets/Plugins/x86_64/onnxruntime.dll", editor: true, windows64: false);
            ConfigureNativePlugin("Assets/Plugins/x86_64/onnxruntime_providers_shared.dll", editor: true, windows64: false);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/onnxruntime.dll", editor: false, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/onnxruntime_providers_cuda.dll", editor: false, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/onnxruntime_providers_shared.dll", editor: false, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/onnxruntime_providers_tensorrt.dll", editor: false, windows64: true);
        }

        private static void ConfigureNativePlugin(string path, bool editor, bool windows64)
        {
            if (!(AssetImporter.GetAtPath(path) is PluginImporter importer)) return;
            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(editor);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, windows64);
            importer.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "x86_64");
            importer.SaveAndReimport();
        }
    }
}

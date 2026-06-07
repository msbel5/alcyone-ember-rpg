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
            EnsureRuntimeShadersIncluded();
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
                    extraScriptingDefines = new[] { "EXCLUDE_BCL_MEMORY", "EXCLUDE_BCL_NUMERICS", "USE_LLAMASHARP" },
                };
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new System.InvalidOperationException("Windows64 build failed: " + report.summary.result);
                UnityEngine.Debug.Log("[Windows64BuildMenu] Build succeeded: " + output + " bytes=" + report.summary.totalSize);
                CopyGeneratedCoreToBuild(Path.GetDirectoryName(output));
            }
            finally
            {
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, oldStripping);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, oldBackend);
            }
        }

        // The player loads generated assets from <buildDir>/Assets/Generated/Core (ForgeRuntimeHelpers /
        // GeneratedCoreSpriteLoader resolve the runtime root as the parent of the _Data folder). Unity does NOT
        // bundle the project's Assets/Generated/Core there, so ship the editor-generated PNGs explicitly.
        private static void CopyGeneratedCoreToBuild(string buildDir)
        {
            try
            {
                var projectRoot = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
                var src = Path.Combine(projectRoot, "Assets", "Generated", "Core");
                if (!Directory.Exists(src)) return;
                var dst = Path.Combine(buildDir, "Assets", "Generated", "Core");
                if (Directory.Exists(dst)) Directory.Delete(dst, true);
                Directory.CreateDirectory(dst);
                var count = 0;
                foreach (var file in Directory.GetFiles(src, "*.png", SearchOption.TopDirectoryOnly))
                {
                    File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
                    count++;
                }
                UnityEngine.Debug.Log("[Windows64BuildMenu] Copied " + count + " generated Core PNGs to " + dst);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Windows64BuildMenu] Generated Core copy failed: " + ex.Message);
            }
        }

        private static void ConfigureOnnxNativePlugins()
        {
            // User goal: "Editorda SDXL olucak her zaman, en iyi modelleri kullanıcaz" — Editor
            // must use the same CUDA-accelerated ONNX runtime as the build, so SDXL Turbo works
            // in Play Mode at Daggerfall-quality. The earlier split (CPU-only in Editor, CUDA
            // in Build) meant Editor Play Mode always fell back to Sd15LcmPipeline → 128px
            // blurry icons. Now: the CUDA pair under Assets/Plugins/x86_64/cuda/ is enabled for
            // both Editor and Win64; the CPU-only pair at Assets/Plugins/x86_64/ is disabled so
            // there is no duplicate-onnxruntime.dll conflict in either platform.
            ConfigureNativePlugin("Assets/Plugins/x86_64/onnxruntime.dll", editor: false, windows64: false);
            ConfigureNativePlugin("Assets/Plugins/x86_64/onnxruntime_providers_shared.dll", editor: false, windows64: false);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/onnxruntime.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/onnxruntime_providers_cuda.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/onnxruntime_providers_shared.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/onnxruntime_providers_tensorrt.dll", editor: true, windows64: true);

            // cuDNN DLLs — gitignored binaries that Unity creates STUB .meta files for on first
            // import (just fileFormatVersion + guid, no PluginImporter platformData). Stubs let
            // Unity bundle the .dll into the Win64 build (defaults work), but the Editor refuses
            // to load them at Play Mode → CUDA session init fails → SdxlTurboPipeline returns
            // "sdxl_requires_cuda" → ForgeBootstrap falls back to Sd15LcmPipeline → blurry icon
            // generation. Configuring them here turns each stub into a proper PluginImporter
            // (Editor + Win64 enabled, CPU x86_64, OS Windows) so the next run of the build menu
            // self-heals the import config even if someone else stubs the .metas later.
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/cudnn64_9.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/cudnn_adv64_9.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/cudnn_cnn64_9.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/cudnn_engines_precompiled64_9.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/cudnn_engines_runtime_compiled64_9.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/cudnn_engines_tensor_ir64_9.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/cudnn_ext64_9.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/cudnn_graph64_9.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/cudnn_heuristic64_9.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/cuda/cudnn_ops64_9.dll", editor: true, windows64: true);

            // LLM native stack — llama.cpp + ggml backends + multimodal. Same stub-.meta
            // problem as cuDNN: Unity auto-generates GUID-only stubs and the Editor refuses to
            // load them in Play Mode, so any in-Editor AskDM/NPC-dialog LLM call silently
            // skipped the native path. Enable for both Editor and Win64 with x86_64.
            ConfigureNativePlugin("Assets/Plugins/x86_64/llama.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/ggml.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/ggml-base.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/ggml-cpu.dll", editor: true, windows64: true);
            ConfigureNativePlugin("Assets/Plugins/x86_64/mtmd.dll", editor: true, windows64: true);
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

        // Some runtime materials resolve their shader via Shader.Find at play time — notably the streaming
        // terrain ("Universal Render Pipeline/Terrain/Lit"). Unity STRIPS any shader that no built asset
        // references, so deleting the old baked scenes removed the last reference and the terrain rendered
        // MAGENTA in the player. Force-include the runtime-resolved shaders into GraphicsSettings so they
        // always ship, independent of which scenes exist. Resolved here (Editor has every shader) by name.
        private static void EnsureRuntimeShadersIncluded()
        {
            string[] names =
            {
                "Universal Render Pipeline/Terrain/Lit",
                "Universal Render Pipeline/Lit",
                "Skybox/Procedural",
            };

            var settings = UnityEngine.Rendering.GraphicsSettings.GetGraphicsSettings();
            var so = new SerializedObject(settings);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            bool changed = false;
            foreach (var name in names)
            {
                var shader = UnityEngine.Shader.Find(name);
                if (shader == null) continue;
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) { present = true; break; }
                }
                if (present) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                changed = true;
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("[Windows64BuildMenu] Force-included runtime shaders (terrain/lit/skybox) so they are not stripped.");
            }
        }
    }
}

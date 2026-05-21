using System.Collections.Generic;
using System.IO;
using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneRecipes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.Tools
{
    /// <summary>
    /// Captures one PNG per faz scene without touching MCP or Play Mode. Each capture
    /// opens the scene, renders the active camera into a render texture, writes the
    /// PNG under <c>docs/screenshots/</c>. Scenes that do not exist yet are silently
    /// skipped — re-run after <c>Ember &gt; Build Scene &gt; All</c> to refresh.
    /// </summary>
    public static class EmberScreenshotCapture
    {
        private const int Width = 1920;
        private const int Height = 1080;
        private const string ScreenshotsRootRelative = "docs/screenshots";

        [MenuItem("Ember/Capture/All Faz Scene Screenshots")]
        public static void CaptureAll()
        {
            var screenshotsDir = ScreenshotsAbsolutePath();
            Directory.CreateDirectory(screenshotsDir);

            foreach (var recipe in AllRecipes())
            {
                var scenePath = EmberSceneSavePolicy.ResolveScenePath(recipe.SceneName);
                if (!File.Exists(scenePath))
                {
                    Debug.LogWarning($"[EmberCapture] {scenePath} missing — run Ember/Build Scene/All first");
                    continue;
                }

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var pngPath = Path.Combine(screenshotsDir, recipe.SceneName + ".png");
                CaptureActiveSceneCamera(pngPath);
                Debug.Log($"[EmberCapture] wrote {pngPath}");
            }
        }

        [MenuItem("Ember/Capture/Active Scene Screenshot")]
        public static void CaptureActive()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var name = string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name;
            var screenshotsDir = ScreenshotsAbsolutePath();
            Directory.CreateDirectory(screenshotsDir);
            CaptureActiveSceneCamera(Path.Combine(screenshotsDir, name + ".png"));
        }

        private static void CaptureActiveSceneCamera(string absolutePath)
        {
            var camera = ResolveCaptureCamera();
            if (camera == null)
            {
                Debug.LogWarning("[EmberCapture] no camera found in active scene");
                return;
            }

            var rt = new RenderTexture(Width, Height, 24);
            camera.targetTexture = rt;
            var previousActive = RenderTexture.active;
            RenderTexture.active = rt;
            camera.Render();

            var snap = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            snap.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            snap.Apply();

            camera.targetTexture = null;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(rt);

            File.WriteAllBytes(absolutePath, snap.EncodeToPNG());
            Object.DestroyImmediate(snap);
        }

        private static Camera ResolveCaptureCamera()
        {
            var byTag = GameObject.FindGameObjectWithTag("MainCamera");
            if (byTag != null && byTag.TryGetComponent(out Camera tagged)) return tagged;
            var all = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            return all.Length > 0 ? all[0] : null;
        }

        private static string ScreenshotsAbsolutePath()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, ScreenshotsRootRelative);
        }

        private static IEnumerable<IEmberSceneRecipe> AllRecipes()
        {
            yield return new Faz3SmithingSceneRecipe();
            yield return new Faz4ColonyNeedsSceneRecipe();
            yield return new Faz5FarmSceneRecipe();
            yield return new Faz6TradeSceneRecipe();
            yield return new Faz7CombatSceneRecipe();
            yield return new Faz8MagicSceneRecipe();
            yield return new Faz9DialogSceneRecipe();
            yield return new Faz10DmQuerySceneRecipe();
            yield return new Faz11VisualLayerSceneRecipe();
            yield return new Faz12LlmFlavourSceneRecipe();
        }
    }
}

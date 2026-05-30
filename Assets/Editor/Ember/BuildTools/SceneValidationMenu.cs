using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmberCrpg.Editor.Ember.Build
{
    /// <summary>
    /// EMB-054: opens every enabled build scene and reports structural health that static YAML
    /// inspection cannot prove — missing MonoBehaviour scripts (m_Script resolves to null), absence
    /// of a Camera, and absence of an EventSystem (UI input dead without it). Editor-only; writes a
    /// report to validation-output/scene-validation.txt and logs a summary. Run via the menu or
    /// headless: -executeMethod EmberCrpg.Editor.Ember.Build.SceneValidationMenu.ValidateAll
    /// </summary>
    public static class SceneValidationMenu
    {
        [MenuItem("Ember/Validate/Scenes")]
        public static void ValidateAll()
        {
            var sb = new StringBuilder();
            int totalMissing = 0, scenesWithIssues = 0;
            var scenes = EditorBuildSettings.scenes;

            foreach (var s in scenes)
            {
                if (!s.enabled) continue;
                var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
                int missing = 0;
                bool hasCamera = false, hasEventSystem = false;

                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var comp in root.GetComponentsInChildren<Component>(true))
                    {
                        if (comp == null) { missing++; continue; } // null component == missing script
                    }
                    if (root.GetComponentInChildren<Camera>(true) != null) hasCamera = true;
                    if (HasEventSystem(root)) hasEventSystem = true;
                }

                // Missing scripts are the hard failure. Camera/EventSystem absence is informational:
                // the bootstrap scenes (Boot/MainMenu/CharacterCreation) create those at runtime, so
                // their absence in authored YAML is expected, not a defect.
                bool issue = missing > 0;
                if (issue) scenesWithIssues++;
                totalMissing += missing;
                sb.AppendLine(System.IO.Path.GetFileNameWithoutExtension(s.path)
                    + " : missingScripts=" + missing
                    + " camera=" + hasCamera
                    + " eventSystem=" + hasEventSystem
                    + (issue ? "   <-- REVIEW" : "   OK"));
            }

            var header = "Ember scene validation (EMB-054): " + scenes.Length + " scenes, "
                + scenesWithIssues + " with issues, " + totalMissing + " missing scripts total.\n\n";
            var report = header + sb;
            System.IO.Directory.CreateDirectory("validation-output");
            System.IO.File.WriteAllText("validation-output/scene-validation.txt", report);
            Debug.Log("[SceneValidationMenu]\n" + report);
        }

        private static bool HasEventSystem(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Component>(true))
                if (c != null && c.GetType().Name == "EventSystem") return true;
            return false;
        }
    }
}

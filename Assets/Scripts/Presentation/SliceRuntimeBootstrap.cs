using UnityEngine;
using UnityEngine.SceneManagement;

// Design note:
// SliceRuntimeBootstrap auto-creates the slice presentation entry point in an empty scene.
// Inputs: Unity scene load only.
// Outputs: one persistent SliceGameController GameObject for the current slice sprint.
// Bible reference: PRD Sprint 1 FR-08, Sprint 2 FR-01.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Runtime bootstrap that avoids requiring a hand-authored scene for the slice.</summary>
    public static class SliceRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateController()
        {
            var activeScene = SceneManager.GetActiveScene();
            var sceneName = activeScene.name ?? string.Empty;

            // Bail when an existing slice controller is already present.
            if (Object.FindFirstObjectByType<SliceGameController>() != null) return;

            // Codex review (2026-05-21): the previous name/path heuristic
            // ("Faz*", "MainMenu", "/Scenes/Ember/") was brittle — any
            // out-of-convention scene name silently re-enabled the slice
            // controller next to the host. Detect intent instead: look for
            // EmberWorldHost via reflection so this Sprint 1/2 file does
            // not take an assembly-level dependency on the newer Ember
            // bootstrap layer. The reflective lookup is allocation-free and
            // happens once per scene load.
            var emberHostType = System.Type.GetType(
                "EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldHost, EmberCrpg.Presentation");
            if (emberHostType != null
                && Object.FindFirstObjectByType(emberHostType, FindObjectsInactive.Include) != null)
                return;

            // Fallback for the original Sprint 4 combat foundation scenes.
            if (sceneName.Contains("Sprint4")) return;

            var controller = new GameObject("Sprint2SliceController");
            Object.DontDestroyOnLoad(controller);
            controller.AddComponent<SliceGameController>();
        }
    }
}

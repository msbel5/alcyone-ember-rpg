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

            // Skip when an existing controller is present, or when the active
            // scene belongs to a non-slice surface: Sprint 4 combat foundation,
            // any Faz acceptance scene (Faz3..Faz12), the Ember Main Menu, or
            // any scene saved under Assets/Scenes/Ember/. The slice runtime is
            // a legacy Sprint 1/2 entry point and must not steal control of
            // the new EmberWorldHost-driven scenes.
            if (Object.FindFirstObjectByType<SliceGameController>() != null) return;
            if (sceneName.Contains("Sprint4")) return;
            if (sceneName.StartsWith("Faz")) return;
            if (sceneName.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase)) return;
            if (activeScene.path != null && activeScene.path.Contains("/Scenes/Ember/")) return;

            var controller = new GameObject("Sprint2SliceController");
            Object.DontDestroyOnLoad(controller);
            controller.AddComponent<SliceGameController>();
        }
    }
}

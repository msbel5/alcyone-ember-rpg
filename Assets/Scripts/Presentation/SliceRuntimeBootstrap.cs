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

            // Codex audit (second pass E-P2): when an Ember scene is loaded
            // after the slice scene (DontDestroyOnLoad slice controller
            // survives), the previous guard short-circuited new creation but
            // left the stale controller dangling next to EmberWorldHost.
            // Destroy it first so the Ember scene starts clean.
            var emberHostType = System.Type.GetType(
                "EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldHost, EmberCrpg.Presentation");
            bool emberHostPresent = emberHostType != null
                && Object.FindFirstObjectByType(emberHostType, FindObjectsInactive.Include) != null;

            var existingController = Object.FindFirstObjectByType<SliceGameController>(FindObjectsInactive.Include);

            if (emberHostPresent)
            {
                if (existingController != null)
                    Object.Destroy(existingController.gameObject);
                return;
            }

            // Fallback for the original Sprint 4 combat foundation scenes.
            if (sceneName.Contains("Sprint4")) return;

            // Codex audit (sixth pass E-P2 #E1): the canonical opt-in is now
            // SliceRuntimeMarker authored at scene root. New scenes that want
            // the auto-spawn place that MonoBehaviour. Legacy slice scenes
            // without the marker still bootstrap via the name fallback below.
            var marker = Object.FindFirstObjectByType<SliceRuntimeMarker>(FindObjectsInactive.Include);
            bool nameFallback = sceneName.Contains("Slice") || sceneName.Contains("Sprint1") || sceneName.Contains("Sprint2");
            if (marker == null && !nameFallback) return;

            // Bail when an existing slice controller is already present and we
            // are NOT in an Ember scene — keep using the live one.
            if (existingController != null) return;

            var controller = new GameObject("Sprint2SliceController");
            Object.DontDestroyOnLoad(controller);
            controller.AddComponent<SliceGameController>();
        }
    }
}

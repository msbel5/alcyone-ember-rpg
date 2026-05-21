using UnityEngine;

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

            // Codex audit (seventh pass E-P2 #16): the marker is now the
            // ONLY opt-in. The previous Slice/Sprint1/Sprint2 name fallback
            // was kept "for legacy scenes" but no such scenes exist in
            // Assets/Scenes/, so the fallback was dead and any future
            // accidental name collision would have spawned a stray
            // SliceGameController. Drop the fallback entirely; new scenes
            // that want the auto-bootstrap MUST author a SliceRuntimeMarker.
            var marker = Object.FindFirstObjectByType<SliceRuntimeMarker>(FindObjectsInactive.Include);
            if (marker == null) return;

            // Bail when an existing slice controller is already present and we
            // are NOT in an Ember scene — keep using the live one.
            if (existingController != null) return;

            var controller = new GameObject("Sprint2SliceController");
            Object.DontDestroyOnLoad(controller);
            controller.AddComponent<SliceGameController>();
        }
    }
}

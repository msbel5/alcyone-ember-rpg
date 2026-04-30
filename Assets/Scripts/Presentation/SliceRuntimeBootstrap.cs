using UnityEngine;

// Design note:
// SliceRuntimeBootstrap auto-creates the Sprint 1 presentation entry point in an empty scene.
// Inputs: Unity scene load only.
// Outputs: one persistent SliceGameController GameObject.
// Bible reference: PRD FR-08.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Runtime bootstrap that avoids requiring a hand-authored scene for the slice.</summary>
    public static class SliceRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateController()
        {
            if (Object.FindFirstObjectByType<SliceGameController>() != null)
                return;

            var controller = new GameObject("Sprint1SliceController");
            Object.DontDestroyOnLoad(controller);
            controller.AddComponent<SliceGameController>();
        }
    }
}

using UnityEngine;

namespace EmberCrpg.Presentation.Slice
{
    /// <summary>
    /// Intent marker for SliceRuntimeBootstrap. Authoring a scene with this
    /// MonoBehaviour at root says "yes, please auto-spawn the
    /// SliceGameController here." Codex audit (sixth pass E-P2 #E1): without
    /// this marker the bootstrap relied on name-based heuristics
    /// (scene-name substrings, absence of EmberWorldHost, etc.),
    /// which collided with Faz scenes that happened to look like slice
    /// scenes. The marker is now the canonical opt-in contract; the
    /// name-based fallback is preserved for the original slice scene.
    /// </summary>
    public sealed class SliceRuntimeMarker : MonoBehaviour
    {
    }
}

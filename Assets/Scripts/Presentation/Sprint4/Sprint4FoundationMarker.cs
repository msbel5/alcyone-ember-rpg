using UnityEngine;

namespace EmberCrpg.Presentation.Sprint4
{
    /// <summary>
    /// Codex audit (fifth pass E-P2): intent marker for scenes that should
    /// receive the Sprint 4 greybox bootstrap. Authored into the scene root
    /// instead of the scene-name substring guard.
    /// <see cref="Sprint4FoundationBootstrap"/> now prefers this marker; the
    /// `Contains("Sprint4")` scene-name fallback remains as a soft contract
    /// for legacy Sprint 4 sandbox scenes that predate the marker convention.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Sprint4FoundationMarker : MonoBehaviour
    {
    }
}

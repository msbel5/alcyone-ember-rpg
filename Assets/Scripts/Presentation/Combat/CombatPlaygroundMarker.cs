using UnityEngine;

namespace EmberCrpg.Presentation.Combat
{
    /// <summary>
    /// Codex audit (fifth pass E-P2): intent marker for scenes that should
    /// receive the combat playground bootstrap. Authored into the scene root
    /// instead of the scene-name substring guard.
    /// <see cref="CombatPlaygroundBootstrap"/> now prefers this marker; the
    /// scene-name fallback remains as a soft contract for legacy playground
    /// scenes that predate the marker convention.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatPlaygroundMarker : MonoBehaviour
    {
    }
}

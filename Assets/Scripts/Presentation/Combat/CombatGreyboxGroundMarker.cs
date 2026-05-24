using UnityEngine;

namespace EmberCrpg.Presentation.Combat
{
    /// <summary>
    /// Idempotency marker on the bootstrap-spawned combat playground ground.
    /// Codex audit (seventh pass E-P3 #17): replaced the prior
    /// `GameObject.Find("Combat Playground Ground")` name probe so a rename
    /// or a colliding GameObject name no longer fools the duplicate guard.
    /// </summary>
    public sealed class CombatGreyboxGroundMarker : MonoBehaviour
    {
    }
}

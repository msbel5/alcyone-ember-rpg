using UnityEngine;

namespace EmberCrpg.Presentation.Sprint4
{
    /// <summary>
    /// Idempotency marker on the bootstrap-spawned Sprint 4 greybox ground.
    /// Codex audit (seventh pass E-P3 #17): replaced the prior
    /// `GameObject.Find("Sprint4 Greybox Ground")` name probe so a rename
    /// or a colliding GameObject name no longer fools the duplicate guard.
    /// </summary>
    public sealed class Sprint4GreyboxGroundMarker : MonoBehaviour
    {
    }
}

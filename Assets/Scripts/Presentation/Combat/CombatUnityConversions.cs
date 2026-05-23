using EmberCrpg.Simulation.Movement;
using UnityEngine;

namespace EmberCrpg.Presentation.Combat
{
    internal static class CombatUnityConversions
    {
        public static Vector3 ToUnity(this CombatVector3 value)
            => new Vector3(value.X, value.Y, value.Z);

        public static CombatVector3 ToCombat(this Vector3 value)
            => new CombatVector3(value.x, value.y, value.z);
    }
}

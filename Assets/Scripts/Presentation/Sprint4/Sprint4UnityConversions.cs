using EmberCrpg.Simulation.Movement;
using UnityEngine;

namespace EmberCrpg.Presentation.Sprint4
{
    internal static class Sprint4UnityConversions
    {
        public static Vector3 ToUnity(this Sprint4Vector3 value)
            => new Vector3(value.X, value.Y, value.Z);

        public static Sprint4Vector3 ToSprint4(this Vector3 value)
            => new Sprint4Vector3(value.x, value.y, value.z);
    }
}

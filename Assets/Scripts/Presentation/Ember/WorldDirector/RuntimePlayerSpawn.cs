using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>F15: the realize step records where the player rig spawned so the death-screen
    /// "awaken" action can return the BODY to the plaza (the sim actor moves via the adapter;
    /// the rig needs the matching world-space point). One writer (WorldSceneDirector), one reader.</summary>
    public static class RuntimePlayerSpawn
    {
        public static Vector3 Position { get; private set; } = new Vector3(0f, 0.2f, 0f);

        public static void Record(Vector3 spawn) => Position = spawn;
    }
}

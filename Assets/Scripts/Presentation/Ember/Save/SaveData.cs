using System;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Save
{
    /// <summary>
    /// Codex audit (fifth pass J-P3): SaveData previously shared
    /// EmberSaveService.cs as a public top-level class. Pulling it into its
    /// own file matches the project's one-public-type-per-file convention
    /// and makes external callers (Continue button, future tooling)
    /// reference the type without coupling to the MonoBehaviour file.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string sceneName;
        public Vector3 playerPosition;
        public float playerYaw;
        public int tickIndex;
        // Codex audit Batch 2 / Finding 3: opaque round-trippable JSON envelope
        // produced by IDomainSimulationAdapter.ExportStateJson(). Holds the full
        // deterministic simulation state so save/load is not limited to player
        // transform. Empty string means "no domain adapter wired" (placeholder).
        public string domainStateJson;
    }
}

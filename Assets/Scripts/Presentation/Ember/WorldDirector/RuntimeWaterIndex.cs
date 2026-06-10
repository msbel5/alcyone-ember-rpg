using System.Collections.Generic;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F3/swim: world water-surface levels by terrain tile, registered as the streamer spawns sheets.
    /// The SwimView polls it to know whether the camera is below the local waterline.
    /// </summary>
    public static class RuntimeWaterIndex
    {
        private static readonly Dictionary<long, float> Levels = new Dictionary<long, float>();
        private static float _tileSize = 128f;

        public static void Clear() => Levels.Clear();

        public static void Register(int tileX, int tileZ, float worldWaterY, float tileSize)
        {
            _tileSize = tileSize;
            Levels[Key(tileX, tileZ)] = worldWaterY;
        }

        public static bool TryGetAt(Vector3 worldPos, out float waterY)
        {
            int tx = Mathf.FloorToInt(worldPos.x / _tileSize);
            int tz = Mathf.FloorToInt(worldPos.z / _tileSize);
            return Levels.TryGetValue(Key(tx, tz), out waterY);
        }

        private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
    }

    /// <summary>
    /// Underwater treatment v1: dense blue exponential fog while the camera is below the local waterline,
    /// restored exactly on surfacing. No drowning (per the ship goal's v1 scope).
    /// </summary>
    public sealed class SwimView : MonoBehaviour
    {
        private bool _under;
        private bool _hadFog;
        private Color _fogColor;
        private FogMode _fogMode;
        private float _fogDensity;

        public static void Attach(GameObject playerRig)
        {
            if (playerRig != null && playerRig.GetComponent<SwimView>() == null)
                playerRig.AddComponent<SwimView>();
        }

        private void Update()
        {
            var cam = UnityEngine.Camera.main; // fully qualified: EmberCrpg.Presentation.Ember.Camera namespace shadows it
            var probe = cam != null ? cam.transform.position : transform.position + (Vector3.up * 1.6f);
            bool under = RuntimeWaterIndex.TryGetAt(probe, out float waterY) && probe.y < waterY;
            if (under == _under) return;
            _under = under;

            if (under)
            {
                _hadFog = RenderSettings.fog;
                _fogColor = RenderSettings.fogColor;
                _fogMode = RenderSettings.fogMode;
                _fogDensity = RenderSettings.fogDensity;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = new Color(0.10f, 0.25f, 0.42f);
                RenderSettings.fogDensity = 0.18f;
            }
            else
            {
                RenderSettings.fog = _hadFog;
                RenderSettings.fogColor = _fogColor;
                RenderSettings.fogMode = _fogMode;
                RenderSettings.fogDensity = _fogDensity;
            }
            Debug.Log($"[Swim] underwater={under}" + (under ? $" (waterline {waterY:0.#}m)" : string.Empty) + ".");
        }
    }
}

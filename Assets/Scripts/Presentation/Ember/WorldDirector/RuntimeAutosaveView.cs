using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F34: the 5-minute AUTOSAVE cadence — rides the rig like the other directors and calls the
    /// existing save service's autosave path. Unscaled time (modals pause timeScale, the world
    /// still deserves its save). One log line per save; failures log honestly and retry next tick.
    /// </summary>
    public sealed class RuntimeAutosaveView : MonoBehaviour
    {
        private const float CadenceSeconds = 300f;
        // STATIC anchor — the marathon proof caught the bug: frequent travel reloads rebirth the
        // rig and a per-instance Start() reset the 300s window every time, so it NEVER elapsed.
        // The cadence belongs to the app run, not to any one rig instance.
        private static float s_nextSaveAt;

        private void Start()
        {
            if (s_nextSaveAt <= 0f) s_nextSaveAt = Time.unscaledTime + CadenceSeconds;
        }

        private void Update()
        {
            if (Time.unscaledTime < s_nextSaveAt) return;
            s_nextSaveAt = Time.unscaledTime + CadenceSeconds;
            bool saved = EmberCrpg.Presentation.Ember.Save.EmberSaveService.TryAutosaveActiveScene();
            Debug.Log(saved
                ? "[Autosave] world saved (5min cadence)."
                : "[Autosave] skipped — no save service in this scene (retry in 5min).");
        }
    }
}

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
        private float _nextSaveAt;

        private void Start()
        {
            _nextSaveAt = Time.unscaledTime + CadenceSeconds;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextSaveAt) return;
            _nextSaveAt = Time.unscaledTime + CadenceSeconds;
            bool saved = EmberCrpg.Presentation.Ember.Save.EmberSaveService.TryAutosaveActiveScene();
            Debug.Log(saved
                ? "[Autosave] world saved (5min cadence)."
                : "[Autosave] skipped — no save service in this scene (retry in 5min).");
        }
    }
}

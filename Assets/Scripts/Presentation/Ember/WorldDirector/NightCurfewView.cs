using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F6/schedule staging (the DFU/OpenMW night-street feel): citizens vanish from the street between
    /// 22:00 and 06:00 — they're indoors — while PROWLERS (guards on watch, outlaws at work) remain.
    /// Renderers toggle (the GameObject stays alive so the id-keyed sim sync keeps tracking positions).
    /// </summary>
    public sealed class NightCurfewView : MonoBehaviour
    {
        public bool Prowler;

        private Renderer[] _renderers;
        private float _nextPoll;
        private bool _shown = true;

        private void Update()
        {
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 2f;
            if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

            int hour = RuntimeFieldMirror.HourOfDay;
            bool night = hour >= 22 || hour < 6;
            bool show = Prowler || !night;
            if (show == _shown) return;
            _shown = show;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].enabled = show;
        }
    }
}

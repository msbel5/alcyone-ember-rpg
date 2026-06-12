using EmberCrpg.Presentation.Ember.Adapters;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F20: the boss room's locked door — a slab across the connector. Approach with the Tarnished
    /// Key in your pack (≤2.6m) and the lock CONSUMES it: the slab grinds upward and the way to the
    /// Warden is open. Approach without it and the HUD tells you (once per approach) what's missing.
    /// </summary>
    public sealed class RuntimeLockedDoorView : MonoBehaviour
    {
        private const float ReachMeters = 2.6f;
        private const float RearmMeters = 4.2f;
        private Transform _player;
        private bool _open;
        private bool _lockedLineSaid;
        private float _nextPoll;
        private float _openProgress;

        private void Update()
        {
            if (_open)
            {
                // Grind upward over ~1.2s, then stop ticking.
                if (_openProgress < 1f)
                {
                    _openProgress = Mathf.Min(1f, _openProgress + Time.deltaTime / 1.2f);
                    transform.localPosition += Vector3.up * (2.6f * Time.deltaTime / 1.2f);
                    if (_openProgress >= 1f) enabled = false;
                }
                return;
            }

            if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + 0.3f;
                if (_player == null)
                {
                    var rig = GameObject.Find("PlayerRig");
                    _player = rig != null ? rig.transform : null;
                }
            }
            if (_player == null) return;
            var delta = _player.position - transform.position;
            delta.y = 0f;
            float sqr = delta.sqrMagnitude;
            if (sqr > RearmMeters * RearmMeters)
            {
                _lockedLineSaid = false; // walked away — the locked line may speak again
                return;
            }
            if (sqr > ReachMeters * ReachMeters) return;

            if (EmberDomainAdapterLocator.Current is DomainSimulationAdapter adapter)
            {
                if (adapter.TryConsumeDelveKey())
                {
                    _open = true;
                    RuntimeAudioDirector.PlayDoorCreak(transform.position);
                }
                else if (!_lockedLineSaid)
                {
                    _lockedLineSaid = true;
                    adapter.LogCombat("Locked. Somewhere in these halls lies its key.");
                    Debug.Log("[Door] locked — no key in the pack.");
                }
            }
        }
    }
}

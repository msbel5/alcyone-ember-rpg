using EmberCrpg.Presentation.Ember.Adapters;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F20: a VISIBLE crushing plate on the boss-path corridor floor. Step on it (≤1.3m) and it
    /// slams: 8 damage through the adapter, an audible thunk (the door-creak synth doubles as the
    /// mechanism groan), the plate sinks, and the HUD combat line tells you what just happened.
    /// One-shot per realize — Daggerfall traps don't re-arm while you watch.
    /// </summary>
    public sealed class RuntimeTrapView : MonoBehaviour
    {
        private const float TriggerMeters = 1.3f;
        private Transform _player;
        private bool _fired;
        private float _nextPoll;

        private void Update()
        {
            if (_fired) return;
            if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + 0.25f;
                if (_player == null)
                {
                    var rig = GameObject.Find("PlayerRig");
                    _player = rig != null ? rig.transform : null;
                }
            }
            if (_player == null) return;
            var delta = _player.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > TriggerMeters * TriggerMeters) return;

            _fired = true;
            transform.localPosition += Vector3.down * 0.12f; // the plate gives way underfoot
            if (EmberDomainAdapterLocator.Current is DomainSimulationAdapter adapter)
            {
                adapter.TakePlayerDamage(8);
                adapter.LogCombat("A crushing plate slams into your legs — 8 damage!");
            }
            RuntimeAudioDirector.PlayDoorCreak(transform.position); // the mechanism's groan
            Debug.Log("[Trap] crushing plate fired: 8 damage.");
        }
    }
}

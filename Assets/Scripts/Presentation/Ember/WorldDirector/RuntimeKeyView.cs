using EmberCrpg.Presentation.Ember.Adapters;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F20: the Tarnished Key on its pedestal in a middle room. Walking up to it (≤1.6m) takes it —
    /// no extra keypress, the pickup IS the discovery — adds the real inventory item through the
    /// adapter and hides the key glint. The boss door's lock consumes the item.
    /// </summary>
    public sealed class RuntimeKeyView : MonoBehaviour
    {
        private const float PickupMeters = 1.6f;
        private Transform _player;
        private bool _taken;
        private float _nextPoll;

        private void Update()
        {
            if (_taken) return;
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
            if (delta.sqrMagnitude > PickupMeters * PickupMeters) return;

            _taken = true;
            if (EmberDomainAdapterLocator.Current is DomainSimulationAdapter adapter)
                adapter.LogCombat(adapter.PickUpDelveKey());
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
                renderer.enabled = false; // the glint is gone — the key is in your pack
            RuntimeAudioDirector.PlayUiClick();
        }
    }
}

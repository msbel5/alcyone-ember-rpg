using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Inputs;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F16: the delve chest opens like the doors do — proximity + E (no raycaster/dialog coupling).
    /// Pressing E within reach grants the chest loot through the adapter, swings the lid open, and
    /// plays the door-creak (a hinged lid IS a hinge). One open per realize; the adapter's template
    /// guard keeps the reward once-per-world even across re-realizes.
    /// </summary>
    public sealed class RuntimeChestView : MonoBehaviour
    {
        private const float ReachMeters = 2.4f;
        private Transform _player;
        private Transform _lid;
        private bool _opened;
        private float _nextPoll;

        public void Bind(Transform lid) => _lid = lid;

        private void Update()
        {
            if (_opened) return;
            if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + 0.5f;
                if (_player == null)
                {
                    var rig = GameObject.Find("PlayerRig");
                    _player = rig != null ? rig.transform : null;
                }
            }
            if (_player == null) return;
            if (Vector3.Distance(_player.position, transform.position) > ReachMeters) return;
            if (!EmberInput.Interact) return;

            _opened = true;
            if (EmberDomainAdapterLocator.Current is DomainSimulationAdapter adapter)
                Debug.Log("[Chest] " + adapter.LootDungeonChest());
            if (_lid != null)
                _lid.localRotation = Quaternion.Euler(-62f, 0f, 0f); // lid swings back
            RuntimeAudioDirector.PlayDoorCreak(transform.position);
        }

        /// <summary>Proof hook: open programmatically (the driver can't press E).</summary>
        public string ProofOpen()
        {
            _opened = true;
            if (_lid != null) _lid.localRotation = Quaternion.Euler(-62f, 0f, 0f);
            return EmberDomainAdapterLocator.Current is DomainSimulationAdapter adapter
                ? adapter.LootDungeonChest()
                : "no adapter";
        }
    }
}

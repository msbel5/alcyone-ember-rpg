using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F5/facades (DFU DaggerfallActionDoor recipe, proximity-automated): the hinge swings −90° over 1.5s
    /// when the player comes within reach and swings shut when they leave, with a click at each transition.
    /// Polled at 4Hz — dozens of doors stay free.
    /// </summary>
    public sealed class RuntimeDoorView : MonoBehaviour
    {
        private const float OpenAngle = -90f;   // DFU openAngle
        private const float SwingSeconds = 1.5f; // DFU openDuration
        private const float OpenRange = 2.6f;
        private const float CloseRange = 3.6f;  // hysteresis: no flapping at the threshold

        private Transform _player;
        private float _nextPoll;
        private bool _open;
        private float _t; // 0 closed .. 1 open
        private Quaternion _closedRot;

        private void Awake() => _closedRot = transform.localRotation;

        private void Update()
        {
            if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + 0.25f;
                if (_player == null)
                {
                    var rig = GameObject.Find("PlayerRig");
                    _player = rig != null ? rig.transform : null;
                }
                if (_player != null)
                {
                    float d = Vector3.Distance(_player.position, transform.position);
                    bool want = _open ? d < CloseRange : d < OpenRange;
                    if (want != _open)
                    {
                        _open = want;
                        // F11 ("kapı sesi tiz zil gibiydi" root fix): the v1 latch literally played the
                        // 1320Hz UI click. Doors now creak — EKS stick-slip clip, door-origin dedup.
                        RuntimeAudioDirector.PlayDoorCreak(transform.position);
                    }
                }
            }

            float target = _open ? 1f : 0f;
            if (!Mathf.Approximately(_t, target))
            {
                _t = Mathf.MoveTowards(_t, target, Time.deltaTime / SwingSeconds);
                transform.localRotation = _closedRot * Quaternion.Euler(0f, OpenAngle * _t, 0f);
            }
        }
    }
}

using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F6/night staging, v2. v1 made citizens VANISH 22:00-06:00 but left their box colliders
    /// solid — the live playtest walked into invisible bodies piled downtown ("collision oluyor
    /// ama onlari goremiyoruz"). Sleepers now stay VISIBLE: the billboard lies flat like the
    /// death pose (camera-facing off) and the colliders sleep with them so streets stay
    /// walkable. Prowlers (guards, outlaws) keep standing watch. The GameObject stays alive —
    /// the id-keyed sim sync still tracks positions.
    /// </summary>
    public sealed class NightCurfewView : MonoBehaviour
    {
        public bool Prowler;

        private SpriteRenderer _sprite;
        private Behaviour _facing;
        private Collider[] _colliders;
        private EmberCrpg.Presentation.Ember.Views.ActorView _actorView;
        private Quaternion _uprightRotation;
        private Vector3 _uprightLocalPos;
        private float _nextPoll;
        private bool _asleep;

        private void Update()
        {
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 2f;
            if (_sprite == null)
            {
                _sprite = GetComponentInChildren<SpriteRenderer>(true);
                if (_sprite == null) return;
                _facing = GetComponentInChildren<EmberCrpg.Presentation.Ember.Views.CameraFacingBillboard>(true);
                _actorView = GetComponent<EmberCrpg.Presentation.Ember.Views.ActorView>();
                _colliders = GetComponentsInChildren<Collider>(true);
                _uprightRotation = _sprite.transform.localRotation;
                _uprightLocalPos = _sprite.transform.localPosition;
            }

            int hour = RuntimeFieldMirror.HourOfDay;
            bool sleep = !Prowler && (hour >= 22 || hour < 6);
            if (sleep == _asleep) return;
            _asleep = sleep;
            if (_actorView != null) _actorView.ExternalPoseOverride = sleep; // stops the bob/lean writers

            // If the actor died overnight the corpse pose owns this transform; corpses are
            // despawned by the death sweep, so a dawn restore touching one is rare and harmless.
            if (_facing != null) _facing.enabled = !sleep;
            var t = _sprite.transform;
            if (sleep)
            {
                t.localRotation = Quaternion.Euler(90f, 0f, 0f); // flat on the back — a bedroll pose
                t.localPosition = new Vector3(_uprightLocalPos.x, 0.12f, _uprightLocalPos.z);
            }
            else
            {
                t.localRotation = _uprightRotation;
                t.localPosition = _uprightLocalPos;
            }
            for (int i = 0; i < _colliders.Length; i++)
                if (_colliders[i] != null) _colliders[i].enabled = !sleep;
        }
    }
}

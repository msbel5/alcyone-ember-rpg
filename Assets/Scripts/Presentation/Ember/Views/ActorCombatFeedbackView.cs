using EmberCrpg.Presentation.Ember.WorldDirector;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// F10 hit feel: when the player's strike lands on THIS actor the billboard flashes red for 0.15s;
    /// when the actor is felled the billboard lies flat and greys out (a corpse, not a vanish). Polls
    /// <see cref="WorldCombatFeedbackFeed"/> stamps — unscaled time, because the combat modal pauses
    /// timeScale and the flash must still run behind it.
    /// </summary>
    public sealed class ActorCombatFeedbackView : MonoBehaviour
    {
        private ulong _actorId;
        private SpriteRenderer _sprite;
        private Behaviour _billboardFacing; // CameraFacingBillboard — disabled once the corpse lies down
        private int _hitSeen, _felledSeen;
        private float _flashUntil;
        private bool _fallen;
        private Color _baseColor = Color.white;

        public void Bind(ulong actorId, SpriteRenderer sprite, Behaviour billboardFacing)
        {
            _actorId = actorId;
            _sprite = sprite;
            _billboardFacing = billboardFacing;
            if (_sprite != null) _baseColor = _sprite.color;
            _hitSeen = WorldCombatFeedbackFeed.HitStamp;
            _felledSeen = WorldCombatFeedbackFeed.FelledStamp;
        }

        private void Update()
        {
            if (WorldCombatFeedbackFeed.HitStamp != _hitSeen)
            {
                _hitSeen = WorldCombatFeedbackFeed.HitStamp;
                if (WorldCombatFeedbackFeed.HitTargetId == _actorId && !_fallen)
                    _flashUntil = Time.unscaledTime + 0.15f;
            }

            if (_sprite != null && !_fallen)
                _sprite.color = Time.unscaledTime < _flashUntil ? new Color(1f, 0.25f, 0.2f) : _baseColor;

            if (!_fallen && WorldCombatFeedbackFeed.FelledStamp != _felledSeen)
            {
                _felledSeen = WorldCombatFeedbackFeed.FelledStamp;
                if (WorldCombatFeedbackFeed.FelledTargetId == _actorId)
                    Fall();
            }
        }

        private void Fall()
        {
            _fallen = true;
            if (_billboardFacing != null) _billboardFacing.enabled = false;
            var board = _sprite != null ? _sprite.transform : transform;
            board.localRotation = Quaternion.Euler(90f, 0f, 0f); // face-up on the floor
            board.localPosition = new Vector3(board.localPosition.x, 0.15f, board.localPosition.z);
            if (_sprite != null) _sprite.color = new Color(0.55f, 0.50f, 0.50f);
        }
    }
}

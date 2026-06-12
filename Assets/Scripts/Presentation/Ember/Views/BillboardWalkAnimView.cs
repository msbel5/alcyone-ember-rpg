using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// F33: the two-frame Daggerfall walk — while the root MOVES, the billboard alternates its
    /// mirror frame (flipX) on a step cadence and pulses a slight squash, so the glide reads as
    /// a gait instead of an ice-skate. Frame B is the mirror of frame A (the classic sprite
    /// trick); a real forged second frame can replace the mirror later without touching this.
    /// </summary>
    public sealed class BillboardWalkAnimView : MonoBehaviour
    {
        private const float StepSeconds = 0.28f;
        private const float SquashScale = 0.95f;

        private SpriteRenderer _sprite;
        private Vector3 _baseScale;
        private Vector3 _lastPos;
        private float _nextStep;
        private bool _frameB;

        public void Bind(SpriteRenderer sprite)
        {
            _sprite = sprite;
            if (_sprite != null) _baseScale = _sprite.transform.localScale;
            _lastPos = transform.position;
        }

        private void Update()
        {
            if (_sprite == null) return;
            var pos = transform.position;
            var delta = pos - _lastPos;
            delta.y = 0f;
            _lastPos = pos;
            bool moving = delta.sqrMagnitude > 0.000004f; // ~2mm/frame — the glide, not jitter

            if (!moving)
            {
                if (_frameB)
                {
                    _frameB = false;
                    _sprite.flipX = false;
                    _sprite.transform.localScale = _baseScale;
                }
                return;
            }

            if (Time.time < _nextStep) return;
            _nextStep = Time.time + StepSeconds;
            _frameB = !_frameB;
            _sprite.flipX = _frameB;
            _sprite.transform.localScale = _frameB
                ? new Vector3(_baseScale.x, _baseScale.y * SquashScale, _baseScale.z)
                : _baseScale;
        }
    }
}

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
    // B24 (VARIANT B, step 2a): pin this AFTER ActorView (default order 0). ActorView writes the base
    // billboard pose (stride+idle+shake, and its red damage-tint when active) first each frame; the
    // feedback view then overlays the F14 lunge as an ADDITIVE offset and the F10 flash on top of the
    // tint. Same last-writer discipline the compositor variant would enforce, expressed via execution
    // order so no new component / spawner wiring is needed.
    [DefaultExecutionOrder(50)]
    public sealed class ActorCombatFeedbackView : MonoBehaviour
    {
        private ulong _actorId;
        private SpriteRenderer _sprite;
        private Behaviour _billboardFacing; // CameraFacingBillboard — disabled once the corpse lies down
        private ActorView _actorView; // B24: read DamageTinting so the color arbiter respects Apply()'s red
        private int _hitSeen, _felledSeen, _strikeSeen;
        private float _flashUntil;
        private float _lungeUntil;
        private bool _fallen;
        private Color _baseColor = Color.white;
        // B24 (VARIANT B, step 2b): cached lunge offset, refreshed each frame during the strike window
        // and cleared to zero when it ends. Applied ADDITIVELY on top of whatever ActorView wrote — no
        // more `= new Vector3(0, y, 0)` snap that used to clobber ActorView's shake x/z contribution.
        private Vector3 _lungeOffset;

        public void Bind(ulong actorId, SpriteRenderer sprite, Behaviour billboardFacing)
        {
            _actorId = actorId;
            _sprite = sprite;
            _billboardFacing = billboardFacing;
            _actorView = GetComponent<ActorView>();
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
                {
                    _flashUntil = Time.unscaledTime + 0.15f;
                    // F33: the landed strike throws SPARKS from the struck billboard.
                    RuntimeHitSparks.Burst(transform.position + Vector3.up * 1.1f);
                }
            }

            // B24 (VARIANT B, step 2c): color arbiter — flash wins, then ActorView's damage tint is
            // preserved (do NOT overwrite _baseColor while Apply()'s red is running), else restore base.
            // Runs after ActorView (DefaultExecutionOrder above), so DamageTinting reflects this frame.
            if (_sprite != null && !_fallen)
            {
                if (Time.unscaledTime < _flashUntil)
                    _sprite.color = new Color(1f, 0.25f, 0.2f);
                else if (_actorView == null || !_actorView.DamageTinting)
                    _sprite.color = _baseColor;
            }

            if (!_fallen && WorldCombatFeedbackFeed.FelledStamp != _felledSeen)
            {
                _felledSeen = WorldCombatFeedbackFeed.FelledStamp;
                if (WorldCombatFeedbackFeed.FelledTargetId == _actorId)
                    Fall();
            }

            // F14 attack tell: when THIS actor swings, its billboard lunges toward the camera for 0.2s.
            // Offset only the child board (the sync owns the root); it snaps back when the window ends.
            if (WorldCombatFeedbackFeed.EnemyStrikeStamp != _strikeSeen)
            {
                _strikeSeen = WorldCombatFeedbackFeed.EnemyStrikeStamp;
                if (WorldCombatFeedbackFeed.EnemyStrikeId == _actorId && !_fallen)
                    _lungeUntil = Time.unscaledTime + 0.2f;
            }
            if (_sprite != null && !_fallen)
            {
                var cam = UnityEngine.Camera.main; // fully-qualified: Ember.Camera namespace shadows the type
                var board = _sprite.transform;
                // B24: recompute the lunge offset every frame while the strike window is open, else zero
                // it. The old code wrote `board.localPosition = new Vector3(0, y, 0) + dir` — the absolute
                // set clobbered ActorView's shake x/z the next frame and the else-branch snap zeroed the
                // shake entirely. Now we ADD on top of whatever ActorView wrote (execution order 50 above
                // guarantees ActorView.Update ran first this frame).
                if (Time.unscaledTime < _lungeUntil && cam != null)
                {
                    var toCam = cam.transform.position - board.parent.position;
                    toCam.y = 0f;
                    if (toCam.sqrMagnitude > 0.01f)
                    {
                        var dir = board.parent.InverseTransformDirection(toCam.normalized) * 0.35f;
                        _lungeOffset = new Vector3(dir.x, 0f, dir.z);
                    }
                }
                else
                {
                    _lungeOffset = Vector3.zero;
                }
                if (_lungeOffset.x != 0f || _lungeOffset.z != 0f)
                {
                    board.localPosition = new Vector3(
                        board.localPosition.x + _lungeOffset.x,
                        board.localPosition.y,
                        board.localPosition.z + _lungeOffset.z);
                }
            }
        }

        private void Fall()
        {
            _fallen = true;
            var view = GetComponent<ActorView>();
            if (view != null) view.ExternalPoseOverride = true; // the corpse owns its transform now
            if (_billboardFacing != null) _billboardFacing.enabled = false;
            var board = _sprite != null ? _sprite.transform : transform;
            board.localRotation = Quaternion.Euler(90f, 0f, 0f); // face-up on the floor
            board.localPosition = new Vector3(board.localPosition.x, 0.15f, board.localPosition.z);
            if (_sprite != null) _sprite.color = new Color(0.55f, 0.50f, 0.50f);
        }
    }
}

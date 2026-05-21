using UnityEngine;
using EmberCrpg.Presentation.Ember.Interaction;
using EmberCrpg.Presentation.Ember.Views;
using System.Collections;

namespace EmberCrpg.Presentation.Ember.Combat
{
    public sealed class EmberPlayerMeleeSwing : MonoBehaviour
    {
        private Transform _eye;
        private bool _isSwinging;

        private void Awake()
        {
            _eye = transform.Find("EyeCamera");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F) && !_isSwinging)
            {
                StartCoroutine(SwingRoutine());
            }
        }

        private IEnumerator SwingRoutine()
        {
            _isSwinging = true;
            
            // Camera roll effect
            float duration = 0.1f;
            float elapsed = 0f;
            Quaternion originalRotation = _eye.localRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float roll = Mathf.Sin(t * Mathf.PI) * 10f;
                _eye.localRotation = originalRotation * Quaternion.Euler(0f, 0f, roll);
                yield return null;
            }
            _eye.localRotation = originalRotation;

            // Hit detection
            Ray ray = new Ray(_eye.position, _eye.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 2.0f))
            {
                var sink = hit.collider.GetComponentInParent<IDamageSink>();
                if (sink != null)
                {
                    // Codex audit (fourth pass A-P1): the previous flow ran
                    // the IDamageSink visual + counter-hit BEFORE the domain
                    // adapter confirmed the strike. A rejected/no-target
                    // command would still flash red and ding the player's HP.
                    // Resolve the domain strike FIRST; only run the visual
                    // damage tint AND counter-hit when the command accepts.
                    var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current;
                    bool accepted = false;
                    if (adapter != null)
                    {
                        // Codex review on PR #196 (P1): use ActorView.DomainActorKey
                        // (the same field EmberWorldHost.PushWorldViews uses when
                        // calling TryReadActor). The previous version used
                        // actorView.gameObject.name, which guarantees a mismatch
                        // in scenes that author a different domain key —
                        // TryMeleeStrike would always fail with "No target" and
                        // the gated visual + counter-hit never fire.
                        var actorView = hit.collider.GetComponentInParent<ActorView>();
                        var targetName = actorView != null
                            ? actorView.DomainActorKey
                            : (hit.collider.gameObject != null ? hit.collider.gameObject.name : string.Empty);
                        accepted = adapter.TryMeleeStrike(targetName, rawDamage: 10);
                    }
                    if (accepted)
                    {
                        sink.Apply(10);
                        if (adapter != null) adapter.TakePlayerDamage(2); // counter-hit
                    }
                }
            }

            _isSwinging = false;
        }
    }
}

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
                    sink.Apply(10);

                    // Codex audit (third pass A-P1): the previous flow applied
                    // the visual damage tint via IDamageSink but only mutated
                    // the adapter via TakePlayerDamage(2) (incoming damage).
                    // Route the outgoing strike through
                    // IPlayerCommandSink.TryMeleeStrike so the adapter can
                    // resolve it against domain state instead of leaving the
                    // kill entirely in view-tint land.
                    var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current;
                    if (adapter != null)
                    {
                        var targetName = hit.collider.gameObject != null ? hit.collider.gameObject.name : string.Empty;
                        adapter.TryMeleeStrike(targetName, rawDamage: 10);
                        adapter.TakePlayerDamage(2); // counter-hit
                    }
                }
            }

            _isSwinging = false;
        }
    }
}

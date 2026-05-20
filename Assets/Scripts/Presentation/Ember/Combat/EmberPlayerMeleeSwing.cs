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
                    
                    var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current;
                    if (adapter != null)
                    {
                        adapter.TakePlayerDamage(2); // Player loses 2 HP on hit
                    }
                }
            }

            _isSwinging = false;
        }
    }
}

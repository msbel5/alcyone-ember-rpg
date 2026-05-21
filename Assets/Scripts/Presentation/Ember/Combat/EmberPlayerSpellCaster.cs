using UnityEngine;
using EmberCrpg.Presentation.Ember.Adapters;
using System.Collections;

namespace EmberCrpg.Presentation.Ember.Combat
{
    public sealed class EmberPlayerSpellCaster : MonoBehaviour
    {
        private Transform _eye;
        private LineRenderer _line;

        private void Awake()
        {
            _eye = transform.Find("EyeCamera");
            
            var go = new GameObject("SpellRipple", typeof(LineRenderer));
            go.transform.SetParent(transform, false);
            _line = go.GetComponent<LineRenderer>();
            _line.startWidth = 0.05f;
            _line.endWidth = 0.5f;
            _line.positionCount = 2;
            _line.enabled = false;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.material.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        }

        private void Update()
        {
            for (int i = 0; i < 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    Cast(i);
                }
            }
        }

        private void Cast(int slotIndex)
        {
            var adapter = EmberDomainAdapterLocator.Current;
            if (adapter == null) return;

            var spells = adapter.SpellSlots;
            if (slotIndex >= spells.Count) return;

            string spellName = spells[slotIndex];

            // Codex audit (third pass A-P1): previously the spell input only
            // wrote a log line — no mana, cooldown, terrain, or world-event
            // mutation. Route the cast through IPlayerCommandSink.TryCastSpell;
            // the adapter is now responsible for delegating to SpellResolver
            // and surfacing failure via LogCombat. If the command sink reports
            // false we still flash the visual (UX feedback) and write a
            // fallback log line for placeholder adapters.
            bool routed = adapter.TryCastSpell(slotIndex);
            if (!routed)
                adapter.LogCombat($"You cast {spellName}!");

            if (_eye != null)
            {
                StopAllCoroutines();
                StartCoroutine(ShowRipple());
            }
        }

        private IEnumerator ShowRipple()
        {
            _line.enabled = true;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                _line.SetPosition(0, _eye.position + _eye.forward * 0.5f);
                _line.SetPosition(1, _eye.position + _eye.forward * (0.5f + t * 5f));
                
                var color = _line.material.color;
                color.a = 1f - t;
                _line.material.color = color;
                
                yield return null;
            }

            _line.enabled = false;
        }
    }
}

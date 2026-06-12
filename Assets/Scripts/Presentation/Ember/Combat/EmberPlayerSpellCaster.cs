using UnityEngine;
using EmberCrpg.Presentation.Ember.Adapters;
using System.Collections;
using EmberCrpg.Presentation.Ember.Inputs;

namespace EmberCrpg.Presentation.Ember.Combat
{
    public sealed class EmberPlayerSpellCaster : MonoBehaviour
    {
        private Transform _eye;
        private Transform _bolt;
        private Material _boltMaterial;
        private Light _boltLight;

        private void Awake()
        {
            _eye = transform.Find("EyeCamera");

            // PLAYTEST FIX ("büyü çok ince bir çizgi/çapraz görünüyor"): the old vfx was a LineRenderer
            // fired along the view axis — edge-on it reads as a hair-thin cross. The bolt is now a
            // CAMERA-FACING glowing quad + its own point light, so it reads as a fireball from any angle.
            var bolt = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bolt.name = "SpellBolt";
            var boltCollider = bolt.GetComponent<Collider>();
            if (boltCollider != null) Destroy(boltCollider);
            // Build-safe shader resolution (the old "magenta after ~1 minute" lesson): Sprites/Default is
            // Always-Included; URP fallbacks keep a null shader from ever reaching the material.
            var boltShader = Shader.Find("Sprites/Default")
                             ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                             ?? Shader.Find("Universal Render Pipeline/Unlit");
            _boltMaterial = new Material(boltShader);
            _boltMaterial.color = new Color(1f, 0.62f, 0.25f, 0.95f);
            bolt.GetComponent<MeshRenderer>().sharedMaterial = _boltMaterial;
            bolt.transform.localScale = Vector3.one * 0.35f;
            bolt.AddComponent<EmberCrpg.Presentation.Ember.Views.CameraFacingBillboard>();
            _boltLight = bolt.AddComponent<Light>();
            _boltLight.type = LightType.Point;
            _boltLight.color = new Color(1f, 0.55f, 0.2f);
            _boltLight.intensity = 2.4f;
            _boltLight.range = 6f;
            _boltLight.shadows = LightShadows.None;
            _bolt = bolt.transform;
            bolt.SetActive(false);
        }

        private void Update()
        {
            for (int i = 0; i < 5; i++)
            {
                if (EmberInput.NumberKeyDown(i + 1))
                {
                    Cast(i);
                }
            }
        }

        private void Cast(int slotIndex)
        {
            // Codex audit (sixth pass A-P1 #8): bail out when a modal
            // (dialog / inventory) panel owns the input. Prevents the
            // dialog topic chooser's Alpha key from also firing a spell.
            if (EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldHost.IsModalOpen()) return;
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
            // Codex audit (fourth pass A-P2): when TryCastSpell returns false
            // the adapter has already logged a refusal reason (insufficient
            // mana / no caster / slot out of range). Do NOT overwrite that
            // with a fake "You cast ..." success — leave the refusal text
            // visible so the player understands the cast did not fire.
            // The bolt flies only when the cast actually FIRED — a refused cast (no target in range,
            // insufficient mana) keeps its refusal text and shows nothing, instead of lying visually.
            bool castFired = adapter.TryCastSpell(slotIndex);

            if (castFired && _eye != null)
            {
                StopAllCoroutines();
                StartCoroutine(FlyBolt());
            }
        }

        private IEnumerator FlyBolt()
        {
            _bolt.gameObject.SetActive(true);
            Vector3 from = _eye.position + _eye.forward * 0.6f - _eye.up * 0.15f;
            Vector3 to = from + _eye.forward * 8f; // matches FLAME BOLT's 8-tile reach
            const float duration = 0.28f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _bolt.position = Vector3.Lerp(from, to, t);
                _boltLight.intensity = 2.4f * (1f - t * 0.6f);
                var c = _boltMaterial.color; c.a = 0.95f * (1f - t * 0.4f); _boltMaterial.color = c;
                yield return null;
            }
            _bolt.gameObject.SetActive(false);
        }
    }
}

using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>F28: the cast path publishes signature-spell state here; the rig-side view and the
    /// bolt vfx consume it. Static channel (the field-mirror family).</summary>
    public static class RuntimeSpellFxMirror
    {
        public static string LastCastTemplate = string.Empty; // bolt tint key
        public static float LightUntilRealtime;               // lantern_glow orb window
        public static float HasteUntilRealtime;               // wind_step (presentation PARTIAL)
        public static bool RecallRequested;                   // recall_gate rig snap (one-shot)
    }

    /// <summary>
    /// F28: rig-side consumer — keeps a warm light ORB alive while lantern_glow lasts, and snaps the
    /// rig to the recorded player spawn when recall_gate fires (the actor already moved in-world).
    /// Attached next to the audio director on the player rig.
    /// </summary>
    public sealed class RuntimeSpellFxView : MonoBehaviour
    {
        private GameObject _orb;
        private Light _orbLight;

        private void Update()
        {
            bool lightOn = Time.unscaledTime < RuntimeSpellFxMirror.LightUntilRealtime;
            if (lightOn && _orb == null)
            {
                _orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _orb.name = "LanternGlowOrb";
                Destroy(_orb.GetComponent<Collider>());
                _orb.transform.SetParent(transform, worldPositionStays: false);
                // Held-lantern pose: below-right-front of the EYE (~1.6) so the orb itself reads in
                // first person — at the old overhead spot only its light reached the frame.
                _orb.transform.localPosition = new Vector3(0.35f, 1.45f, 0.85f);
                _orb.transform.localScale = Vector3.one * 0.28f;
                _orb.GetComponent<MeshRenderer>().sharedMaterial =
                    RuntimeMaterialPalette.Solid(new Color(1f, 0.92f, 0.6f));
                _orbLight = _orb.AddComponent<Light>();
                _orbLight.type = LightType.Point;
                _orbLight.color = new Color(1f, 0.88f, 0.55f);
                _orbLight.range = 16f;
                _orbLight.intensity = 2.6f;
                _orbLight.shadows = LightShadows.None;
                Debug.Log("[Spell] lantern glow orb lit (60s).");
            }
            else if (!lightOn && _orb != null)
            {
                Destroy(_orb);
                _orb = null;
            }

            if (RuntimeSpellFxMirror.RecallRequested)
            {
                RuntimeSpellFxMirror.RecallRequested = false;
                var spawn = RuntimePlayerSpawn.Position;
                if (spawn != Vector3.zero)
                {
                    var cc = GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    transform.position = spawn + Vector3.up * 0.2f;
                    if (cc != null) cc.enabled = true;
                    Debug.Log("[Spell] recall gate: rig snapped to the settlement spawn.");
                }
            }
        }
    }
}

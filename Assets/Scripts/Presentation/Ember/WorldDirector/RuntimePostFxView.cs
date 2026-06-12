using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F33: the URP post-FX rig — a runtime-built GLOBAL Volume (light bloom + vignette + colour
    /// grade) attached next to the rig's other directors. The grade follows CONTEXT: each delve
    /// archetype gets its own cast (warm cave / cold crypt / sickly ruin), the open world stays
    /// near-neutral. Built entirely in code (no Volume assets); whether the UberPost variants
    /// survive the player build is exactly what the before/after proof frames must show.
    /// </summary>
    public sealed class RuntimePostFxView : MonoBehaviour
    {
        private Volume _volume;
        private Bloom _bloom;
        private Vignette _vignette;
        private ColorAdjustments _grade;
        private float _nextPoll;
        private string _context = "";

        private void Start()
        {
            var camera = GetComponentInChildren<UnityEngine.Camera>(true);
            if (camera != null)
            {
                var data = camera.GetComponent<UniversalAdditionalCameraData>()
                           ?? camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
            }

            var go = new GameObject("RuntimePostFxVolume");
            go.transform.SetParent(transform, false);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _bloom = profile.Add<Bloom>();
            _bloom.intensity.Override(0.65f);
            _bloom.threshold.Override(1.05f);
            _vignette = profile.Add<Vignette>();
            _vignette.intensity.Override(0.22f);
            _vignette.smoothness.Override(0.45f);
            _grade = profile.Add<ColorAdjustments>();
            _volume.profile = profile;
            Debug.Log("[PostFx] runtime volume armed: bloom 0.65 + vignette 0.22 + context grade.");
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextPoll || _grade == null) return;
            _nextPoll = Time.unscaledTime + 2f;

            // Context: inside a realized delve the grade wears the archetype; outside, near-neutral.
            bool inDelve = GameObject.Find("DungeonInterior") != null;
            string context = inDelve ? RuntimeDungeonLayoutInfo.ArchetypeName : "overworld";
            if (context == _context) return;
            _context = context;

            switch (context)
            {
                case "Kripta": // cold grave-light
                    _grade.postExposure.Override(0f);
                    _grade.saturation.Override(-14f);
                    _grade.colorFilter.Override(new Color(0.88f, 0.96f, 1.05f));
                    break;
                case "Harabe": // sickly green-gold
                    _grade.postExposure.Override(0f);
                    _grade.saturation.Override(-8f);
                    _grade.colorFilter.Override(new Color(0.97f, 1.03f, 0.88f));
                    break;
                case "Mağara": // warm firelit dark
                    _grade.postExposure.Override(-0.1f);
                    _grade.saturation.Override(-4f);
                    _grade.colorFilter.Override(new Color(1.05f, 0.97f, 0.9f));
                    break;
                default: // overworld: a whisper of warmth, nothing else
                    _grade.postExposure.Override(0f);
                    _grade.saturation.Override(4f);
                    _grade.colorFilter.Override(Color.white);
                    break;
            }
            Debug.Log($"[PostFx] grade context={context}.");
        }
    }
}

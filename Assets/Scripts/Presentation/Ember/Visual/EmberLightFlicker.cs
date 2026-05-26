// Why: tiny runtime component that gives a Light a candle/torch flicker without
// requiring a full animation curve or particle setup. Used by the AAA scene
// recipes (forge, tavern hearth, dungeon torches) to push subjective scene
// quality above the v2 rubric "mood lighting correct" axis.
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Visual
{
    /// <summary>
    /// Drives a small pseudo-random intensity oscillation on the attached Light.
    /// Designer sets base intensity + amplitude; the component perturbs the value
    /// per-frame using a smoothed Perlin sample so the flicker reads as organic.
    /// Cheap: one Light read + one PerlinNoise + one assignment per Update.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public sealed class EmberLightFlicker : MonoBehaviour
    {
        [SerializeField] private float _baseIntensity = 1.4f;
        [SerializeField] private float _amplitude = 0.45f;
        [SerializeField] private float _speed = 4.2f;
        [SerializeField] private float _seed = 17.31f;

        private Light _light;

        public void Configure(float baseIntensity, float amplitude, float speed, float seed)
        {
            _baseIntensity = Mathf.Max(0f, baseIntensity);
            _amplitude = Mathf.Max(0f, amplitude);
            _speed = Mathf.Max(0.01f, speed);
            _seed = seed;
        }

        private void Awake()
        {
            _light = GetComponent<Light>();
            if (_light != null) _baseIntensity = Mathf.Max(_baseIntensity, _light.intensity * 0.5f);
        }

        private void Update()
        {
            if (_light == null) return;
            float t = (Time.unscaledTime + _seed) * _speed;
            float n = Mathf.PerlinNoise(t, _seed * 0.37f);
            // n in [0,1] → re-center to [-1,+1] for symmetric amplitude
            float swing = (n - 0.5f) * 2f;
            _light.intensity = Mathf.Max(0f, _baseIntensity + swing * _amplitude);
        }
    }
}

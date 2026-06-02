using EmberCrpg.Presentation.Ember.Adapters;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Day/night sky for the generated world, driven by the SIM's time of day (240 ticks = one game day).
    ///
    /// The sky was BLACK because the eye camera clears to a near-black SolidColor (RuntimePlayerRig) and the
    /// procedural skybox shader is stripped out of player builds (Shader.Find returns null at runtime), so nothing
    /// was ever drawn behind the world. Rather than fight shader stripping, this controller paints the sky as the
    /// camera's clear colour and cycles it blue -> dusk-amber -> navy across the day, while rotating + dimming the
    /// sun (the moving shadows the player already sees). This is the deterministic, build-safe baseline; a
    /// generated painterly sky image can later be cross-faded on top without changing this controller's shape.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkyController : MonoBehaviour
    {
        private const int TicksPerDay = 240;

        // Sky clear colour through the day. Dawn/dusk warm the horizon; night is a deep navy (never pure black).
        private static readonly Color DaySky = new Color(0.40f, 0.62f, 0.86f);
        private static readonly Color NightSky = new Color(0.03f, 0.05f, 0.12f);
        private static readonly Color DuskSky = new Color(0.78f, 0.42f, 0.26f);

        private Light _sun;
        private Color _baseAmbient = Color.gray;
        private UnityEngine.Camera _camera;

        public void Bind(Light sun)
        {
            _sun = sun;
            _baseAmbient = RenderSettings.ambientLight;
            ResolveCamera();
        }

        private void ResolveCamera()
        {
            _camera = UnityEngine.Camera.main;
            if (_camera != null)
                _camera.clearFlags = CameraClearFlags.SolidColor; // we own the clear colour as the sky
        }

        private void Update()
        {
            float dayFraction = DayFraction();

            // Daylight curve: 0 at midnight, 1 at noon. Dusk peaks at dawn/sunset (used to warm the sky).
            float daylight = Mathf.Clamp01((Mathf.Sin((dayFraction * Mathf.PI * 2f) - (Mathf.PI * 0.5f)) * 0.5f) + 0.5f);
            float dusk = 1f - Mathf.Abs(daylight - 0.5f) * 2f;

            if (_sun != null)
            {
                float sunPitch = (dayFraction * 360f) - 90f;
                _sun.transform.rotation = Quaternion.Euler(sunPitch, 150f, 0f);
                _sun.intensity = Mathf.Lerp(0.04f, 1.15f, daylight);
            }

            RenderSettings.ambientLight = _baseAmbient * Mathf.Lerp(0.22f, 1f, daylight);

            if (_camera == null) ResolveCamera();
            if (_camera != null)
            {
                Color sky = Color.Lerp(NightSky, DaySky, daylight);
                sky = Color.Lerp(sky, DuskSky, dusk * 0.45f);
                _camera.backgroundColor = sky;
            }
        }

        private static float DayFraction()
        {
            var clock = EmberDomainAdapterLocator.Current;
            if (clock != null)
                return (clock.TickIndex % TicksPerDay) / (float)TicksPerDay;
            return (Time.time % 120f) / 120f; // real-time fallback: a 2-minute day
        }
    }
}

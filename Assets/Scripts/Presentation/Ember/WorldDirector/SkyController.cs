using EmberCrpg.Presentation.Ember.Adapters;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Day/night sky for the generated world. The realized scene had a BLACK sky (no skybox). This sets a
    /// procedural skybox and drives the sun rotation + brightness + ambient from the SIM's time of day, so the
    /// game's days and nights are actually visible (240 ticks = one game day). Falls back to a slow real-time
    /// cycle when no sim clock is registered. Seasons/weather tinting can extend DayFraction/colour later
    /// without changing this controller's shape.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkyController : MonoBehaviour
    {
        private const int TicksPerDay = 240;

        private Light _sun;
        private Color _baseAmbient = Color.gray;

        public void Bind(Light sun)
        {
            _sun = sun;
            _baseAmbient = RenderSettings.ambientLight;

            var shader = Shader.Find("Skybox/Procedural");
            if (shader != null)
            {
                var sky = new Material(shader);
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.05f);
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1.0f);
                RenderSettings.skybox = sky;
            }
            RenderSettings.sun = sun;
            DynamicGI.UpdateEnvironment();
        }

        private void Update()
        {
            if (_sun == null) return;

            float dayFraction = DayFraction();

            // Sun pitch over the day: below the horizon at night, high at noon (dayFraction 0.5).
            float sunPitch = (dayFraction * 360f) - 90f;
            _sun.transform.rotation = Quaternion.Euler(sunPitch, 150f, 0f);

            // Daylight curve: 0 at midnight, 1 at noon.
            float daylight = Mathf.Clamp01((Mathf.Sin((dayFraction * Mathf.PI * 2f) - (Mathf.PI * 0.5f)) * 0.5f) + 0.5f);
            _sun.intensity = Mathf.Lerp(0.04f, 1.15f, daylight);
            RenderSettings.ambientLight = _baseAmbient * Mathf.Lerp(0.22f, 1f, daylight);
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
                RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(0.25f, 1.3f, daylight));
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

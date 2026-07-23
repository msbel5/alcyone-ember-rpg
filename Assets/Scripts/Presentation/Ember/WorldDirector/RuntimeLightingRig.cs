using EmberCrpg.Domain.Overland;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Runtime ambient + directional sun for the generated location. A direct logic port of the editor-only
    /// EmberLightingBuilder (whose AddDirectionalSun/SetAmbientMood were already free of UnityEditor APIs) so
    /// the realized scene is lit without the editor assembly. Ambient tone follows the biome.
    /// </summary>
    public static class RuntimeLightingRig
    {
        public static void Apply(Transform parent, BiomeKind biome)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            // R2 (live-editor probe): raw biome ground colour as FLAT ambient washed the whole
            // scene mint-green. Keep a 35% biome mood over a neutral base - shader-independent,
            // so this lever works in stripped-fog player builds as well.
            RenderSettings.ambientLight = Color.Lerp(
                new Color(0.44f, 0.44f, 0.46f),
                RuntimeMaterialPalette.GroundColor(biome), 0.35f) * 1.15f;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = false;

            var sun = new GameObject("Sun");
            sun.transform.SetParent(parent, worldPositionStays: false);
            sun.transform.rotation = Quaternion.Euler(48f, 140f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.93f, 0.80f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;

            // Sky: a procedural skybox + a day/night cycle driven by the sim clock, so the sky is no longer
            // black and the game's days/nights are visible. The controller drives the sun + ambient each frame.
            sun.AddComponent<SkyController>().Bind(light);
            // F25: per-day deterministic weather (rain/fog/snow particles + fog + rain hiss) by biome+season.
            sun.AddComponent<RuntimeWeatherController>().Bind(biome);
        }
    }
}

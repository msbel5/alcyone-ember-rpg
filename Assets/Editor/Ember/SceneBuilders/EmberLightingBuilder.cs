// Why: scene recipes call into here to construct the per-scene mood lighting
// (warm forge, cold dungeon, candle tavern, amber shrine). One builder per
// stable concept so recipe code reads as a designer specification rather than
// a Unity GameObject hierarchy assembly.
using EmberCrpg.Presentation.Ember.Visual;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Light rig builder used by every AAA scene recipe. All methods create a
    /// fresh named GameObject; recipes are expected to wipe-and-rebuild via the
    /// SceneRecipe pipeline so duplicate runs are not a real concern.
    /// </summary>
    public static class EmberLightingBuilder
    {
        public static GameObject AddDirectionalSun(
            Color color,
            float intensity,
            Vector3 eulerAngles,
            string name = "Sun")
        {
            var go = new GameObject(name);
            go.transform.rotation = Quaternion.Euler(eulerAngles);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            return go;
        }

        /// <summary>Static colored point light. Use for hero focal props (forge glow, anvil emission, hearth core).</summary>
        public static GameObject AddPointLight(
            string name,
            Vector3 position,
            Color color,
            float intensity,
            float range,
            bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return go;
        }

        /// <summary>Point light with attached EmberLightFlicker (candles, torches, forge sparks glow).</summary>
        public static GameObject AddFlickeringPointLight(
            string name,
            Vector3 position,
            Color color,
            float baseIntensity,
            float amplitude,
            float range,
            float speed = 4.2f,
            float seed = 17.31f)
        {
            var go = AddPointLight(name, position, color, baseIntensity, range, shadows: false);
            var flicker = go.AddComponent<EmberLightFlicker>();
            flicker.Configure(baseIntensity, amplitude, speed, seed);
            return go;
        }

        /// <summary>
        /// Sets URP ambient mood via RenderSettings. Recipes call this once at the
        /// start of Build() to set the scene's base tonal floor (cold-dungeon-blue,
        /// candle-amber, forge-orange-tinted etc.).
        /// </summary>
        public static void SetAmbientMood(Color color, float intensity = 1f, float fogStart = 0f, float fogEnd = 60f, bool enableFog = false, Color? fogColor = null)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = color * Mathf.Max(0f, intensity);
            RenderSettings.ambientIntensity = Mathf.Clamp(intensity, 0.1f, 2f);
            RenderSettings.fog = enableFog;
            if (enableFog)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = fogStart;
                RenderSettings.fogEndDistance = fogEnd;
                RenderSettings.fogColor = fogColor ?? color;
            }
        }

        /// <summary>Soft fill spot light used to backlight a focal prop (rim light).</summary>
        public static GameObject AddSpotFill(
            string name,
            Vector3 position,
            Vector3 eulerAngles,
            Color color,
            float intensity,
            float range,
            float spotAngle = 55f)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(eulerAngles);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = spotAngle;
            light.shadows = LightShadows.None;
            return go;
        }
    }
}

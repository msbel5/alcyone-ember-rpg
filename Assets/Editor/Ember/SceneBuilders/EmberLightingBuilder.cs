using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Adds a single directional light shaped like a warm sun plus a soft ambient floor.
    /// Recipe code calls <see cref="AddDirectionalSun"/> once per scene; ambient is left
    /// to the scene's <c>RenderSettings</c> which we don't mutate from here.
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
    }
}

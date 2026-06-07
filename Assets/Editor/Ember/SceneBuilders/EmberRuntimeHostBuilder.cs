using UnityEngine;
using UnityEditor;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Adds the one runtime host every generated acceptance scene needs. Recipes build
    /// world geometry and UI; the host binds those views to deterministic placeholder
    /// sources until the backend adapter is connected.
    /// </summary>
    public static class EmberRuntimeHostBuilder
    {
        public static GameObject EnsureHost()
        {
            var hostType = ResolveRuntimeType("EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldHost");
            var existingComponent = hostType != null
                ? Object.FindFirstObjectByType(hostType, FindObjectsInactive.Include)
                : null;
            if (existingComponent is Component existingHost)
            {
                return existingHost.gameObject;
            }

            var host = new GameObject("EmberWorldHost");
            AddRuntimeComponent(host, hostType, "EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldHost");
            AddAmbientAudio(host);
            return host;
        }

        private static void AddAmbientAudio(GameObject host)
        {
            var type = ResolveRuntimeType("EmberCrpg.Presentation.Ember.Audio.EmberAmbientAudio");
            if (type == null) return;

            var comp = host.AddComponent(type);
            var sceneName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
            bool indoors = sceneName.Contains("Dungeon") || sceneName.Contains("Hall") || sceneName.Contains("Tavern") || sceneName.Contains("Shrine");
            
            var serialized = new SerializedObject(comp);
            var prop = serialized.FindProperty("_type");
            if (prop != null)
            {
                prop.enumValueIndex = indoors ? 1 : 0; // 1 = Indoors, 0 = Outdoors
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AddRuntimeComponent(GameObject host, System.Type type, string fullName)
        {
            if (type == null)
            {
                Debug.LogWarning($"Could not resolve runtime component {fullName}");
                return;
            }

            host.AddComponent(type);
        }

        private static System.Type ResolveRuntimeType(string fullName)
        {
            var qualified = System.Type.GetType(fullName + ", EmberCrpg.Presentation");
            if (qualified != null) return qualified;

            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }
    }
}

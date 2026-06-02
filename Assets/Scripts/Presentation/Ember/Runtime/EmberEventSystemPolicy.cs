using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace EmberCrpg.Presentation.Ember.Runtime
{
    public static class EmberEventSystemPolicy
    {
        public static EventSystem EnsureInputSystemEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
            {
                var go = new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                return go.GetComponent<EventSystem>();
            }

            var hasInputModule = false;
            foreach (var module in eventSystem.GetComponents<BaseInputModule>())
            {
                if (module is InputSystemUIInputModule)
                {
                    hasInputModule = true;
                }
                else
                {
                    module.enabled = false;
                    Object.Destroy(module);
                }
            }

            if (!hasInputModule)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            return eventSystem;
        }
    }
}

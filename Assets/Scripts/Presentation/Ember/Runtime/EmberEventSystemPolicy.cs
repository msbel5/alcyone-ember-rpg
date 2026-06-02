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

    public static class WorldHostInputPolicy
    {
        public static bool IsModalOpen()
        {
            return Object.FindFirstObjectByType<UI.DialogBoxPanel>(FindObjectsInactive.Exclude) != null;
        }

        public static float StepEscapeHoldTimer(
            float currentTimer,
            bool modalOpen,
            bool pauseMenuPresent,
            bool pauseDown,
            bool pauseHeld,
            float unscaledDeltaTime,
            float holdQuitSeconds,
            System.Action onQuit)
        {
            if (modalOpen || pauseMenuPresent) return 0f;

            if (pauseDown)
            {
                ToggleCursor();
                currentTimer = 0f;
            }

            if (!pauseHeld) return 0f;

            currentTimer += unscaledDeltaTime;
            if (currentTimer <= holdQuitSeconds) return currentTimer;

            onQuit?.Invoke();
            return 0f;
        }

        public static int ResolveSelectedSpellSlot(
            bool modalOpen,
            int currentSlot,
            int spellSlotCount,
            System.Func<int, bool> numberKeyDown)
        {
            if (modalOpen) return currentSlot;
            var clampedCount = spellSlotCount < 1 ? 1 : spellSlotCount;
            for (var index = 0; index < clampedCount; index++)
            {
                if (numberKeyDown(index + 1))
                    return index;
            }

            return currentSlot;
        }

        public static float StepFateTimer(float currentTimer, float deltaTime, System.Action onExpired)
        {
            if (currentTimer <= 0f) return 0f;
            var next = currentTimer - deltaTime;
            if (next > 0f) return next;
            onExpired?.Invoke();
            return 0f;
        }

        private static void ToggleCursor()
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                ? CursorLockMode.None
                : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }
    }
}

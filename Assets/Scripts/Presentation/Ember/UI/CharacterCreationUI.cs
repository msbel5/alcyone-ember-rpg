using EmberCrpg.Presentation.Ember.CharacterCreation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class CharacterCreationUI : MonoBehaviour
    {
        [SerializeField] private string _firstSceneName = "SmithingOverworld";
        [SerializeField] private uint _defaultSeed = 42u;

        private void Awake()
        {
            EnsureEventSystemExists();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            DisableLegacyCanvasRendering();

            VisibleUiSurface.Ensure();
            var controller = GetComponent<CharacterCreationController>();
            if (controller == null) controller = gameObject.AddComponent<CharacterCreationController>();

            controller.SetStartScene(_firstSceneName);
            controller.Configure(_defaultSeed, string.Empty);

            var pending = EmberWorldGenIntent.Pending;
            var name = string.IsNullOrWhiteSpace(pending?.PlayerName) ? "Ash-Born Commander" : pending.PlayerName;
            controller.SetCommanderIdentity(name, _defaultSeed.ToString(), pending?.Mood);
        }

        private void DisableLegacyCanvasRendering()
        {
            foreach (var canvas in GetComponentsInChildren<Canvas>(true))
                canvas.enabled = false;
            foreach (var raycaster in GetComponentsInChildren<GraphicRaycaster>(true))
                raycaster.enabled = false;
        }

        private static void EnsureEventSystemExists()
        {
            if (EventSystem.current != null) return;
            var existing = FindFirstObjectByType<EventSystem>();
            if (existing != null) return;
            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}

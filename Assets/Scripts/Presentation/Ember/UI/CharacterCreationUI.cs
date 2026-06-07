using EmberCrpg.Presentation.Ember.CharacterCreation;
using EmberCrpg.Presentation.Ember.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class CharacterCreationUI : MonoBehaviour
    {
        [SerializeField] private string _firstSceneName = "SmithingOverworld";
        [SerializeField] private uint _defaultSeed = 42u;
        [SerializeField] private bool _randomizeSeed = true;

        private void Awake()
        {
            EnsureEventSystemExists();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            DisableLegacyCanvasRendering();

            VisibleUiSurface.Ensure();
            var controller = GetComponent<CharacterCreationController>();
            if (controller == null) controller = gameObject.AddComponent<CharacterCreationController>();

            // New Game rolls a fresh world seed each time so every playthrough is a different world. The seed is the
            // ONLY entropy source -- worldgen stays fully deterministic given it (same seed -> same world). A player
            // can still pin a world by typing a seed in the seed field, or by unchecking _randomizeSeed (then the
            // serialized _defaultSeed is used).
            var seed = _randomizeSeed ? RollWorldSeed() : _defaultSeed;
            controller.SetStartScene(_firstSceneName);
            controller.Configure(seed, string.Empty);

            var pending = EmberWorldGenIntent.Pending;
            var name = string.IsNullOrWhiteSpace(pending?.PlayerName) ? "Ash-Born Commander" : pending.PlayerName;
            controller.SetCommanderIdentity(name, seed.ToString(), pending?.Mood);
        }

        private static uint RollWorldSeed()
        {
            // Fresh entropy per New Game. Non-zero so EmberWorldHost treats it as an explicit seed (0 would fall
            // back to folding the char-creation answers). Presentation layer: this entropy never reaches
            // authoritative Domain/Simulation randomness, so determinism guarantees are preserved.
            var rolled = unchecked((uint)System.Guid.NewGuid().GetHashCode());
            return rolled == 0u ? 1u : rolled;
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
            _ = EmberEventSystemPolicy.EnsureInputSystemEventSystem();
        }
    }
}

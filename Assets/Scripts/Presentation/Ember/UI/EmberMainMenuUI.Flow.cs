// REF-e (LEFT-020): menu flow (New Game / Continue / Load / Quit + scene transition) split out of EmberMainMenuUI.cs (partial, zero behaviour change).
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Simulation.Generation;
using EmberCrpg.Ui.Foundation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed partial class EmberMainMenuUI
    {
        public void NewGame()
        {
            _ = NewGameAsync();
        }

        private async Task NewGameAsync()
        {
            await RunScenarioAssetTopUpAsync();
            LoadingScreen.Dismiss();
            await Task.Delay(350);
            SceneManager.LoadScene(string.IsNullOrWhiteSpace(_firstSceneName) ? EmberScenes.CharacterCreation : _firstSceneName);
        }

        public void LoadGame()
        {
            // BD-14 (EMB3-019): probe through EmberSaveService (durable file slot first, PlayerPrefs
            // legacy fallback inside the service) instead of reading PlayerPrefs("ember.save.v1")
            // directly, so the "no saves" decision matches what Continue()/in-game load will actually
            // find. If a save exists, Continue() reloads it via the same unified path.
            if (!EmberCrpg.Presentation.Ember.Save.EmberSaveService.TryResolveLatestSave(out _))
            {
                _titlePanel?.SetText("status", "No saves yet — starting a new game.");
                NewGame();
                return;
            }
            Continue();
        }

        public void Continue()
        {
            // BD-14 (EMB3-019): resolve the save through EmberSaveService so the menu uses the SAME
            // store precedence as the in-game quick-load (durable file slot first, legacy PlayerPrefs
            // blob only as a fallback INSIDE the service). Previously this read PlayerPrefs("ember.save.v1")
            // directly, which skipped the file slot and let the menu and in-game load diverge.
            if (EmberCrpg.Presentation.Ember.Save.EmberSaveService.TryResolveLatestSave(out var data)
                && IsKnownBuildScene(data.sceneName))
            {
                EmberCrpg.Presentation.Ember.Save.EmberSaveService.PreparePendingLoad(data);
                LoadingScreen.ShowForContext(new LoadingScreenContext(data.sceneName, data.sceneName, "area_transition"));
                SceneManager.LoadScene(data.sceneName);
                return;
            }
            NewGame();
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void EnsureEventSystemExists()
        {
            if (EventSystem.current != null) return;
            if (FindFirstObjectByType<EventSystem>() != null) return;
            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static bool IsKnownBuildScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return false;
#if UNITY_EDITOR
            foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
            {
                if (scene == null || string.IsNullOrEmpty(scene.path)) continue;
                var stem = Path.GetFileNameWithoutExtension(scene.path);
                if (string.Equals(stem, sceneName, System.StringComparison.Ordinal)) return true;
            }
            return false;
#else
            // LEFT-011: don't trust an arbitrary save scene name in a player build — validate against the
            // shipped build list at runtime (Application.CanStreamedLevelBeLoaded) instead of returning true.
            return Application.CanStreamedLevelBeLoaded(sceneName);
#endif
        }
    }
}

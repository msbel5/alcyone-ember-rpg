// REF-e (LEFT-020): menu flow (New Game / Continue / Load / Quit + scene transition) split out of EmberMainMenuUI.cs (partial, zero behaviour change).
using System.Threading.Tasks;
using EmberCrpg.Domain.Configuration;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Presentation.Ember.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            await Task.Delay(EmberRuntimeOptionsProvider.Current.Menu.PreSceneDelayMs);
            var sceneName = string.IsNullOrWhiteSpace(_firstSceneName)
                ? EmberRuntimeOptionsProvider.Current.Menu.FirstSceneDefault
                : _firstSceneName;
            SceneManager.LoadScene(sceneName);
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
            _ = EmberEventSystemPolicy.EnsureInputSystemEventSystem();
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static bool IsKnownBuildScene(string sceneName)
        {
            return EmberSceneCatalog.IsKnownBuildScene(sceneName);
        }
    }
}

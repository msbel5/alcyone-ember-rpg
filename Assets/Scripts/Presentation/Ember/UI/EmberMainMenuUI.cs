using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class EmberMainMenuUI : MonoBehaviour
    {
        [SerializeField] private string _firstSceneName = "Faz3SmithingOverworld";

        private void Awake()
        {
            // Mami playtest fix: buttons were unclickable because the scene
            // shipped without an EventSystem (Unity needs it for UI input
            // dispatch) AND cursor stayed locked from any previous gameplay
            // scene. Both issues now self-heal at menu Awake — no scene
            // re-authoring required.
            EnsureEventSystemExists();
            UnlockCursor();

            var newGameBtn = transform.Find("Panel/New Game")?.GetComponent<Button>();
            if (newGameBtn != null) newGameBtn.onClick.AddListener(NewGame);

            var continueBtn = transform.Find("Panel/Continue")?.GetComponent<Button>();
            if (continueBtn != null) continueBtn.onClick.AddListener(Continue);

            var quitBtn = transform.Find("Panel/Quit")?.GetComponent<Button>();
            if (quitBtn != null) quitBtn.onClick.AddListener(Quit);
        }

        private static void EnsureEventSystemExists()
        {
            if (EventSystem.current != null) return;
            var existing = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existing != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void NewGame()
        {
            SceneManager.LoadScene(_firstSceneName);
        }

        public void Continue()
        {
            string json = PlayerPrefs.GetString("ember.save.v1");
            if (!string.IsNullOrEmpty(json))
            {
                // Codex audit Batch 2 / Finding 4: the previous version loaded the
                // saved scene but never pushed the deserialized SaveData into the
                // next-scene EmberSaveService, so player transform and domain
                // state both reset on Continue. PreparePendingLoad stashes the
                // payload statically; the EmberSaveService.Start() in the target
                // scene picks it up and calls RestorePosition + RestoreStateJson.
                var data = JsonUtility.FromJson<EmberCrpg.Presentation.Ember.Save.SaveData>(json);
                if (data != null && !string.IsNullOrEmpty(data.sceneName)
                    && IsKnownBuildScene(data.sceneName))
                {
                    // Codex audit (third pass A-P3): previously LoadScene ran
                    // even when the saved scene was not in build settings,
                    // which surfaces Unity's "scene not in build" error at
                    // runtime. Validate against EditorBuildSettings (best
                    // effort — only the Editor knows the build list, so the
                    // player build skips this check and lets Unity surface
                    // the error its own way).
                    EmberCrpg.Presentation.Ember.Save.EmberSaveService.PreparePendingLoad(data);
                    SceneManager.LoadScene(data.sceneName);
                    return;
                }
            }
            NewGame();
        }

        /// <summary>
        /// Codex audit (third pass A-P3): validate scene against the build
        /// list before LoadScene. In the Editor we check EditorBuildSettings;
        /// in a player build we accept any name (Unity surfaces its own
        /// "scene not in build" error if mismatched). Defensive: never blocks
        /// the menu Continue path entirely — only an unknown Editor scene
        /// falls back to NewGame().
        /// </summary>
        private static bool IsKnownBuildScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return false;
#if UNITY_EDITOR
            foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
            {
                if (scene == null || string.IsNullOrEmpty(scene.path)) continue;
                var stem = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                if (string.Equals(stem, sceneName, System.StringComparison.Ordinal))
                    return true;
            }
            return false;
#else
            return true; // Player build trusts Unity's runtime scene resolution
#endif
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // Codex audit (sixth pass B-P2 #B3): the editor-only BuildMenuScene
        // helper used to live here behind `#if UNITY_EDITOR`. It now lives at
        // Assets/Editor/Ember/Menu/EmberMainMenuSceneBuilder.cs in the editor
        // asmdef, where it belongs. Runtime callers never used it, so this
        // file is now pure runtime UI code.
    }
}

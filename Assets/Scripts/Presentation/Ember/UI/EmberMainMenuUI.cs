// Why this file is intentionally long: it owns the legacy UGUI main-menu fallback so the shipped MainMenu scene stays playable even when only the script root exists.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class EmberMainMenuUI : MonoBehaviour
    {
        [SerializeField] private string _firstSceneName = "CharacterCreation";
        private Transform _uiRoot;

        private void Awake()
        {
            Debug.Log("[EmberMainMenuUI] Awake called.");
            EnsureEventSystemExists();
            UnlockCursor();
            _uiRoot = EnsureCanvasShell();
            EnsureFallbackMenuHierarchy();

            var newGameBtn = FindButton("New Game");
            if (newGameBtn != null) 
            {
                Debug.Log("[EmberMainMenuUI] Found New Game button.");
                newGameBtn.onClick.AddListener(NewGame);
            }
            else
            {
                Debug.LogWarning("[EmberMainMenuUI] FAILED to find New Game button.");
            }

            var continueBtn = FindButton("Continue");
            if (continueBtn != null) continueBtn.onClick.AddListener(Continue);

            var quitBtn = FindButton("Quit");
            if (quitBtn != null) quitBtn.onClick.AddListener(Quit);
        }

        private static void EnsureEventSystemExists()
        {
            if (EventSystem.current != null) return;
            var existing = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existing != null) return;
            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void NewGame()
        {
            Debug.Log("[EmberMainMenuUI] NewGame invoked.");
            var worldGen = FindDeep(transform, "WorldGenUI");
            if (worldGen != null)
            {
                Debug.Log("[EmberMainMenuUI] Activating WorldGenUI.");
                worldGen.gameObject.SetActive(true);
                FindDeep(transform, "Panel")?.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[EmberMainMenuUI] WorldGenUI NOT found, loading scene.");
                SpawnLoadingScreen();
                SceneManager.LoadScene(string.IsNullOrWhiteSpace(_firstSceneName) ? "CharacterCreation" : _firstSceneName);
            }
        }

        private Transform EnsureCanvasShell()
        {
            var root = transform.Find("RuntimeCanvas");
            if (root == null)
            {
                var go = new GameObject("RuntimeCanvas", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                root = go.transform;
            }

            var rect = (RectTransform)root;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var canvas = root.GetComponent<Canvas>();
            if (canvas == null) canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            if (root.GetComponent<GraphicRaycaster>() == null) root.gameObject.AddComponent<GraphicRaycaster>();
            return root;
        }

        private void EnsureFallbackMenuHierarchy()
        {
            var root = _uiRoot == null ? transform : _uiRoot;
            if (FindButton("New Game") != null) return;

            var panel = CreateRect("Panel", root, Vector2.zero, Vector2.one);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.07f, 0.055f, 0.045f, 0.96f);

            var title = CreateText("Title", panel, "EMBER CRPG", 44, TextAnchor.MiddleCenter);
            title.color = new Color(0.95f, 0.72f, 0.42f);
            SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(520f, 80f));

            var subtitle = CreateText("Subtitle", panel, "visible generation cutover", 20, TextAnchor.MiddleCenter);
            subtitle.color = new Color(0.72f, 0.64f, 0.54f);
            SetAnchored(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(520f, 40f));

            var buttons = CreateRect("Buttons", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            buttons.sizeDelta = new Vector2(360f, 230f);
            buttons.anchoredPosition = new Vector2(0f, -40f);
            var layout = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            CreateButton(buttons, "New Game");
            CreateButton(buttons, "Continue");
            CreateButton(buttons, "Quit");
        }

        private Button FindButton(string name)
        {
            return FindDeep(transform, name)?.GetComponent<Button>();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Text CreateText(string name, Transform parent, string text, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.alignment = anchor;
            label.color = Color.white;
            return label;
        }

        private static void CreateButton(Transform parent, string label)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(360f, 54f);
            go.GetComponent<Image>().color = new Color(0.18f, 0.115f, 0.075f, 0.95f);
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.37f, 0.22f, 0.12f, 1f);
            colors.pressedColor = new Color(0.55f, 0.29f, 0.11f, 1f);
            button.colors = colors;

            var text = CreateText("Text", go.transform, label, 22, TextAnchor.MiddleCenter);
            text.color = new Color(0.93f, 0.84f, 0.68f);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        /// <summary>
        /// Audit (eighth pass D-P1): EmberLoadingScreen was never instantiated.
        /// Spin one up DontDestroyOnLoad before transitioning so the next
        /// scene's first frames are masked by the fade.
        /// </summary>
        private static void SpawnLoadingScreen()
        {
            if (EmberLoadingScreen.Instance != null) return;
            var go = new GameObject("EmberLoadingScreen", typeof(EmberLoadingScreen));
            // DontDestroyOnLoad is applied inside EmberLoadingScreen.Awake.
            UnityEngine.Object.DontDestroyOnLoad(go);
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
                    SpawnLoadingScreen();
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

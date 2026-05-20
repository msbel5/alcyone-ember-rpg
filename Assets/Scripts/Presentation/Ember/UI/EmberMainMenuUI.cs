using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class EmberMainMenuUI : MonoBehaviour
    {
        [SerializeField] private string _firstSceneName = "Faz3SmithingOverworld";

        private void Awake()
        {
            var newGameBtn = transform.Find("Panel/New Game")?.GetComponent<Button>();
            if (newGameBtn != null) newGameBtn.onClick.AddListener(NewGame);

            var continueBtn = transform.Find("Panel/Continue")?.GetComponent<Button>();
            if (continueBtn != null) continueBtn.onClick.AddListener(Continue);

            var quitBtn = transform.Find("Panel/Quit")?.GetComponent<Button>();
            if (quitBtn != null) quitBtn.onClick.AddListener(Quit);
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
                // The EmberSaveService in the target scene will handle the pending load
                // if we set the static flag. But we need to load the scene first.
                var data = JsonUtility.FromJson<EmberCrpg.Presentation.Ember.Save.SaveData>(json);
                SceneManager.LoadScene(data.sceneName);
            }
            else
            {
                NewGame();
            }
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #if UNITY_EDITOR
                public static void BuildMenuScene(string path)
                {
                    var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
            
                    var canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    var canvas = canvasObj.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
                    var menu = canvasObj.AddComponent<EmberMainMenuUI>();
            
                    var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                    panel.transform.SetParent(canvas.transform, false);
                    var rt = panel.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f);

                    var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
                    title.transform.SetParent(panel.transform, false);
                    var titleRt = title.GetComponent<RectTransform>();
                    titleRt.anchorMin = new Vector2(0.5f, 0.8f);
                    titleRt.anchorMax = new Vector2(0.5f, 0.8f);
                    titleRt.sizeDelta = new Vector2(400, 100);
                    var titleText = title.GetComponent<Text>();
                    titleText.text = "EMBER";
                    titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    titleText.fontSize = 72;
                    titleText.alignment = TextAnchor.MiddleCenter;
                    titleText.color = new Color(1f, 0.8f, 0.2f);

                    string[] buttons = { "New Game", "Continue", "Quit" };
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        var btnObj = new GameObject(buttons[i], typeof(RectTransform), typeof(Image), typeof(Button));
                        btnObj.transform.SetParent(panel.transform, false);
                        var btnRt = btnObj.GetComponent<RectTransform>();
                        btnRt.anchorMin = new Vector2(0.5f, 0.5f - i * 0.15f);
                        btnRt.anchorMax = new Vector2(0.5f, 0.5f - i * 0.15f);
                        btnRt.sizeDelta = new Vector2(200, 50);
                
                        btnObj.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f);
                        var btn = btnObj.GetComponent<Button>();
                
                        var txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                        txtObj.transform.SetParent(btnObj.transform, false);
                        var txtRt = txtObj.GetComponent<RectTransform>();
                        txtRt.anchorMin = Vector2.zero;
                        txtRt.anchorMax = Vector2.one;
                        txtRt.offsetMin = Vector2.zero;
                        txtRt.offsetMax = Vector2.zero;
                        var txt = txtObj.GetComponent<Text>();
                        txt.text = buttons[i];
                        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        txt.alignment = TextAnchor.MiddleCenter;
                        txt.color = Color.white;

                        int index = i;
                        // We'll use UnityEvent in editor to wire these if we were using a real builder,
                        // but since we are script-generating the scene, we'll just use code.
                        // Note: Persistent listeners are better for prefabs, but for a one-off scene:
                    }
            
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
                }
        #endif
}
}

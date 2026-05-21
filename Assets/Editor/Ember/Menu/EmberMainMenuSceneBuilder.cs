using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using EmberCrpg.Presentation.Ember.UI;

namespace EmberCrpg.Editor.Ember.Menu
{
    /// <summary>
    /// Editor-only helper that materialises the EmberMainMenu scene from code so
    /// build menu scaffolds and prototype scenes do not have to be authored by
    /// hand.
    /// Codex audit (sixth pass B-P2 #B3): previously lived on EmberMainMenuUI as
    /// `#if UNITY_EDITOR public static void BuildMenuScene(string path)` — an
    /// editor-only entry point that bled into the runtime UI script. Moving it
    /// here keeps the Presentation asmdef purely runtime and matches the
    /// other Editor/Ember/Menu/* utilities (e.g. EmberBuildSettingsMenu).
    /// </summary>
    public static class EmberMainMenuSceneBuilder
    {
        public static void BuildMenuScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

            var canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasObj.AddComponent<EmberMainMenuUI>();

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
                // Button click handlers are wired by the runtime EmberMainMenuUI
                // MonoBehaviour at scene start (Awake/Start). The Editor builder
                // only authors the GameObject layout, never the listeners.
            }

            EditorSceneManager.SaveScene(scene, path);
        }
    }
}

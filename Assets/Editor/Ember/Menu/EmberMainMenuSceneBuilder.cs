using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Editor.Ember.SceneBuilders;

namespace EmberCrpg.Editor.Ember.Menu
{
    /// <summary>
    /// AAA Polished MainMenu builder. Uses TMP and integrates WorldGen UI.
    /// </summary>
    public static class EmberMainMenuSceneBuilder
    {
        private const string ParchmentGuid = "b259be95db4d1994b856cf6659355a50";
        private const string FontGuid = "8f586378b4e144a9851e7b34d9b748ee";

        public static void BuildMenuScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            var parchment = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(ParchmentGuid));
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FontGuid));

            var canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<EmberMainMenuUI>();

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bgImg = panel.GetComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.07f);

            // Title
            var title = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            title.transform.SetParent(panel.transform, false);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.85f);
            titleRt.anchorMax = new Vector2(0.5f, 0.85f);
            titleRt.sizeDelta = new Vector2(600, 150);
            var titleText = title.GetComponent<TextMeshProUGUI>();
            titleText.text = "EMBER";
            if (font != null) titleText.font = font;
            titleText.fontSize = 120;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.85f, 0.3f);

            var buttonRoot = new GameObject("Buttons", typeof(RectTransform));
            buttonRoot.transform.SetParent(panel.transform, false);
            var brRt = buttonRoot.GetComponent<RectTransform>();
            brRt.anchorMin = new Vector2(0.4f, 0.1f);
            brRt.anchorMax = new Vector2(0.6f, 0.6f);
            brRt.sizeDelta = Vector2.zero;

            string[] buttons = { "New Game", "Continue", "Quit" };
            for (int i = 0; i < buttons.Length; i++)
            {
                var btnObj = new GameObject(buttons[i], typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(buttonRoot.transform, false);
                var btnRt = btnObj.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0f, 1f - (i + 1) * 0.25f);
                btnRt.anchorMax = new Vector2(1f, 1f - i * 0.25f);
                btnRt.offsetMin = new Vector2(0, 10);
                btnRt.offsetMax = new Vector2(0, -10);

                var btnImg = btnObj.GetComponent<Image>();
                btnImg.color = Color.white;
                if (parchment != null) { btnImg.sprite = parchment; btnImg.type = Image.Type.Sliced; }

                var txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(btnObj.transform, false);
                var txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                var txt = txtObj.GetComponent<TextMeshProUGUI>();
                txt.text = buttons[i].ToUpper();
                if (font != null) txt.font = font;
                txt.fontSize = 24;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = new Color(0.2f, 0.1f, 0.05f);
            }

            // WorldGen UI (Hidden)
            var worldGenGo = new GameObject("WorldGenUI", typeof(RectTransform));
            worldGenGo.transform.SetParent(canvas.transform, false);
            worldGenGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            worldGenGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            worldGenGo.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            var worldGenComp = worldGenGo.AddComponent<EmberWorldGenUI>();
            
            // Assign assets via SerializedObject
            var so = new SerializedObject(worldGenComp);
            so.FindProperty("_font").objectReferenceValue = font;
            so.FindProperty("_panelFrame").objectReferenceValue = parchment;
            so.ApplyModifiedProperties();
            
            worldGenGo.SetActive(false);

            EditorSceneManager.SaveScene(scene, path);
        }
    }
}


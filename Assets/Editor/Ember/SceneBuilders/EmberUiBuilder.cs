using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Builds the screen-space overlay canvas plus reusable panel scaffolds.
    /// Updated for AAA Polish: uses parchment 9-slice frames and TMP fonts.
    /// </summary>
    public static class EmberUiBuilder
    {
        private const string ParchmentGuid = "b259be95db4d1994b856cf6659355a50";
        private const string FontGuid = "8f586378b4e144a9851e7b34d9b748ee";

        public static Canvas BuildOverlayCanvas(string name = "EmberHUD")
        {
            var root = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Audit (eighth pass E-P2): EmberMainMenuUI.EnsureEventSystemExists
            // creates a DontDestroyOnLoad EventSystem at runtime. When a Faz*
            // scene that was built via this helper loads, BOTH EventSystems
            // are present and Unity warns "Multiple EventSystems in scene...".
            // Skip creation here when one already exists in the scene.
            var existingEventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(
                FindObjectsInactive.Include);
            if (existingEventSystem == null)
            {
                var eventSystem = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                eventSystem.transform.SetParent(root.transform, worldPositionStays: false);
            }

            return canvas;
        }

        public static RectTransform BuildPanel(Canvas canvas, string name, Vector2 anchorMin, Vector2 anchorMax, Color background)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(canvas.transform, worldPositionStays: false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            var spritePath = AssetDatabase.GUIDToAssetPath(ParchmentGuid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = background;
            }
            
            return rt;
        }

        public static void AttachRuntimeScript(GameObject host, string scriptFullName)
        {
            var type = System.Type.GetType(scriptFullName + ", EmberCrpg.Presentation");
            if (type != null)
            {
                var comp = host.AddComponent(type);
                
                // Auto-assign font and frame if they exist
                var so = new SerializedObject(comp);
                var fontProp = so.FindProperty("_font");
                var frameProp = so.FindProperty("_panelFrame") ?? so.FindProperty("_backgroundFrame");
                
                if (fontProp != null && fontProp.objectReferenceValue == null)
                {
                    var fontPath = AssetDatabase.GUIDToAssetPath(FontGuid);
                    fontProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
                }
                
                if (frameProp != null && frameProp.objectReferenceValue == null)
                {
                    var spritePath = AssetDatabase.GUIDToAssetPath(ParchmentGuid);
                    frameProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                }
                
                so.ApplyModifiedProperties();
            }
        }
    }
}


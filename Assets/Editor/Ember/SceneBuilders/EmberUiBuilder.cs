using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Builds the screen-space overlay canvas plus reusable panel scaffolds.
    /// Each panel root carries a runtime view script via name lookup so the editor
    /// assembly stays decoupled from the runtime assembly.
    /// </summary>
    public static class EmberUiBuilder
    {
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

            var eventSystem = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            eventSystem.transform.SetParent(root.transform, worldPositionStays: false);

            return canvas;
        }

        public static RectTransform BuildPanel(Canvas canvas, string name, Vector2 anchorMin, Vector2 anchorMax, Color background)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, worldPositionStays: false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = background;
            return rt;
        }

        public static void AttachRuntimeScript(GameObject host, string scriptFullName)
        {
            var type = System.Type.GetType(scriptFullName + ", EmberCrpg.Presentation");
            if (type != null) host.AddComponent(type);
        }
    }
}

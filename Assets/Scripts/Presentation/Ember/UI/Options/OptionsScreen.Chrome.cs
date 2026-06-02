using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    /// <summary>Pattern: composite nav host chrome. Why: shell styling stays shared while host logic stays small.</summary>
    public sealed partial class OptionsScreen
    {
        private static readonly Color Parchment = new Color(0.949f, 0.859f, 0.620f, 1f);
        private static readonly Color ParchmentDim = new Color(0.902f, 0.851f, 0.702f, 1f);
        private static readonly Color Gold = new Color(1f, 0.851f, 0.298f, 1f);
        private static readonly Color PanelBrown = new Color(0.18f, 0.14f, 0.09f, 0.92f);
        private static readonly Color PanelBrownHi = new Color(0.227f, 0.18f, 0.114f, 1f);
        private static readonly Color GoldHairline = new Color(0.949f, 0.859f, 0.620f, 0.30f);

        // Why: the main panel needs the same dark frame language as the pause/HUD shell.
        private RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = Box(name, parent, Vector2.zero, Vector2.zero);
            Place(rect, min, max, Vector2.zero, Vector2.zero);
            var image = rect.GetComponent<Image>();
            if (_panelFrame != null) { image.sprite = _panelFrame; image.type = Image.Type.Sliced; }
            return rect;
        }

        // Why: left-nav and content areas reuse one framed box builder for a consistent shell.
        private RectTransform Box(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            go.GetComponent<Image>().color = PanelBrown;
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(go.transform, false);
            Stretch(border.GetComponent<RectTransform>());
            var borderImage = border.GetComponent<Image>();
            borderImage.color = GoldHairline;
            borderImage.raycastTarget = false;
            return rect;
        }

        // Why: nav tabs and the back action share one button builder and one visual language.
        private Button Button(Transform parent, string text, UnityAction action, out Image fill)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            fill = go.GetComponent<Image>();
            fill.color = PanelBrown;
            if (_panelFrame != null) { fill.sprite = _panelFrame; fill.type = Image.Type.Sliced; }
            var button = go.GetComponent<Button>();
            button.targetGraphic = fill;
            button.onClick.AddListener(action);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 42f);
            var label = Label(go.transform, text, 17, TextAlignmentOptions.Center, Parchment);
            Stretch(label.rectTransform);
            return button;
        }

        // Why: titles and fallback copy should read like the rest of Ember's parchment UI.
        private TextMeshProUGUI Label(Transform parent, string text, float size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            if (_font != null) label.font = _font;
            label.text = text;
            label.fontSize = size;
            label.alignment = align;
            label.color = color;
            label.raycastTarget = false;
            label.outlineWidth = 0.22f;
            label.outlineColor = new Color32(0, 0, 0, 220);
            return label;
        }

        // Why: the shell uses explicit anchors so the left rail and header stay stable across resolutions.
        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        // Why: fullscreen and border children need a shared stretch helper.
        private static void Stretch(RectTransform rect)
        {
            Place(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }
    }
}

using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    public sealed partial class GeneratedAssetsSection
    {
        private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }

        private static Texture2D TryLoad(string path)
        {
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(File.ReadAllBytes(path))) return tex;
                UnityEngine.Object.Destroy(tex);
            }
            catch
            {
            }
            return null;
        }

        private void SetNote(string text)
        {
            if (_note != null) _note.text = text;
        }

        private void SetBadge(TileView tile, string text, Color color)
        {
            if (tile?.Badge == null) return;
            tile.Badge.text = text;
            tile.Badge.color = color;
        }

        private Button MakeButton(Transform parent, string text, Action action)
        {
            var button = NewRect(text, parent, typeof(Image), typeof(Button)).GetComponent<Button>();
            var fill = button.GetComponent<Image>();
            fill.color = PanelBrown;
            button.targetGraphic = fill;
            if (action != null) button.onClick.AddListener(() => action());
            var label = MakeLabel(button.transform, text, 15f, Parchment, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            return button;
        }

        private TMP_Text MakeLabel(Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var label = NewRect("Label", parent, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color32(0, 0, 0, 220);
            return label;
        }

        private static RectTransform NewRect(string name, Transform parent, params Type[] components)
        {
            var types = new Type[components.Length + 1];
            types[0] = typeof(RectTransform);
            Array.Copy(components, 0, types, 1, components.Length);
            var go = new GameObject(name, types);
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect) => Place(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }
    }
}

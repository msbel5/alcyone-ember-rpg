using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    /// <summary>Pattern: IOptionsSection content builder. Why: settings UI plugs into the existing options host without host edits.</summary>
    [UnityEngine.Scripting.Preserve]
    public sealed partial class SettingsSection : IOptionsSection
    {
        private static readonly Color Parchment = new Color(0.949f, 0.859f, 0.620f, 1f);
        private static readonly Color ParchmentDim = new Color(0.902f, 0.851f, 0.702f, 1f);
        private static readonly Color Gold = new Color(1f, 0.851f, 0.298f, 1f);
        private static readonly Color PanelBrown = new Color(0.18f, 0.14f, 0.09f, 0.92f);
        private static readonly Color GoldHairline = new Color(0.949f, 0.859f, 0.620f, 0.30f);
        private TMP_FontAsset _font;
        private Sprite _frame;

        public string Title => "Settings";
        public int Order => 10;

        // Why: each tab rebuilds its own content tree under the host-provided mount.
        public void Build(Transform contentMount)
        {
            CaptureTheme(contentMount);
            var root = Box("SettingsRoot", contentMount, false, false);
            Stretch(root, new Vector2(16f, 16f), new Vector2(-16f, -16f));
            var note = Label("Note", root, "Edits write through EmberRuntimeOptionsProvider.Current. World fallback values are used on the next world generation.", 14, TextAlignmentOptions.TopLeft, ParchmentDim);
            Place(note.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -42f), new Vector2(-12f, -8f));
            var content = ScrollContent(root);
            BuildWorld(content);
            BuildInput(content);
            BuildTiming(content);
        }

        // Why: donor font/frame keeps the section visually consistent with the surrounding shell.
        private void CaptureTheme(Transform mount)
        {
            var donorText = mount.GetComponentInParent<TMP_Text>() ?? UnityEngine.Object.FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
            _font = donorText != null ? donorText.font : null;
            var donorImage = mount.GetComponent<Image>();
            _frame = donorImage != null ? donorImage.sprite : null;
        }

        // Why: the section has more fields than the fixed content box can show at once.
        private RectTransform ScrollContent(Transform parent)
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            Place(scrollRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -52f));
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect, new Vector2(0f, 8f), new Vector2(0f, -8f));
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var content = Box("Content", viewport.transform, true, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }

        // Why: group cards keep each settings slice legible inside the shared scroll column.
        private RectTransform Section(Transform parent, string title, string note)
        {
            var panel = Box(title, parent, true, true);
            var header = Label("Header", panel, title, 20, TextAlignmentOptions.Left, Gold);
            header.fontStyle = FontStyles.Bold;
            header.characterSpacing = 4f;
            panel.gameObject.AddComponent<LayoutElement>().minHeight = 0f;
            if (!string.IsNullOrEmpty(note)) Label("GroupNote", panel, note, 13, TextAlignmentOptions.Left, ParchmentDim);
            return panel;
        }

        // Why: one row builder keeps labels, controls, and hints aligned across all groups.
        private RectTransform Row(Transform parent, string label, string hint)
        {
            var row = new GameObject(label, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            FieldLabel(row.transform, label, 220f, Parchment);
            if (!string.IsNullOrEmpty(hint)) FieldLabel(row.transform, hint, 84f, ParchmentDim, TextAlignmentOptions.MidlineRight);
            return (RectTransform)row.transform;
        }

        // Why: editable rows commit back through a single text-control pattern.
        private void Editable(Transform parent, string label, string value, string hint, Func<string, string> commit)
        {
            var row = Row(parent, label, null);
            var input = Input(row, value);
            FieldLabel(row, hint, 84f, ParchmentDim, TextAlignmentOptions.MidlineRight);
            input.onEndEdit.AddListener(text => input.text = commit(text));
        }

        // Why: restart-only values should remain visible without pretending they apply live.
        private void ReadOnly(Transform parent, string label, string value)
        {
            var row = Row(parent, label, null);
            FieldLabel(row, value, 236f, Parchment, TextAlignmentOptions.MidlineLeft);
            FieldLabel(row, "(restart)", 84f, ParchmentDim, TextAlignmentOptions.MidlineRight);
        }

        // Why: field text needs a fixed column width so long keybind paths stay readable.
        private TMP_Text FieldLabel(Transform parent, string text, float width, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var label = Label("Label", parent, text, 15, align, color);
            var layout = label.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            return label;
        }

        // Why: TMP input construction is repeated for every editable setting.
        private TMP_InputField Input(Transform parent, string value)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<LayoutElement>();
            layout.minWidth = 236f;
            layout.preferredWidth = 236f;
            go.GetComponent<Image>().color = new Color(0.12f, 0.10f, 0.08f, 0.98f);
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(go.transform, false);
            Stretch(border.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            border.GetComponent<Image>().color = GoldHairline;
            border.GetComponent<Image>().raycastTarget = false;
            var field = go.GetComponent<TMP_InputField>();
            var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(go.transform, false);
            Stretch(textArea.GetComponent<RectTransform>(), new Vector2(10f, 6f), new Vector2(-10f, -6f));
            var text = Label("Value", textArea.transform, value, 15, TextAlignmentOptions.MidlineLeft, Parchment);
            Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
            field.textViewport = textArea.GetComponent<RectTransform>();
            field.textComponent = (TextMeshProUGUI)text;
            field.text = value;
            return field;
        }

    }
}

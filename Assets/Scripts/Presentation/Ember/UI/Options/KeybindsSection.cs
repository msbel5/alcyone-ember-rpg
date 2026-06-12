using UnityEngine;
using TMPro;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    /// <summary>
    /// F32: KEYBINDS — the canonical control list, read-only (rebinding is out of scope; this
    /// screen exists so the player never has to guess a key). Auto-discovered by the registry.
    /// </summary>
    public sealed class KeybindsSection : IOptionsSection
    {
        private static readonly Color Parchment = new Color(0.949f, 0.859f, 0.620f, 1f);
        private static readonly Color Gold = new Color(1f, 0.851f, 0.298f, 1f);

        public string Title => "Keybinds";
        public int Order => 30;

        private TMP_FontAsset _font;

        // One row per REAL binding — this list mirrors EmberInput + the live screens; a key
        // listed here must do what it says (the F32 "no dead buttons" rule cuts both ways).
        private static readonly (string key, string action)[] Bindings =
        {
            ("W / A / S / D", "Move"),
            ("Mouse", "Look"),
            ("Shift", "Sprint (forward)"),
            ("Space", "Jump"),
            ("E", "Interact (talk / doors / chests / sleep / heal)"),
            ("Left click", "Melee strike"),
            ("1 - 8", "Cast spell slot"),
            ("Tab", "Inventory"),
            ("M", "World map"),
            ("J", "Journal / quests"),
            ("C", "Character sheet"),
            ("R", "Ask the oracle"),
            ("Esc", "Pause menu (save / load / options) / close screens"),
        };

        public void Build(Transform contentMount)
        {
            var donorText = contentMount.GetComponentInParent<TMP_Text>()
                ?? Object.FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
            _font = donorText != null ? donorText.font : null;

            float y = -24f;
            foreach (var (key, action) in Bindings)
            {
                AddLabel(contentMount, key, new Vector2(24f, y), Gold, 230f);
                AddLabel(contentMount, action, new Vector2(270f, y), Parchment, 460f);
                y -= 32f;
            }
        }

        private void AddLabel(Transform parent, string text, Vector2 pos, Color color, float width)
        {
            var go = new GameObject("Bind", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            if (_font != null) label.font = _font;
            label.fontSize = 17;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(width, 28f);
        }
    }
}

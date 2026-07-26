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
        // Palette moved to EmberPalette (one home for the six shared Ember UI colors).
        private static readonly Color Parchment = EmberPalette.Parchment;
        private static readonly Color Gold = EmberPalette.Gold;

        public string Title => "Keybinds";
        public int Order => 30;

        private TMP_FontAsset _font;

        // B25/B30 truth-up: this list mirrors the hard-coded switch in
        // InGameUiController.HandleScreenInput — which is what the player actually presses
        // against while the in-game UI owns input. The rebindable "Input" tab in Options
        // edits InputRuntimeOptions.* paths that only the pre-InGameUi consumers listen for
        // (EmberWorldHost.Input.cs gates them behind !InGameUiController.OwnsInput), so this
        // read-only cheatsheet is the single source of truth for what's live in-game. Every
        // row here must reflect a real KeyCode branch above; the F32 "no dead buttons" rule.
        private static readonly (string key, string action)[] Bindings =
        {
            ("W / A / S / D  +  arrows", "Move"),
            ("Mouse", "Look"),
            ("Shift", "Sprint (forward)"),
            ("Space", "Jump"),
            ("E", "Interact (talk / doors / chests / sleep / heal)"),
            ("F", "Melee swing (bound)"),
            ("Left click", "Melee strike (raycast attack)"),
            ("1 - 9", "Cast spell slot"),
            ("F1", "Toggle cursor"),
            ("F5 / F9", "Quick save / quick load"),
            ("Tab", "Open ☰ browser"),
            ("I", "Inventory"),
            ("M", "World map"),
            ("J", "Journal / quests"),
            ("K", "Colony"),
            ("C", "Character sheet"),
            ("R", "Ask the oracle"),
            ("B", "Forge / crafting"),
            ("T", "Wait one hour"),
            ("H", "Sleep to dawn"),
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

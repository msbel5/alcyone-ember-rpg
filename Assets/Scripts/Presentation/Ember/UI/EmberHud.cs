using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// In-world HUD shared by every gameplay scene. Implements the P1.5 design-system frame
    /// from <c>Reference/PRDs/PRD_frontend_action_bar_v1.md</c>:
    ///   • bottom-LEFT  : labeled HEALTH / FATIGUE / MANA bars (red / yellow / blue, with
    ///                    parchment word labels + numeric "70 / 100" overlay)
    ///   • bottom-CENTER: 12-button BG1-style context action strip (UAW_STANDARD: ATK CAST
    ///                    TALK INV CHAR MAP JOURN SRCH STLTH MODAL FORM EQUIP), gold hairline
    ///                    border, F1..F12 hotkey hints
    ///   • top-LEFT     : tick / day / weather status line (replaced by the formal clock widget
    ///                    when T-Clock lands; kept here so we don't lose the runtime breadcrumb)
    ///
    /// Vitals are read each refresh from the injected <see cref="IEmberHudSource"/> when it
    /// also implements <see cref="ICombatHudSource"/> (EmberWorldHost does), so the panel stays
    /// domain-agnostic. Slot click handlers are stubs for slice 1 — wiring the real action-level
    /// state machine + structured-action commands lives in a later T-HUD pass.
    ///
    /// History (audit trail): previous shape was "top vitals pills + numbered 1-9 hotbar". That
    /// was the wrong direction (T3) — the PRD wants bottom-labeled bars and a 12-button strip,
    /// not top pills and 9 slots. Rebuilt here per the PRD on feat/p15-design-alignment.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmberHud : MonoBehaviour
    {
        public IEmberHudSource Source { get; set; }

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Sprite _backgroundFrame;

        private TMP_Text _statusLabel;
        private Image _background;
        private float _refreshTimer;
        private const float RefreshIntervalSeconds = 0.25f;

        // Vital rows — bottom-left labeled bars.
        private Image _hpFill, _ftFill, _mpFill;
        private TMP_Text _hpNumeric, _ftNumeric, _mpNumeric;

        // 12-button BG1 context action strip — bottom-center.
        private readonly ActionSlot[] _slots = new ActionSlot[StandardActions.Length];

        // Standard action set (BG1 UAW_STANDARD) — placeholders until icons + real command
        // wiring land. Order matches PRD FR-03 (attack first, formation last) so muscle memory
        // ports cleanly when icons replace the text glyphs.
        //
        // tuple = (slot label shown in the strip, tooltip text, F-key hint shown bottom-right).
        private static readonly (string Label, string Tooltip, string Hotkey)[] StandardActions = new[]
        {
            ("ATK",   "Attack (melee or ranged)",           "F1"),
            ("CAST",  "Cast memorized spell",               "F2"),
            ("TALK",  "Talk to nearest NPC",                "F3"),
            ("INV",   "Inventory",                          "F4"),
            ("CHAR",  "Character sheet",                    "F5"),
            ("MAP",   "World map",                          "F6"),
            ("JOURN", "Journal",                            "F7"),
            ("SRCH",  "Search the area",                    "F8"),
            ("STLTH", "Toggle stealth",                     "F9"),
            ("MODAL", "Toggle modal (turn undead, bard song, etc.)", "F10"),
            ("FORM",  "Formation presets",                  "F11"),
            ("EQUIP", "Quick equipment slot",               "F12"),
        };

        // Ember design tokens (mirror Assets/Scripts/Ui/Foundation/UiTokens.cs).
        private static readonly Color VitalHealth   = new Color(0.851f, 0.200f, 0.122f, 1f); // #D9331F
        private static readonly Color VitalFatigue  = new Color(0.851f, 0.702f, 0.102f, 1f); // #D9B31A
        private static readonly Color VitalMana     = new Color(0.200f, 0.451f, 0.949f, 1f); // #3373F2
        private static readonly Color Parchment     = new Color(0.949f, 0.859f, 0.620f, 1f); // #F2DB9E
        private static readonly Color ParchmentDim  = new Color(0.902f, 0.851f, 0.702f, 1f); // #E6D9B3
        private static readonly Color Gold          = new Color(1.000f, 0.851f, 0.298f, 1f); // #FFD94C
        private static readonly Color PanelBrown    = new Color(0.180f, 0.140f, 0.090f, 0.92f); // #2E2417 @ 92%
        private static readonly Color PanelBrownHi  = new Color(0.227f, 0.180f, 0.114f, 1f); // #3A2E1D hover lift
        private static readonly Color BarTrack      = new Color(0.070f, 0.060f, 0.050f, 0.90f);
        private static readonly Color GoldHairline  = new Color(0.949f, 0.859f, 0.620f, 0.30f);
        private static readonly Color InkOnGold     = new Color(0.149f, 0.102f, 0.051f, 1f); // #261A0D

        private void Awake()
        {
            // Force this HUD container to fullscreen. In CombatDungeon the host creates a fresh
            // GameObject with fullscreen anchors so the bottom-row furniture (vitals + action
            // strip) lands at the actual bottom of the screen. In the other 9 scenes the
            // EmberHud is scene-authored on a child RectTransform that is NOT fullscreen — so
            // a naive bottom-anchored child renders inside that small rect (top-left of
            // screen). Normalizing to (0,0)→(1,1) here makes the layout identical across every
            // scene without touching the scene assets.
            if (transform is RectTransform selfRt)
            {
                selfRt.anchorMin = Vector2.zero;
                selfRt.anchorMax = Vector2.one;
                selfRt.offsetMin = Vector2.zero;
                selfRt.offsetMax = Vector2.zero;
                selfRt.pivot     = new Vector2(0.5f, 0.5f);
                selfRt.anchoredPosition = Vector2.zero;
                selfRt.sizeDelta = Vector2.zero;
            }

            _background = GetComponent<Image>();
            if (_backgroundFrame != null && _background != null)
            {
                _background.sprite = _backgroundFrame;
                _background.type = Image.Type.Sliced;
            }

            // Keep any pre-existing TMP child (some scenes seed one) but rewire it as the
            // top-left status label. New scenes get a fresh one from BuildStatusLabel().
            _statusLabel = GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (_statusLabel == null) _statusLabel = BuildStatusLabel();
            else { _statusLabel.color = Parchment; }

            BuildVitalsBars();
            BuildActionStrip();
        }

        private void Update()
        {
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = RefreshIntervalSeconds;
            if (_statusLabel != null)
                _statusLabel.text = Source != null ? Source.GetHudText() : "Tick 0  •  Day 1  •  Calm";
            RefreshVitals();
        }

        // -------------------------------------------------------------------------------------
        // Vitals  (bottom-left, three labeled bars)
        // -------------------------------------------------------------------------------------
        private void RefreshVitals()
        {
            if (_hpFill == null) return;
            int hp = 80, hpMax = 100, ft = 70, ftMax = 100, mp = 50, mpMax = 100;
            if (Source is ICombatHudSource combat)
            {
                var s = combat.Read();
                hp = s.Health;  hpMax = s.HealthMax;
                ft = s.Stamina; ftMax = s.StaminaMax;
                mp = s.Mana;    mpMax = s.ManaMax;
            }
            SetBar(_hpFill, _hpNumeric, hp, hpMax);
            SetBar(_ftFill, _ftNumeric, ft, ftMax);
            SetBar(_mpFill, _mpNumeric, mp, mpMax);
        }

        private static void SetBar(Image fill, TMP_Text numeric, int cur, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
            var rt = fill.rectTransform;
            rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
            if (numeric != null) numeric.text = cur + " / " + max;
        }

        private void BuildVitalsBars()
        {
            var panel = NewRect("VitalsPanel", transform);
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
            panel.anchoredPosition = new Vector2(24f, 22f);
            panel.sizeDelta = new Vector2(290f, 116f); // 3 rows × 32px + 2 × 6px gap + 16px padding

            _hpFill = MakeLabeledBar(panel, 0, "HEALTH",  VitalHealth,  out _hpNumeric);
            _ftFill = MakeLabeledBar(panel, 1, "FATIGUE", VitalFatigue, out _ftNumeric);
            _mpFill = MakeLabeledBar(panel, 2, "MANA",    VitalMana,    out _mpNumeric);
        }

        // One row = parchment WORD label (left, 70px) + colored bar with numeric overlay (right).
        // Bars stack top-down so HEALTH reads first; matches PRD reading order.
        private Image MakeLabeledBar(RectTransform parent, int row, string word, Color color, out TMP_Text numeric)
        {
            const float h = 32f, gap = 6f, labelW = 72f;
            var rowRt = NewRect("Row_" + word, parent);
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot     = new Vector2(0f, 1f);
            rowRt.anchoredPosition = new Vector2(0f, -row * (h + gap));
            rowRt.sizeDelta = new Vector2(0f, h);

            // word label, left side
            var label = NewText("Word", rowRt, 14, Parchment, TextAlignmentOptions.MidlineLeft);
            var labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0f, 1f);
            labelRt.pivot     = new Vector2(0f, 0.5f);
            labelRt.sizeDelta = new Vector2(labelW, 0f);
            labelRt.anchoredPosition = new Vector2(0f, 0f);
            label.text = word;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 8f;
            label.outlineWidth = 0.18f;
            label.outlineColor = new Color32(0, 0, 0, 200);

            // bar track + colored fill, right side
            var track = NewRect("Track", rowRt);
            track.anchorMin = new Vector2(0f, 0f);
            track.anchorMax = new Vector2(1f, 1f);
            track.offsetMin = new Vector2(labelW + 6f, 4f);
            track.offsetMax = new Vector2(0f, -4f);
            var trackImg = track.gameObject.AddComponent<Image>();
            trackImg.color = BarTrack;

            var hairline = NewRect("Hairline", track);
            hairline.anchorMin = Vector2.zero;
            hairline.anchorMax = Vector2.one;
            hairline.offsetMin = Vector2.zero;
            hairline.offsetMax = Vector2.zero;
            var hairlineImg = hairline.gameObject.AddComponent<Image>();
            hairlineImg.color = GoldHairline;
            hairlineImg.raycastTarget = false;

            var fillRt = NewRect("Fill", track);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(1f, 1f);
            fillRt.offsetMax = new Vector2(-1f, -1f);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.color = color;

            numeric = NewText("Numeric", track, 12, Parchment, TextAlignmentOptions.Center);
            var numRt = numeric.rectTransform;
            numRt.anchorMin = Vector2.zero;
            numRt.anchorMax = Vector2.one;
            numRt.offsetMin = new Vector2(4f, 0f);
            numRt.offsetMax = new Vector2(-4f, 0f);
            numeric.outlineWidth = 0.22f;
            numeric.outlineColor = new Color32(0, 0, 0, 220);
            return fill;
        }

        // -------------------------------------------------------------------------------------
        // Action strip  (bottom-center, 12 BG1 buttons)
        // -------------------------------------------------------------------------------------
        private void BuildActionStrip()
        {
            // 56px slot fits the widest STANDARD label ("JOURN") at font 11 without wrapping.
            // 12 slots × 56 + 11 × 4 gap = 716px wide — comfortable bottom-center on 1920×1080.
            const float slot = 56f, gap = 4f;
            int count = StandardActions.Length;
            float width = count * slot + (count - 1) * gap;

            var strip = NewRect("ActionStrip", transform);
            strip.anchorMin = strip.anchorMax = new Vector2(0.5f, 0f);
            strip.pivot     = new Vector2(0.5f, 0f);
            strip.anchoredPosition = new Vector2(0f, 22f);
            strip.sizeDelta = new Vector2(width, slot);

            for (int i = 0; i < count; i++)
                _slots[i] = ActionSlot.Build(strip, i, slot, gap, StandardActions[i], _font);
        }

        // Encapsulates one of the 12 BG1 buttons. Slice-1 click handler is a stub that just
        // logs through Debug; the action-level state machine (UAW_QSPELLS pop-up, modal
        // toggles, formation presets) lands in a later T-HUD pass.
        private sealed class ActionSlot
        {
            public Button Button;
            public TMP_Text LabelText;
            public TMP_Text HotkeyText;
            public Image Background;
            public string Tooltip;

            public static ActionSlot Build(
                RectTransform parent, int index, float size, float gap,
                (string Label, string Tooltip, string Hotkey) cfg, TMP_FontAsset font)
            {
                var rt = New("Slot_" + (index + 1), parent);
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot     = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(index * (size + gap), 0f);
                rt.sizeDelta = new Vector2(size, size);

                var bg = rt.gameObject.AddComponent<Image>();
                bg.color = PanelBrown;

                var border = New("Border", rt);
                border.anchorMin = Vector2.zero; border.anchorMax = Vector2.one;
                border.offsetMin = Vector2.zero; border.offsetMax = Vector2.zero;
                var borderImg = border.gameObject.AddComponent<Image>();
                borderImg.color = GoldHairline;
                borderImg.raycastTarget = false;

                var button = rt.gameObject.AddComponent<Button>();
                var colors = button.colors;
                colors.normalColor      = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
                colors.pressedColor     = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.selectedColor    = Color.white;
                colors.fadeDuration     = 0.08f;
                button.colors = colors;
                button.targetGraphic = bg;
                int captured = index;
                string capturedLabel = cfg.Label;
                button.onClick.AddListener(() =>
                {
                    Debug.Log("[EmberHud] action_strip slot " + (captured + 1) + " (" + capturedLabel + ") — stub; real command wiring lands in a later T-HUD slice.");
                });

                var label = NewText("Label", rt, 11, Gold, TextAlignmentOptions.Center, font);
                var labelRt = label.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(1f, 14f);
                labelRt.offsetMax = new Vector2(-1f, -3f);
                label.text = cfg.Label;
                label.fontStyle = FontStyles.Bold;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Overflow;
                label.outlineWidth = 0.22f;
                label.outlineColor = new Color32(0, 0, 0, 220);

                var hotkey = NewText("Hotkey", rt, 9, ParchmentDim, TextAlignmentOptions.BottomRight, font);
                var hotRt = hotkey.rectTransform;
                hotRt.anchorMin = Vector2.zero;
                hotRt.anchorMax = Vector2.one;
                hotRt.offsetMin = new Vector2(2f, 2f);
                hotRt.offsetMax = new Vector2(-3f, -2f);
                hotkey.text = cfg.Hotkey;

                return new ActionSlot
                {
                    Button = button,
                    LabelText = label,
                    HotkeyText = hotkey,
                    Background = bg,
                    Tooltip = cfg.Tooltip,
                };
            }

            private static RectTransform New(string name, Transform parent)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, worldPositionStays: false);
                return (RectTransform)go.transform;
            }

            private static TMP_Text NewText(string name, Transform parent, float size, Color color,
                TextAlignmentOptions align, TMP_FontAsset font)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(parent, worldPositionStays: false);
                var t = go.GetComponent<TextMeshProUGUI>();
                t.fontSize = size;
                t.color = color;
                t.alignment = align;
                t.raycastTarget = false;
                if (font != null) t.font = font;
                return t;
            }
        }

        // -------------------------------------------------------------------------------------
        // Status label (top-left tick / day / weather — placeholder until T-Clock lands)
        // -------------------------------------------------------------------------------------
        private TMP_Text BuildStatusLabel()
        {
            var t = NewText("HudLabel", transform, 20, Parchment, TextAlignmentOptions.TopLeft);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(24f, -56f);
            rt.offsetMax = new Vector2(-24f, -16f);
            return t;
        }

        // -------------------------------------------------------------------------------------
        // Tiny UGUI helpers (kept identical to legacy EmberHud signatures so call sites in this
        // file stay readable).
        // -------------------------------------------------------------------------------------
        private RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            return (RectTransform)go.transform;
        }

        private TMP_Text NewText(string name, Transform parent, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, worldPositionStays: false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            if (_font != null) t.font = _font;
            return t;
        }
    }

    /// <summary>Adapter the HUD reads each refresh. Implement on the simulation/host side.</summary>
    public interface IEmberHudSource
    {
        string GetHudText();
    }
}

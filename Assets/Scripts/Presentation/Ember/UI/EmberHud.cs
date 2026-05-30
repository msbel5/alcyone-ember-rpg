using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmberCrpg.Presentation.Ember.Adapters; // EMB-014: IPlayerCommandSink via EmberDomainAdapterLocator
using EmberCrpg.Presentation.Ember.Inputs;    // EMB-014/015: F1..F12 hotkeys through the input facade

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>The BG1-style "unified action window" levels the bottom strip can switch between.
    /// Standard is the root; CAST/MODAL/FORM open a sub-level whose last slot is BACK.</summary>
    public enum ActionLevel { Standard, QWeapons, QSpells, QItems, Innate, Songs, Modal, Formation }

    /// <summary>
    /// In-world HUD shared by every gameplay scene. Implements the P1.5 design-system frame
    /// from <c>Reference/PRDs/PRD_frontend_action_bar_v1.md</c>:
    ///   • bottom-LEFT  : labeled HEALTH / FATIGUE / MANA bars (red / yellow / blue, with
    ///                    parchment word labels + numeric "70 / 100" overlay)
    ///   • bottom-CENTER: 12-button BG1-style context action strip with an action-LEVEL state
    ///                    machine (EMB-014): Standard ⇄ QSpells/Modal/Formation; gold hairline
    ///                    border, F1..F12 hotkey hints
    ///   • top-LEFT     : tick / day / weather status line
    ///
    /// EMB-014: the strip is no longer a row of Debug.Log stubs. Slots carry a typed command and
    /// route through <see cref="IPlayerCommandSink"/> (obtained from
    /// <see cref="EmberDomainAdapterLocator.PlayerCommandSink"/>): CAST switches to the QSpells
    /// level whose SPL1..SPL5 slots issue real <c>TryCastSpell</c> commands; ATK issues a
    /// <c>TryMeleeStrike</c>; SRCH issues <c>TryInteract</c>; not-yet-built panels report through
    /// <c>LogCombat</c> rather than the console. F1..F12 trigger the matching slot of the current
    /// level (F5/F9 are reserved for the global quicksave/quickload bindings).
    ///
    /// Vitals are read each refresh from the injected <see cref="IEmberHudSource"/> when it also
    /// implements <see cref="ICombatHudSource"/> (EmberWorldHost does), so the panel stays
    /// domain-agnostic.
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
        private const int SlotCount = 12;
        private readonly ActionSlot[] _slots = new ActionSlot[SlotCount];
        private ActionLevel _level = ActionLevel.Standard;

        // EMB-014: what a slot does when clicked or hotkeyed. Arg/Info carry the command payload.
        private enum ActionCmd { None, SwitchTo, Back, CastSlot, Attack, Interact, Info }

        private readonly struct ActionDef
        {
            public ActionDef(string label, string tooltip, string hotkey, ActionCmd cmd, int arg = 0, string info = null)
            {
                Label = label; Tooltip = tooltip; Hotkey = hotkey; Cmd = cmd; Arg = arg; Info = info;
            }

            public readonly string Label;
            public readonly string Tooltip;
            public readonly string Hotkey;
            public readonly ActionCmd Cmd;
            public readonly int Arg;     // target ActionLevel (SwitchTo) or spell slot (CastSlot)
            public readonly string Info; // interact tag (Interact) or status copy (Info)

            public bool IsEmpty => string.IsNullOrEmpty(Label);
            public static ActionDef Empty => new ActionDef(null, null, null, ActionCmd.None);
        }

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

        // -------------------------------------------------------------------------------------
        // Action-level definitions (EMB-014). Each level returns exactly SlotCount entries; the
        // hotkey F(k) always sits at index (k-1) so an F-key maps straight to a slot.
        // -------------------------------------------------------------------------------------
        private static ActionDef[] ActionsFor(ActionLevel level)
        {
            switch (level)
            {
                case ActionLevel.QSpells:
                    return Pad(
                        new ActionDef("SPL1", "Cast spell slot 1", "F1", ActionCmd.CastSlot, 0),
                        new ActionDef("SPL2", "Cast spell slot 2", "F2", ActionCmd.CastSlot, 1),
                        new ActionDef("SPL3", "Cast spell slot 3", "F3", ActionCmd.CastSlot, 2),
                        new ActionDef("SPL4", "Cast spell slot 4", "F4", ActionCmd.CastSlot, 3),
                        new ActionDef("SPL5", "Cast spell slot 5 (click only — F5 quicksaves)", "F5", ActionCmd.CastSlot, 4));

                case ActionLevel.Modal:
                    return Pad(
                        new ActionDef("DETECT", "Toggle detect", "F1", ActionCmd.Info, 0, "Detect modal not yet available."),
                        new ActionDef("TURN", "Turn undead", "F2", ActionCmd.Info, 0, "Turn undead not yet available."),
                        new ActionDef("BLESS", "Bless aura", "F3", ActionCmd.Info, 0, "Bless modal not yet available."));

                case ActionLevel.Formation:
                    return Pad(
                        new ActionDef("LINE", "Line formation", "F1", ActionCmd.Info, 0, "Formation presets not yet available."),
                        new ActionDef("WEDGE", "Wedge formation", "F2", ActionCmd.Info, 0, "Formation presets not yet available."),
                        new ActionDef("BOX", "Box formation", "F3", ActionCmd.Info, 0, "Formation presets not yet available."),
                        new ActionDef("SKEIN", "Skein formation", "F4", ActionCmd.Info, 0, "Formation presets not yet available."));

                case ActionLevel.QWeapons:
                    return Pad(
                        new ActionDef("WPN1", "Quick weapon 1", "F1", ActionCmd.Info, 0, "Weapon swap not yet available."),
                        new ActionDef("WPN2", "Quick weapon 2", "F2", ActionCmd.Info, 0, "Weapon swap not yet available."),
                        new ActionDef("WPN3", "Quick weapon 3", "F3", ActionCmd.Info, 0, "Weapon swap not yet available."),
                        new ActionDef("WPN4", "Quick weapon 4", "F4", ActionCmd.Info, 0, "Weapon swap not yet available."));

                case ActionLevel.QItems:
                    return Pad(
                        new ActionDef("ITM1", "Quick item 1", "F1", ActionCmd.Info, 0, "Quick items not yet available."),
                        new ActionDef("ITM2", "Quick item 2", "F2", ActionCmd.Info, 0, "Quick items not yet available."),
                        new ActionDef("ITM3", "Quick item 3", "F3", ActionCmd.Info, 0, "Quick items not yet available."),
                        new ActionDef("ITM4", "Quick item 4", "F4", ActionCmd.Info, 0, "Quick items not yet available."));

                case ActionLevel.Innate:
                    return Pad(
                        new ActionDef("INN1", "Innate ability 1", "F1", ActionCmd.Info, 0, "Innate abilities not yet available."),
                        new ActionDef("INN2", "Innate ability 2", "F2", ActionCmd.Info, 0, "Innate abilities not yet available."),
                        new ActionDef("INN3", "Innate ability 3", "F3", ActionCmd.Info, 0, "Innate abilities not yet available."));

                case ActionLevel.Songs:
                    return Pad(
                        new ActionDef("SONG1", "Bard song 1", "F1", ActionCmd.Info, 0, "Bard songs not yet available."),
                        new ActionDef("SONG2", "Bard song 2", "F2", ActionCmd.Info, 0, "Bard songs not yet available."),
                        new ActionDef("SONG3", "Bard song 3", "F3", ActionCmd.Info, 0, "Bard songs not yet available."));

                default: // Standard (BG1 UAW_STANDARD)
                    return new[]
                    {
                        new ActionDef("ATK",   "Attack nearest",   "F1",  ActionCmd.Attack),
                        new ActionDef("CAST",  "Quick-cast a spell","F2", ActionCmd.SwitchTo, (int)ActionLevel.QSpells),
                        new ActionDef("TALK",  "Talk to nearest NPC","F3",ActionCmd.Info, 0, "Approach an NPC and press E to talk."),
                        new ActionDef("INV",   "Inventory",        "F4",  ActionCmd.Info, 0, "Inventory panel not yet available."),
                        new ActionDef("CHAR",  "Character sheet",  "F5",  ActionCmd.Info, 0, "Character sheet not yet available."),
                        new ActionDef("MAP",   "World map",        "F6",  ActionCmd.Info, 0, "World map not yet available."),
                        new ActionDef("JOURN", "Journal",          "F7",  ActionCmd.Info, 0, "Journal not yet available."),
                        new ActionDef("SRCH",  "Search the area",  "F8",  ActionCmd.Interact, 0, "search"),
                        new ActionDef("STLTH", "Toggle stealth",   "F9",  ActionCmd.Info, 0, "Stealth not yet available."),
                        new ActionDef("MODAL", "Modal abilities",  "F10", ActionCmd.SwitchTo, (int)ActionLevel.Modal),
                        new ActionDef("FORM",  "Formation presets","F11", ActionCmd.SwitchTo, (int)ActionLevel.Formation),
                        new ActionDef("EQUIP", "Quick equipment",  "F12", ActionCmd.Info, 0, "Equipment panel not yet available."),
                    };
            }
        }

        // Pad a sub-level's actions to SlotCount, leaving a BACK affordance in the final slot.
        private static ActionDef[] Pad(params ActionDef[] head)
        {
            var defs = new ActionDef[SlotCount];
            for (int i = 0; i < SlotCount; i++) defs[i] = ActionDef.Empty;
            for (int i = 0; i < head.Length && i < SlotCount - 1; i++) defs[i] = head[i];
            defs[SlotCount - 1] = new ActionDef("BACK", "Return to standard actions", "F12", ActionCmd.Back);
            return defs;
        }

        private void Awake()
        {
            // Force this HUD container to fullscreen so the bottom-row furniture (vitals + action
            // strip) lands at the actual bottom of the screen in every scene regardless of how the
            // EmberHud RectTransform was authored.
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

            _statusLabel = GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (_statusLabel == null) _statusLabel = BuildStatusLabel();
            else { _statusLabel.color = Parchment; }

            BuildVitalsBars();
            BuildActionStrip();
            SetLevel(ActionLevel.Standard);
        }

        private void Update()
        {
            HandleHotkeys(); // every frame — GetKeyDown must not be throttled

            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = RefreshIntervalSeconds;
            if (_statusLabel != null)
                _statusLabel.text = Source != null ? Source.GetHudText() : "Tick 0  •  Day 1  •  Calm";
            RefreshVitals();
        }

        // -------------------------------------------------------------------------------------
        // Action-level state machine (EMB-014)
        // -------------------------------------------------------------------------------------
        private void SetLevel(ActionLevel level)
        {
            _level = level;
            var defs = ActionsFor(level);
            for (int i = 0; i < SlotCount; i++)
                _slots[i]?.Apply(defs[i], this);
        }

        private void HandleHotkeys()
        {
            int fk = EmberInput.FunctionKeyDown();
            // F5/F9 are reserved for the global quicksave/quickload bindings (EmberSaveService);
            // the slots sitting on those positions stay click-only to avoid a double-trigger.
            if (fk < 1 || fk > SlotCount || fk == 5 || fk == 9) return;
            var defs = ActionsFor(_level);
            var def = defs[fk - 1];
            if (!def.IsEmpty) Execute(def);
        }

        private void Execute(ActionDef def)
        {
            switch (def.Cmd)
            {
                case ActionCmd.SwitchTo:
                    SetLevel((ActionLevel)def.Arg);
                    break;
                case ActionCmd.Back:
                    SetLevel(ActionLevel.Standard);
                    break;
                case ActionCmd.CastSlot:
                    Sink()?.TryCastSpell(def.Arg);
                    SetLevel(ActionLevel.Standard); // BG1: quick-cast returns to the standard level
                    break;
                case ActionCmd.Attack:
                    // Issue a real strike command; the adapter resolves/refuses a target and logs it.
                    Sink()?.TryMeleeStrike(string.Empty, 6);
                    break;
                case ActionCmd.Interact:
                    Sink()?.TryInteract(def.Info ?? string.Empty);
                    break;
                case ActionCmd.Info:
                    // Replaces the old Debug.Log stub: route the "not yet available" / hint copy
                    // through the command sink's combat line so it surfaces in-world, not the console.
                    Sink()?.LogCombat(def.Info ?? def.Tooltip ?? def.Label);
                    break;
            }
        }

        private static IPlayerCommandSink Sink() => EmberDomainAdapterLocator.PlayerCommandSink;

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
        private Image MakeLabeledBar(RectTransform parent, int row, string word, Color color, out TMP_Text numeric)
        {
            const float h = 32f, gap = 6f, labelW = 72f;
            var rowRt = NewRect("Row_" + word, parent);
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot     = new Vector2(0f, 1f);
            rowRt.anchoredPosition = new Vector2(0f, -row * (h + gap));
            rowRt.sizeDelta = new Vector2(0f, h);

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
            const float slot = 56f, gap = 4f;
            int count = SlotCount;
            float width = count * slot + (count - 1) * gap;

            var strip = NewRect("ActionStrip", transform);
            strip.anchorMin = strip.anchorMax = new Vector2(0.5f, 0f);
            strip.pivot     = new Vector2(0.5f, 0f);
            strip.anchoredPosition = new Vector2(0f, 22f);
            strip.sizeDelta = new Vector2(width, slot);

            // Build the slot furniture once; SetLevel() fills in label/hotkey/command per level.
            for (int i = 0; i < count; i++)
                _slots[i] = ActionSlot.Build(strip, i, slot, gap, _font);
        }

        // Encapsulates one of the 12 BG1 buttons. EMB-014: the button fires the slot's mutable
        // OnClick, which SetLevel rebinds per action level — no hardcoded handler.
        private sealed class ActionSlot
        {
            public RectTransform Root;
            public Button Button;
            public TMP_Text LabelText;
            public TMP_Text HotkeyText;
            public Image Background;
            public string Tooltip;
            public Action OnClick;

            public void Apply(ActionDef def, EmberHud hud)
            {
                bool empty = def.IsEmpty;
                if (Root != null) Root.gameObject.SetActive(!empty);
                if (empty) { OnClick = null; return; }
                if (LabelText != null) LabelText.text = def.Label;
                if (HotkeyText != null) HotkeyText.text = def.Hotkey ?? string.Empty;
                Tooltip = def.Tooltip;
                var captured = def;
                OnClick = () => hud.Execute(captured);
            }

            public static ActionSlot Build(RectTransform parent, int index, float size, float gap, TMP_FontAsset font)
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

                var slot = new ActionSlot { Root = rt, Button = button, Background = bg };
                button.onClick.AddListener(() => slot.OnClick?.Invoke());

                var label = NewText("Label", rt, 11, Gold, TextAlignmentOptions.Center, font);
                var labelRt = label.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(1f, 14f);
                labelRt.offsetMax = new Vector2(-1f, -3f);
                label.fontStyle = FontStyles.Bold;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Overflow;
                label.outlineWidth = 0.22f;
                label.outlineColor = new Color32(0, 0, 0, 220);
                slot.LabelText = label;

                var hotkey = NewText("Hotkey", rt, 9, ParchmentDim, TextAlignmentOptions.BottomRight, font);
                var hotRt = hotkey.rectTransform;
                hotRt.anchorMin = Vector2.zero;
                hotRt.anchorMax = Vector2.one;
                hotRt.offsetMin = new Vector2(2f, 2f);
                hotRt.offsetMax = new Vector2(-3f, -2f);
                slot.HotkeyText = hotkey;

                return slot;
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
        // Tiny UGUI helpers.
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

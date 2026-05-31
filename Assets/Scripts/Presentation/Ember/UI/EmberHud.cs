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
    public sealed partial class EmberHud : MonoBehaviour
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

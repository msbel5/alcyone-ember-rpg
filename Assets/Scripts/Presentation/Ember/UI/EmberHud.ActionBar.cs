// REF-c (LEFT-019/020): action-bar region split out of EmberHud.cs (partial, zero behaviour change).
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmberCrpg.Presentation.Ember.Adapters; // EMB-014: IPlayerCommandSink via EmberDomainAdapterLocator
using EmberCrpg.Presentation.Ember.Inputs;    // EMB-014/015: F1..F12 hotkeys through the input facade

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed partial class EmberHud
    {
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
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class CombatView
    {
        private readonly VisualElement _overlay;

        public CombatView(VisualElement stageCanvas, Action onClose, Action<string> onAction = null, Action onFlee = null)
        {
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.pickingMode = PickingMode.Position;

            _overlay.Add(BuildEnemyPanel());
            _overlay.Add(BuildAttackPrompt(onAction));
            _overlay.Add(BuildLogAndVitals());
            _overlay.Add(BuildActionBar(onAction));
            _overlay.Add(BuildTopNav(onFlee ?? onClose));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static VisualElement BuildEnemyPanel()
        {
            var panel = Row();
            panel.style.position = Position.Absolute;
            panel.style.left = 26;
            panel.style.top = 52;
            panel.style.paddingTop = 12;
            panel.style.paddingBottom = 12;
            panel.style.paddingLeft = 16;
            panel.style.paddingRight = 16;
            panel.style.backgroundColor = C(8, 6, 4, 0.82f);
            Border(panel, Alpha(Health, 0.40f), 1);
            Radius(panel, 12);

            var portrait = new VisualElement();
            portrait.style.width = 52;
            portrait.style.height = 64;
            portrait.style.backgroundColor = Alpha(Panel, 0.80f);
            Border(portrait, Alpha(Health, 0.30f), 1);
            Radius(portrait, 8);
            portrait.style.alignItems = Align.Center;
            portrait.style.justifyContent = Justify.Center;
            portrait.Add(Text("B", Serif, 20, Alpha(Health, 0.50f)));
            panel.Add(portrait);

            var textWrap = new VisualElement();
            textWrap.Add(Text("Ashton Bandit", Sans, 14, Parch, FontStyle.Bold));
            var track = new VisualElement();
            track.style.width = 160;
            track.style.height = 10;
            track.style.marginTop = 5;
            track.style.backgroundColor = C(0, 0, 0, 0.4f);
            Radius(track, 999);
            var fill = new VisualElement();
            fill.style.width = Length.Percent(64);
            fill.style.height = 10;
            fill.style.backgroundColor = Health;
            Radius(fill, 999);
            track.Add(fill);
            textWrap.Add(track);
            var hp = Text("HP 64 / 64", Sans, 10, Alpha(Health, 0.65f));
            hp.style.letterSpacing = 0.6f;
            hp.style.marginTop = 3;
            textWrap.Add(hp);
            panel.Add(textWrap);
            return panel;
        }

        private static VisualElement BuildAttackPrompt(Action<string> onAction)
        {
            var wrap = new VisualElement();
            wrap.style.position = Position.Absolute;
            wrap.style.left = Length.Percent(50);
            wrap.style.top = Length.Percent(38);
            wrap.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            wrap.style.alignItems = Align.Center;
            var attack = new Button(() => onAction?.Invoke("attack")) { text = "ATTACK" };
            ResetButton(attack);
            attack.style.height = 40;
            attack.style.paddingLeft = 18;
            attack.style.paddingRight = 18;
            attack.style.backgroundColor = Gold;
            attack.style.color = Ink;
            attack.style.fontSize = 13;
            attack.style.letterSpacing = 1.2f;
            attack.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(attack, Sans);
            Border(attack, Amber, 1);
            Radius(attack, 8);
            wrap.Add(attack);
            var swing = Text("SWING!", Serif, 18, EmberGlow);
            swing.style.marginTop = 6;
            wrap.Add(swing);
            return wrap;
        }

        private static VisualElement BuildLogAndVitals()
        {
            var pane = new VisualElement();
            pane.style.position = Position.Absolute;
            pane.style.left = 26;
            pane.style.bottom = 92;
            pane.style.width = 340;

            string[] lines = { "You draw your sword.", "The bandit raises his blade.", "You strike for 8 damage." };
            for (int i = 0; i < lines.Length; i++)
            {
                var label = Text(lines[i], Serif, 12, PA(0.35f + (i * 0.15f)), FontStyle.Italic);
                label.style.marginBottom = 2;
                pane.Add(label);
            }

            var bars = Row();
            bars.style.marginTop = 8;
            bars.Add(BuildBar("HP", IgMockData.Player.Hp, IgMockData.Player.HpMax, Health));
            bars.Add(BuildBar("FAT", IgMockData.Player.Fatigue, IgMockData.Player.FatigueMax, Fatigue));
            bars.Add(BuildBar("MP", IgMockData.Player.Mana, IgMockData.Player.ManaMax, Mana));
            pane.Add(bars);
            return pane;
        }

        private static VisualElement BuildActionBar(Action<string> onAction)
        {
            var bar = Row();
            bar.style.position = Position.Absolute;
            bar.style.left = Length.Percent(50);
            bar.style.bottom = 92;
            bar.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
            bar.style.alignItems = Align.FlexEnd;

            for (int i = 0; i < IgMockData.SpellBar.Length; i++)
                bar.Add(BuildSpellBox(IgMockData.SpellBar[i], onAction));

            bar.Add(BuildAction("B", "Block", Success, () => onAction?.Invoke("block")));
            bar.Add(BuildAction("D", "Dodge", ParchDim, () => onAction?.Invoke("dodge")));
            return bar;
        }

        private static VisualElement BuildTopNav(Action onClose)
        {
            var nav = Row();
            nav.style.position = Position.Absolute;
            nav.style.right = 26;
            nav.style.top = 14;
            nav.style.alignItems = Align.Center;
            var state = Text("⚔ IN COMBAT", Sans, 11, Alpha(Health, 0.70f));
            state.style.letterSpacing = 1.4f;
            nav.Add(state);

            var flee = new Button(() => onClose?.Invoke()) { text = "FLEE" };
            ResetButton(flee);
            flee.style.height = 32;
            flee.style.paddingLeft = 12;
            flee.style.paddingRight = 12;
            flee.style.backgroundColor = C(8, 6, 4, 0.70f);
            flee.style.color = PA(0.38f);
            flee.style.fontSize = 11;
            ApplyFont(flee, Sans);
            Border(flee, PA(0.15f), 1);
            Radius(flee, 7);
            nav.Add(flee);
            return nav;
        }

        private static VisualElement BuildBar(string label, int value, int max, Color color)
        {
            var root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.height = 20;
            root.style.backgroundColor = C(0, 0, 0, 0.4f);
            Radius(root, 999);
            var fill = new VisualElement();
            fill.style.position = Position.Absolute;
            fill.style.left = 2;
            fill.style.top = 2;
            fill.style.bottom = 2;
            fill.style.width = Length.Percent((float)value / max * 100f);
            fill.style.backgroundColor = color;
            Radius(fill, 999);
            root.Add(fill);
            var text = Text($"{label} {value}/{max}", Sans, 9, Bone, FontStyle.Bold);
            text.style.letterSpacing = 0.6f;
            text.style.position = Position.Absolute;
            text.style.left = 0;
            text.style.right = 0;
            text.style.top = 0;
            text.style.bottom = 0;
            text.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(text);
            return root;
        }

        private static VisualElement BuildSpellBox(SpellBarSlotData slot, Action<string> onAction)
        {
            var box = new Button(() => onAction?.Invoke(string.IsNullOrEmpty(slot.Spell) ? "empty-slot-" + slot.Slot : slot.Spell));
            ResetButton(box);
            box.style.width = 50;
            box.style.height = 50;
            box.style.backgroundColor = slot.Spell != null ? Alpha(C(51, 51, 51), 0.82f) : Dark(0.55f);
            Border(box, slot.Selected ? Gold : PA(0.18f), slot.Selected ? 2 : 1);
            Radius(box, 9);
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;
            box.style.position = Position.Relative;
            var num = Text(slot.Slot.ToString(), Sans, 9, WA(0.4f));
            num.style.position = Position.Absolute;
            num.style.top = 3;
            num.style.left = 5;
            box.Add(num);
            if (!string.IsNullOrEmpty(slot.Spell))
            {
                var text = Text(Short(slot.Spell), Sans, 7, ParchDim);
                text.style.marginTop = 4;
                box.Add(text);
            }
            return box;
        }

        private static VisualElement BuildAction(string key, string label, Color color, Action onClick)
        {
            var button = new Button(() => onClick?.Invoke());
            ResetButton(button);
            button.style.width = 68;
            button.style.height = 50;
            button.style.backgroundColor = Alpha(Panel, 0.72f);
            Border(button, Alpha(color, 0.50f), 1);
            Radius(button, 9);
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
            button.Add(Text(key, Serif, 16, color));
            var copy = Text(label.ToUpperInvariant(), Sans, 9, ParchDim);
            copy.style.marginTop = 2;
            button.Add(copy);
            return button;
        }

        private static string Short(string spell)
        {
            string first = spell.Split(' ')[0];
            return first.Substring(0, Mathf.Min(5, first.Length));
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class LevelUpView
    {
        private readonly VisualElement _overlay;

        public LevelUpView(VisualElement stageCanvas, Action onClose)
        {
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = C(4, 3, 2, 0.86f);
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            _overlay.pickingMode = PickingMode.Position;

            var panel = new ScrollView();
            panel.style.width = 680;
            panel.style.maxHeight = 860;
            panel.style.backgroundColor = Alpha(VoidWarm, 0.98f);
            Border(panel, Gold, 1);
            Radius(panel, 20);
            panel.style.paddingTop = 36;
            panel.style.paddingBottom = 36;
            panel.style.paddingLeft = 36;
            panel.style.paddingRight = 36;
            _overlay.Add(panel);

            var fanfare = new VisualElement();
            fanfare.style.alignItems = Align.Center;
            fanfare.style.marginBottom = 28;
            fanfare.Add(BuildCenter("LEVEL UP!", Serif, 14, Gold, FontStyle.Bold, 6f));
            fanfare.Add(BuildCenter($"Level {IgMockData.Player.Level + 1}", Sans, 48, Parch, FontStyle.Bold));
            fanfare.Add(BuildCenter("Distribute 5 attribute points.", Serif, 15, PA(0.55f), FontStyle.Italic));
            panel.Add(fanfare);

            var remain = new VisualElement();
            remain.style.height = 36;
            remain.style.width = 220;
            remain.style.marginLeft = StyleKeyword.Auto;
            remain.style.marginRight = StyleKeyword.Auto;
            remain.style.marginBottom = 20;
            remain.style.backgroundColor = Alpha(Panel, 0.55f);
            Border(remain, PA(0.18f), 1);
            Radius(remain, 20);
            remain.style.alignItems = Align.Center;
            remain.style.justifyContent = Justify.Center;
            remain.Add(Text("5 POINTS REMAINING", Sans, 14, Amber, FontStyle.Bold));
            panel.Add(remain);

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.marginBottom = 24;
            panel.Add(grid);
            for (int i = 0; i < IgMockData.Player.Stats.Length; i++)
                grid.Add(BuildStatRow(IgMockData.Player.Stats[i]));

            var spellSection = Text("SPELLS", Sans, 10, Gold, FontStyle.Bold);
            spellSection.style.letterSpacing = 1.8f;
            spellSection.style.marginBottom = 10;
            panel.Add(spellSection);
            var spells = new VisualElement();
            spells.style.flexDirection = FlexDirection.Row;
            spells.style.flexWrap = Wrap.Wrap;
            spells.style.marginBottom = 24;
            panel.Add(spells);
            var options = IgMockData.GetAllSpells();
            for (int i = 0; i < Mathf.Min(6, options.Count); i++)
                spells.Add(BuildSpellChoice(options[i], i == 0));

            var confirm = new Button(() => onClose?.Invoke()) { text = "CONFIRM" };
            ResetButton(confirm);
            confirm.style.height = 46;
            confirm.style.width = 180;
            confirm.style.marginLeft = StyleKeyword.Auto;
            confirm.style.marginRight = StyleKeyword.Auto;
            confirm.style.backgroundColor = Gold;
            confirm.style.color = Ink;
            confirm.style.fontSize = 15;
            confirm.style.letterSpacing = 2f;
            confirm.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(confirm, Serif);
            Border(confirm, Amber, 1);
            Radius(confirm, 10);
            panel.Add(confirm);

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static Label BuildCenter(string text, Font font, int size, Color color, FontStyle style, float letterSpacing = 0f)
        {
            var label = Text(text, font, size, color, style);
            label.style.letterSpacing = letterSpacing;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return label;
        }

        private static VisualElement BuildStatRow(StatData stat)
        {
            var row = Row();
            row.style.width = Length.Percent(48.5f);
            row.style.marginRight = (stat.Abbr == "AGI" || stat.Abbr == "MND" || stat.Abbr == "PRE") ? 0 : Length.Percent(3f);
            row.style.marginBottom = 10;
            row.style.paddingTop = 12;
            row.style.paddingBottom = 12;
            row.style.paddingLeft = 14;
            row.style.paddingRight = 14;
            row.style.backgroundColor = Dark(0.72f);
            Border(row, PA(0.12f), 1);
            Radius(row, 10);
            row.style.alignItems = Align.Center;

            var left = new VisualElement();
            left.style.minWidth = 80;
            var abbr = Text(stat.Abbr, Sans, 10, Stat(stat.Abbr), FontStyle.Bold);
            abbr.style.letterSpacing = 1.2f;
            left.Add(abbr);
            var num = Text($"{stat.Value} +1", Sans, 28, Parch, FontStyle.Bold);
            left.Add(num);
            row.Add(left);

            var controls = Row();
            controls.style.marginLeft = StyleKeyword.Auto;
            controls.Add(BuildAdjust("−", false, ParchDim));
            controls.Add(BuildAdjust("+", true, Stat(stat.Abbr)));
            row.Add(controls);
            return row;
        }

        private static VisualElement BuildSpellChoice(SpellData spell, bool selected)
        {
            string school = FindSchool(spell.Name);
            var card = new VisualElement();
            card.style.width = Length.Percent(48.5f);
            card.style.marginRight = selected || spell.Name == "Shock Burst" || spell.Name == "Cure Poison" || spell.Name == "Charm" ? 0 : Length.Percent(3f);
            card.style.marginBottom = 10;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;
            card.style.backgroundColor = selected ? GA(0.10f) : Dark(0.62f);
            Border(card, selected ? Gold : PA(0.10f), selected ? 2 : 1);
            Radius(card, 10);
            card.Add(Text(spell.Name, Sans, 13, selected ? Parch : ParchDim, FontStyle.Bold));
            var head = Text(school.ToUpperInvariant(), Sans, 10, School(school));
            head.style.letterSpacing = 0.8f;
            head.style.marginTop = 4;
            card.Add(head);
            var fx = Text($"{spell.Effect} · {spell.ManaCost} MP", Serif, 12, PA(0.50f), FontStyle.Italic);
            fx.style.marginTop = 6;
            fx.style.whiteSpace = WhiteSpace.Normal;
            card.Add(fx);
            return card;
        }

        private static Button BuildAdjust(string text, bool bright, Color color)
        {
            var button = new Button { text = text };
            ResetButton(button);
            button.style.width = 28;
            button.style.height = 28;
            button.style.backgroundColor = bright ? Alpha(color, 0.12f) : Alpha(Panel, 0.72f);
            button.style.color = bright ? color : ParchDim;
            Border(button, bright ? color : PA(0.18f), 1);
            Radius(button, 6);
            button.style.fontSize = 16;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(button, Sans);
            return button;
        }

        private static string FindSchool(string spellName)
        {
            for (int i = 0; i < IgMockData.SpellSchools.Length; i++)
            {
                for (int s = 0; s < IgMockData.SpellSchools[i].Spells.Length; s++)
                {
                    if (IgMockData.SpellSchools[i].Spells[s].Name == spellName)
                        return IgMockData.SpellSchools[i].Name;
                }
            }
            return "Destruction";
        }
    }
}

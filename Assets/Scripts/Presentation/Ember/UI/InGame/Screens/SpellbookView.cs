using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class SpellbookView
    {
        private readonly VisualElement _overlay;

        public SpellbookView(VisualElement stageCanvas, Action onClose)
        {
            _overlay = IgModal.Build("Spellbook", false, () => { Close(); onClose?.Invoke(); }, out var content);
            content.style.flexDirection = FlexDirection.Row;
            content.style.backgroundColor = VoidWarm;

            var school = IgMockData.SpellSchools[0];
            var selected = school.Spells[0];

            content.Add(BuildSchoolPane(school.Name));
            content.Add(BuildSpellListPane(school, selected));
            content.Add(BuildSpellDetailPane(school.Name, selected));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static VisualElement BuildSchoolPane(string activeSchool)
        {
            var pane = new VisualElement();
            pane.style.width = 170;
            pane.style.borderRightWidth = 1;
            pane.style.borderRightColor = PA(0.10f);
            pane.style.paddingTop = 16;
            pane.style.paddingBottom = 16;
            pane.style.paddingLeft = 12;
            pane.style.paddingRight = 12;
            pane.style.flexShrink = 0;

            var label = Text("SCHOOL", Sans, 10, PA(0.35f), FontStyle.Bold);
            label.style.letterSpacing = 1.8f;
            label.style.marginBottom = 6;
            pane.Add(label);

            for (int i = 0; i < IgMockData.SpellSchools.Length; i++)
            {
                var school = IgMockData.SpellSchools[i];
                bool active = school.Name == activeSchool;
                var row = Row();
                row.style.alignItems = Align.Center;
                row.style.height = 40;
                row.style.marginBottom = 6;
                row.style.paddingLeft = 12;
                row.style.paddingRight = 12;
                row.style.backgroundColor = active ? Alpha(School(school.Name), 0.10f) : Dark(0.5f);
                Border(row, active ? School(school.Name) : PA(0.10f), 1);
                Radius(row, 9);

                var dot = new VisualElement();
                dot.style.width = 8;
                dot.style.height = 8;
                dot.style.marginRight = 10;
                dot.style.backgroundColor = School(school.Name);
                Radius(dot, 999);
                row.Add(dot);

                var title = Text(school.Name, Sans, 12, active ? Parch : PA(0.50f), FontStyle.Bold);
                row.Add(title);

                var count = Text(school.Spells.Length.ToString(), Sans, 10, active ? School(school.Name) : PA(0.28f));
                count.style.marginLeft = StyleKeyword.Auto;
                row.Add(count);
                pane.Add(row);
            }

            return pane;
        }

        private static VisualElement BuildSpellListPane(SpellSchoolData school, SpellData selected)
        {
            var pane = new ScrollView();
            pane.style.flexGrow = 1;
            pane.style.paddingTop = 16;
            pane.style.paddingBottom = 16;
            pane.style.paddingLeft = 20;
            pane.style.paddingRight = 20;

            var label = Text($"{school.Name.ToUpperInvariant()} · {school.Spells.Length} SPELLS", Sans, 10, School(school.Name), FontStyle.Bold);
            label.style.letterSpacing = 1.8f;
            label.style.marginBottom = 14;
            pane.Add(label);

            for (int i = 0; i < school.Spells.Length; i++)
            {
                var spell = school.Spells[i];
                bool isSelected = spell.Name == selected.Name;
                var card = Row();
                card.style.alignItems = Align.Center;
                card.style.marginBottom = 8;
                card.style.paddingTop = 14;
                card.style.paddingBottom = 14;
                card.style.paddingLeft = 16;
                card.style.paddingRight = 16;
                card.style.backgroundColor = isSelected ? Alpha(School(school.Name), 0.08f) : Dark(0.65f);
                Border(card, isSelected ? School(school.Name) : PA(0.12f), isSelected ? 2 : 1);
                Radius(card, 10);

                var icon = new VisualElement();
                icon.style.width = 36;
                icon.style.height = 36;
                icon.style.marginRight = 14;
                icon.style.backgroundColor = Alpha(School(school.Name), 0.13f);
                Border(icon, Alpha(School(school.Name), 0.27f), 1);
                Radius(icon, 8);
                icon.style.alignItems = Align.Center;
                icon.style.justifyContent = Justify.Center;
                icon.Add(Text(spell.Name.Substring(0, 1), Serif, 14, School(school.Name)));
                card.Add(icon);

                var textWrap = new VisualElement();
                textWrap.style.flexGrow = 1;
                var name = Text(spell.Name, Sans, 14, isSelected ? Parch : ParchDim, FontStyle.Bold);
                textWrap.Add(name);
                var sub = Text($"{spell.Effect} · {spell.Range}", Sans, 11, PA(0.40f));
                sub.style.marginTop = 2;
                textWrap.Add(sub);
                card.Add(textWrap);

                var cost = Text($"{spell.ManaCost} MP", Sans, 12, Mana, FontStyle.Bold);
                card.Add(cost);
                pane.Add(card);
            }

            return pane;
        }

        private static VisualElement BuildSpellDetailPane(string school, SpellData spell)
        {
            var pane = new VisualElement();
            pane.style.width = 250;
            pane.style.borderLeftWidth = 1;
            pane.style.borderLeftColor = PA(0.10f);
            pane.style.paddingTop = 18;
            pane.style.paddingBottom = 18;
            pane.style.paddingLeft = 16;
            pane.style.paddingRight = 16;
            pane.style.flexShrink = 0;

            pane.Add(Text(spell.Name, Serif, 16, Parch));
            var schoolLabel = Text(school.ToUpperInvariant(), Sans, 11, School(school));
            schoolLabel.style.letterSpacing = 0.8f;
            schoolLabel.style.marginTop = 4;
            schoolLabel.style.marginBottom = 14;
            pane.Add(schoolLabel);

            pane.Add(BuildDetail("Effect", spell.Effect));
            pane.Add(BuildDetail("Range", spell.Range));
            pane.Add(BuildDetail("Duration", spell.Duration));
            pane.Add(BuildDetail("Cost", $"{spell.ManaCost} MP"));

            var assign = Text("ASSIGN TO BAR", Sans, 10, PA(0.38f), FontStyle.Bold);
            assign.style.letterSpacing = 1.4f;
            assign.style.marginTop = 16;
            assign.style.marginBottom = 8;
            pane.Add(assign);

            var slots = Row();
            for (int i = 0; i < IgMockData.SpellBar.Length; i++)
            {
                var sl = IgMockData.SpellBar[i];
                bool active = sl.Spell == spell.Name;
                var box = new VisualElement();
                box.style.width = 40;
                box.style.height = 40;
                box.style.marginRight = i < IgMockData.SpellBar.Length - 1 ? 6 : 0;
                box.style.backgroundColor = sl.Spell != null ? Alpha(Panel, 0.72f) : Dark(0.55f);
                Border(box, active ? Gold : PA(0.18f), 1);
                Radius(box, 7);
                box.style.alignItems = Align.Center;
                box.style.justifyContent = Justify.Center;
                box.Add(Text(sl.Slot.ToString(), Sans, 8, WA(0.4f)));
                if (!string.IsNullOrEmpty(sl.Spell))
                {
                    var mini = Text(Short(sl.Spell), Sans, 7, ParchDim);
                    mini.style.position = Position.Absolute;
                    mini.style.bottom = 4;
                    box.Add(mini);
                }
                slots.Add(box);
            }
            pane.Add(slots);

            var cast = new Button { text = "CAST NOW" };
            ResetButton(cast);
            cast.style.marginTop = 14;
            cast.style.height = 36;
            cast.style.backgroundColor = Gold;
            cast.style.color = Ink;
            cast.style.fontSize = 12;
            cast.style.letterSpacing = 0.8f;
            cast.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(cast, Sans);
            Border(cast, Amber, 1);
            Radius(cast, 7);
            pane.Add(cast);
            return pane;
        }

        private static VisualElement BuildDetail(string key, string value)
        {
            var row = Row();
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingTop = 5;
            row.style.paddingBottom = 5;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = PA(0.07f);
            row.Add(Text(key, Sans, 12, PA(0.38f)));
            row.Add(Text(value, Sans, 12, Parch));
            return row;
        }

        private static string Short(string spell)
        {
            string first = spell.Split(' ')[0];
            return first.Substring(0, Mathf.Min(4, first.Length));
        }
    }
}

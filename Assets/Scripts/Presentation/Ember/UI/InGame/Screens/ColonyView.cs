using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class ColonyView
    {
        private readonly VisualElement _overlay;

        public ColonyView(VisualElement stageCanvas, Action onClose, Action<string, string> onAssignTask = null)
        {
            _overlay = IgModal.Build("Colony", true, () => { Close(); onClose?.Invoke(); }, out var content);
            content.style.flexDirection = FlexDirection.Row;

            var selected = IgMockData.ColonyNpcs[0];
            content.Add(BuildNpcGrid(selected.Name));
            content.Add(BuildTaskPane(selected, onAssignTask));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static ScrollView BuildNpcGrid(string selectedName)
        {
            var pane = new ScrollView();
            pane.style.flexGrow = 1;
            pane.style.paddingTop = 20;
            pane.style.paddingBottom = 20;
            pane.style.paddingLeft = 24;
            pane.style.paddingRight = 24;

            var wrap = new VisualElement();
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.flexWrap = Wrap.Wrap;
            pane.Add(wrap);

            for (int i = 0; i < IgMockData.ColonyNpcs.Length; i++)
            {
                var npc = IgMockData.ColonyNpcs[i];
                bool selected = npc.Name == selectedName;
                var card = new VisualElement();
                card.style.width = Length.Percent(48.8f);
                card.style.marginRight = (i % 2) == 0 ? Length.Percent(2.4f) : 0;
                card.style.marginBottom = 14;
                card.style.paddingTop = 16;
                card.style.paddingBottom = 16;
                card.style.paddingLeft = 18;
                card.style.paddingRight = 18;
                card.style.backgroundColor = selected ? GA(0.09f) : Dark(0.65f);
                Border(card, selected ? Gold : PA(0.12f), selected ? 2 : 1);
                Radius(card, 14);

                var top = Row();
                top.style.marginBottom = 12;
                var portrait = new VisualElement();
                portrait.style.width = 42;
                portrait.style.height = 52;
                portrait.style.marginRight = 12;
                portrait.style.backgroundColor = Alpha(Panel, 0.72f);
                Border(portrait, PA(0.18f), 1);
                Radius(portrait, 8);
                portrait.style.alignItems = Align.Center;
                portrait.style.justifyContent = Justify.Center;
                portrait.Add(Text(npc.Name.Substring(0, 1), Serif, 18, PA(0.30f)));
                top.Add(portrait);

                var ident = new VisualElement();
                ident.style.flexGrow = 1;
                ident.Add(Text(npc.Name, Sans, 14, Parch, FontStyle.Bold));
                var role = Text(npc.Role, Sans, 11, Amber);
                role.style.letterSpacing = 0.6f;
                ident.Add(role);
                var mood = Row();
                mood.style.marginTop = 4;
                var dot = new VisualElement();
                dot.style.width = 7;
                dot.style.height = 7;
                dot.style.marginRight = 5;
                dot.style.backgroundColor = MoodColor(npc.Mood);
                Radius(dot, 999);
                mood.Add(dot);
                mood.Add(Text(npc.Mood, Sans, 10, MoodColor(npc.Mood)));
                ident.Add(mood);
                top.Add(ident);

                var hpWrap = new VisualElement();
                hpWrap.style.alignItems = Align.FlexEnd;
                hpWrap.Add(Text($"{npc.Hp}/{npc.HpMax}", Sans, 13, Health, FontStyle.Bold));
                hpWrap.Add(Text("HP", Sans, 9, PA(0.35f)));
                top.Add(hpWrap);
                card.Add(top);

                for (int n = 0; n < npc.Needs.Length; n++)
                    card.Add(BuildNeed(npc.Needs[n]));

                var task = Text("⟶ " + npc.Task, Serif, 12, PA(0.50f), FontStyle.Italic);
                task.style.marginTop = 8;
                task.style.paddingTop = 8;
                task.style.borderTopWidth = 1;
                task.style.borderTopColor = PA(0.07f);
                card.Add(task);

                wrap.Add(card);
            }

            return pane;
        }

        private static VisualElement BuildTaskPane(ColonyNpcData npc, Action<string, string> onAssignTask)
        {
            var pane = new VisualElement();
            pane.style.width = 240;
            pane.style.flexShrink = 0;
            pane.style.borderLeftWidth = 1;
            pane.style.borderLeftColor = PA(0.10f);
            pane.style.paddingTop = 18;
            pane.style.paddingBottom = 18;
            pane.style.paddingLeft = 16;
            pane.style.paddingRight = 16;

            var label = Text($"ASSIGN: {npc.Name.Split(' ')[0]}".ToUpperInvariant(), Sans, 10, Gold, FontStyle.Bold);
            label.style.letterSpacing = 1.8f;
            label.style.marginBottom = 14;
            pane.Add(label);

            string[] tasks =
            {
                "Rest", "Forage Food", "Fetch Water", "Guard Perimeter",
                "Craft at Workbench", "Patrol Route", "Idle",
            };
            for (int i = 0; i < tasks.Length; i++)
            {
                bool active = npc.Task == tasks[i];
                var row = new Button(() => onAssignTask?.Invoke(npc.Name, tasks[i]));
                ResetButton(row);
                row.style.flexDirection = FlexDirection.Row;
                row.style.height = 38;
                row.style.marginBottom = 6;
                row.style.paddingLeft = 12;
                row.style.paddingRight = 12;
                row.style.justifyContent = Justify.Center;
                row.style.backgroundColor = active ? GA(0.10f) : Dark(0.55f);
                Border(row, active ? Gold : PA(0.10f), 1);
                Radius(row, 8);
                row.Add(Text(tasks[i], Sans, 12, active ? Parch : PA(0.55f)));
                pane.Add(row);
            }

            return pane;
        }

        private static VisualElement BuildNeed(NeedData need)
        {
            var row = Row();
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 5;
            var label = Text(need.Name.ToUpperInvariant(), Sans, 9, PA(0.38f));
            label.style.letterSpacing = 0.6f;
            label.style.width = 48;
            row.Add(label);

            var track = new VisualElement();
            track.style.flexGrow = 1;
            track.style.height = 5;
            track.style.backgroundColor = C(0, 0, 0, 0.35f);
            Radius(track, 3);
            var fill = new VisualElement();
            fill.style.width = Length.Percent(need.Value);
            fill.style.height = 5;
            fill.style.backgroundColor = need.Value > 75 ? NeedColor(need.Name) : need.Value > 40 ? Amber : Health;
            Radius(fill, 3);
            track.Add(fill);
            row.Add(track);

            var value = Text(need.Value.ToString(), Sans, 9, PA(0.35f));
            value.style.width = 24;
            value.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(value);
            return row;
        }

        private static Color MoodColor(string mood)
        {
            switch (mood)
            {
                case "Content": return Success;
                case "Anxious": return Amber;
                case "Tired": return Fatigue;
                default: return Violet;
            }
        }

        private static Color NeedColor(string need) => need == "Hunger" ? Health : need == "Fatigue" ? Fatigue : Mana;
    }
}

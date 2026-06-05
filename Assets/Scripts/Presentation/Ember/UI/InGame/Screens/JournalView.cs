using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class JournalView
    {
        private readonly VisualElement _overlay;

        public JournalView(VisualElement stageCanvas, Action onClose)
        {
            _overlay = IgModal.Build("Journal", false, () => { Close(); onClose?.Invoke(); }, out var content);
            content.style.flexDirection = FlexDirection.Row;
            content.style.backgroundColor = VoidWarm;

            // TODO(real-data): no host source yet.
            var selected = IgMockData.Quests[0];
            content.Add(BuildQuestList(selected.Id));
            content.Add(BuildQuestDetail(selected));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static ScrollView BuildQuestList(int selectedId)
        {
            var pane = new ScrollView();
            pane.style.width = 250;
            pane.style.flexShrink = 0;
            pane.style.borderRightWidth = 1;
            pane.style.borderRightColor = PA(0.10f);
            pane.style.paddingTop = 16;
            pane.style.paddingBottom = 16;
            pane.style.paddingLeft = 12;
            pane.style.paddingRight = 12;

            pane.Add(BuildSection("ACTIVE", Amber));
            for (int i = 0; i < IgMockData.Quests.Length; i++)
            {
                if (IgMockData.Quests[i].Status != "active") continue;
                pane.Add(BuildQuestRow(IgMockData.Quests[i], IgMockData.Quests[i].Id == selectedId));
            }

            pane.Add(BuildSection("COMPLETED", PA(0.30f), 14));
            for (int i = 0; i < IgMockData.Quests.Length; i++)
            {
                if (IgMockData.Quests[i].Status != "completed") continue;
                pane.Add(BuildQuestRow(IgMockData.Quests[i], false));
            }

            return pane;
        }

        private static ScrollView BuildQuestDetail(QuestData quest)
        {
            var pane = new ScrollView();
            pane.style.flexGrow = 1;
            pane.style.paddingTop = 24;
            pane.style.paddingBottom = 24;
            pane.style.paddingLeft = 32;
            pane.style.paddingRight = 32;

            var title = Text(quest.Title, Serif, 22, Parch, FontStyle.Bold);
            pane.Add(title);
            var status = Text(quest.Status.ToUpperInvariant(), Sans, 11, quest.Status == "completed" ? Success : Amber);
            status.style.letterSpacing = 1f;
            status.style.marginTop = 6;
            status.style.marginBottom = 18;
            pane.Add(status);

            var desc = Text(quest.Description, Serif, 16, ParchDim);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.unityFontStyleAndWeight = FontStyle.Italic;
            desc.style.marginBottom = 24;
            pane.Add(desc);

            var head = BuildSection("OBJECTIVES", Gold, 0);
            head.style.marginBottom = 10;
            pane.Add(head);

            for (int i = 0; i < quest.Tasks.Length; i++)
                pane.Add(BuildTask(quest.Tasks[i]));

            var note = new VisualElement();
            note.style.marginTop = 24;
            note.style.paddingTop = 14;
            note.style.paddingBottom = 14;
            note.style.paddingLeft = 18;
            note.style.paddingRight = 18;
            note.style.backgroundColor = Alpha(VoidWarm, 0.70f);
            Border(note, PA(0.12f), 1);
            Radius(note, 10);
            var dm = Text("DM NOTE", Sans, 9, Gold, FontStyle.Bold);
            dm.style.letterSpacing = 2f;
            dm.style.marginBottom = 8;
            note.Add(dm);
            var copy = Text("The road east has not been honest with you yet. The caravan's story is not finished.", Serif, 14, PA(0.55f), FontStyle.Italic);
            copy.style.whiteSpace = WhiteSpace.Normal;
            note.Add(copy);
            pane.Add(note);
            return pane;
        }

        private static Label BuildSection(string text, Color color, int top = 0)
        {
            var label = Text(text, Sans, 10, color, FontStyle.Bold);
            label.style.letterSpacing = 1.8f;
            label.style.marginTop = top;
            label.style.marginBottom = 8;
            return label;
        }

        private static VisualElement BuildQuestRow(QuestData quest, bool selected)
        {
            var row = new VisualElement();
            row.style.marginBottom = 5;
            row.style.paddingTop = 10;
            row.style.paddingBottom = 10;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.backgroundColor = selected ? GA(0.11f) : Dark(0.50f);
            Border(row, selected ? Gold : PA(0.10f), selected ? 2 : 1);
            Radius(row, 9);
            row.Add(Text(quest.Title, Sans, 13, selected ? Parch : ParchDim, FontStyle.Bold));
            var status = Text(quest.Status.ToUpperInvariant(), Sans, 10, quest.Status == "completed" ? Success : GA(0.55f));
            status.style.letterSpacing = 0.8f;
            status.style.marginTop = 3;
            row.Add(status);
            return row;
        }

        private static VisualElement BuildTask(QuestTaskData task)
        {
            var row = Row();
            row.style.alignItems = Align.FlexStart;
            row.style.marginBottom = 8;

            var box = new VisualElement();
            box.style.width = 18;
            box.style.height = 18;
            box.style.marginRight = 10;
            box.style.marginTop = 1;
            box.style.backgroundColor = task.Done ? Success : PA(0.10f);
            Border(box, task.Done ? Success : PA(0.22f), 1);
            Radius(box, 4);
            if (task.Done)
            {
                var tick = Text("✓", Sans, 10, Bone, FontStyle.Bold);
                tick.style.unityTextAlign = TextAnchor.MiddleCenter;
                tick.style.width = 18;
                tick.style.height = 18;
                box.Add(tick);
            }
            row.Add(box);

            var copy = Text(task.Text, Serif, 15, ParchDim);
            copy.style.whiteSpace = WhiteSpace.Normal;
            copy.style.opacity = task.Done ? 0.55f : 1f;
            row.Add(copy);
            return row;
        }
    }
}

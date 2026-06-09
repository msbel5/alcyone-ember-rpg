using System;
using System.Collections.Generic;
using EmberCrpg.Presentation.Ember.UI.InGame;
using UnityEngine;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class JournalView
    {
        private readonly VisualElement _overlay;

        public JournalView(VisualElement stageCanvas, Action onClose)
        {
            _overlay = IgModal.Build("Journal", false, () => { Close(); onClose?.Invoke(); }, out var content);
            content.style.flexDirection = FlexDirection.Column;

            var chapters = IgJournalData.Chapters ?? Array.Empty<JournalChapterData>();
            if (chapters.Length == 0)
            {
                content.Add(EmptyState(
                    IgJournalData.EmptyTitle,
                    IgJournalData.EmptyBody,
                    IgJournalData.EmptyDetail));
                stageCanvas.Add(_overlay);
                return;
            }

            int chapterIndex = Mathf.Clamp(IgJournalData.CurrentChapter, 0, chapters.Length - 1);
            var chapter = chapters[chapterIndex];
            content.Add(BuildHeader(chapter, chapters.Length));
            content.Add(BuildEntries(chapter));
            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static VisualElement BuildHeader(JournalChapterData chapter, int chapterCount)
        {
            var header = new VisualElement();
            header.style.marginBottom = 18;
            header.Add(Text(chapter.Title, Serif, 20, Parch, FontStyle.Bold));

            var meta = Text(
                "Chapter " + (chapter.ChapterIndex + 1).ToString() + " of " + chapterCount.ToString() + " · " + chapter.Entries.Length.ToString() + " entries",
                Sans,
                11,
                Gold,
                FontStyle.Bold);
            meta.style.letterSpacing = 1.2f;
            meta.style.marginTop = 4;
            header.Add(meta);
            return header;
        }

        private static ScrollView BuildEntries(JournalChapterData chapter)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.minHeight = 0;
            StyleScroll(scroll);

            var active = new List<JournalEntryData>();
            var resolved = new List<JournalEntryData>();
            for (int i = 0; i < chapter.Entries.Length; i++)
            {
                var entry = chapter.Entries[i];
                if (entry == null) continue;
                if (entry.Status == EmberCrpg.Presentation.Ember.UI.JournalEntryStatus.Active) active.Add(entry);
                else resolved.Add(entry);
            }

            AddSection(scroll, "Active Quests", active);
            AddSection(scroll, "Resolved Threads", resolved);
            return scroll;
        }

        private static void AddSection(ScrollView scroll, string title, IReadOnlyList<JournalEntryData> entries)
        {
            var head = Text(title.ToUpperInvariant(), Sans, 10, Gold, FontStyle.Bold);
            head.style.letterSpacing = 2.0f;
            head.style.marginBottom = 10;
            head.style.marginTop = scroll.childCount == 0 ? 0 : 18;
            scroll.Add(head);

            if (entries == null || entries.Count == 0)
            {
                var note = Text("No entries.", Sans, 12, PA(0.34f), FontStyle.Italic);
                note.style.marginBottom = 8;
                scroll.Add(note);
                return;
            }

            for (int i = 0; i < entries.Count; i++)
                scroll.Add(BuildEntry(entries[i]));
        }

        private static VisualElement BuildEntry(JournalEntryData entry)
        {
            var card = new VisualElement();
            card.style.marginBottom = 10;
            card.style.paddingLeft = 16;
            card.style.paddingRight = 16;
            card.style.paddingTop = 14;
            card.style.paddingBottom = 14;
            card.style.backgroundColor = Dark(0.58f);
            Border(card, EntryBorder(entry.Status), 1);
            Radius(card, 10);

            var top = Row();
            top.style.alignItems = Align.Center;
            top.Add(Text(entry.Title, Sans, 13, Parch, FontStyle.Bold));
            var status = Text(entry.StatusLabel.ToUpperInvariant(), Sans, 10, EntryBorder(entry.Status), FontStyle.Bold);
            status.style.marginLeft = StyleKeyword.Auto;
            status.style.letterSpacing = 1f;
            top.Add(status);
            card.Add(top);

            var meta = Text(entry.DateLabel + " · " + entry.CategoryLabel, Sans, 10, PA(0.38f));
            meta.style.marginTop = 4;
            card.Add(meta);

            var body = Text(entry.Body, Serif, 14, ParchDim);
            body.style.marginTop = 8;
            body.style.whiteSpace = WhiteSpace.Normal;
            card.Add(body);
            return card;
        }

        private static Color EntryBorder(EmberCrpg.Presentation.Ember.UI.JournalEntryStatus status)
        {
            switch (status)
            {
                case EmberCrpg.Presentation.Ember.UI.JournalEntryStatus.Completed: return Alpha(Success, 0.70f);
                case EmberCrpg.Presentation.Ember.UI.JournalEntryStatus.Failed: return Alpha(Health, 0.70f);
                default: return Alpha(Gold, 0.70f);
            }
        }
    }
}

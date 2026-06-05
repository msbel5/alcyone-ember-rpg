using System.Collections.Generic;

namespace EmberCrpg.Presentation.Ember.UI
{
    public enum JournalEntryStatus
    {
        Active = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct JournalEntryRow
    {
        public readonly string EntryId;
        public readonly string Title;
        public readonly string DateLabel;
        public readonly string Body;
        public readonly string CategoryLabel;
        public readonly JournalEntryStatus Status;

        public JournalEntryRow(string entryId, string title, string dateLabel, string body, string categoryLabel, JournalEntryStatus status)
        {
            EntryId = entryId ?? string.Empty;
            Title = title ?? string.Empty;
            DateLabel = dateLabel ?? string.Empty;
            Body = body ?? string.Empty;
            CategoryLabel = categoryLabel ?? string.Empty;
            Status = status;
        }
    }

    public readonly struct JournalChapterRow
    {
        public readonly int ChapterIndex;
        public readonly string Title;
        public readonly IReadOnlyList<JournalEntryRow> Entries;

        public JournalChapterRow(int chapterIndex, string title, IReadOnlyList<JournalEntryRow> entries)
        {
            ChapterIndex = chapterIndex;
            Title = title ?? string.Empty;
            Entries = entries ?? System.Array.Empty<JournalEntryRow>();
        }
    }

    public interface IJournalSource
    {
        IReadOnlyList<JournalChapterRow> GetChapters();
        int GetCurrentChapter();
    }
}

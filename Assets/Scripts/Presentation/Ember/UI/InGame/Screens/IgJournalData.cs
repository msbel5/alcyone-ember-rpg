namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public static class IgJournalData
    {
        public static readonly JournalChapterData[] DefaultChapters =
        {
            new JournalChapterData(
                0,
                "Chapter 1 · First Threads",
                new[]
                {
                    new JournalEntryData(
                        "default-forge",
                        "Forge an Iron Ingot",
                        "Year 1 · Day 1 · 00:00",
                        "A settlement smith needs proof that you can shape raw iron into something useful.",
                        "Main",
                        "Active",
                        EmberCrpg.Presentation.Ember.UI.JournalEntryStatus.Active)
                })
        };

        public static JournalChapterData[] Chapters = DefaultChapters;
        public static int CurrentChapter = 0;
        public static string EmptyTitle = "Journal";
        public static string EmptyBody = "No active quests — the world has asked nothing of you yet.";
        public static string EmptyDetail = "No live quest has been accepted.";
    }

    public sealed record JournalChapterData(int ChapterIndex, string Title, JournalEntryData[] Entries);
    public sealed record JournalEntryData(
        string EntryId,
        string Title,
        string DateLabel,
        string Body,
        string CategoryLabel,
        string StatusLabel,
        EmberCrpg.Presentation.Ember.UI.JournalEntryStatus Status);
}

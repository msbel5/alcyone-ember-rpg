using System.Collections.Generic;
using System.Globalization;
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Presentation.Ember.UI;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter : IJournalSource
    {
        public IReadOnlyList<JournalChapterRow> GetChapters()
        {
            if (_world?.Quests == null || _world.Quests.Count == 0)
                return System.Array.Empty<JournalChapterRow>();

            var entries = new List<EntryProjection>();
            var settlement = ResolveStartingSettlementName();
            foreach (var pair in _world.Quests.Active)
            {
                var definition = QuestCatalog.Resolve(pair.Key);
                var state = pair.Value;
                if (state == null)
                    continue;

                entries.Add(new EntryProjection(
                    state.StartTick.TotalMinutes,
                    new JournalEntryRow(
                        pair.Key.Value.ToString(CultureInfo.InvariantCulture),
                        definition.DisplayName,
                        FormatJournalDate(state.StartTick),
                        BuildJournalBody(pair.Key, definition.DisplayName, settlement, state),
                        "Main",
                        ResolveStatus(state))));
            }

            if (entries.Count == 0)
                return System.Array.Empty<JournalChapterRow>();

            entries.Sort(CompareEntries);
            var rows = new JournalEntryRow[entries.Count];
            for (int i = 0; i < entries.Count; i++)
                rows[i] = entries[i].Row;
            return new[]
            {
                new JournalChapterRow(0, BuildChapterTitle(settlement), rows)
            };
        }

        public int GetCurrentChapter() => 0;

        private static int CompareEntries(EntryProjection left, EntryProjection right)
        {
            int byDate = left.SortKey.CompareTo(right.SortKey);
            if (byDate != 0) return byDate;
            return string.CompareOrdinal(left.Row.EntryId, right.Row.EntryId);
        }

        private static JournalEntryStatus ResolveStatus(QuestState state)
        {
            if (state == null || !state.IsComplete) return JournalEntryStatus.Active;
            return state.IsSuccess ? JournalEntryStatus.Completed : JournalEntryStatus.Failed;
        }

        private static string FormatJournalDate(EmberCrpg.Domain.Core.GameTime time)
        {
            return "Year " + time.Year.ToString(CultureInfo.InvariantCulture)
                + " · Day " + time.DayOfYear.ToString(CultureInfo.InvariantCulture)
                + " · " + time.Hour.ToString("00", CultureInfo.InvariantCulture)
                + ":" + time.Minute.ToString("00", CultureInfo.InvariantCulture);
        }

        private static string BuildJournalBody(QuestId id, string title, string settlement, QuestState state)
        {
            if (id == QuestCatalog.ForgeIronIngotId)
            {
                if (state != null && state.IsComplete)
                    return "The forge's request has been met. The first iron ingot now anchors your name in local memory.";
                if (!string.IsNullOrWhiteSpace(settlement))
                    return "The forge at " + settlement + " needs iron worked into an ingot. Begin there and prove you can shape something useful.";
            }

            if (state != null && state.IsComplete)
                return title + " is complete.";
            return title + " remains unresolved.";
        }

        private static string BuildChapterTitle(string settlement)
        {
            return string.IsNullOrWhiteSpace(settlement)
                ? "Chapter 1 · First Threads"
                : "Chapter 1 · " + settlement;
        }

        private readonly struct EntryProjection
        {
            public readonly long SortKey;
            public readonly JournalEntryRow Row;

            public EntryProjection(long sortKey, JournalEntryRow row)
            {
                SortKey = sortKey;
                Row = row;
            }
        }
    }
}

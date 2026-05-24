using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EmberCrpg.Domain.Generation
{
    public enum EntryState
    {
        Cached = 0,
        Missing = 1,
        RequiresGeneration = 2,
        Failed = 3,
    }

    public sealed class EntryRow
    {
        public EntryRow(string entryId, string category, string path, EntryState state, string reason)
        {
            EntryId = entryId ?? string.Empty;
            Category = category ?? string.Empty;
            Path = path ?? string.Empty;
            State = state;
            Reason = reason ?? string.Empty;
        }

        public string EntryId { get; }
        public string Category { get; }
        public string Path { get; }
        public EntryState State { get; }
        public string Reason { get; }
    }

    public sealed class ManifestScanReport
    {
        public ManifestScanReport(IReadOnlyList<EntryRow> entries, int failedSinceLastScan = 0)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            Entries = new ReadOnlyCollection<EntryRow>(new List<EntryRow>(entries));
            FailedSinceLastScan = failedSinceLastScan;
            Total = Entries.Count;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].State == EntryState.Cached) Cached++;
                if (Entries[i].State == EntryState.Missing) Missing++;
                if (Entries[i].State == EntryState.RequiresGeneration) RequiresGeneration++;
                if (Entries[i].State == EntryState.Failed) FailedSinceLastScan++;
            }
        }

        public IReadOnlyList<EntryRow> Entries { get; }
        public int Total { get; }
        public int Cached { get; }
        public int Missing { get; }
        public int RequiresGeneration { get; }
        public int FailedSinceLastScan { get; private set; }
    }
}

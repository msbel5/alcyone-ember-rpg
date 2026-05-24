using System.Collections.Generic;
using EmberCrpg.Domain.Actors;

namespace EmberCrpg.Presentation.Visual
{
    /// <summary>Read-only recent memory-fact rows for debug overlays. Pure C#.</summary>
    public sealed class MemoryFactSnapshot
    {
        private readonly IReadOnlyList<MemoryFactRow> _rows;

        public MemoryFactSnapshot(IReadOnlyList<MemoryFactRow> rows)
        {
            _rows = rows ?? new MemoryFactRow[0];
        }

        public IReadOnlyList<MemoryFactRow> Rows => _rows;

        public static MemoryFactSnapshot FromActors(IEnumerable<ActorRecord> actors, int maxRows)
        {
            if (actors == null || maxRows <= 0)
                return new MemoryFactSnapshot(new MemoryFactRow[0]);

            var rows = new List<MemoryFactRow>();
            foreach (var actor in actors)
            {
                if (actor?.Memory == null)
                    continue;
                foreach (var fact in actor.Memory.Facts)
                {
                    rows.Add(new MemoryFactRow(
                        fact.Rememberer.Value,
                        fact.Topic.Code,
                        fact.AboutActor.Value,
                        fact.RecordedAt.TotalMinutes,
                        fact.Detail));
                }
            }

            var start = rows.Count > maxRows ? rows.Count - maxRows : 0;
            if (start == 0)
                return new MemoryFactSnapshot(rows);

            var tail = new List<MemoryFactRow>(rows.Count - start);
            for (var i = start; i < rows.Count; i++)
                tail.Add(rows[i]);
            return new MemoryFactSnapshot(tail);
        }
    }

    public readonly struct MemoryFactRow
    {
        public MemoryFactRow(ulong remembererId, string topicCode, ulong aboutActorId, long recordedAtMinutes, string detail)
        {
            RemembererId = remembererId;
            TopicCode = topicCode ?? string.Empty;
            AboutActorId = aboutActorId;
            RecordedAtMinutes = recordedAtMinutes;
            Detail = detail ?? string.Empty;
        }

        public ulong RemembererId { get; }
        public string TopicCode { get; }
        public ulong AboutActorId { get; }
        public long RecordedAtMinutes { get; }
        public string Detail { get; }
    }
}

using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Presentation.Visual
{
    /// <summary>
    /// Read-only snapshot of the tail of the WorldEventLog for Unity HUD overlays.
    /// Pure C#: no UnityEngine, no mutation. Generic across all sprints — feeds
    /// combat / dialogue / season HUD readouts in Faz 11 scenes.
    /// </summary>
    public sealed class WorldEventTailSnapshot
    {
        private readonly IReadOnlyList<WorldEventRow> _rows;

        public WorldEventTailSnapshot(IReadOnlyList<WorldEventRow> rows)
        {
            _rows = rows ?? new WorldEventRow[0];
        }

        public IReadOnlyList<WorldEventRow> Rows => _rows;

        public static WorldEventTailSnapshot FromLog(WorldEventLog log, int maxRows)
        {
            if (log == null || maxRows <= 0)
                return new WorldEventTailSnapshot(new WorldEventRow[0]);

            var source = log.Events;
            var start = source.Count > maxRows ? source.Count - maxRows : 0;
            var rows = new List<WorldEventRow>(source.Count - start);
            for (var i = start; i < source.Count; i++)
            {
                var e = source[i];
                rows.Add(new WorldEventRow(
                    e.Tick,
                    e.Kind.ToString(),
                    e.ActorId,
                    e.SiteId,
                    e.Reason ?? string.Empty));
            }
            return new WorldEventTailSnapshot(rows);
        }
    }

    /// <summary>One row in <see cref="WorldEventTailSnapshot"/>.</summary>
    public readonly struct WorldEventRow
    {
        public WorldEventRow(GameTime tick, string kindCode, ActorId actorId, SiteId siteId, string reason)
        {
            Tick = tick;
            KindCode = kindCode ?? string.Empty;
            ActorId = actorId;
            SiteId = siteId;
            Reason = reason ?? string.Empty;
        }

        public GameTime Tick { get; }
        public string KindCode { get; }
        public ActorId ActorId { get; }
        public SiteId SiteId { get; }
        public string Reason { get; }
    }
}

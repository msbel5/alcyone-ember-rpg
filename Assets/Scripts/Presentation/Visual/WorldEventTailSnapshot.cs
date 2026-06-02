using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Presentation.Visual
{
    /// <summary>
    /// Read-only snapshot of the tail of the WorldEventLog for Unity HUD overlays.
    /// Pure C#: no UnityEngine, no mutation. Generic across all sprints — feeds
    /// combat / dialogue / season HUD readouts in Phase 11 scenes.
    /// </summary>
    public sealed class WorldEventTailSnapshot
    {
        private readonly IReadOnlyList<WorldEventRow> _rows;

        public WorldEventTailSnapshot(IReadOnlyList<WorldEventRow> rows)
        {
            _rows = rows ?? new WorldEventRow[0];
        }

        public IReadOnlyList<WorldEventRow> Rows => _rows;

        // Why: preserve the shared tail projection path for callers that want the raw latest events.
        public static WorldEventTailSnapshot FromLog(WorldEventLog log, int maxRows)
        {
            if (log == null || maxRows <= 0)
                return new WorldEventTailSnapshot(new WorldEventRow[0]);

            var source = log.Events;
            var start = source.Count > maxRows ? source.Count - maxRows : 0;
            var rows = new List<WorldEventRow>(source.Count - start);
            for (var i = start; i < source.Count; i++)
                rows.Add(ToRow(source[i]));
            return new WorldEventTailSnapshot(rows);
        }

        // Why: project the latest matching events without mutating the deterministic source log.
        public static WorldEventTailSnapshot FromLog(WorldEventLog log, int maxRows, Func<string, bool> kindPredicate)
        {
            if (log == null || maxRows <= 0)
                return new WorldEventTailSnapshot(new WorldEventRow[0]);

            var includeKind = kindPredicate ?? (_ => true);
            var rows = new List<WorldEventRow>(Math.Min(maxRows, log.Events.Count));
            foreach (var worldEvent in log.Events)
            {
                if (!includeKind(worldEvent.Kind.ToString()))
                    continue;

                if (rows.Count == maxRows)
                    rows.RemoveAt(0);

                rows.Add(ToRow(worldEvent));
            }

            return new WorldEventTailSnapshot(rows);
        }

        // Why: keep every HUD-tail projection path on the same row-shaping code.
        private static WorldEventRow ToRow(WorldEvent worldEvent)
        {
            return new WorldEventRow(
                worldEvent.Tick,
                worldEvent.Kind.ToString(),
                worldEvent.ActorId,
                worldEvent.SiteId,
                worldEvent.Reason ?? string.Empty);
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

using System.Collections.Generic;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Presentation.Visual
{
    /// <summary>Read-only tail of combat-resolved events for debug overlays. Pure C#.</summary>
    public sealed class CombatEventTailSnapshot
    {
        private readonly IReadOnlyList<CombatEventTailRow> _rows;

        public CombatEventTailSnapshot(IReadOnlyList<CombatEventTailRow> rows)
        {
            _rows = rows ?? new CombatEventTailRow[0];
        }

        public IReadOnlyList<CombatEventTailRow> Rows => _rows;

        public static CombatEventTailSnapshot FromLog(WorldEventLog log, int maxRows)
        {
            if (log == null || maxRows <= 0)
                return new CombatEventTailSnapshot(new CombatEventTailRow[0]);

            var rows = new List<CombatEventTailRow>();
            foreach (var worldEvent in log.Events)
            {
                if (worldEvent.Kind != WorldEventKind.CombatResolved)
                    continue;
                rows.Add(new CombatEventTailRow(
                    worldEvent.Tick.TotalMinutes,
                    worldEvent.ActorId.Value,
                    worldEvent.SiteId.Value,
                    worldEvent.Reason));
            }

            var start = rows.Count > maxRows ? rows.Count - maxRows : 0;
            if (start == 0)
                return new CombatEventTailSnapshot(rows);

            var tail = new List<CombatEventTailRow>(rows.Count - start);
            for (var i = start; i < rows.Count; i++)
                tail.Add(rows[i]);
            return new CombatEventTailSnapshot(tail);
        }
    }

    public readonly struct CombatEventTailRow
    {
        public CombatEventTailRow(long tickMinutes, ulong actorId, ulong siteId, string reason)
        {
            TickMinutes = tickMinutes;
            ActorId = actorId;
            SiteId = siteId;
            Reason = reason ?? string.Empty;
        }

        public long TickMinutes { get; }
        public ulong ActorId { get; }
        public ulong SiteId { get; }
        public string Reason { get; }
    }
}

using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Resolved work target derived from a <see cref="WorksiteRecord"/>. Carries
    /// the worksite cell, a data-driven tag, and a deterministic queue cell.
    /// Phase 11 Atom 8 spec; closes CO-06 row in docs/sprint-phase-4-atom-map.md.
    /// </summary>
    public readonly struct WorksiteSlot : IEquatable<WorksiteSlot>
    {
        public WorksiteSlot(SiteId siteId, GridPosition position, string worksiteTag, GridPosition queuePosition)
        {
            if (worksiteTag == null)
                throw new ArgumentNullException(nameof(worksiteTag));

            SiteId = siteId;
            Position = position;
            WorksiteTag = worksiteTag;
            QueuePosition = queuePosition;
        }

        public SiteId SiteId { get; }
        public GridPosition Position { get; }
        public string WorksiteTag { get; }
        public GridPosition QueuePosition { get; }

        public static WorksiteSlot FromWorksite(WorksiteRecord record, string worksiteTag, GridPosition queuePosition)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (string.IsNullOrWhiteSpace(worksiteTag))
                throw new ArgumentException("WorksiteTag must be non-blank.", nameof(worksiteTag));

            return new WorksiteSlot(record.SiteId, record.Position, worksiteTag, queuePosition);
        }

        public bool Equals(WorksiteSlot other)
        {
            return SiteId.Equals(other.SiteId)
                && Position.Equals(other.Position)
                && string.Equals(WorksiteTag, other.WorksiteTag, StringComparison.Ordinal)
                && QueuePosition.Equals(other.QueuePosition);
        }

        public override bool Equals(object obj) => obj is WorksiteSlot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) ^ SiteId.GetHashCode();
                hash = (hash * 31) ^ Position.GetHashCode();
                hash = (hash * 31) ^ (WorksiteTag?.GetHashCode() ?? 0);
                hash = (hash * 31) ^ QueuePosition.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"WorksiteSlot(site={SiteId}, pos={Position.X},{Position.Y}, tag={WorksiteTag}, queue={QueuePosition.X},{QueuePosition.Y})";
        }

        public static bool operator ==(WorksiteSlot a, WorksiteSlot b) => a.Equals(b);
        public static bool operator !=(WorksiteSlot a, WorksiteSlot b) => !a.Equals(b);
    }
}

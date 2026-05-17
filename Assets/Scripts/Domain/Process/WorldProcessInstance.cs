using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Domain.Process
{
    /// <summary>Runtime state for one slow world process instance.</summary>
    public sealed class WorldProcessInstance
    {
        public WorldProcessInstance(WorldComponentId id, WorldProcessDef definition, SiteId siteId, WorldComponentId subjectId, int elapsedDays)
        {
            if (id.IsEmpty)
                throw new ArgumentException("WorldComponentId.Empty cannot back a WorldProcessInstance.", nameof(id));
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (siteId.IsEmpty)
                throw new ArgumentException("SiteId.Empty cannot back a WorldProcessInstance.", nameof(siteId));
            if (subjectId.IsEmpty)
                throw new ArgumentException("World process subject id is required.", nameof(subjectId));
            if (elapsedDays < 0 || elapsedDays > definition.DurationDays)
                throw new ArgumentOutOfRangeException(nameof(elapsedDays), "Elapsed days must fit inside process duration.");

            Id = id;
            Definition = definition;
            SiteId = siteId;
            SubjectId = subjectId;
            ElapsedDays = elapsedDays;
        }

        public WorldComponentId Id { get; }
        public WorldProcessDef Definition { get; }
        public SiteId SiteId { get; }
        public WorldComponentId SubjectId { get; }
        public int ElapsedDays { get; }
        public int RemainingDays { get { return Definition.DurationDays - ElapsedDays; } }
        public bool IsComplete { get { return ElapsedDays >= Definition.DurationDays; } }

        public WorldProcessInstance AdvanceOneDay()
        {
            if (IsComplete)
                return this;

            return new WorldProcessInstance(Id, Definition, SiteId, SubjectId, ElapsedDays + 1);
        }
    }
}

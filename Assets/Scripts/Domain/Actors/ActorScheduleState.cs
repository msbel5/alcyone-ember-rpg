using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;

// Design note:
// ActorScheduleState is Phase 3's tiny LIVING row for the actor's current job
// target. It stores only assignment state: no pathfinding, ticking, recipe
// starts, save/load mapping, or EventLog output. JobAssignmentSystem will later
// consume this row through ActorRecord/ActorStore.
// Atom-map ref: docs/sprint-phase-3-atom-map.md Actor job preference and schedule rail.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>
    /// Immutable actor schedule snapshot for idle or job-assigned state.
    /// </summary>
    public readonly struct ActorScheduleState : IEquatable<ActorScheduleState>
    {
        private ActorScheduleState(JobId currentJobId, SiteId targetSiteId, GridPosition targetWorksitePosition)
        {
            CurrentJobId = currentJobId;
            TargetSiteId = targetSiteId;
            TargetWorksitePosition = targetWorksitePosition;
        }

        /// <summary>Explicit idle state with no current job target.</summary>
        public static ActorScheduleState Idle
        {
            get { return default; }
        }

        /// <summary>Creates an assigned state for a concrete job and worksite target.</summary>
        public static ActorScheduleState Assigned(JobId currentJobId, SiteId targetSiteId, GridPosition targetWorksitePosition)
        {
            if (currentJobId.IsEmpty)
                throw new ArgumentException("Assigned actor schedule requires a non-empty job id.", nameof(currentJobId));
            if (targetSiteId.IsEmpty)
                throw new ArgumentException("Assigned actor schedule requires a non-empty target site id.", nameof(targetSiteId));

            return new ActorScheduleState(currentJobId, targetSiteId, targetWorksitePosition);
        }

        /// <summary>Current claimed job, or JobId.Empty when idle.</summary>
        public JobId CurrentJobId { get; }

        /// <summary>Site containing the assigned worksite, or SiteId.Empty when idle.</summary>
        public SiteId TargetSiteId { get; }

        /// <summary>Grid position of the assigned worksite. Meaningful only when assigned.</summary>
        public GridPosition TargetWorksitePosition { get; }

        /// <summary>True when the actor has no current job assignment.</summary>
        public bool IsIdle => CurrentJobId.IsEmpty;

        /// <summary>Returns true when both schedule states carry the same assignment fields.</summary>
        public bool Equals(ActorScheduleState other)
        {
            return CurrentJobId == other.CurrentJobId
                && TargetSiteId == other.TargetSiteId
                && TargetWorksitePosition.Equals(other.TargetWorksitePosition);
        }

        /// <summary>Returns true when the object is a schedule state with the same assignment fields.</summary>
        public override bool Equals(object obj)
        {
            return obj is ActorScheduleState other && Equals(other);
        }

        /// <summary>Returns a hash code derived from assignment fields only.</summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(CurrentJobId, TargetSiteId, TargetWorksitePosition);
        }

        /// <summary>Returns a compact debug label for this schedule state.</summary>
        public override string ToString()
        {
            return IsIdle ? "ActorScheduleState.Idle" : $"ActorScheduleState({CurrentJobId}, {TargetSiteId}, {TargetWorksitePosition})";
        }

        /// <summary>Returns true when schedule states carry the same assignment fields.</summary>
        public static bool operator ==(ActorScheduleState left, ActorScheduleState right)
        {
            return left.Equals(right);
        }

        /// <summary>Returns true when schedule states carry different assignment fields.</summary>
        public static bool operator !=(ActorScheduleState left, ActorScheduleState right)
        {
            return !left.Equals(right);
        }
    }
}

// Design note:
// W32 EAT slice (docs/ruh/w32/04-action-log.md §2.1): one action phase-transition row.
// CONSTRAINT: no strings on the hot path — every field is a number/enum; text is produced
// lazily by sinks/UI. Parallel-array save friendly.
namespace EmberCrpg.Domain.Actors.Actions
{
    /// <summary>Why a transition happened. Closed EAT-slice set; new actions extend it WITH their consumers.</summary>
    public enum ActionLogReason
    {
        None = 0,
        TargetSelected = 1,
        ReservationAcquired = 2,
        ReservationLost = 3,
        Arrived = 4,
        PathBlocked = 5,
        ProgressTicked = 6,
        Completed = 7,
        TargetGone = 8,
        InterruptPreempted = 9,
    }

    /// <summary>One deterministic action phase-transition record.</summary>
    public readonly struct ActionLogEntry
    {
        public ActionLogEntry(long tickMinutes, ulong actorId, ActorIntent intent,
            ActorActionType fromAction, ActionPhase fromPhase,
            ActorActionType toAction, ActionPhase toPhase,
            ulong targetId, ActionLogReason reason)
        {
            TickMinutes = tickMinutes;
            ActorId = actorId;
            Intent = intent;
            FromAction = fromAction;
            FromPhase = fromPhase;
            ToAction = toAction;
            ToPhase = toPhase;
            TargetId = targetId;
            Reason = reason;
        }

        public readonly long TickMinutes;       // GameTime.TotalMinutes — same clock as WorldEvents
        public readonly ulong ActorId;
        public readonly ActorIntent Intent;     // intent AFTER the transition
        public readonly ActorActionType FromAction;
        public readonly ActionPhase FromPhase;
        public readonly ActorActionType ToAction;
        public readonly ActionPhase ToPhase;
        public readonly ulong TargetId;         // site id of the reserved pile; 0 = none
        public readonly ActionLogReason Reason;
    }
}

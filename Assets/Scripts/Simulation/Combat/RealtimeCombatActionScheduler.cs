using System;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;

// Design note:
// RealtimeCombatActionScheduler advances the first Sprint 4 RTWP queue without engine dependencies.
// Inputs: action requests, pause/cancel commands, and fixed/variable delta seconds.
// Outputs: deterministic queue entries plus activation/completion events.
// Bible reference: ARCHITECTURE.md RTWP combat lock, Sprint 4 Faz 2 action queue.
namespace EmberCrpg.Simulation.Combat
{
    /// <summary>Pure timing scheduler for melee/block/dodge/cast action queues.</summary>
    public sealed class RealtimeCombatActionScheduler
    {
        public QueuedCombatAction QueueAction(RealtimeCombatState state, ActorId actorId, CombatActionKind kind, ActorId targetActorId)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var timing = CombatActionTimingProfile.For(kind);
            var startAt = FindActorAvailableAt(state, actorId);
            var action = new QueuedCombatAction(
                state.ReserveSequence(),
                actorId,
                kind,
                state.ElapsedSeconds,
                startAt,
                timing.WindupSeconds,
                timing.ActiveSeconds,
                timing.RecoverySeconds,
                targetActorId);
            state.Add(action);
            return action;
        }

        public bool CancelAction(RealtimeCombatState state, int sequence)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            return state.RemoveBySequence(sequence);
        }

        public RealtimeCombatTickResult Tick(RealtimeCombatState state, double deltaSeconds)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (deltaSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Combat tick delta cannot be negative.");

            var result = new RealtimeCombatTickResult();
            if (state.IsPaused || deltaSeconds <= 0d)
                return result;

            var previous = state.ElapsedSeconds;
            state.Advance(deltaSeconds);
            var now = state.ElapsedSeconds;

            foreach (var action in state.Queue)
            {
                if (!action.IsActivated && action.ActivateAtSeconds > previous && action.ActivateAtSeconds <= now)
                {
                    action.MarkActivated();
                    result.Add(new CombatActionEvent(action, CombatActionEventKind.Activated, action.ActivateAtSeconds));
                }

                if (!action.IsCompleted && action.CompleteAtSeconds > previous && action.CompleteAtSeconds <= now)
                {
                    action.MarkCompleted();
                    result.Add(new CombatActionEvent(action, CombatActionEventKind.Completed, action.CompleteAtSeconds));
                }
            }

            return result;
        }

        public CombatDefenseIntent GetActiveDefenseIntent(RealtimeCombatState state, ActorId actorId)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var intent = CombatDefenseIntent.None;
            foreach (var action in state.Queue)
            {
                if (action.ActorId != actorId || !action.IsActiveAt(state.ElapsedSeconds))
                    continue;
                if (action.Kind == CombatActionKind.Dodge)
                    return CombatDefenseIntent.Dodging;
                if (action.Kind == CombatActionKind.Block)
                    intent = CombatDefenseIntent.Blocking;
            }

            return intent;
        }

        private static double FindActorAvailableAt(RealtimeCombatState state, ActorId actorId)
        {
            var availableAt = state.ElapsedSeconds;
            foreach (var action in state.Queue)
            {
                if (action.ActorId == actorId && !action.IsCompleted && action.CompleteAtSeconds > availableAt)
                    availableAt = action.CompleteAtSeconds;
            }
            return availableAt;
        }
    }
}

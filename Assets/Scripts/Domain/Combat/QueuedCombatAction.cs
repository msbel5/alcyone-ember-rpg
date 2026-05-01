using System;
using EmberCrpg.Domain.Core;

// Design note:
// QueuedCombatAction is a pure scheduled RTWP action with windup/active/recovery timing.
// Inputs: actor id, action kind, optional target, and deterministic scheduler timestamps.
// Outputs: action windows that can be tested or adapted by Unity presentation.
// Bible reference: ARCHITECTURE.md RTWP combat lock, Sprint 4 Faz 2 action queue.
namespace EmberCrpg.Domain.Combat
{
    /// <summary>Scheduled combat verb in actor-local timeline order.</summary>
    public sealed class QueuedCombatAction
    {
        public QueuedCombatAction(
            int sequence,
            ActorId actorId,
            CombatActionKind kind,
            double requestedAtSeconds,
            double startAtSeconds,
            double windupSeconds,
            double activeSeconds,
            double recoverySeconds,
            ActorId targetActorId)
        {
            if (sequence <= 0)
                throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "Action sequence must be positive.");
            if (actorId.IsEmpty)
                throw new ArgumentException("Queued combat actions require an actor id.", nameof(actorId));
            if (windupSeconds < 0d || activeSeconds <= 0d || recoverySeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(activeSeconds), "Action timing requires non-negative windup/recovery and positive active time.");

            Sequence = sequence;
            ActorId = actorId;
            Kind = kind;
            RequestedAtSeconds = requestedAtSeconds;
            StartAtSeconds = startAtSeconds;
            WindupSeconds = windupSeconds;
            ActiveSeconds = activeSeconds;
            RecoverySeconds = recoverySeconds;
            TargetActorId = targetActorId;
        }

        public int Sequence { get; }
        public ActorId ActorId { get; }
        public ActorId TargetActorId { get; }
        public CombatActionKind Kind { get; }
        public double RequestedAtSeconds { get; }
        public double StartAtSeconds { get; }
        public double WindupSeconds { get; }
        public double ActiveSeconds { get; }
        public double RecoverySeconds { get; }
        public double ActivateAtSeconds => StartAtSeconds + WindupSeconds;
        public double CompleteAtSeconds => StartAtSeconds + WindupSeconds + ActiveSeconds + RecoverySeconds;
        public bool IsActivated { get; private set; }
        public bool IsCompleted { get; private set; }

        public bool IsActiveAt(double elapsedSeconds)
        {
            return elapsedSeconds >= ActivateAtSeconds && elapsedSeconds < ActivateAtSeconds + ActiveSeconds;
        }

        public void MarkActivated()
        {
            IsActivated = true;
        }

        public void MarkCompleted()
        {
            IsCompleted = true;
        }
    }
}

using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Combat;

// Design note:
// RealtimeCombatState stores the pause flag, deterministic elapsed time, and mutable action queue.
// Inputs: scheduler queue/cancel/tick operations and pause requests.
// Outputs: inspectable simulation state that can be edited while paused.
// Bible reference: ARCHITECTURE.md RTWP combat lock, Sprint 4 Faz 2 SPACE pause support.
namespace EmberCrpg.Simulation.Combat
{
    /// <summary>Mutable pure state for real-time-with-pause combat.</summary>
    public sealed class RealtimeCombatState
    {
        private readonly List<QueuedCombatAction> _queue = new List<QueuedCombatAction>();
        private int _nextSequence = 1;

        public double ElapsedSeconds { get; private set; }
        public bool IsPaused { get; private set; }
        public IReadOnlyList<QueuedCombatAction> Queue => _queue;

        internal int ReserveSequence()
        {
            return _nextSequence++;
        }

        internal void Add(QueuedCombatAction action)
        {
            _queue.Add(action);
            _queue.Sort(CompareActions);
        }

        internal bool RemoveBySequence(int sequence)
        {
            var index = _queue.FindIndex(action => action.Sequence == sequence);
            if (index < 0)
                return false;
            _queue.RemoveAt(index);
            return true;
        }

        internal void Advance(double deltaSeconds)
        {
            if (deltaSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Combat time cannot advance by a negative delta.");
            ElapsedSeconds += deltaSeconds;
        }

        public void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;
        }

        public void TogglePaused()
        {
            IsPaused = !IsPaused;
        }

        private static int CompareActions(QueuedCombatAction left, QueuedCombatAction right)
        {
            var byStart = left.StartAtSeconds.CompareTo(right.StartAtSeconds);
            return byStart != 0 ? byStart : left.Sequence.CompareTo(right.Sequence);
        }
    }
}

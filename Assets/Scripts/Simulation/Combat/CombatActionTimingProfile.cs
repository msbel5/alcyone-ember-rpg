using System;
using EmberCrpg.Domain.Combat;

// Design note:
// CombatActionTimingProfile centralizes first-pass RTWP windup/active/recovery timings.
// Inputs: requested CombatActionKind.
// Outputs: deterministic timing windows for queue scheduling and tests.
// Bible reference: ARCHITECTURE.md RTWP combat lock, Sprint 4 Phase 2 action queue.
namespace EmberCrpg.Simulation.Combat
{
    /// <summary>Timing tuple for a real-time combat action.</summary>
    public readonly struct CombatActionTimingProfile
    {
        public CombatActionTimingProfile(double windupSeconds, double activeSeconds, double recoverySeconds)
        {
            if (windupSeconds < 0d || activeSeconds <= 0d || recoverySeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(activeSeconds), "Timing requires non-negative windup/recovery and positive active time.");

            WindupSeconds = windupSeconds;
            ActiveSeconds = activeSeconds;
            RecoverySeconds = recoverySeconds;
        }

        public double WindupSeconds { get; }
        public double ActiveSeconds { get; }
        public double RecoverySeconds { get; }

        public static CombatActionTimingProfile For(CombatActionKind kind)
        {
            switch (kind)
            {
                case CombatActionKind.MeleeSwing: return new CombatActionTimingProfile(0.25d, 0.12d, 0.35d);
                case CombatActionKind.Block: return new CombatActionTimingProfile(0.05d, 0.50d, 0.10d);
                case CombatActionKind.Dodge: return new CombatActionTimingProfile(0.10d, 0.18d, 0.35d);
                case CombatActionKind.Cast: return new CombatActionTimingProfile(0.50d, 0.10d, 0.55d);
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}

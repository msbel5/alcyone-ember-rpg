using System;

// Design note:
// VitalStat stores one bounded current/max resource pool such as health, fatigue, or mana.
// Inputs: current and max values plus deterministic damage/restore amounts.
// Outputs: validated immutable pool snapshots.
// Bible reference: MASTER_MECHANICS_BIBLE.md §3, PRD FR-01.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Immutable resource pool with bounded current and max values.</summary>
    public readonly struct VitalStat
    {
        public VitalStat(int current, int max)
        {
            if (max <= 0)
                throw new ArgumentOutOfRangeException(nameof(max), max, "Vital pools require a positive max value.");
            if (current < 0 || current > max)
                throw new ArgumentOutOfRangeException(nameof(current), current, "Current value must stay between zero and max.");

            Current = current;
            Max = max;
        }

        public int Current { get; }
        public int Max { get; }
        public bool IsDepleted => Current <= 0;

        public VitalStat Damage(int amount)
        {
            var next = Current - Math.Max(0, amount);
            return new VitalStat(Math.Max(0, next), Max);
        }

        public VitalStat Restore(int amount)
        {
            var next = Current + Math.Max(0, amount);
            return new VitalStat(Math.Min(Max, next), Max);
        }

        public VitalStat Refill()
        {
            return new VitalStat(Max, Max);
        }
    }
}

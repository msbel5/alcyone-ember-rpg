using System;

// Design note:
// EmberStatBlock stores Sprint 1's six deterministic actor stats.
// Inputs: six bounded stat values.
// Outputs: validated immutable stats plus keyed lookup.
// Bible reference: MASTER_MECHANICS_BIBLE.md §1, PRD FR-01.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Immutable six-stat block for a single actor.</summary>
    public readonly struct EmberStatBlock
    {
        public const int MinValue = 1;
        public const int MaxValue = 100;

        public EmberStatBlock(int mig, int agi, int end, int mnd, int ins, int pre)
        {
            Mig = RequireRange(mig, nameof(mig));
            Agi = RequireRange(agi, nameof(agi));
            End = RequireRange(end, nameof(end));
            Mnd = RequireRange(mnd, nameof(mnd));
            Ins = RequireRange(ins, nameof(ins));
            Pre = RequireRange(pre, nameof(pre));
        }

        public int Mig { get; }
        public int Agi { get; }
        public int End { get; }
        public int Mnd { get; }
        public int Ins { get; }
        public int Pre { get; }
        public int Total => Mig + Agi + End + Mnd + Ins + Pre;

        public int Get(EmberAttribute attribute)
        {
            switch (attribute)
            {
                case EmberAttribute.Mig: return Mig;
                case EmberAttribute.Agi: return Agi;
                case EmberAttribute.End: return End;
                case EmberAttribute.Mnd: return Mnd;
                case EmberAttribute.Ins: return Ins;
                case EmberAttribute.Pre: return Pre;
                default: throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null);
            }
        }

        private static int RequireRange(int value, string paramName)
        {
            if (value < MinValue || value > MaxValue)
                throw new ArgumentOutOfRangeException(paramName, value, "Stats must stay inside the deterministic 1-100 slice bounds.");
            return value;
        }
    }
}

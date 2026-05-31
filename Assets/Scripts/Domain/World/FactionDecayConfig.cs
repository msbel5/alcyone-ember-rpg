using System;

namespace EmberCrpg.Domain.World
{
    public readonly struct FactionDecayConfig : IEquatable<FactionDecayConfig>
    {
        public FactionDecayConfig(
            int baseline = 0,
            int ratePerStep = 1,
            int deadBand = 0,
            int daysPerDecayStep = 1)
        {
            if (baseline != 0)
                throw new ArgumentOutOfRangeException(nameof(baseline), "E7-019 v1 supports neutral baseline only.");
            if (ratePerStep < 0)
                throw new ArgumentOutOfRangeException(nameof(ratePerStep));
            if (deadBand < 0)
                throw new ArgumentOutOfRangeException(nameof(deadBand));
            if (daysPerDecayStep < 1)
                throw new ArgumentOutOfRangeException(nameof(daysPerDecayStep));

            Baseline = baseline;
            RatePerStep = ratePerStep;
            DeadBand = deadBand;
            DaysPerDecayStep = daysPerDecayStep;
        }

        public static FactionDecayConfig Default { get; } = new FactionDecayConfig(0, 1, 0, 1);

        public int Baseline { get; }
        public int RatePerStep { get; }
        public int DeadBand { get; }
        public int DaysPerDecayStep { get; }

        public bool Equals(FactionDecayConfig other)
        {
            return Baseline == other.Baseline &&
                   RatePerStep == other.RatePerStep &&
                   DeadBand == other.DeadBand &&
                   DaysPerDecayStep == other.DaysPerDecayStep;
        }

        public override bool Equals(object obj) => obj is FactionDecayConfig other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Baseline, RatePerStep, DeadBand, DaysPerDecayStep);
    }
}

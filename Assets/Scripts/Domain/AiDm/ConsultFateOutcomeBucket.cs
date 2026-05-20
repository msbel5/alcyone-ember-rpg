using System;

namespace EmberCrpg.Domain.AiDm
{
    /// <summary>
    /// Stable string outcome bucket id for the Consult Fate DM mechanism.
    /// Faz 12 Atom 10.
    /// </summary>
    public readonly struct ConsultFateOutcomeBucket : IEquatable<ConsultFateOutcomeBucket>
    {
        private readonly string _code;
        private ConsultFateOutcomeBucket(string code) { _code = code; }

        public static ConsultFateOutcomeBucket Favourable { get; } = new ConsultFateOutcomeBucket("favourable");
        public static ConsultFateOutcomeBucket Neutral { get; } = new ConsultFateOutcomeBucket("neutral");
        public static ConsultFateOutcomeBucket Setback { get; } = new ConsultFateOutcomeBucket("setback");

        public string Code => _code ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(_code);

        // PR#173 bot review fix: the project's deterministic RNG returns 1..100
        // (XorShiftRng.RollPercent does NextInt(100) + 1), so an in-range top-end
        // roll of 100 used to throw and the 0-based thresholds were inconsistent
        // with the rest of the codebase. Accept 1..100 and use inclusive upper bounds
        // that preserve the original 35/35/30 distribution.
        public static ConsultFateOutcomeBucket FromRoll(int roll1To100)
        {
            if (roll1To100 < 1 || roll1To100 > 100)
                throw new ArgumentOutOfRangeException(nameof(roll1To100), "Roll must be 1..100.");
            if (roll1To100 <= 35) return Setback;
            if (roll1To100 <= 70) return Neutral;
            return Favourable;
        }

        public bool Equals(ConsultFateOutcomeBucket other) => Code == other.Code;
        public override bool Equals(object obj) => obj is ConsultFateOutcomeBucket o && Equals(o);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(ConsultFateOutcomeBucket a, ConsultFateOutcomeBucket b) => a.Equals(b);
        public static bool operator !=(ConsultFateOutcomeBucket a, ConsultFateOutcomeBucket b) => !a.Equals(b);
    }
}

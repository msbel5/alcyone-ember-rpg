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

        public static ConsultFateOutcomeBucket FromRoll(int roll0To99)
        {
            if (roll0To99 < 0 || roll0To99 > 99)
                throw new ArgumentOutOfRangeException(nameof(roll0To99), "Roll must be 0..99.");
            if (roll0To99 < 35) return Setback;
            if (roll0To99 < 70) return Neutral;
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

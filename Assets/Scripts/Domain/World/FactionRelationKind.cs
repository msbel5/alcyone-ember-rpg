using System;

namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// Stable-string relation classification between two factions. The code is the
    /// data row key — new tiers ship as data, not enum branches.
    /// Faz 6 Atom 1.
    /// </summary>
    public readonly struct FactionRelationKind : IEquatable<FactionRelationKind>
    {
        private readonly string _code;

        private FactionRelationKind(string code)
        {
            _code = code;
        }

        public static FactionRelationKind Allied { get; } = new FactionRelationKind("allied");
        public static FactionRelationKind Friendly { get; } = new FactionRelationKind("friendly");
        public static FactionRelationKind Neutral { get; } = new FactionRelationKind("neutral");
        public static FactionRelationKind Hostile { get; } = new FactionRelationKind("hostile");
        public static FactionRelationKind War { get; } = new FactionRelationKind("war");

        public string Code => _code ?? Neutral.Code;

        public static FactionRelationKind FromReputation(int reputation)
        {
            if (reputation >= 75) return Allied;
            if (reputation >= 25) return Friendly;
            if (reputation > -25) return Neutral;
            if (reputation > -75) return Hostile;
            return War;
        }

        public bool Equals(FactionRelationKind other) => Code == other.Code;
        public override bool Equals(object obj) => obj is FactionRelationKind other && Equals(other);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(FactionRelationKind a, FactionRelationKind b) => a.Equals(b);
        public static bool operator !=(FactionRelationKind a, FactionRelationKind b) => !a.Equals(b);
    }
}

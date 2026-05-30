using System;

namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// Stable-string relation classification between two factions. The code is the
    /// data row key — new tiers ship as data, not enum branches.
    /// Phase 6 Atom 1.
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
            // Codex audit (sixth pass A-P2 #10): the previous boundaries were
            // asymmetric — `>= 25` Friendly but `> -25` Neutral made rep=25
            // Friendly while rep=-25 fell into Hostile. The Neutral band was
            // 49 integers wide instead of the intended 50. Switch the lower
            // bound to `>= -25` so Neutral covers a symmetric -25..24 range.
            if (reputation >= 75) return Allied;
            if (reputation >= 25) return Friendly;
            if (reputation >= -25) return Neutral;
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

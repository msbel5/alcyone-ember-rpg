using System;

namespace EmberCrpg.Domain.Combat
{
    /// <summary>Stable string identifier for a combat action. Faz 7 Atom 4.</summary>
    public readonly struct CombatActionId : IEquatable<CombatActionId>
    {
        private readonly string _code;
        public CombatActionId(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("CombatActionId code must be non-blank.", nameof(code));
            _code = code.Trim().ToLowerInvariant();
        }
        public static CombatActionId Empty { get; } = default;
        public string Code => _code ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(_code);
        public bool Equals(CombatActionId other) => Code == other.Code;
        public override bool Equals(object obj) => obj is CombatActionId other && Equals(other);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(CombatActionId a, CombatActionId b) => a.Equals(b);
        public static bool operator !=(CombatActionId a, CombatActionId b) => !a.Equals(b);
    }
}

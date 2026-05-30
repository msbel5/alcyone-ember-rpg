using System;

namespace EmberCrpg.Domain.AiDm
{
    /// <summary>
    /// Stable string identifier for an AI/DM tool. Equality is by code only.
    /// Phase 10 Atom 1.
    /// </summary>
    public readonly struct ToolId : IEquatable<ToolId>
    {
        private readonly string _code;

        public ToolId(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("ToolId code must be non-blank.", nameof(code));
            _code = code.Trim();
        }

        public static ToolId Empty { get; } = default;

        public string Code => _code ?? string.Empty;

        public bool IsEmpty => string.IsNullOrEmpty(_code);

        public bool Equals(ToolId other) => Code == other.Code;
        public override bool Equals(object obj) => obj is ToolId other && Equals(other);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(ToolId a, ToolId b) => a.Equals(b);
        public static bool operator !=(ToolId a, ToolId b) => !a.Equals(b);
    }
}

using System;

namespace EmberCrpg.Domain.AiDm
{
    /// <summary>
    /// Stable string classifier for which actor class a tool belongs to.
    /// Per EMBER_VISION_NOTES_MAMI.md section 1.9: NPC, party member, DM
    /// all hold separate but isomorphic tool surfaces.
    /// Phase 10 Atom 2.
    /// </summary>
    public readonly struct ToolSurfaceKind : IEquatable<ToolSurfaceKind>
    {
        private readonly string _code;

        private ToolSurfaceKind(string code)
        {
            _code = code;
        }

        public static ToolSurfaceKind Npc { get; } = new ToolSurfaceKind("npc");
        public static ToolSurfaceKind Party { get; } = new ToolSurfaceKind("party");
        public static ToolSurfaceKind Dm { get; } = new ToolSurfaceKind("dm");

        public static ToolSurfaceKind FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return default;
            var normalized = code.Trim();
            if (normalized == Npc.Code) return Npc;
            if (normalized == Party.Code) return Party;
            if (normalized == Dm.Code) return Dm;
            return new ToolSurfaceKind(normalized);
        }

        public string Code => _code ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(_code);

        public bool Equals(ToolSurfaceKind other) => Code == other.Code;
        public override bool Equals(object obj) => obj is ToolSurfaceKind other && Equals(other);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(ToolSurfaceKind a, ToolSurfaceKind b) => a.Equals(b);
        public static bool operator !=(ToolSurfaceKind a, ToolSurfaceKind b) => !a.Equals(b);
    }
}

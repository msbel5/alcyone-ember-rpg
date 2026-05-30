using System;

namespace EmberCrpg.Domain.Magic
{
    /// <summary>
    /// Stable string classifier for an effect operation. Phase 8 Atom 1.
    /// </summary>
    public readonly struct EffectOperationKind : IEquatable<EffectOperationKind>
    {
        private readonly string _code;
        private EffectOperationKind(string code) { _code = code; }

        public static EffectOperationKind DirectDamage { get; } = new EffectOperationKind("direct_damage");
        public static EffectOperationKind DirectRestore { get; } = new EffectOperationKind("direct_restore");
        public static EffectOperationKind StatusApply { get; } = new EffectOperationKind("status_apply");
        public static EffectOperationKind AreaApply { get; } = new EffectOperationKind("area_apply");
        public static EffectOperationKind TerrainApply { get; } = new EffectOperationKind("terrain_apply");

        public string Code => _code ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(_code);

        public bool Equals(EffectOperationKind other) => Code == other.Code;
        public override bool Equals(object obj) => obj is EffectOperationKind o && Equals(o);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(EffectOperationKind a, EffectOperationKind b) => a.Equals(b);
        public static bool operator !=(EffectOperationKind a, EffectOperationKind b) => !a.Equals(b);
    }
}

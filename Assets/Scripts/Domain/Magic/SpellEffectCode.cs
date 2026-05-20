// Design note:
using System;

// SpellEffectCode names the Sprint 5 starter effect verbs for the legacy spell foundation.
// Inputs: spell definitions composing one or more effect specs.
// Outputs: stable string codes used by data rows and save/load without enum branching.
// Bible reference: MASTER_MECHANICS_BIBLE.md §15 Magic — Effects & Opcodes (Destruction/Restoration subset),
// EMBER_VISION_BIBLE.md §11 reference rule (read references/openmw-master/apps/openmw/mwmechanics/spells.cpp for shape only).
namespace EmberCrpg.Domain.Magic
{
    /// <summary>Stable string code for deterministic legacy spell effect verbs.</summary>
    public readonly struct SpellEffectCode : IEquatable<SpellEffectCode>
    {
        private readonly string _code;

        private SpellEffectCode(string code)
        {
            _code = code;
        }

        public static SpellEffectCode None { get; } = new SpellEffectCode("none");
        public static SpellEffectCode DirectDamage { get; } = new SpellEffectCode("direct_damage");
        public static SpellEffectCode RestoreHealth { get; } = new SpellEffectCode("restore_health");
        public static SpellEffectCode RestoreFatigue { get; } = new SpellEffectCode("restore_fatigue");
        public static SpellEffectCode ShieldBuff { get; } = new SpellEffectCode("shield_buff");
        public static SpellEffectCode RestoreMana { get; } = new SpellEffectCode("restore_mana");
        public static SpellEffectCode DirectMana { get; } = new SpellEffectCode("direct_mana");
        public static SpellEffectCode DirectFatigue { get; } = new SpellEffectCode("direct_fatigue");

        public string Code => _code ?? None.Code;
        public bool IsEmpty => string.IsNullOrEmpty(_code) || Code == None.Code;

        public static SpellEffectCode FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return None;
            var normalized = code.Trim().ToLowerInvariant();
            if (normalized == None.Code) return None;
            if (normalized == DirectDamage.Code) return DirectDamage;
            if (normalized == RestoreHealth.Code) return RestoreHealth;
            if (normalized == RestoreFatigue.Code) return RestoreFatigue;
            if (normalized == ShieldBuff.Code) return ShieldBuff;
            if (normalized == RestoreMana.Code) return RestoreMana;
            if (normalized == DirectMana.Code) return DirectMana;
            if (normalized == DirectFatigue.Code) return DirectFatigue;
            return new SpellEffectCode(normalized);
        }

        public bool Equals(SpellEffectCode other) => Code == other.Code;
        public override bool Equals(object obj) => obj is SpellEffectCode other && Equals(other);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(SpellEffectCode a, SpellEffectCode b) => a.Equals(b);
        public static bool operator !=(SpellEffectCode a, SpellEffectCode b) => !a.Equals(b);
    }
}

using System;

namespace EmberCrpg.Domain.Combat
{
    /// <summary>
    /// Immutable data row for a combat action. Stamina cost, hit-roll formula
    /// key, damage formula key, animation tag. Phase 7 Atom 4.
    /// </summary>
    public sealed class CombatActionDef : IEquatable<CombatActionDef>
    {
        public CombatActionDef(
            CombatActionId id,
            int staminaCost,
            string hitFormulaKey,
            string damageFormulaKey,
            string animationTag)
        {
            if (id.IsEmpty) throw new ArgumentException("CombatActionDef.Id must be non-empty.", nameof(id));
            if (staminaCost < 0) throw new ArgumentOutOfRangeException(nameof(staminaCost), "Stamina cost must be non-negative.");
            if (string.IsNullOrWhiteSpace(hitFormulaKey)) throw new ArgumentException("HitFormulaKey must be non-blank.", nameof(hitFormulaKey));
            if (string.IsNullOrWhiteSpace(damageFormulaKey)) throw new ArgumentException("DamageFormulaKey must be non-blank.", nameof(damageFormulaKey));

            Id = id;
            StaminaCost = staminaCost;
            HitFormulaKey = hitFormulaKey.Trim();
            DamageFormulaKey = damageFormulaKey.Trim();
            AnimationTag = animationTag ?? string.Empty;
        }

        public CombatActionId Id { get; }
        public int StaminaCost { get; }
        public string HitFormulaKey { get; }
        public string DamageFormulaKey { get; }
        public string AnimationTag { get; }

        public bool Equals(CombatActionDef other) => other != null && Id.Equals(other.Id);
        public override bool Equals(object obj) => Equals(obj as CombatActionDef);
        public override int GetHashCode() => Id.GetHashCode();
    }
}

// Design note:
// RealtimeDamageResult records deterministic RTWP hit resolution evidence.
// Inputs: weapon hit event, attacker/defender stats, body hierarchy, AC math, and seeded RNG.
// Outputs: hit/miss, body part, AC, roll, damage, and UI-safe summary text.
// Bible reference: MASTER_MECHANICS_BIBLE.md §8-§10, Sprint 4 Phase 2 damage pipeline.
namespace EmberCrpg.Domain.Combat
{
    /// <summary>Pure result of one real-time weapon hit resolution.</summary>
    public sealed class RealtimeDamageResult
    {
        public string AttackerName;
        public string DefenderName;
        public string WeaponTag;
        public BodyPart BodyPart;
        public CombatDefenseIntent DefenseIntent;
        public bool Hit;
        public int HitChance;
        public int Roll;
        public int ArmorClass;
        public int RawDamage;
        public int MitigatedDamage;
        public int RemainingHealth;
        public string Summary;
    }
}

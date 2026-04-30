// Design note:
// CombatStrikeResult is the deterministic output of one resolved attack.
// Inputs: attacker/defender state plus seeded combat math.
// Outputs: hit roll evidence, damage numbers, body part, and UI-safe summary text.
// Bible reference: MASTER_MECHANICS_BIBLE.md §8-§10, PRD FR-02.
namespace EmberCrpg.Domain.Combat
{
    /// <summary>Pure data record for one attack resolution.</summary>
    public sealed class CombatStrikeResult
    {
        public string AttackerName;
        public string DefenderName;
        public bool Hit;
        public int HitChance;
        public int Roll;
        public BodyPart BodyPart;
        public int RawDamage;
        public int MitigatedDamage;
        public int RemainingHealth;
        public string Summary;
    }
}

using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Simulation.Rng;

// Design note:
// CombatMathService resolves the deterministic Sprint 1 one-hit combat kernel.
// Inputs: attacker/defender records and seeded RNG.
// Outputs: clamped hit chance, weighted body part, armor mitigation, and updated defender health.
// Bible reference: MASTER_MECHANICS_BIBLE.md §8-§10, PRD FR-02.
namespace EmberCrpg.Simulation.Combat
{
    /// <summary>Pure deterministic combat formulas for the tiny vertical slice.</summary>
    public sealed class CombatMathService
    {
        private readonly BodyPartSelector _bodyParts = new BodyPartSelector();

        public int CalculateHitChance(ActorRecord attacker, ActorRecord defender, int attackerAccuracyBonus = 0)
        {
            var attackScore = 45 + attacker.Accuracy + attackerAccuracyBonus + attacker.Stats.Agi / 2 + attacker.Stats.Ins / 3;
            var defenseScore = defender.Dodge + defender.Stats.Agi / 3 + defender.Armor;
            return ClampPercent(attackScore - defenseScore);
        }

        public CombatStrikeResult ResolveAttack(ActorRecord attacker, ActorRecord defender, IDeterministicRng rng, int attackerAccuracyBonus = 0, int attackerDamageBonus = 0)
        {
            var hitChance = CalculateHitChance(attacker, defender, attackerAccuracyBonus);
            var roll = rng.RollPercent();
            var bodyPart = _bodyParts.Select(rng);
            var result = new CombatStrikeResult();
            result.AttackerName = attacker.Name;
            result.DefenderName = defender.Name;
            result.HitChance = hitChance;
            result.Roll = roll;
            result.BodyPart = bodyPart;

            if (roll > hitChance)
            {
                result.Hit = false;
                result.Summary = $"{attacker.Name} misses {defender.Name} ({roll}>{hitChance}).";
                result.RemainingHealth = defender.Vitals.Health.Current;
                return result;
            }

            var rawDamage = Math.Max(1, attacker.BaseDamage + attackerDamageBonus + attacker.Stats.Mig / 5);
            var mitigatedDamage = Math.Max(1, rawDamage - CalculateArmorMitigation(defender, bodyPart));
            defender.ApplyVitals(defender.Vitals.WithHealth(defender.Vitals.Health.Damage(mitigatedDamage)));

            result.Hit = true;
            result.RawDamage = rawDamage;
            result.MitigatedDamage = mitigatedDamage;
            result.RemainingHealth = defender.Vitals.Health.Current;
            result.Summary = $"{attacker.Name} hits {defender.Name}'s {bodyPart} for {mitigatedDamage} ({roll}<={hitChance}).";
            return result;
        }

        public int CalculateArmorMitigation(ActorRecord defender, BodyPart bodyPart)
        {
            return defender.Armor + GetBodyPartBonus(bodyPart);
        }

        private static int GetBodyPartBonus(BodyPart bodyPart)
        {
            switch (bodyPart)
            {
                case BodyPart.Head: return 1;
                case BodyPart.Chest: return 2;
                case BodyPart.Legs: return 1;
                default: return 0;
            }
        }

        private static int ClampPercent(int value)
        {
            return Math.Max(3, Math.Min(97, value));
        }
    }
}

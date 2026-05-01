using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Simulation.Rng;

// Design note:
// RealtimeDamageService resolves collider-origin hits through AC, body-part, stat, and armor math.
// Inputs: WeaponHitEvent, attacker/defender ActorRecord, active defense intent, and deterministic RNG.
// Outputs: RealtimeDamageResult plus defender health mutation only on confirmed hits.
// Bible reference: MASTER_MECHANICS_BIBLE.md §8-§10, Sprint 4 Faz 2 deterministic damage pipeline.
namespace EmberCrpg.Simulation.Combat
{
    /// <summary>Pure deterministic RTWP damage pipeline for weapon/body hit events.</summary>
    public sealed class RealtimeDamageService
    {
        private readonly BodyPartHierarchy _body = new BodyPartHierarchy();

        public RealtimeDamageResult ResolveWeaponHit(
            WeaponHitEvent hitEvent,
            ActorRecord attacker,
            ActorRecord defender,
            CombatDefenseIntent defenseIntent,
            IDeterministicRng rng)
        {
            if (attacker == null)
                throw new ArgumentNullException(nameof(attacker));
            if (defender == null)
                throw new ArgumentNullException(nameof(defender));
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            if (hitEvent.AttackerId != attacker.Id)
                throw new ArgumentException("Weapon hit attacker id must match the attacker record.", nameof(hitEvent));
            if (hitEvent.DefenderId != defender.Id)
                throw new ArgumentException("Weapon hit defender id must match the defender record.", nameof(hitEvent));

            var bodyPart = hitEvent.ColliderBodyPart ?? _body.Select(rng);
            var node = _body.GetNode(bodyPart);
            var armorClass = CalculateArmorClass(defender, node, defenseIntent);
            var hitChance = ClampPercent(50 + attacker.Accuracy + attacker.Stats.Agi / 4 + attacker.Stats.Ins / 5 - armorClass);
            var roll = rng.RollPercent();

            var result = new RealtimeDamageResult();
            result.AttackerName = attacker.Name;
            result.DefenderName = defender.Name;
            result.WeaponTag = hitEvent.WeaponTag;
            result.BodyPart = bodyPart;
            result.DefenseIntent = defenseIntent;
            result.ArmorClass = armorClass;
            result.HitChance = hitChance;
            result.Roll = roll;
            result.RemainingHealth = defender.Vitals.Health.Current;

            if (roll > hitChance)
            {
                result.Hit = false;
                result.Summary = $"{attacker.Name}'s {hitEvent.WeaponTag} fails to beat AC {armorClass} on {defender.Name}'s {bodyPart} ({roll}>{hitChance}).";
                return result;
            }

            var rawDamage = Math.Max(1, attacker.BaseDamage + attacker.Stats.Mig / 5 + Math.Max(0, hitEvent.ImpactBonus));
            var locatedDamage = Math.Max(1, (rawDamage * node.DamageMultiplierPercent + 50) / 100);
            var mitigation = defender.Armor + defender.Stats.End / 20 + GetDefenseMitigation(defenseIntent, defender);
            var mitigatedDamage = Math.Max(1, locatedDamage - mitigation);
            defender.ApplyVitals(defender.Vitals.WithHealth(defender.Vitals.Health.Damage(mitigatedDamage)));

            result.Hit = true;
            result.RawDamage = rawDamage;
            result.MitigatedDamage = mitigatedDamage;
            result.RemainingHealth = defender.Vitals.Health.Current;
            result.Summary = $"{attacker.Name}'s {hitEvent.WeaponTag} hits {defender.Name}'s {bodyPart} for {mitigatedDamage} after AC {armorClass}.";
            return result;
        }

        public int CalculateArmorClass(ActorRecord defender, BodyPartNode node, CombatDefenseIntent defenseIntent)
        {
            if (defender == null)
                throw new ArgumentNullException(nameof(defender));

            return defender.Armor * 4 + defender.Stats.Agi / 5 + node.ArmorClassModifier + GetDefenseArmorClass(defenseIntent, defender);
        }

        private static int GetDefenseArmorClass(CombatDefenseIntent defenseIntent, ActorRecord defender)
        {
            switch (defenseIntent)
            {
                case CombatDefenseIntent.Blocking: return 18 + defender.Stats.End / 10;
                case CombatDefenseIntent.Dodging: return 24 + defender.Stats.Agi / 8;
                default: return 0;
            }
        }

        private static int GetDefenseMitigation(CombatDefenseIntent defenseIntent, ActorRecord defender)
        {
            return defenseIntent == CombatDefenseIntent.Blocking ? 2 + defender.Stats.End / 20 : 0;
        }

        private static int ClampPercent(int value)
        {
            return Math.Max(3, Math.Min(97, value));
        }
    }
}

using System;
using EmberCrpg.Domain.Core;

// Design note:
// WeaponHitEvent abstracts Unity weapon/body colliders into pure combat data.
// Inputs: attacker, defender, weapon label, optional body-part collider, action sequence, impact bonus.
// Outputs: deterministic damage-pipeline input with no engine dependency.
// Bible reference: ARCHITECTURE.md deterministic simulation seam, Sprint 4 Faz 2 hit abstraction.
namespace EmberCrpg.Domain.Combat
{
    /// <summary>Pure representation of a weapon collider intersecting a defender body collider.</summary>
    public readonly struct WeaponHitEvent
    {
        public WeaponHitEvent(
            ActorId attackerId,
            ActorId defenderId,
            string weaponTag,
            BodyPart? colliderBodyPart,
            int actionSequence,
            int impactBonus)
        {
            if (attackerId.IsEmpty)
                throw new ArgumentException("Weapon hits require an attacker id.", nameof(attackerId));
            if (defenderId.IsEmpty)
                throw new ArgumentException("Weapon hits require a defender id.", nameof(defenderId));

            AttackerId = attackerId;
            DefenderId = defenderId;
            WeaponTag = string.IsNullOrWhiteSpace(weaponTag) ? "Unarmed" : weaponTag;
            ColliderBodyPart = colliderBodyPart;
            ActionSequence = actionSequence;
            ImpactBonus = impactBonus;
        }

        public ActorId AttackerId { get; }
        public ActorId DefenderId { get; }
        public string WeaponTag { get; }
        public BodyPart? ColliderBodyPart { get; }
        public int ActionSequence { get; }
        public int ImpactBonus { get; }
    }
}

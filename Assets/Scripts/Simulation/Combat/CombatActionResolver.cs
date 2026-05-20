using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Combat
{
    /// <summary>
    /// Orchestrates a single combat action: stamina check -> hit roll -> damage
    /// roll -> emit CombatResolved. Faz 7 Atom 7.
    /// </summary>
    public sealed class CombatActionResolver
    {
        private readonly CombatHitRollService _hit;
        private readonly CombatDamageService _damage;

        public CombatActionResolver(CombatHitRollService hit, CombatDamageService damage)
        {
            _hit = hit ?? throw new ArgumentNullException(nameof(hit));
            _damage = damage ?? throw new ArgumentNullException(nameof(damage));
        }

        public CombatActionOutcome Resolve(
            CombatActionDef action,
            ActorRecord attacker,
            ActorRecord defender,
            int damageBandWidth,
            IDeterministicRng rng,
            GameTime now,
            SiteId siteId,
            WorldEventLog events)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (siteId.IsEmpty) throw new ArgumentException("SiteId must be non-empty.", nameof(siteId));

            if (attacker.Vitals.Fatigue.Current < action.StaminaCost)
            {
                events.Append(new WorldEvent(
                    now,
                    WorldEventKind.CombatResolved,
                    attacker.Id,
                    siteId,
                    $"combat_resolved action:{action.Id} attacker:{attacker.Id} defender:{defender.Id} rejected:insufficient_stamina"));
                return new CombatActionOutcome(false, 0);
            }

            if (action.StaminaCost > 0)
                attacker.ApplyVitals(attacker.Vitals.WithFatigue(attacker.Vitals.Fatigue.Damage(action.StaminaCost)));

            var hit = _hit.Roll(attacker.Accuracy, defender.Dodge, rng);
            var damage = hit ? _damage.Roll(attacker.BaseDamage, damageBandWidth, defender.Armor, rng) : 0;
            if (damage > 0)
                defender.ApplyVitals(defender.Vitals.WithHealth(defender.Vitals.Health.Damage(damage)));

            events.Append(new WorldEvent(
                now,
                WorldEventKind.CombatResolved,
                attacker.Id,
                siteId,
                $"combat_resolved action:{action.Id} attacker:{attacker.Id} defender:{defender.Id} hit:{hit} damage:{damage}"));

            return new CombatActionOutcome(hit, damage);
        }
    }

    public readonly struct CombatActionOutcome
    {
        public CombatActionOutcome(bool hit, int damage) { Hit = hit; Damage = damage; }
        public bool Hit { get; }
        public int Damage { get; }
    }
}

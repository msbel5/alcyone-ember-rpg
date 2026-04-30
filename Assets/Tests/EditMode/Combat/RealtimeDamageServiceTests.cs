using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Rng;
using NUnit.Framework;

// Design note:
// These tests pin the Sprint 4 deterministic weapon-hit damage pipeline.
// They cover seed determinism, collider body-part mapping, body hierarchy modifiers, and defense AC.
namespace EmberCrpg.Tests.EditMode.Combat
{
    /// <summary>Verifies RTWP hit abstraction and damage math without UnityEngine.</summary>
    public sealed class RealtimeDamageServiceTests
    {
        [Test]
        public void ResolveWeaponHit_WithSameSeed_IsDeterministic()
        {
            var first = ResolveWithSeed(31337u);
            var second = ResolveWithSeed(31337u);

            Assert.That(first.BodyPart, Is.EqualTo(second.BodyPart));
            Assert.That(first.Roll, Is.EqualTo(second.Roll));
            Assert.That(first.Hit, Is.EqualTo(second.Hit));
            Assert.That(first.MitigatedDamage, Is.EqualTo(second.MitigatedDamage));
            Assert.That(first.RemainingHealth, Is.EqualTo(second.RemainingHealth));
        }

        [Test]
        public void ResolveWeaponHit_UsesColliderBodyPartWhenProvided()
        {
            var service = new RealtimeDamageService();
            var attacker = CreateActor(1UL, "Player", 70, 15, 1, 12, new EmberStatBlock(70, 50, 45, 30, 35, 20));
            var defender = CreateActor(2UL, "Enemy", 5, 5, 0, 5, new EmberStatBlock(40, 30, 45, 20, 20, 15));
            var hitEvent = new WeaponHitEvent(attacker.Id, defender.Id, "test blade", BodyPart.Head, 11, 0);

            var result = service.ResolveWeaponHit(hitEvent, attacker, defender, CombatDefenseIntent.None, new FixedRng(1));

            Assert.That(result.BodyPart, Is.EqualTo(BodyPart.Head));
            Assert.That(result.Hit, Is.True);
            Assert.That(result.MitigatedDamage, Is.GreaterThan(0));
        }

        [Test]
        public void BodyPartHierarchy_ChestAndFeetExposeDifferentRiskProfiles()
        {
            var body = new BodyPartHierarchy();
            var chest = body.GetNode(BodyPart.Chest);
            var feet = body.GetNode(BodyPart.Feet);

            Assert.That(chest.Parent.HasValue, Is.False);
            Assert.That(feet.Parent, Is.EqualTo(BodyPart.Legs));
            Assert.That(chest.DamageMultiplierPercent, Is.GreaterThan(feet.DamageMultiplierPercent));
            Assert.That(chest.ArmorClassModifier, Is.GreaterThan(feet.ArmorClassModifier));
        }

        [Test]
        public void ResolveWeaponHit_BlockingRaisesArmorClassAndReducesDamage()
        {
            var attacker = CreateActor(1UL, "Player", 80, 10, 0, 14, new EmberStatBlock(70, 40, 40, 20, 20, 20));
            var openDefender = CreateActor(2UL, "Enemy", 5, 5, 1, 5, new EmberStatBlock(35, 25, 60, 20, 20, 10));
            var blockingDefender = CreateActor(2UL, "Enemy", 5, 5, 1, 5, new EmberStatBlock(35, 25, 60, 20, 20, 10));
            var hitEvent = new WeaponHitEvent(attacker.Id, openDefender.Id, "axe", BodyPart.Chest, 3, 0);
            var service = new RealtimeDamageService();

            var open = service.ResolveWeaponHit(hitEvent, attacker, openDefender, CombatDefenseIntent.None, new FixedRng(1));
            var blocked = service.ResolveWeaponHit(hitEvent, attacker, blockingDefender, CombatDefenseIntent.Blocking, new FixedRng(1));

            Assert.That(blocked.ArmorClass, Is.GreaterThan(open.ArmorClass));
            Assert.That(blocked.MitigatedDamage, Is.LessThan(open.MitigatedDamage));
        }

        private static RealtimeDamageResult ResolveWithSeed(uint seed)
        {
            var service = new RealtimeDamageService();
            var attacker = CreateActor(1UL, "Player", 65, 15, 1, 10, new EmberStatBlock(60, 55, 45, 35, 40, 20));
            var defender = CreateActor(2UL, "Enemy", 15, 8, 2, 6, new EmberStatBlock(45, 35, 50, 25, 25, 15));
            var hitEvent = new WeaponHitEvent(attacker.Id, defender.Id, "iron dagger", null, 7, 1);
            return service.ResolveWeaponHit(hitEvent, attacker, defender, CombatDefenseIntent.None, new XorShiftRng(seed));
        }

        private static ActorRecord CreateActor(ulong id, string name, int accuracy, int dodge, int armor, int baseDamage, EmberStatBlock stats)
        {
            return new ActorRecord(
                new ActorId(id),
                name,
                ActorRole.Enemy,
                stats,
                new ActorVitals(new VitalStat(40, 40), new VitalStat(20, 20), new VitalStat(12, 12)),
                new GridPosition(1, 1),
                accuracy,
                dodge,
                armor,
                baseDamage);
        }

        private sealed class FixedRng : IDeterministicRng
        {
            private readonly int _roll;

            public FixedRng(int roll) { _roll = roll; }
            public int NextInt(int exclusiveMax) { return 0; }
            public int RollPercent() { return _roll; }
        }
    }
}

using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Rng;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Combat
{
    public sealed class CombatPrimitivesTests
    {
        // ----- CombatActionId -----
        [Test]
        public void CombatActionId_RejectsBlank_NormalizesCase()
        {
            Assert.Throws<System.ArgumentException>(() => new CombatActionId(""));
            Assert.That(new CombatActionId("  Slash  ").Code, Is.EqualTo("slash"));
            Assert.That(new CombatActionId("Slash"), Is.EqualTo(new CombatActionId("slash")));
        }

        // ----- CombatActionDef -----
        [Test]
        public void CombatActionDef_HappyPath()
        {
            var def = new CombatActionDef(new CombatActionId("slash"), 2, "default_hit", "default_damage", "swing_a");
            Assert.That(def.StaminaCost, Is.EqualTo(2));
            Assert.That(def.HitFormulaKey, Is.EqualTo("default_hit"));
            Assert.That(def.DamageFormulaKey, Is.EqualTo("default_damage"));
            Assert.That(def.AnimationTag, Is.EqualTo("swing_a"));
        }

        [Test]
        public void CombatActionDef_RejectsInvalidInputs()
        {
            var id = new CombatActionId("slash");
            Assert.Throws<System.ArgumentException>(() => new CombatActionDef(default, 1, "h", "d", "a"));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new CombatActionDef(id, -1, "h", "d", "a"));
            Assert.Throws<System.ArgumentException>(() => new CombatActionDef(id, 1, "", "d", "a"));
            Assert.Throws<System.ArgumentException>(() => new CombatActionDef(id, 1, "h", "", "a"));
        }

        // ----- CombatHitRollService -----
        [Test]
        public void Hit_AccuracyMinusDodgeAbove100_AlwaysHits()
        {
            var hit = new CombatHitRollService().Roll(150, 10, new XorShiftRng(1));
            Assert.That(hit, Is.True);
        }

        [Test]
        public void Hit_AccuracyMinusDodgeBelowZero_NeverHits()
        {
            var hit = new CombatHitRollService().Roll(5, 50, new XorShiftRng(1));
            Assert.That(hit, Is.False);
        }

        [Test]
        public void Hit_SameSeed_SameResult()
        {
            var a = new CombatHitRollService().Roll(60, 10, new XorShiftRng(42));
            var b = new CombatHitRollService().Roll(60, 10, new XorShiftRng(42));
            Assert.That(a, Is.EqualTo(b));
        }

        // ----- CombatDamageService -----
        [Test]
        public void Damage_NegativeAfterArmor_ClampsAtZero()
        {
            var dmg = new CombatDamageService().Roll(2, 0, 10, new XorShiftRng(1));
            Assert.That(dmg, Is.EqualTo(0));
        }

        [Test]
        public void Damage_BaseMinusArmor_NoBand()
        {
            var dmg = new CombatDamageService().Roll(10, 0, 3, null);
            Assert.That(dmg, Is.EqualTo(7));
        }

        // ----- CombatActionResolver -----
        [Test]
        public void Resolver_HitTrue_EmitsCombatResolvedEvent()
        {
            var attacker = new ActorRecord(
                new ActorId(1UL), "Att", ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(0, 0),
                accuracy: 150, dodge: 0, armor: 0, baseDamage: 5);
            var defender = new ActorRecord(
                new ActorId(2UL), "Def", ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(1, 0),
                accuracy: 10, dodge: 0, armor: 1, baseDamage: 1);
            var def = new CombatActionDef(new CombatActionId("slash"), 0, "h", "d", "swing");
            var events = new WorldEventLog();

            var outcome = new CombatActionResolver(new CombatHitRollService(), new CombatDamageService())
                .Resolve(def, attacker, defender, 0, new XorShiftRng(1), default, new SiteId(1UL), events);

            Assert.That(outcome.Hit, Is.True);
            Assert.That(outcome.Damage, Is.EqualTo(4));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.Events[0].Kind, Is.EqualTo(WorldEventKind.CombatResolved));
        }
    }
}

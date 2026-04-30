using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Rng;
using NUnit.Framework;

// Design note:
// These tests pin deterministic combat formulas without Unity presentation.
// They cover hit-chance clamping and armor mitigation on resolved strikes.
namespace EmberCrpg.Tests.EditMode.Combat
{
    /// <summary>Verifies the Sprint 1 combat math kernel.</summary>
    public sealed class CombatMathServiceTests
    {
        [Test]
        public void CalculateHitChance_ClampsToValidPercentRange()
        {
            var service = new CombatMathService();
            var attacker = CreateActor(80, 30, 0, 8);
            var defender = CreateActor(10, 95, 12, 2);
            var chance = service.CalculateHitChance(attacker, defender);
            Assert.That(chance, Is.InRange(3, 97));
        }

        [Test]
        public void ResolveAttack_HitAppliesArmorMitigatedDamage()
        {
            var service = new CombatMathService();
            var attacker = CreateActor(70, 15, 0, 10);
            var defender = CreateActor(10, 0, 3, 1);
            var result = service.ResolveAttack(attacker, defender, new FixedRng(1, 8));
            Assert.That(result.Hit && result.MitigatedDamage < result.RawDamage && defender.Vitals.Health.Current == 24 - result.MitigatedDamage, Is.True);
        }

        private static ActorRecord CreateActor(int accuracy, int dodge, int armor, int baseDamage)
        {
            return new ActorRecord(
                new ActorId((ulong)(accuracy + dodge + armor + baseDamage + 1)),
                "Actor",
                ActorRole.Enemy,
                new EmberStatBlock(60, 50, 40, 30, 20, 10),
                new ActorVitals(new VitalStat(24, 24), new VitalStat(18, 18), new VitalStat(12, 12)),
                new GridPosition(1, 1),
                accuracy,
                dodge,
                armor,
                baseDamage);
        }

        private sealed class FixedRng : IDeterministicRng
        {
            private readonly int[] _values;
            private int _index;

            public FixedRng(params int[] values) { _values = values; }
            public int NextInt(int exclusiveMax) { return _values[_index++] % exclusiveMax; }
            public int RollPercent() { return _values[_index++]; }
        }
    }
}

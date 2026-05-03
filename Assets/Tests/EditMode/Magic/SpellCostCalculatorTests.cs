using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the Sprint 5 deterministic spell cost estimator shape: sum per-effect components,
// then apply target multiplier with integer rounding. This is not success chance, cooldown, or resistance.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic spell mana cost estimation.</summary>
    public sealed class SpellCostCalculatorTests
    {
        [Test]
        public void TargetMultipliers_OrderFromSelfTouchToRangedArea()
        {
            var calculator = new SpellCostCalculator();

            Assert.That(calculator.ApplyTargetMultiplier(10, SpellTargetKind.CasterSelf), Is.EqualTo(10));
            Assert.That(calculator.ApplyTargetMultiplier(10, SpellTargetKind.Touch), Is.EqualTo(10));
            Assert.That(calculator.ApplyTargetMultiplier(10, SpellTargetKind.SingleTarget), Is.EqualTo(15));
            Assert.That(calculator.ApplyTargetMultiplier(10, SpellTargetKind.AreaAroundCaster), Is.EqualTo(20));
            Assert.That(calculator.ApplyTargetMultiplier(10, SpellTargetKind.AreaAtRange), Is.EqualTo(25));
        }

        [Test]
        public void EstimateTotalManaCost_DurationIncreasesCostWithDeterministicRounding()
        {
            var instant = CreateSpell(
                SpellTargetKind.SingleTarget,
                new[] { new SpellEffectSpec(SpellEffectKind.DirectDamage, 5, 0) });
            var timed = CreateSpell(
                SpellTargetKind.SingleTarget,
                new[] { new SpellEffectSpec(SpellEffectKind.DirectDamage, 5, 25) });
            var calculator = new SpellCostCalculator();

            Assert.That(calculator.EstimateTotalManaCost(instant), Is.EqualTo(8));
            Assert.That(calculator.EstimateTotalManaCost(timed), Is.EqualTo(12));
            Assert.That(calculator.EstimateTotalManaCost(timed), Is.GreaterThan(calculator.EstimateTotalManaCost(instant)));
        }

        [Test]
        public void EstimateTotalManaCost_MultipleEffectsSumBeforeTargetMultiplier()
        {
            var spell = CreateSpell(
                SpellTargetKind.AreaAtRange,
                new[]
                {
                    new SpellEffectSpec(SpellEffectKind.DirectDamage, 4, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreFatigue, 2, 20),
                });
            var calculator = new SpellCostCalculator();

            Assert.That(calculator.EstimateTotalManaCost(spell), Is.EqualTo(20));
        }

        [Test]
        public void EstimateTotalManaCost_CatalogManaCostsRemainAtOrAboveEstimator()
        {
            var calculator = new SpellCostCalculator();

            for (var i = 0; i < SliceSpellCatalog.All.Count; i++)
            {
                var spell = SliceSpellCatalog.All[i];
                Assert.That(spell.ManaCost, Is.GreaterThanOrEqualTo(calculator.EstimateTotalManaCost(spell)), spell.TemplateId);
            }
        }

        private static SpellDefinition CreateSpell(SpellTargetKind targetKind, SpellEffectSpec[] effects)
        {
            return new SpellDefinition("cost_test", "Cost Test", MagicSchool.Destruction, targetKind, 1, effects);
        }
    }
}

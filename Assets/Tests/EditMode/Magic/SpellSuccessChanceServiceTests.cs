using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the Sprint 5 deterministic spell success probability seam before spell RNG is introduced.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic spell success chance calculations and breakdown fields.</summary>
    public sealed class SpellSuccessChanceServiceTests
    {
        [Test]
        public void Calculate_NullCaster_FailsWithStableError()
        {
            var service = new SpellSuccessChanceService();

            var result = service.Calculate(null, SliceSpellCatalog.CreateFlameBolt());

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellSuccessChanceError.InvalidCaster));
            Assert.That(result.ChancePercent, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_NullSpell_FailsWithStableError()
        {
            var service = new SpellSuccessChanceService();

            var result = service.Calculate(CreateActor("Acolyte", 40, 60), null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellSuccessChanceError.InvalidSpell));
            Assert.That(result.ChancePercent, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_IncapacitatedCaster_FailsWithStableError()
        {
            var service = new SpellSuccessChanceService();
            var caster = CreateActor("Collapsed Mage", 40, 60, health: 0);

            var result = service.Calculate(caster, SliceSpellCatalog.CreateFlameBolt());

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellSuccessChanceError.InvalidCaster));
            Assert.That(result.ChancePercent, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_InvalidSchool_FailsWithStableError()
        {
            var service = new SpellSuccessChanceService();
            var invalidSchoolSpell = new SpellDefinition(
                "bad_school",
                "Bad School",
                (MagicSchool)999,
                SpellTargetKind.SingleTarget,
                5,
                8,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 3, 0) });

            var result = service.Calculate(CreateActor("Acolyte", 40, 60), invalidSchoolSpell);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellSuccessChanceError.InvalidSpell));
            Assert.That(result.ChancePercent, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_InvalidTargetKind_FailsWithStableError()
        {
            var service = new SpellSuccessChanceService();
            var invalidTargetSpell = new SpellDefinition(
                "bad_target",
                "Bad Target",
                MagicSchool.Destruction,
                (SpellTargetKind)999,
                5,
                8,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 3, 0) });

            var result = service.Calculate(CreateActor("Acolyte", 40, 60), invalidTargetSpell);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellSuccessChanceError.InvalidSpell));
            Assert.That(result.ChancePercent, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_DestructionSpell_UsesMindAsPrimaryAttribute()
        {
            var service = new SpellSuccessChanceService();
            var mindFocusedCaster = CreateActor("Pyromancer", mind: 80, insight: 20);
            var insightFocusedCaster = CreateActor("Mystic", mind: 20, insight: 80);

            var mindResult = service.Calculate(mindFocusedCaster, SliceSpellCatalog.CreateFlameBolt());
            var insightResult = service.Calculate(insightFocusedCaster, SliceSpellCatalog.CreateFlameBolt());

            Assert.That(mindResult.Success, Is.True);
            Assert.That(mindResult.PrimaryAttributeBonus, Is.EqualTo(40));
            Assert.That(mindResult.SecondaryAttributeBonus, Is.EqualTo(5));
            Assert.That(mindResult.ChancePercent, Is.GreaterThan(insightResult.ChancePercent));
        }

        [Test]
        public void Calculate_RestorationSpell_UsesInsightAsPrimaryAttribute()
        {
            var service = new SpellSuccessChanceService();
            var insightFocusedCaster = CreateActor("Mender", mind: 20, insight: 80);
            var mindFocusedCaster = CreateActor("Scholar", mind: 80, insight: 20);

            var insightResult = service.Calculate(insightFocusedCaster, SliceSpellCatalog.CreateMendingTouch());
            var mindResult = service.Calculate(mindFocusedCaster, SliceSpellCatalog.CreateMendingTouch());

            Assert.That(insightResult.Success, Is.True);
            Assert.That(insightResult.PrimaryAttributeBonus, Is.EqualTo(40));
            Assert.That(insightResult.SecondaryAttributeBonus, Is.EqualTo(5));
            Assert.That(insightResult.ChancePercent, Is.GreaterThan(mindResult.ChancePercent));
        }

        [Test]
        public void Calculate_SingleTargetRangeSpell_IncludesExpectedPenaltyBreakdown()
        {
            var service = new SpellSuccessChanceService();
            var caster = CreateActor("Acolyte", mind: 60, insight: 44);

            var result = service.Calculate(caster, SliceSpellCatalog.CreateFlameBolt());

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellSuccessChanceError.None));
            Assert.That(result.BaseChance, Is.EqualTo(40));
            Assert.That(result.PrimaryAttributeBonus, Is.EqualTo(30));
            Assert.That(result.SecondaryAttributeBonus, Is.EqualTo(11));
            Assert.That(result.ManaCostPenalty, Is.EqualTo(6));
            Assert.That(result.EffectComplexityPenalty, Is.EqualTo(3));
            Assert.That(result.TargetPenalty, Is.EqualTo(7));
            Assert.That(result.RangePenalty, Is.EqualTo(4));
            Assert.That(result.ChancePercent, Is.EqualTo(61));
        }

        [Test]
        public void Calculate_NonSingleTargetSpell_DoesNotPayRangePenalty()
        {
            var service = new SpellSuccessChanceService();
            var caster = CreateActor("Arcanist", mind: 70, insight: 70);
            var areaSpell = new SpellDefinition(
                "storm_ring",
                "Storm Ring",
                MagicSchool.Destruction,
                SpellTargetKind.AreaAtRange,
                18,
                6,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 8, 0) });
            var touchSpell = SliceSpellCatalog.CreateMendingTouch();

            var areaResult = service.Calculate(caster, areaSpell);
            var touchResult = service.Calculate(caster, touchSpell);

            Assert.That(areaResult.Success, Is.True);
            Assert.That(areaResult.TargetPenalty, Is.EqualTo(14));
            Assert.That(areaResult.RangePenalty, Is.EqualTo(0));
            Assert.That(areaResult.ChancePercent, Is.LessThan(touchResult.ChancePercent));
        }

        [Test]
        public void Calculate_ResultClampsToMinimumAndMaximumBounds()
        {
            var service = new SpellSuccessChanceService();
            var fragileCaster = CreateActor("Novice", mind: 5, insight: 5);
            var legendaryCaster = CreateActor("Archsage", mind: 100, insight: 100);
            var punishingSpell = new SpellDefinition(
                "meteor_call",
                "Meteor Call",
                MagicSchool.Conjuration,
                SpellTargetKind.AreaAtRange,
                90,
                20,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 20, 0),
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 20, 0),
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 20, 0),
                });
            var gentleSpell = new SpellDefinition(
                "inner_spark",
                "Inner Spark",
                MagicSchool.Alteration,
                SpellTargetKind.CasterSelf,
                0,
                new[] { new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 1, 0) });

            var minimum = service.Calculate(fragileCaster, punishingSpell);
            var maximum = service.Calculate(legendaryCaster, gentleSpell);

            Assert.That(minimum.ChancePercent, Is.EqualTo(5));
            Assert.That(maximum.ChancePercent, Is.EqualTo(95));
        }

        private static ActorRecord CreateActor(string name, int mind, int insight, int health = 16)
        {
            return new ActorRecord(
                new ActorId(999),
                name,
                ActorRole.Player,
                new EmberStatBlock(20, 20, 20, mind, insight, 20),
                new ActorVitals(new VitalStat(health, 16), new VitalStat(12, 12), new VitalStat(20, 20)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 6,
                armor: 1,
                baseDamage: 3);
        }
    }
}

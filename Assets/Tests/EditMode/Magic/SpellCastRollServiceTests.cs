using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using EmberCrpg.Simulation.Rng;
using NUnit.Framework;

// Design note:
// Pins the Sprint 5 deterministic Tier 3 spell-cast roll seam. SpellCastRollService must combine the
// SpellSuccessChanceService threshold with a seeded RNG and expose roll/threshold/breakdown without
// mutating any actor or service state.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic spell-cast rolls and their forwarded chance breakdown.</summary>
    public sealed class SpellCastRollServiceTests
    {
        [Test]
        public void Roll_NullCaster_FailsWithStableError()
        {
            var service = new SpellCastRollService();

            var result = service.Roll(null, SliceSpellCatalog.CreateFlameBolt(), new XorShiftRng(1u));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastRollError.InvalidCaster));
            Assert.That(result.Roll, Is.EqualTo(0));
            Assert.That(result.Threshold, Is.EqualTo(0));
            Assert.That(result.Chance, Is.Null);
        }

        [Test]
        public void Roll_NullSpell_FailsWithStableError()
        {
            var service = new SpellCastRollService();

            var result = service.Roll(CreateActor("Acolyte", 40, 60), null, new XorShiftRng(1u));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastRollError.InvalidSpell));
            Assert.That(result.Roll, Is.EqualTo(0));
            Assert.That(result.Threshold, Is.EqualTo(0));
        }

        [Test]
        public void Roll_NullRng_FailsWithStableError()
        {
            var service = new SpellCastRollService();

            var result = service.Roll(CreateActor("Acolyte", 40, 60), SliceSpellCatalog.CreateFlameBolt(), null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastRollError.InvalidRng));
            Assert.That(result.Roll, Is.EqualTo(0));
            Assert.That(result.Threshold, Is.EqualTo(0));
        }

        [Test]
        public void Roll_IncapacitatedCaster_PropagatesUpstreamRefusal()
        {
            var service = new SpellCastRollService();
            var caster = CreateActor("Collapsed Mage", 40, 60, health: 0);

            var result = service.Roll(caster, SliceSpellCatalog.CreateFlameBolt(), new XorShiftRng(1u));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastRollError.InvalidCaster));
            Assert.That(result.Chance, Is.Not.Null);
            Assert.That(result.Chance.Success, Is.False);
            Assert.That(result.Chance.Error, Is.EqualTo(SpellSuccessChanceError.InvalidCaster));
            Assert.That(result.Roll, Is.EqualTo(0));
            Assert.That(result.Threshold, Is.EqualTo(0));
        }

        [Test]
        public void Roll_InvalidSpellSchool_PropagatesAsChanceCalculationFailed()
        {
            var service = new SpellCastRollService();
            var caster = CreateActor("Acolyte", 40, 60);
            var invalidSchoolSpell = new SpellDefinition(
                "bad_school",
                "Bad School",
                (MagicSchool)999,
                SpellTargetKind.SingleTarget,
                5,
                8,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 3, 0) });

            var result = service.Roll(caster, invalidSchoolSpell, new XorShiftRng(1u));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastRollError.InvalidSpell));
            Assert.That(result.Chance, Is.Not.Null);
            Assert.That(result.Chance.Error, Is.EqualTo(SpellSuccessChanceError.InvalidSpell));
        }

        [Test]
        public void Roll_HealthyCaster_ForwardsChanceBreakdownAsThreshold()
        {
            var service = new SpellCastRollService();
            var caster = CreateActor("Acolyte", mind: 60, insight: 44);
            var spell = SliceSpellCatalog.CreateFlameBolt();
            var expectedChance = new SpellSuccessChanceService().Calculate(caster, spell);

            var result = service.Roll(caster, spell, new XorShiftRng(1u));

            Assert.That(result.Chance, Is.Not.Null);
            Assert.That(result.Chance.Success, Is.True);
            Assert.That(result.Threshold, Is.EqualTo(expectedChance.ChancePercent));
            Assert.That(result.Chance.BaseChance, Is.EqualTo(expectedChance.BaseChance));
            Assert.That(result.Chance.PrimaryAttributeBonus, Is.EqualTo(expectedChance.PrimaryAttributeBonus));
            Assert.That(result.Chance.SecondaryAttributeBonus, Is.EqualTo(expectedChance.SecondaryAttributeBonus));
            Assert.That(result.Chance.ManaCostPenalty, Is.EqualTo(expectedChance.ManaCostPenalty));
            Assert.That(result.Chance.EffectComplexityPenalty, Is.EqualTo(expectedChance.EffectComplexityPenalty));
            Assert.That(result.Chance.TargetPenalty, Is.EqualTo(expectedChance.TargetPenalty));
            Assert.That(result.Chance.RangePenalty, Is.EqualTo(expectedChance.RangePenalty));
        }

        [Test]
        public void Roll_RollWithinThreshold_ReportsSuccess()
        {
            var service = new SpellCastRollService();
            var legendaryCaster = CreateActor("Archsage", mind: 100, insight: 100);
            var gentleSpell = new SpellDefinition(
                "inner_spark",
                "Inner Spark",
                MagicSchool.Alteration,
                SpellTargetKind.CasterSelf,
                0,
                new[] { new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 1, 0) });

            var result = service.Roll(legendaryCaster, gentleSpell, new XorShiftRng(1u));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellCastRollError.None));
            Assert.That(result.Threshold, Is.EqualTo(95));
            Assert.That(result.Roll, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.Roll, Is.LessThanOrEqualTo(result.Threshold));
        }

        [Test]
        public void Roll_RollAboveThreshold_ReportsMiss()
        {
            var service = new SpellCastRollService();
            var fragileCaster = CreateActor("Novice", mind: 5, insight: 5);
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

            // At threshold 5, almost any seeded percent roll fails. Pin a known seed.
            var result = service.Roll(fragileCaster, punishingSpell, new XorShiftRng(1u));

            Assert.That(result.Threshold, Is.EqualTo(5));
            Assert.That(result.Roll, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.Roll, Is.LessThanOrEqualTo(100));
            Assert.That(result.Success, Is.EqualTo(result.Roll <= result.Threshold));
            Assert.That(result.Error, Is.EqualTo(SpellCastRollError.None));
        }

        [Test]
        public void Roll_SameSeed_ProducesSameRollAndThreshold()
        {
            var service = new SpellCastRollService();
            var caster = CreateActor("Acolyte", mind: 60, insight: 44);
            var spell = SliceSpellCatalog.CreateFlameBolt();

            var first = service.Roll(caster, spell, new XorShiftRng(123u));
            var second = service.Roll(caster, spell, new XorShiftRng(123u));

            Assert.That(first.Threshold, Is.EqualTo(second.Threshold));
            Assert.That(first.Roll, Is.EqualTo(second.Roll));
            Assert.That(first.Success, Is.EqualTo(second.Success));
        }

        [Test]
        public void Roll_DifferentSeeds_CanProduceDifferentRolls()
        {
            var service = new SpellCastRollService();
            var caster = CreateActor("Acolyte", mind: 60, insight: 44);
            var spell = SliceSpellCatalog.CreateFlameBolt();

            var seedA = service.Roll(caster, spell, new XorShiftRng(1u));
            var seedB = service.Roll(caster, spell, new XorShiftRng(2u));

            Assert.That(seedA.Threshold, Is.EqualTo(seedB.Threshold));
            Assert.That(seedA.Roll, Is.Not.EqualTo(seedB.Roll));
        }

        [Test]
        public void Roll_DoesNotMutateCasterMana()
        {
            var service = new SpellCastRollService();
            var caster = CreateActor("Acolyte", mind: 60, insight: 44);
            var manaBefore = caster.Vitals.Mana.Current;

            var result = service.Roll(caster, SliceSpellCatalog.CreateFlameBolt(), new XorShiftRng(1u));

            Assert.That(result.Chance.Success, Is.True);
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(manaBefore));
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

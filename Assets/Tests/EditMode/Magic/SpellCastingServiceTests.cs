using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the Sprint 5 casting gate: spell lookup, learned-spell validation, mana spend, and refusal paths.
// These tests intentionally stop before damage/healing resolution, which belongs to later Sprint 5 phases.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic spell casting validation and mana spend.</summary>
    public sealed class SpellCastingServiceTests
    {
        [Test]
        public void TryCast_KnownSpellWithEnoughMana_SpendsManaAndReturnsSuccess()
        {
            var caster = CreateCaster(mana: 20, health: 16);
            var service = new SpellCastingService();

            var result = service.TryCast(
                caster,
                SliceSpellCatalog.FlameBoltTemplateId,
                new[] { SliceSpellCatalog.FlameBoltTemplateId });

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellCastError.None));
            Assert.That(result.Spell.TemplateId, Is.EqualTo(SliceSpellCatalog.FlameBoltTemplateId));
            Assert.That(result.ManaSpent, Is.EqualTo(12));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(8));
        }

        [Test]
        public void TryCast_InsufficientMana_DoesNotSpendMana()
        {
            var caster = CreateCaster(mana: 5, health: 16);
            var service = new SpellCastingService();

            var result = service.TryCast(
                caster,
                SliceSpellCatalog.FlameBoltTemplateId,
                new[] { SliceSpellCatalog.FlameBoltTemplateId });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastError.InsufficientMana));
            Assert.That(result.ManaSpent, Is.EqualTo(0));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(5));
        }

        [Test]
        public void TryCast_UnlearnedSpell_DoesNotSpendMana()
        {
            var caster = CreateCaster(mana: 20, health: 16);
            var service = new SpellCastingService();

            var result = service.TryCast(
                caster,
                SliceSpellCatalog.FlameBoltTemplateId,
                new[] { SliceSpellCatalog.MendingTouchTemplateId });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastError.SpellNotKnown));
            Assert.That(result.Spell.TemplateId, Is.EqualTo(SliceSpellCatalog.FlameBoltTemplateId));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
        }

        [Test]
        public void TryCast_NullCaster_IsRejected()
        {
            var service = new SpellCastingService();

            var result = service.TryCast(
                null,
                SliceSpellCatalog.FlameBoltTemplateId,
                new[] { SliceSpellCatalog.FlameBoltTemplateId });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastError.InvalidCaster));
            Assert.That(result.Spell, Is.Null);
        }

        [Test]
        public void TryCast_BlankSpellId_ReturnsSpellNotFound()
        {
            var caster = CreateCaster(mana: 20, health: 16);
            var service = new SpellCastingService();

            var result = service.TryCast(caster, " ", new[] { SliceSpellCatalog.FlameBoltTemplateId });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastError.SpellNotFound));
            Assert.That(result.Spell, Is.Null);
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
        }

        [Test]
        public void TryCast_NullKnownSpellSet_ReturnsSpellNotKnownWithoutSpendingMana()
        {
            var caster = CreateCaster(mana: 20, health: 16);
            var service = new SpellCastingService();

            var result = service.TryCast(caster, SliceSpellCatalog.FlameBoltTemplateId, null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastError.SpellNotKnown));
            Assert.That(result.Spell.TemplateId, Is.EqualTo(SliceSpellCatalog.FlameBoltTemplateId));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
        }

        [Test]
        public void TryCast_UnknownSpell_ReturnsSpellNotFound()
        {
            var caster = CreateCaster(mana: 20, health: 16);
            var service = new SpellCastingService();

            var result = service.TryCast(caster, "missing_spell", new[] { "missing_spell" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastError.SpellNotFound));
            Assert.That(result.Spell, Is.Null);
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
        }

        [Test]
        public void TryCast_IncapacitatedCaster_IsRejected()
        {
            var caster = CreateCaster(mana: 20, health: 0);
            var service = new SpellCastingService();

            var result = service.TryCast(
                caster,
                SliceSpellCatalog.FlameBoltTemplateId,
                new[] { SliceSpellCatalog.FlameBoltTemplateId });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastError.InvalidCaster));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
        }

        private static ActorRecord CreateCaster(int mana, int health)
        {
            return new ActorRecord(
                new ActorId(501),
                "Acolyte",
                ActorRole.Player,
                new EmberStatBlock(10, 11, 12, 14, 9, 8),
                new ActorVitals(new VitalStat(health, 16), new VitalStat(12, 12), new VitalStat(mana, 20)),
                new GridPosition(1, 1),
                accuracy: 11,
                dodge: 6,
                armor: 1,
                baseDamage: 3);
        }
    }
}

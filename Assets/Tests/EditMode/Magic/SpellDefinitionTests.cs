using System;
using EmberCrpg.Domain.Magic;
using NUnit.Framework;

// Design note:
// Pins SpellDefinition / SpellEffectSpec validation so domain contracts stay honest
// without depending on Simulation services or Unity types.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies Sprint 5 magic domain contracts validate inputs.</summary>
    public sealed class SpellDefinitionTests
    {
        [Test]
        public void EffectSpec_RejectsNoneKind()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellEffectSpec(SpellEffectKind.None, 1, 0));
        }

        [Test]
        public void EffectSpec_RejectsNegativeMagnitudeOrDuration()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellEffectSpec(SpellEffectKind.DirectDamage, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellEffectSpec(SpellEffectKind.DirectDamage, 1, -1));
        }

        [Test]
        public void Definition_RejectsEmptyTemplateIdOrDisplayName()
        {
            var effect = new[] { new SpellEffectSpec(SpellEffectKind.DirectDamage, 1, 0) };
            Assert.Throws<ArgumentException>(() => new SpellDefinition("", "Name", MagicSchool.Destruction, 1, effect));
            Assert.Throws<ArgumentException>(() => new SpellDefinition("id", " ", MagicSchool.Destruction, 1, effect));
        }

        [Test]
        public void Definition_RejectsNoneSchoolTargetKindNegativeManaCostNegativeRangeAndNegativeCooldown()
        {
            var effect = new[] { new SpellEffectSpec(SpellEffectKind.DirectDamage, 1, 0) };
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellDefinition("id", "Name", MagicSchool.None, 1, effect));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellDefinition("id", "Name", MagicSchool.Destruction, SpellTargetKind.None, 1, effect));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellDefinition("id", "Name", MagicSchool.Destruction, -1, effect));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellDefinition("id", "Name", MagicSchool.Destruction, SpellTargetKind.SingleTarget, 1, -1, effect));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellDefinition("id", "Name", MagicSchool.Destruction, SpellTargetKind.SingleTarget, 1, 0, -1, effect));
        }

        [Test]
        public void Definition_ExistingConstructorDefaultsToSingleTargetUnboundedRangeAndZeroCooldown()
        {
            var spell = new SpellDefinition(
                "id",
                "Name",
                MagicSchool.Destruction,
                5,
                new[] { new SpellEffectSpec(SpellEffectKind.DirectDamage, 4, 0) });

            Assert.That(spell.TargetKind, Is.EqualTo(SpellTargetKind.SingleTarget));
            Assert.That(spell.RangeInTiles, Is.EqualTo(0));
            Assert.That(spell.CooldownTicks, Is.EqualTo(0));
        }

        [Test]
        public void Definition_RangeAwareConstructor_PersistsRangeAndCooldown()
        {
            var spell = new SpellDefinition(
                "id",
                "Name",
                MagicSchool.Destruction,
                SpellTargetKind.SingleTarget,
                5,
                7,
                9,
                new[] { new SpellEffectSpec(SpellEffectKind.DirectDamage, 4, 0) });

            Assert.That(spell.TargetKind, Is.EqualTo(SpellTargetKind.SingleTarget));
            Assert.That(spell.RangeInTiles, Is.EqualTo(7));
            Assert.That(spell.CooldownTicks, Is.EqualTo(9));
        }

        [Test]
        public void Definition_RequiresAtLeastOneEffect()
        {
            Assert.Throws<ArgumentException>(() => new SpellDefinition("id", "Name", MagicSchool.Destruction, 1, new SpellEffectSpec[0]));
        }

        [Test]
        public void Definition_EffectsCollectionIsReadOnlySnapshot()
        {
            var input = new[] { new SpellEffectSpec(SpellEffectKind.DirectDamage, 4, 0) };
            var spell = new SpellDefinition("id", "Name", MagicSchool.Destruction, 5, input);
            input[0] = new SpellEffectSpec(SpellEffectKind.RestoreHealth, 99, 0);
            Assert.That(spell.Effects[0].Kind, Is.EqualTo(SpellEffectKind.DirectDamage));
            Assert.That(spell.Effects[0].Magnitude, Is.EqualTo(4));
        }
    }
}

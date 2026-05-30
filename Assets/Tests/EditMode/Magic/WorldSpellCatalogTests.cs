using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the Sprint 5 deterministic catalog: stable ids, schools, mana costs, effect specs.
// Catches accidental drift in the starter spell set without coupling to mana-spend behaviour.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies the slice spell catalog is deterministic and well-formed.</summary>
    public sealed class WorldSpellCatalogTests
    {
        [Test]
        public void All_ListsThreeStarterSpellsInStableOrder()
        {
            var ids = new[]
            {
                WorldSpellCatalog.All[0].TemplateId,
                WorldSpellCatalog.All[1].TemplateId,
                WorldSpellCatalog.All[2].TemplateId,
            };

            Assert.That(WorldSpellCatalog.All.Count, Is.EqualTo(3));
            Assert.That(ids, Is.EqualTo(new[]
            {
                WorldSpellCatalog.FlameBoltTemplateId,
                WorldSpellCatalog.MendingTouchTemplateId,
                WorldSpellCatalog.EmberWardTemplateId,
            }));
        }

        [Test]
        public void Find_ReturnsSpellByTemplateId()
        {
            var spell = WorldSpellCatalog.Find(WorldSpellCatalog.FlameBoltTemplateId);
            Assert.That(spell, Is.Not.Null);
            Assert.That(spell.School, Is.EqualTo(MagicSchool.Destruction));
            Assert.That(spell.TargetKind, Is.EqualTo(SpellTargetKind.SingleTarget));
            Assert.That(spell.ManaCost, Is.EqualTo(12));
            Assert.That(spell.RangeInTiles, Is.EqualTo(WorldSpellCatalog.FlameBoltRangeInTiles));
            Assert.That(spell.CooldownTicks, Is.EqualTo(WorldSpellCatalog.FlameBoltCooldownTicks));
            Assert.That(spell.Effects.Count, Is.EqualTo(1));
            Assert.That(spell.Effects[0].Kind, Is.EqualTo(SpellEffectCode.DirectDamage));
        }

        [Test]
        public void Find_ReturnsNullForUnknownOrEmptyId()
        {
            Assert.That(WorldSpellCatalog.Find("not_a_spell"), Is.Null);
            Assert.That(WorldSpellCatalog.Find(""), Is.Null);
            Assert.That(WorldSpellCatalog.Find(null), Is.Null);
        }

        [Test]
        public void EmberWard_HasNonZeroDuration_AndShieldEffect()
        {
            var spell = WorldSpellCatalog.Find(WorldSpellCatalog.EmberWardTemplateId);
            Assert.That(spell.School, Is.EqualTo(MagicSchool.Alteration));
            Assert.That(spell.TargetKind, Is.EqualTo(SpellTargetKind.CasterSelf));
            Assert.That(spell.Effects[0].Kind, Is.EqualTo(SpellEffectCode.ShieldBuff));
            Assert.That(spell.Effects[0].DurationTicks, Is.GreaterThan(0));
            Assert.That(spell.Effects[0].IsInstantaneous, Is.False);
        }

        [Test]
        public void StarterSpells_ExposeDeterministicCooldownTicks()
        {
            var flameBolt = WorldSpellCatalog.Find(WorldSpellCatalog.FlameBoltTemplateId);
            var mendingTouch = WorldSpellCatalog.Find(WorldSpellCatalog.MendingTouchTemplateId);
            var emberWard = WorldSpellCatalog.Find(WorldSpellCatalog.EmberWardTemplateId);

            Assert.That(flameBolt.CooldownTicks, Is.EqualTo(WorldSpellCatalog.FlameBoltCooldownTicks));
            Assert.That(mendingTouch.CooldownTicks, Is.EqualTo(WorldSpellCatalog.MendingTouchCooldownTicks));
            Assert.That(emberWard.CooldownTicks, Is.EqualTo(WorldSpellCatalog.EmberWardCooldownTicks));

            Assert.That(WorldSpellCatalog.FlameBoltCooldownTicks, Is.GreaterThan(0));
            Assert.That(WorldSpellCatalog.MendingTouchCooldownTicks, Is.GreaterThan(0));
            Assert.That(WorldSpellCatalog.EmberWardCooldownTicks, Is.EqualTo(emberWard.Effects[0].DurationTicks));
        }
    }
}

using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the Sprint 5 deterministic catalog: stable ids, schools, mana costs, effect specs.
// Catches accidental drift in the starter spell set without coupling to mana-spend behaviour.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies the slice spell catalog is deterministic and well-formed.</summary>
    public sealed class SliceSpellCatalogTests
    {
        [Test]
        public void All_ListsThreeStarterSpellsInStableOrder()
        {
            var ids = new[]
            {
                SliceSpellCatalog.All[0].TemplateId,
                SliceSpellCatalog.All[1].TemplateId,
                SliceSpellCatalog.All[2].TemplateId,
            };

            Assert.That(SliceSpellCatalog.All.Count, Is.EqualTo(3));
            Assert.That(ids, Is.EqualTo(new[]
            {
                SliceSpellCatalog.FlameBoltTemplateId,
                SliceSpellCatalog.MendingTouchTemplateId,
                SliceSpellCatalog.EmberWardTemplateId,
            }));
        }

        [Test]
        public void Find_ReturnsSpellByTemplateId()
        {
            var spell = SliceSpellCatalog.Find(SliceSpellCatalog.FlameBoltTemplateId);
            Assert.That(spell, Is.Not.Null);
            Assert.That(spell.School, Is.EqualTo(MagicSchool.Destruction));
            Assert.That(spell.TargetKind, Is.EqualTo(SpellTargetKind.SingleTarget));
            Assert.That(spell.ManaCost, Is.EqualTo(12));
            Assert.That(spell.RangeInTiles, Is.EqualTo(SliceSpellCatalog.FlameBoltRangeInTiles));
            Assert.That(spell.Effects.Count, Is.EqualTo(1));
            Assert.That(spell.Effects[0].Kind, Is.EqualTo(SpellEffectKind.DirectDamage));
        }

        [Test]
        public void Find_ReturnsNullForUnknownOrEmptyId()
        {
            Assert.That(SliceSpellCatalog.Find("not_a_spell"), Is.Null);
            Assert.That(SliceSpellCatalog.Find(""), Is.Null);
            Assert.That(SliceSpellCatalog.Find(null), Is.Null);
        }

        [Test]
        public void EmberWard_HasNonZeroDuration_AndShieldEffect()
        {
            var spell = SliceSpellCatalog.Find(SliceSpellCatalog.EmberWardTemplateId);
            Assert.That(spell.School, Is.EqualTo(MagicSchool.Alteration));
            Assert.That(spell.TargetKind, Is.EqualTo(SpellTargetKind.CasterSelf));
            Assert.That(spell.Effects[0].Kind, Is.EqualTo(SpellEffectKind.ShieldBuff));
            Assert.That(spell.Effects[0].DurationTicks, Is.GreaterThan(0));
            Assert.That(spell.Effects[0].IsInstantaneous, Is.False);
        }
    }
}

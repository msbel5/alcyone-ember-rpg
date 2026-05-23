using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.CharacterCreation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.CharacterCreation
{
    public sealed class BirthsignBonusTests
    {
        [TestCase("the_tower", EmberAttribute.End, 4)]
        [TestCase("the_lover", EmberAttribute.Pre, 4)]
        [TestCase("the_lady", EmberAttribute.Ins, 4)]
        [TestCase("the_atronach", EmberAttribute.Mnd, 6)]
        [TestCase("the_warrior", EmberAttribute.Mig, 5)]
        [TestCase("the_mage", EmberAttribute.Mnd, 5)]
        [TestCase("the_thief", EmberAttribute.Agi, 5)]
        [TestCase("the_serpent", EmberAttribute.Agi, 3)]
        [TestCase("the_steed", EmberAttribute.Agi, 4)]
        [TestCase("the_ritual", EmberAttribute.Pre, 3)]
        [TestCase("the_shadow", EmberAttribute.Ins, 3)]
        [TestCase("the_lord", EmberAttribute.End, 5)]
        public void Birthsign_AppliesExactSingleStatDelta(string id, EmberAttribute attribute, int delta)
        {
            var baseStats = new EmberStatBlock(50, 50, 50, 50, 50, 50);
            var sign = CharacterCreationCatalog.GetBirthsign(id);
            var applied = sign.ApplyTo(baseStats);

            Assert.That(applied.Get(attribute), Is.EqualTo(50 + delta));
            foreach (EmberAttribute other in System.Enum.GetValues(typeof(EmberAttribute)))
            {
                if (other == attribute) continue;
                Assert.That(applied.Get(other), Is.EqualTo(50), other.ToString());
            }
        }

        [Test]
        public void CatalogShipsTwelveBirthsigns()
        {
            Assert.That(CharacterCreationCatalog.Birthsigns.Count, Is.EqualTo(12));
        }
    }
}

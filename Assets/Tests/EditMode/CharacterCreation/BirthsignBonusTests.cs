using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.CharacterCreation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.CharacterCreation
{
    public sealed class BirthsignBonusTests
    {
        [TestCase("the_anvil", EmberAttribute.Mig, 5)]
        [TestCase("the_hammer", EmberAttribute.Mig, 3)]
        [TestCase("the_kiln", EmberAttribute.End, 5)]
        [TestCase("the_ash", EmberAttribute.End, 4)]
        [TestCase("the_ember", EmberAttribute.Mnd, 5)]
        [TestCase("the_forgefire", EmberAttribute.Mnd, 6)]
        [TestCase("the_spark", EmberAttribute.Agi, 5)]
        [TestCase("the_wisp", EmberAttribute.Agi, 3)]
        [TestCase("the_beacon", EmberAttribute.Pre, 5)]
        [TestCase("the_pyre", EmberAttribute.Pre, 3)]
        [TestCase("the_cinder", EmberAttribute.Ins, 4)]
        [TestCase("the_smoke", EmberAttribute.Ins, 4)]
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

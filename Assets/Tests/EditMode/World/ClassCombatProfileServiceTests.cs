using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>
    /// PLAYTEST pin ("siniflar anlamli degil"): class choice must change the realtime melee
    /// dice, and the derived numbers must stay inside the dice clamps (hit 15..95 band).
    /// </summary>
    public sealed class ClassCombatProfileServiceTests
    {
        [Test]
        public void Warrior_HitsHarder_ThanScholar()
        {
            var warrior = CharacterCreationCatalog.GetClass("warrior").PrimaryStats;
            var scholar = CharacterCreationCatalog.GetClass("scholar").PrimaryStats;
            Assert.That(ClassCombatProfileService.DeriveBaseDamage(warrior),
                Is.GreaterThan(ClassCombatProfileService.DeriveBaseDamage(scholar)),
                "might must translate into damage or class choice is cosmetic");
        }

        [Test]
        public void Rogue_LandsTruer_AndSlips_MoreThanWarrior()
        {
            var rogue = CharacterCreationCatalog.GetClass("rogue").PrimaryStats;
            var warrior = CharacterCreationCatalog.GetClass("warrior").PrimaryStats;
            Assert.That(ClassCombatProfileService.DeriveAccuracy(rogue),
                Is.GreaterThan(ClassCombatProfileService.DeriveAccuracy(warrior)));
            Assert.That(ClassCombatProfileService.DeriveDodge(rogue),
                Is.GreaterThan(ClassCombatProfileService.DeriveDodge(warrior)));
        }

        [Test]
        public void EveryClass_StaysInsideTheDiceClamps()
        {
            foreach (var klass in CharacterCreationCatalog.Classes)
            {
                var s = klass.PrimaryStats;
                Assert.That(ClassCombatProfileService.DeriveAccuracy(s), Is.InRange(6, 30), klass.Id + " accuracy");
                Assert.That(ClassCombatProfileService.DeriveDodge(s), Is.InRange(3, 22), klass.Id + " dodge");
                Assert.That(ClassCombatProfileService.DeriveArmor(s), Is.InRange(1, 4), klass.Id + " armor");
                Assert.That(ClassCombatProfileService.DeriveBaseDamage(s), Is.InRange(3, 12), klass.Id + " damage");
            }
        }
    }
}

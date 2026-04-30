using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin the role-differentiated loadouts seeded into the slice world.
// They cover deterministic starting distinctions, not presentation text.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies actor roles no longer share cloned starting profiles.</summary>
    public sealed class SliceWorldFactoryTests
    {
        [Test]
        public void Create_AssignsDistinctRoleVitalsAndCombatFields()
        {
            var world = new SliceWorldFactory().Create(1337);
            Assert.That(world.Player.Vitals.Health.Max, Is.GreaterThan(world.Talker.Vitals.Health.Max));
            Assert.That(world.Guard.Armor, Is.GreaterThan(world.Merchant.Armor));
            Assert.That(world.Merchant.Stats.Pre, Is.GreaterThan(world.Enemy.Stats.Pre));
            Assert.That(world.Enemy.Dodge, Is.GreaterThan(world.Guard.Dodge));
        }
    }
}

using EmberCrpg.Simulation.World;
using NUnit.Framework;
using EmberCrpg.Domain.Actors;

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
            Assert.That(world.Actors.FirstByRole(ActorRole.Player).Vitals.Health.Max, Is.GreaterThan(world.Actors.FirstByRole(ActorRole.Talker).Vitals.Health.Max));
            Assert.That(world.Actors.FirstByRole(ActorRole.Guard).Armor, Is.GreaterThan(world.Actors.FirstByRole(ActorRole.Merchant).Armor));
            Assert.That(world.Actors.FirstByRole(ActorRole.Merchant).Stats.Pre, Is.GreaterThan(world.Actors.FirstByRole(ActorRole.Enemy).Stats.Pre));
            Assert.That(world.Actors.FirstByRole(ActorRole.Enemy).Dodge, Is.GreaterThan(world.Actors.FirstByRole(ActorRole.Guard).Dodge));
        }
    }
}

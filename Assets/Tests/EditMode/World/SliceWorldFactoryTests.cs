using EmberCrpg.Simulation.World;
using NUnit.Framework;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;

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

        [Test]
        public void Create_SeedsBackendAnchorsForGeneratedEmberScenes()
        {
            var world = new SliceWorldFactory().Create(1337);

            Assert.That(world.Sites.Count, Is.GreaterThanOrEqualTo(8));
            Assert.That(world.Factions.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(world.Stockpiles.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(world.TradeRoutes.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(world.Caravans.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(world.FindStockpile(new SiteId(1)).Get("iron"), Is.EqualTo(8));
            Assert.That(world.Prices.GetPrice(new SiteId(1), "iron"), Is.EqualTo(10));
        }
    }
}

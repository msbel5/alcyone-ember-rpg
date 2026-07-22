using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// Review-mandated coverage: the eating loop's core semantics (eat-to-satiation at the
    /// larder, reach gating, NEAREST-pile routing) previously had zero direct unit tests —
    /// only the integration gates pinned them.
    /// </summary>
    public sealed class NeedConsumptionSystemTests
    {
        private static WorldState World(params (ulong siteId, int minX, int wheat)[] larders)
        {
            var world = new WorldState();
            world.EnsureInvariants();
            foreach (var (siteId, minX, wheat) in larders)
            {
                world.Sites.Add(new SiteRecord(new SiteId(siteId), SiteKind.Settlement, $"S{siteId}",
                    new GridPosition(minX, 0), new GridPosition(minX + 4, 4)));
                var pile = new StockpileComponent(new SiteId(siteId));
                pile.Add("wheat", wheat);
                world.Stockpiles.Add(pile);
            }
            return world;
        }

        private static ActorRecord Hungry(ulong id, GridPosition position, int hunger)
        {
            var actor = new ActorRecord(
                new ActorId(id), "Eater", ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                position, accuracy: 50, dodge: 10, armor: 0, baseDamage: 1);
            actor.ApplyNeeds(actor.Needs.WithHunger(new NeedValue(hunger)));
            return actor;
        }

        [Test]
        public void Tick_HungryActorAtTheLarder_EatsToSatiationAndDecrementsStock()
        {
            var world = World((5UL, 0, 10));
            world.Actors.Add(Hungry(1, new GridPosition(2, 2), 80)); // site centre = (2,2)

            int meals = new NeedConsumptionSystem().Tick(world, hourOfDay: 12);

            Assert.That(meals, Is.EqualTo(1));
            Assert.That(world.Actors.Get(new ActorId(1)).Needs.Hunger.Value,
                Is.EqualTo(NeedConsumptionSystem.MealHungerFloor));
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(9), "one meal consumes one wheat");
        }

        [Test]
        public void Tick_HungryActorOutOfReach_DoesNotEat()
        {
            var world = World((5UL, 0, 10));
            world.Actors.Add(Hungry(1, new GridPosition(9, 9), 80)); // Chebyshev 7 > EatReachCells

            Assert.That(new NeedConsumptionSystem().Tick(world, 12), Is.EqualTo(0));
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(10));
        }

        [Test]
        public void Tick_TwoLarders_ActorEatsFromTheNearestOne()
        {
            var world = World((5UL, 0, 10), (6UL, 40, 10));
            world.Actors.Add(Hungry(1, new GridPosition(42, 2), 80)); // beside site 6's centre (42,2)

            Assert.That(new NeedConsumptionSystem().Tick(world, 12), Is.EqualTo(1));
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(10), "far larder untouched");
            Assert.That(world.Stockpiles[1].Get("wheat"), Is.EqualTo(9), "NEAREST larder feeds the actor");
        }

        [Test]
        public void FoodSpots_ReturnsOneCentrePerFoodHoldingPile()
        {
            var world = World((5UL, 0, 10), (6UL, 40, 0)); // site 6 pile is EMPTY
            var spots = NeedConsumptionSystem.FoodSpots(world);

            Assert.That(spots, Is.EqualTo(new[] { new GridPosition(2, 2) }),
                "empty piles are not gathering spots");
        }
    }
}

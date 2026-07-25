using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// W32 EAT moved the eating half to the action layer (EatActionStoryTests owns those pins);
    /// W34 SLEEP moved the night fatigue fiat to SleepAdvancer (the S-series owns recovery pins
    /// now — heirs of the deleted hourly-rate pin). What remains here is food-spot geometry.
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

        // W34: the two Tick pins died WITH the fiat they pinned (NeedConsumptionSystem.Tick is
        // deleted — sleep recovery is the SleepAdvancer's 2-per-3-ticks ladder now, and "the
        // hourly step never feeds" became vacuously true when the hourly step itself retired).

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

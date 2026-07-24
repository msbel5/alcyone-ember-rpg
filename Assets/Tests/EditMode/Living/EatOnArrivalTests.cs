using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>P0 pin (ARCHITECTURE_GAPS #1): reaching the table IS the meal - nobody stands
    /// at the plaza for a game hour waiting for the Hourly step.</summary>
    public sealed class EatOnArrivalTests
    {
        [Test]
        public void TickArrivals_HungryCivilianAtTheLarder_EatsThisVeryTick()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(10, 10)));
            var pile = new StockpileComponent(new SiteId(1));
            pile.Add("wheat", 10);
            world.Stockpiles.Add(pile);

            var diner = new ActorRecord(
                new ActorId(7), "Diner", ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(5, 5), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1);
            diner.ApplyNeeds(diner.Needs.WithHunger(new NeedValue(80)));
            world.Actors.Add(diner);

            int meals = new NeedConsumptionSystem().TickArrivals(world, new GameTime(61));

            Assert.That(meals, Is.EqualTo(1), "arrival resolves the meal on the SAME tick");
            Assert.That(world.Actors.Get(new ActorId(7)).Needs.Hunger.Value,
                Is.EqualTo(NeedConsumptionSystem.MealHungerFloor), "hunger drops to the meal floor");
        }

        [Test]
        public void TickArrivals_NotHungryOrNotThere_NoMeal()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(10, 10)));
            var pile = new StockpileComponent(new SiteId(1));
            pile.Add("wheat", 10);
            world.Stockpiles.Add(pile);

            var farAndHungry = new ActorRecord(
                new ActorId(8), "Far", ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(50, 50), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1);
            farAndHungry.ApplyNeeds(farAndHungry.Needs.WithHunger(new NeedValue(80)));
            world.Actors.Add(farAndHungry);

            Assert.That(new NeedConsumptionSystem().TickArrivals(world, new GameTime(61)), Is.EqualTo(0),
                "hunger without proximity buys nothing - the walk still matters");
        }
    }
}

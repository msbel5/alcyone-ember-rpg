using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// W32 EAT: the eating half moved to the action layer (EatActionStoryTests owns those
    /// pins); what remains here is the sleep/metabolism half and the food-spot geometry.
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
        public void Tick_NightHour_TiredCivilianSleepsAndMoodFollows()
        {
            var world = World((5UL, 0, 10));
            var sleeper = Hungry(1, new GridPosition(2, 2), 10);
            sleeper.ApplyNeeds(sleeper.Needs.WithFatigue(new NeedValue(60)));
            world.Actors.Add(sleeper);

            new NeedConsumptionSystem().Tick(world, hourOfDay: 23);

            Assert.That(world.Actors.Get(new ActorId(1)).Needs.Fatigue.Value,
                Is.EqualTo(60 - NeedConsumptionSystem.NightSleepFatigueRecovery),
                "a night hour recovers fatigue by the fixed rate");
        }

        [Test]
        public void Tick_DayHour_DoesNotRecoverFatigue_AndNeverFeeds()
        {
            var world = World((5UL, 0, 10));
            var worker = Hungry(1, new GridPosition(2, 2), 80); // AT the larder, starving
            worker.ApplyNeeds(worker.Needs.WithFatigue(new NeedValue(60)));
            world.Actors.Add(worker);

            new NeedConsumptionSystem().Tick(world, hourOfDay: 12);

            Assert.That(world.Actors.Get(new ActorId(1)).Needs.Fatigue.Value, Is.EqualTo(60));
            Assert.That(world.Actors.Get(new ActorId(1)).Needs.Hunger.Value, Is.EqualTo(80),
                "W32: the hourly step NEVER feeds — meals belong to the action layer");
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(10), "stock untouched");
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

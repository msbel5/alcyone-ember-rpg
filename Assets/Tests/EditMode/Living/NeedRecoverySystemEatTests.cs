using System;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

// Design note:
// These tests pin meal recovery as a concrete PROCESS/LIVING consumer:
// inventory is consumed only on success, hunger drops, mood is recomputed, and
// a NeedChanged trace records the player-visible recovery.
namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>Verifies deterministic meal recovery.</summary>
    public sealed class NeedRecoverySystemEatTests
    {
        [Test]
        public void EatMeal_ConsumesFoodLowersHungerAndLogsReasonTrace()
        {
            var actor = CreateActor(new ActorNeeds(new NeedValue(80), new NeedValue(40), NeedValue.Comfortable));
            var inventory = MealInventory(quantity: 2);
            var log = new WorldEventLog();
            var recipe = MealRecipe();

            var recovered = new NeedRecoverySystem().EatMeal(actor, inventory, recipe, log, new GameTime(180));

            Assert.That(recovered, Is.True);
            Assert.That(actor.Needs.Hunger.Value, Is.EqualTo(30));
            Assert.That(actor.Mood.Value, Is.EqualTo(27));
            Assert.That(inventory.Items.Single().Quantity, Is.EqualTo(1));

            var evt = log.Events.Single();
            Assert.That(evt.Kind, Is.EqualTo(WorldEventKind.NeedChanged));
            Assert.That(evt.ActorId, Is.EqualTo(actor.Id));
            Assert.That(evt.Reason, Is.EqualTo($"need_recovered:{actor.Id.Value}"));
            Assert.That(evt.ReasonTrace.Causes, Is.EqualTo(new[]
            {
                "need_recovery",
                "action:eat_meal",
                "recipe:meal-basic",
                $"actor:{actor.Id.Value}",
                "item:simple_meal",
                "hunger:80->30",
                "mood:27",
            }));
        }

        [Test]
        public void EatMeal_MissingFoodPreservesInventoryActorAndLog()
        {
            var actor = CreateActor(new ActorNeeds(new NeedValue(80), NeedValue.Comfortable, NeedValue.Comfortable));
            var inventory = new InventoryState(4);
            var log = new WorldEventLog();

            var recovered = new NeedRecoverySystem().EatMeal(actor, inventory, MealRecipe(), log, new GameTime(180));

            Assert.That(recovered, Is.False);
            Assert.That(actor.Needs.Hunger.Value, Is.EqualTo(80));
            Assert.That(actor.Mood, Is.EqualTo(ActorMood.Neutral));
            Assert.That(inventory.IsEmpty(), Is.True);
            Assert.That(log.IsEmpty, Is.True);
        }

        [Test]
        public void EatMeal_RejectsInvalidInputs()
        {
            var system = new NeedRecoverySystem();
            var actor = CreateActor(new ActorNeeds(new NeedValue(80), NeedValue.Comfortable, NeedValue.Comfortable));
            var inventory = MealInventory(quantity: 1);
            var log = new WorldEventLog();

            Assert.Throws<ArgumentNullException>(() => system.EatMeal(null, inventory, MealRecipe(), log, new GameTime(0)));
            Assert.Throws<ArgumentNullException>(() => system.EatMeal(actor, null, MealRecipe(), log, new GameTime(0)));
            Assert.Throws<ArgumentNullException>(() => system.EatMeal(actor, inventory, null, log, new GameTime(0)));
            Assert.Throws<ArgumentNullException>(() => system.EatMeal(actor, inventory, MealRecipe(), null, new GameTime(0)));
            Assert.Throws<ArgumentException>(() => system.EatMeal(actor, inventory, SleepRecipe(), log, new GameTime(0)));
        }

        private static NeedRecoveryRecipe MealRecipe()
        {
            return new NeedRecoveryRecipe("meal-basic", NeedRecoverySystem.EatMealAction, NeedKind.Hunger, 50, "simple_meal");
        }

        private static NeedRecoveryRecipe SleepRecipe()
        {
            return new NeedRecoveryRecipe("sleep-basic", NeedRecoverySystem.SleepAction, NeedKind.Fatigue, 40);
        }

        private static InventoryState MealInventory(int quantity)
        {
            var inventory = new InventoryState(4);
            inventory.TryAdd(new InventoryItem(new ItemId(900UL), "simple_meal", "Simple Meal", quantity));
            return inventory;
        }

        private static ActorRecord CreateActor(ActorNeeds needs)
        {
            return new ActorRecord(
                new ActorId(31),
                "Meal Recovery",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                10,
                5,
                1,
                3,
                needs: needs);
        }
    }

    internal static class InventoryStateTestExtensions
    {
        public static bool IsEmpty(this InventoryState inventory)
        {
            return inventory.Items.Count == 0;
        }
    }
}

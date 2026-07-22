using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// CAN SUYU H1: the missing half of the needs loop. NeedsSystem only ever RAISED hunger/fatigue/
// thirst (a one-way ratchet — every actor ground into the job-refusal gate while stockpiles
// filled with food nobody ate). This system closes the circuit: hungry actors EAT from real
// stockpiles (stock drops → prices finally see demand → shortages become real), and tired
// actors SLEEP through the night hours. Deterministic, event-emitting, pure Simulation.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Hourly consumption: eat from stockpiles when hungry, sleep at night when tired.</summary>
    public sealed class NeedConsumptionSystem
    {
        public const int HungerEatThreshold = 60;
        public const int MealHungerFloor = 5;    // eat to satiation, not by a fixed bite
        public const int MealThirstRecovery = 40; // the meal includes a drink (no water sim yet)
        public const int NightSleepFatigueRecovery = 40;
        public const int NightStartHour = 22;
        public const int NightEndHour = 6;

        private readonly NeedMoodEvaluator _moodEvaluator = new NeedMoodEvaluator();

        /// <summary>Runs once per game-hour. Returns meals eaten (proof metric).</summary>
        public int Tick(WorldState world, int hourOfDay)
        {
            if (world?.Actors == null) return 0;
            int meals = 0;
            bool night = hourOfDay >= NightStartHour || hourOfDay < NightEndHour;

            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive) continue;
                if (actor.Role == ActorRole.Player || actor.Role == ActorRole.Enemy) continue;

                if (actor.Needs.Hunger.Value >= HungerEatThreshold && TryEat(world, actor))
                    meals++;

                if (night && actor.Needs.Fatigue.Value > 0)
                {
                    var rested = actor.Needs.WithFatigue(
                        new NeedValue(actor.Needs.Fatigue.Value - NightSleepFatigueRecovery));
                    actor.ApplyNeeds(rested);
                    actor.ApplyMood(_moodEvaluator.Evaluate(rested));
                }
            }
            return meals;
        }

        private bool TryEat(WorldState world, ActorRecord actor)
        {
            var pile = FindFoodPile(world, out var foodTag);
            if (pile == null) return false;

            pile.Remove(foodTag, 1);
            var fed = actor.Needs
                .WithHunger(new NeedValue(MealHungerFloor))
                .WithThirst(new NeedValue(actor.Needs.Thirst.Value - MealThirstRecovery));
            actor.ApplyNeeds(fed);
            actor.ApplyMood(_moodEvaluator.Evaluate(fed));
            world.Events?.Append(new WorldEvent(
                world.Time, WorldEventKind.NeedChanged, actor.Id, pile.SiteId,
                $"meal_eaten item:{foodTag} hunger:{fed.Hunger.Value}"));
            return true;
        }

        // Food = whatever the fields grow (plant species tags own the harvest stock). Nearest-pile
        // routing lands with H2's decision layer; the aggregate flow is what H1 must close.
        private static StockpileComponent FindFoodPile(WorldState world, out string foodTag)
        {
            foodTag = null;
            if (world.Stockpiles == null) return null;
            var species = FoodTags(world);
            foreach (var pile in world.Stockpiles)
            {
                if (pile == null) continue;
                foreach (var tag in species)
                    if (pile.Get(tag) > 0)
                    {
                        foodTag = tag;
                        return pile;
                    }
            }
            return null;
        }

        private static List<string> FoodTags(WorldState world)
        {
            var tags = new List<string>();
            if (world.Plants == null) return tags;
            foreach (var row in world.Plants.Rows)
                if (row.Value != null && !string.IsNullOrEmpty(row.Value.SpeciesId) && !tags.Contains(row.Value.SpeciesId))
                    tags.Add(row.Value.SpeciesId);
            return tags;
        }
    }
}

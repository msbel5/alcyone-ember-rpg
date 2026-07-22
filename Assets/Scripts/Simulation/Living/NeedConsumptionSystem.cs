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
        public const int HungerEatThreshold = 55; // aligned with the H2 utility crossover (WorkScore)
        public const int EatReachCells = 2;       // H2: you eat AT the table — walk all the way
        public const int MealHungerFloor = 5;    // eat to satiation, not by a fixed bite
        public const int MealThirstRecovery = 40; // the meal includes a drink (no water sim yet)
        public const int NightSleepFatigueRecovery = 40;
        public const int NightStartHour = 22;
        public const int NightEndHour = 6;

        private readonly NeedMoodEvaluator _moodEvaluator = new NeedMoodEvaluator();

        /// <summary>Runs once per game-hour. Returns meals eaten (proof metric).</summary>
        public int Tick(WorldState world, int hourOfDay) => Tick(world, hourOfDay, world?.Time ?? default);

        // Catchup contract (Codex ninth-pass lesson): events are stamped at the cadence
        // BOUNDARY, not at post-advance world.Time — day-by-day and multi-day jumps must
        // write identical logs.
        public int Tick(WorldState world, int hourOfDay, EmberCrpg.Domain.Core.GameTime stamp)
        {
            if (world?.Actors == null) return 0;
            int meals = 0;
            bool night = hourOfDay >= NightStartHour || hourOfDay < NightEndHour;

            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive) continue;
                if (actor.Role == ActorRole.Player || actor.Role == ActorRole.Enemy) continue;

                if (actor.Needs.Hunger.Value >= HungerEatThreshold && TryEat(world, actor, stamp))
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

        private bool TryEat(WorldState world, ActorRecord actor, EmberCrpg.Domain.Core.GameTime stamp)
        {
            var pile = FindFoodPile(world, out var foodTag);
            if (pile == null) return false;
            // H2: eating happens AT the larder — the utility selector walks you there first.
            // This is what turns "numbers drift" into a VISIBLE walk-eat-return rhythm.
            if (!WithinReach(world, actor, pile)) return false;

            pile.Remove(foodTag, 1);
            var fed = actor.Needs
                .WithHunger(new NeedValue(MealHungerFloor))
                .WithThirst(new NeedValue(actor.Needs.Thirst.Value - MealThirstRecovery));
            actor.ApplyNeeds(fed);
            actor.ApplyMood(_moodEvaluator.Evaluate(fed));
            world.Events?.Append(new WorldEvent(
                stamp, WorldEventKind.NeedChanged, actor.Id, pile.SiteId,
                $"meal_eaten item:{foodTag} hunger:{fed.Hunger.Value}"));
            return true;
        }

        private static bool WithinReach(WorldState world, ActorRecord actor, StockpileComponent pile)
        {
            if (world.Sites?.Records == null) return true; // siteless worlds (bare tests) stay permissive
            foreach (var site in world.Sites.Records)
            {
                if (site == null || !site.Id.Equals(pile.SiteId)) continue;
                int cx = (site.MinBound.X + site.MaxBound.X) / 2;
                int cy = (site.MinBound.Y + site.MaxBound.Y) / 2;
                int dx = System.Math.Abs(actor.Position.X - cx);
                int dy = System.Math.Abs(actor.Position.Y - cy);
                return System.Math.Max(dx, dy) <= EatReachCells;
            }
            return true; // pile without a site record: no geometry to enforce
        }

        /// <summary>H2: the communal food spot — the first food-holding pile's site centre. The
        /// tick publishes this to the utility selector as the walk target.</summary>
        public static GridPosition? FoodSpot(WorldState world)
        {
            var pile = FindFoodPile(world, out _);
            if (pile == null || world.Sites?.Records == null) return null;
            foreach (var site in world.Sites.Records)
                if (site != null && site.Id.Equals(pile.SiteId))
                    return new GridPosition(
                        (site.MinBound.X + site.MaxBound.X) / 2,
                        (site.MinBound.Y + site.MaxBound.Y) / 2);
            return null;
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
            // "wheat" is the staple even in plotless worlds; live species extend the menu.
            var tags = new List<string> { "wheat" };
            if (world.Plants == null) return tags;
            foreach (var row in world.Plants.Rows)
                if (row.Value != null && !string.IsNullOrEmpty(row.Value.SpeciesId) && !tags.Contains(row.Value.SpeciesId))
                    tags.Add(row.Value.SpeciesId);
            return tags;
        }
    }
}

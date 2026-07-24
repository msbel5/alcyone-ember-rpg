using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;

// CAN SUYU H1: the missing half of the needs loop. NeedsSystem only ever RAISED hunger/fatigue/
// thirst (a one-way ratchet). W32 EAT: the eating half moved to the action layer — decision
// (ActionLifecycleSystem) reserves a real unit and the EAT phase machine walks/takes/consumes
// it. What remains here is the SLEEP/metabolism half plus the shared food constants and the
// site-centre truth the action layer imports (threshold/reach/meal math must not fork).
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Hourly metabolism half: tired actors sleep through the night hours.</summary>
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

        /// <summary>Runs once per game-hour.</summary>
        public void Tick(WorldState world, int hourOfDay)
        {
            if (world?.Actors == null) return;
            bool night = hourOfDay >= NightStartHour || hourOfDay < NightEndHour;
            if (!night) return;

            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive) continue;
                if (actor.Role == ActorRole.Player || actor.Role == ActorRole.Enemy) continue;

                // Sleep is intentionally UNLOGGED: a per-actor event every night hour would
                // spam the log (the NeedsStep summary lesson); Gate1 pins the effect instead.
                if (actor.Needs.Fatigue.Value > 0)
                {
                    var rested = actor.Needs.WithFatigue(
                        new NeedValue(actor.Needs.Fatigue.Value - NightSleepFatigueRecovery));
                    actor.ApplyNeeds(rested);
                    actor.ApplyMood(_moodEvaluator.Evaluate(rested));
                }
            }
        }

        /// <summary>Single source of site-centre truth — was duplicated four times (review fix).</summary>
        public static bool TryGetSiteCentre(WorldState world, EmberCrpg.Domain.Core.SiteId siteId, out GridPosition centre)
        {
            centre = default;
            if (world?.Sites?.Records == null) return false;
            foreach (var site in world.Sites.Records)
                if (site != null && site.Id.Equals(siteId))
                {
                    centre = new GridPosition(
                        (site.MinBound.X + site.MaxBound.X) / 2,
                        (site.MinBound.Y + site.MaxBound.Y) / 2);
                    return true;
                }
            return false;
        }

        /// <summary>All food-holding piles' site centres. Multi-settlement worlds have MANY
        /// larders; gate tests sample gathering waves around these.</summary>
        public static List<GridPosition> FoodSpots(WorldState world)
        {
            var spots = new List<GridPosition>();
            if (world?.Stockpiles == null || world.Sites?.Records == null) return spots;
            var species = FoodPileCache.FoodTags(world);
            foreach (var entry in FoodPileCache.Build(world, species))
                if (entry.HasSite)
                    spots.Add(new GridPosition(entry.CentreX, entry.CentreY));
            return spots;
        }

        public static GridPosition? FoodSpot(WorldState world)
        {
            var spots = FoodSpots(world);
            return spots.Count > 0 ? spots[0] : (GridPosition?)null;
        }
    }
}

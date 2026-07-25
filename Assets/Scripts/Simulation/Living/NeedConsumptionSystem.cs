using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;

// CAN SUYU H1, narrowed twice: W32 EAT moved the eating half to the action layer; W34 SLEEP
// moved the last mutation — the night fatigue fiat — to the SleepAdvancer (recovery now
// requires a real MoveToBed walk and a Running Sleep in bed). What remains is a CONSTANTS +
// SITE-CENTRE TRUTH class: the shared food/night constants and the site-centre/food-spot
// lookups the action layer imports (threshold/reach/meal/night math must not fork).
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Shared need constants + site-centre/food-spot truth for the action layer.</summary>
    public sealed class NeedConsumptionSystem
    {
        public const int HungerEatThreshold = 55; // aligned with the H2 utility crossover (WorkScore)
        public const int EatReachCells = 2;       // H2: you eat AT the table — walk all the way
        public const int MealHungerFloor = 5;    // eat to satiation, not by a fixed bite
        public const int MealThirstRecovery = 40; // the meal includes a drink (no water sim yet)
        // W34: the night window [22,06) lives HERE and is read ONLY via SleepOperations.IsNightHour
        // (single-predicate rule); the fiat NightSleepFatigueRecovery(40) died into
        // SleepAdvancer's 2-points-per-3-ticks ladder — the same hourly rate, tick-granular.
        public const int NightStartHour = 22;
        public const int NightEndHour = 6;

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

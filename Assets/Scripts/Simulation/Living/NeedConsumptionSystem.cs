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

                // Sleep is intentionally UNLOGGED: a per-actor event every night hour would
                // spam the log (the NeedsStep summary lesson); Gate1 pins the effect instead.
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

        /// <summary>P0 (ARCHITECTURE_GAPS #1): PerTick arrival meals - a hungry civilian who has
        /// REACHED a larder eats immediately instead of standing at the table up to a full game
        /// hour waiting for the Hourly step. The Hourly step remains the metabolism half.</summary>
        public int TickArrivals(WorldState world, EmberCrpg.Domain.Core.GameTime stamp)
        {
            if (world?.Actors == null) return 0;
            int meals = 0;
            // TICKPERF ('EatOnArrival 152s/day and tripling'): the old path ran, PER HUNGRY ACTOR
            // PER TICK, a scan of every pile x every site plus a FoodTags allocation - actors x
            // piles x sites work that grew with the starving crowd until one replayed travel day
            // took minutes. Hoist the per-tick invariants ONCE: the species list and each
            // food-bearing pile's site centre (in stockpile order, so nearest-selection ties
            // break IDENTICALLY). Selection math is unchanged - the chunking-invariance and
            // digest goldens prove the histories stay bit-identical.
            var species = FoodTags(world);
            var cache = BuildFoodPileCache(world, species);
            if (cache.Count == 0) return 0;
            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive) continue;
                if (actor.Role == ActorRole.Player || actor.Role == ActorRole.Enemy) continue;
                if (actor.Needs.Hunger.Value < HungerEatThreshold) continue; // cheap pre-filter
                if (TryEatCached(world, actor, stamp, species, cache)) meals++;
            }
            return meals;
        }

        private readonly struct FoodPileEntry
        {
            public FoodPileEntry(StockpileComponent pile, int cx, int cy, bool hasSite)
            { Pile = pile; CentreX = cx; CentreY = cy; HasSite = hasSite; }
            public readonly StockpileComponent Pile;
            public readonly int CentreX;
            public readonly int CentreY;
            public readonly bool HasSite;
        }

        private static List<FoodPileEntry> BuildFoodPileCache(WorldState world, List<string> species)
        {
            var cache = new List<FoodPileEntry>();
            if (world.Stockpiles == null) return cache;
            foreach (var pile in world.Stockpiles)
            {
                if (pile == null) continue;
                bool hasFood = false;
                foreach (var candidate in species)
                    if (pile.Get(candidate) > 0) { hasFood = true; break; }
                if (!hasFood) continue;
                bool hasSite = false;
                int cx = 0, cy = 0;
                if (world.Sites?.Records != null)
                    foreach (var site in world.Sites.Records)
                        if (site != null && site.Id.Equals(pile.SiteId))
                        {
                            cx = (site.MinBound.X + site.MaxBound.X) / 2;
                            cy = (site.MinBound.Y + site.MaxBound.Y) / 2;
                            hasSite = true;
                            break;
                        }
                cache.Add(new FoodPileEntry(pile, cx, cy, hasSite));
            }
            return cache;
        }

        private bool TryEatCached(WorldState world, ActorRecord actor,
            EmberCrpg.Domain.Core.GameTime stamp, List<string> species, List<FoodPileEntry> cache)
        {
            // Same selection as FindNearestFoodPile: nearest food-bearing pile by Chebyshev to
            // its site centre, siteless piles sort first (dist 0), strict '<' keeps first-wins
            // tie-breaks in stockpile order. Piles drained EARLIER THIS TICK re-verify via Get.
            StockpileComponent best = null;
            string bestTag = null;
            int bestCx = 0, bestCy = 0;
            bool bestHasSite = false;
            long bestDist = long.MaxValue;
            for (int i = 0; i < cache.Count; i++)
            {
                var entry = cache[i];
                string tag = null;
                foreach (var candidate in species)
                    if (entry.Pile.Get(candidate) > 0) { tag = candidate; break; }
                if (tag == null) continue; // drained by an earlier diner this very tick
                long dist = entry.HasSite
                    ? System.Math.Max(System.Math.Abs(actor.Position.X - entry.CentreX),
                                      System.Math.Abs(actor.Position.Y - entry.CentreY))
                    : 0L;
                if (dist < bestDist)
                { bestDist = dist; best = entry.Pile; bestTag = tag; bestCx = entry.CentreX; bestCy = entry.CentreY; bestHasSite = entry.HasSite; }
            }
            if (best == null) return false;
            // WithinReach, against the cached centre: permissive when the pile has no site row.
            if (world.Sites?.Records != null && bestHasSite)
            {
                int dx = System.Math.Abs(actor.Position.X - bestCx);
                int dy = System.Math.Abs(actor.Position.Y - bestCy);
                if (System.Math.Max(dx, dy) > EatReachCells) return false;
            }

            best.Remove(bestTag, 1);
            var fed = actor.Needs
                .WithHunger(new NeedValue(MealHungerFloor))
                .WithThirst(new NeedValue(actor.Needs.Thirst.Value - MealThirstRecovery));
            actor.ApplyNeeds(fed);
            actor.ApplyMood(_moodEvaluator.Evaluate(fed));
            world.Events?.Append(new WorldEvent(
                stamp, WorldEventKind.NeedChanged, actor.Id, best.SiteId,
                $"meal_eaten item:{bestTag} hunger:{fed.Hunger.Value}"));
            return true;
        }

        private bool TryEat(WorldState world, ActorRecord actor, EmberCrpg.Domain.Core.GameTime stamp)
        {
            var pile = FindNearestFoodPile(world, actor.Position, out var foodTag);
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

        /// <summary>All food-holding piles' site centres. Multi-settlement worlds have MANY
        /// larders — routing everyone to the globally-first pile marched whole towns across the
        /// map. Presentation's ScheduleStep feeds this list; actors pick their nearest.</summary>
        public static List<GridPosition> FoodSpots(WorldState world)
        {
            var spots = new List<GridPosition>();
            if (world?.Stockpiles == null || world.Sites?.Records == null) return spots;
            var species = FoodTags(world);
            foreach (var pile in world.Stockpiles)
            {
                if (pile == null) continue;
                bool hasFood = false;
                foreach (var tag in species)
                    if (pile.Get(tag) > 0) { hasFood = true; break; }
                if (!hasFood) continue;
                foreach (var site in world.Sites.Records)
                    if (site != null && site.Id.Equals(pile.SiteId))
                    {
                        spots.Add(new GridPosition(
                            (site.MinBound.X + site.MaxBound.X) / 2,
                            (site.MinBound.Y + site.MaxBound.Y) / 2));
                        break;
                    }
            }
            return spots;
        }

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
        private static StockpileComponent FindNearestFoodPile(WorldState world, GridPosition from, out string foodTag)
        {
            foodTag = null;
            if (world.Stockpiles == null) return null;
            var species = FoodTags(world);
            StockpileComponent best = null;
            string bestTag = null;
            long bestDist = long.MaxValue;
            foreach (var pile in world.Stockpiles)
            {
                if (pile == null) continue;
                string tag = null;
                foreach (var candidate in species)
                    if (pile.Get(candidate) > 0) { tag = candidate; break; }
                if (tag == null) continue;

                long dist = 0; // site-less piles sort FIRST (bare test worlds stay permissive)
                if (world.Sites?.Records != null)
                    foreach (var site in world.Sites.Records)
                        if (site != null && site.Id.Equals(pile.SiteId))
                        {
                            int cx = (site.MinBound.X + site.MaxBound.X) / 2;
                            int cy = (site.MinBound.Y + site.MaxBound.Y) / 2;
                            dist = System.Math.Max(System.Math.Abs(from.X - cx), System.Math.Abs(from.Y - cy));
                            break;
                        }
                if (dist < bestDist) { bestDist = dist; best = pile; bestTag = tag; }
            }
            foodTag = bestTag;
            return best;
        }

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

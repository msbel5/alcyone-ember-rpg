using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Living
{
    /// <summary>
    /// P1 ambient life, Hourly. Deterministic and dirt-cheap:
    /// - population: settlements with a stockpile keep up to MaxRats rats and MaxCats cats
    ///   (ids hashed from site+ordinal; spawn cells hashed off the site bounds);
    /// - rats step toward the site LARDER cell (the food-spot centre); within reach they
    ///   STEAL one unit of real stock - the shortage/price chain reacts on its own;
    /// - cats step toward the nearest rat of their site; adjacent, the rat is caught
    ///   (removed + event) - the inn cat earns its keep.
    /// No choreography: every effect is shared world state or the event log.
    /// </summary>
    public sealed class AmbientLifeSystem
    {
        public const int MaxRatsPerSite = 2;
        public const int MaxCatsPerSite = 1;
        public const int StealReach = 1;
        private const ulong CritterIdBase = 900_000_000UL;

        public int Tick(WorldState world, GameTime stamp)
        {
            if (world?.Sites?.Records == null || world.Stockpiles == null || world.Events == null)
                return 0;
            world.Critters ??= new List<AmbientCritter>();
            var foodTags = FoodPileCache.FoodTags(world);
            int happenings = 0;

            foreach (var site in world.Sites.Records)
            {
                if (site == null || site.Kind != SiteKind.Settlement) continue;
                var pile = FindPile(world, site.Id);
                if (pile == null) continue;
                var larder = Centre(site);
                EnsurePopulation(world, site, larder);

                foreach (var critter in world.Critters)
                {
                    if (critter == null || !critter.SiteId.Equals(site.Id)) continue;
                    if (critter.Kind == "rat")
                    {
                        critter.Cell = MovementService.StepToward(critter.Cell, larder, world.NavView);
                        if (critter.Cell.ChebyshevDistanceTo(larder) <= StealReach)
                        {
                            var tag = FirstStockedFoodTag(pile, foodTags);
                            if (tag != null)
                            {
                                var removed = pile.Remove(tag, 1);
                                if (removed <= 0) continue;
                                world.Events.Append(new WorldEvent(stamp, WorldEventKind.VerminTheft,
                                    default, site.Id,
                                    $"vermin_theft item:{tag} qty:{removed} sink:vermin critter:{critter.Id}"));
                                happenings++;
                                critter.Cell = new GridPosition(larder.X + 3, larder.Y + 3); // scurries off
                            }
                        }
                    }
                }

                var cats = new List<AmbientCritter>();
                foreach (var critter in world.Critters)
                    if (critter != null && critter.Kind == "cat" && critter.SiteId.Equals(site.Id))
                        cats.Add(critter);
                foreach (var cat in cats)
                {
                    var prey = NearestRat(world, site.Id, cat.Cell);
                    if (prey == null) { cat.Cell = MovementService.StepToward(cat.Cell, larder, world.NavView); continue; }
                    cat.Cell = MovementService.StepToward(cat.Cell, prey.Cell, world.NavView);
                    if (cat.Cell.ChebyshevDistanceTo(prey.Cell) <= 1)
                    {
                        world.Critters.Remove(prey);
                        world.Events.Append(new WorldEvent(stamp, WorldEventKind.CritterCaught,
                            default, site.Id, $"cat_catch critter:{prey.Id}"));
                        happenings++;
                    }
                }
            }
            return happenings;
        }

        private static void EnsurePopulation(WorldState world, SiteRecord site, GridPosition larder)
        {
            int rats = 0, cats = 0;
            foreach (var critter in world.Critters)
            {
                if (critter == null || !critter.SiteId.Equals(site.Id)) continue;
                if (critter.Kind == "rat") rats++; else if (critter.Kind == "cat") cats++;
            }
            for (ulong ordinal = 0; rats < MaxRatsPerSite; ordinal++)
            {
                var candidateId = CritterIdFor(site.Id, ordinal);
                if (ContainsCritter(world, candidateId)) continue;
                world.Critters.Add(Spawn(site, larder, "rat", ordinal));
                rats++;
            }
            for (ulong ordinal = 8; cats < MaxCatsPerSite; ordinal++)
            {
                var candidateId = CritterIdFor(site.Id, ordinal);
                if (ContainsCritter(world, candidateId)) continue;
                world.Critters.Add(Spawn(site, larder, "cat", ordinal));
                cats++;
            }
        }

        private static AmbientCritter Spawn(SiteRecord site, GridPosition larder, string kind, ulong ordinal)
        {
            ulong hash = (site.Id.Value * 2654435761UL) + ordinal * 40503UL + 17UL;
            int dx = (int)(hash % 9UL) - 4;
            int dy = (int)((hash >> 8) % 9UL) - 4;
            if (kind == "cat") { dx = 6; dy = 6 + (int)(ordinal % 3UL); } // cats start OFF the rat ring - no same-tick ambush
            return new AmbientCritter
            {
                Id = CritterIdFor(site.Id, ordinal),
                SiteId = site.Id,
                Cell = new GridPosition(larder.X + dx, larder.Y + dy),
                Kind = kind,
            };
        }

        private static ulong CritterIdFor(SiteId siteId, ulong ordinal)
            => CritterIdBase + siteId.Value * 64UL + ordinal;

        private static bool ContainsCritter(WorldState world, ulong id)
        {
            foreach (var critter in world.Critters)
                if (critter != null && critter.Id == id)
                    return true;
            return false;
        }

        private static Domain.Process.StockpileComponent FindPile(WorldState world, SiteId siteId)
        {
            for (int i = 0; i < world.Stockpiles.Count; i++)
            {
                var pile = world.Stockpiles[i];
                if (pile != null && pile.SiteId.Equals(siteId)) return pile;
            }
            return null;
        }

        private static string FirstStockedFoodTag(
            Domain.Process.StockpileComponent pile,
            IReadOnlyList<string> foodTags)
        {
            if (foodTags == null) return null;
            for (var i = 0; i < foodTags.Count; i++)
                if (pile.Get(foodTags[i]) > 0)
                    return foodTags[i];
            return null;
        }

        private static AmbientCritter NearestRat(WorldState world, SiteId siteId, GridPosition from)
        {
            AmbientCritter best = null;
            int bestDist = int.MaxValue;
            foreach (var critter in world.Critters)
            {
                if (critter == null || critter.Kind != "rat" || !critter.SiteId.Equals(siteId)) continue;
                int dist = from.ChebyshevDistanceTo(critter.Cell);
                if (dist < bestDist) { best = critter; bestDist = dist; }
            }
            return best;
        }

        private static GridPosition Centre(SiteRecord site)
            => new GridPosition((site.MinBound.X + site.MaxBound.X) / 2, (site.MinBound.Y + site.MaxBound.Y) / 2);

        // B10 §A5: the duplicate wall-blind primitive is retired — MovementService.StepToward with
        // world.NavView is the ONE grid stepper. Rats & cats now respect blockers like civilians do.
        // Grid distance measurement lives on GridPosition.ChebyshevDistanceTo — the sole primitive.
    }
}

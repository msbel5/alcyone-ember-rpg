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
            if (world?.Sites?.Records == null || world.Stockpiles == null) return 0;
            world.Critters ??= new List<AmbientCritter>();
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
                        critter.Cell = StepToward(critter.Cell, larder);
                        if (Chebyshev(critter.Cell, larder) <= StealReach)
                        {
                            var tag = FirstStockedTag(pile);
                            if (tag != null)
                            {
                                pile.Remove(tag, 1);
                                world.Events?.Append(new WorldEvent(stamp, WorldEventKind.NeedChanged,
                                    default, site.Id, $"vermin_theft item:{tag} critter:{critter.Id}"));
                                happenings++;
                                critter.Cell = new GridPosition(larder.X + 3, larder.Y + 3); // scurries off
                            }
                        }
                    }
                }

                for (int i = world.Critters.Count - 1; i >= 0; i--)
                {
                    var cat = world.Critters[i];
                    if (cat == null || cat.Kind != "cat" || !cat.SiteId.Equals(site.Id)) continue;
                    var prey = NearestRat(world, site.Id, cat.Cell);
                    if (prey == null) { cat.Cell = StepToward(cat.Cell, larder); continue; }
                    cat.Cell = StepToward(cat.Cell, prey.Cell);
                    if (Chebyshev(cat.Cell, prey.Cell) <= 1)
                    {
                        world.Critters.Remove(prey);
                        world.Events?.Append(new WorldEvent(stamp, WorldEventKind.NeedChanged,
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
            for (int i = rats; i < MaxRatsPerSite; i++)
                world.Critters.Add(Spawn(site, larder, "rat", (ulong)i));
            for (int i = cats; i < MaxCatsPerSite; i++)
                world.Critters.Add(Spawn(site, larder, "cat", 8UL + (ulong)i));
        }

        private static AmbientCritter Spawn(SiteRecord site, GridPosition larder, string kind, ulong ordinal)
        {
            ulong hash = (site.Id.Value * 2654435761UL) + ordinal * 40503UL + 17UL;
            int dx = (int)(hash % 9UL) - 4;
            int dy = (int)((hash >> 8) % 9UL) - 4;
            if (kind == "cat") { dx = 6; dy = 6 + (int)(ordinal % 3UL); } // cats start OFF the rat ring - no same-tick ambush
            return new AmbientCritter
            {
                Id = CritterIdBase + site.Id.Value * 64UL + ordinal,
                SiteId = site.Id,
                Cell = new GridPosition(larder.X + dx, larder.Y + dy),
                Kind = kind,
            };
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

        private static string FirstStockedTag(Domain.Process.StockpileComponent pile)
        {
            foreach (var entry in pile.Entries)
                if (entry.Value > 0) return entry.Key;
            return null;
        }

        private static AmbientCritter NearestRat(WorldState world, SiteId siteId, GridPosition from)
        {
            AmbientCritter best = null;
            int bestDist = int.MaxValue;
            foreach (var critter in world.Critters)
            {
                if (critter == null || critter.Kind != "rat" || !critter.SiteId.Equals(siteId)) continue;
                int dist = Chebyshev(from, critter.Cell);
                if (dist < bestDist) { best = critter; bestDist = dist; }
            }
            return best;
        }

        private static GridPosition Centre(SiteRecord site)
            => new GridPosition((site.MinBound.X + site.MaxBound.X) / 2, (site.MinBound.Y + site.MaxBound.Y) / 2);

        private static GridPosition StepToward(GridPosition from, GridPosition to)
            => new GridPosition(from.X + System.Math.Sign(to.X - from.X), from.Y + System.Math.Sign(to.Y - from.Y));

        private static int Chebyshev(GridPosition a, GridPosition b)
            => System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y));
    }
}

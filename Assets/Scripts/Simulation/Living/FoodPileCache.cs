using System.Collections.Generic;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// W32-02 §3.4: the per-tick food-pile cache extracted from NeedConsumptionSystem so the
// decision layer and any remaining consumers share ONE build (TICKPERF hoist lesson:
// 'EatOnArrival 152s/day' came from rebuilding species x piles x sites per hungry actor).
// Entries keep stockpile order so nearest-selection ties break IDENTICALLY to the old path.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Per-tick snapshot of food-bearing piles with their cached site centres.</summary>
    public static class FoodPileCache
    {
        public readonly struct Entry
        {
            public Entry(StockpileComponent pile, int cx, int cy, bool hasSite)
            { Pile = pile; CentreX = cx; CentreY = cy; HasSite = hasSite; }
            public readonly StockpileComponent Pile;
            public readonly int CentreX;
            public readonly int CentreY;
            public readonly bool HasSite;
        }

        /// <summary>"wheat" is the staple even in plotless worlds; live species extend the menu.</summary>
        public static List<string> FoodTags(WorldState world)
        {
            var tags = new List<string> { "wheat" };
            if (world.Plants == null) return tags;
            foreach (var row in world.Plants.Rows)
                if (row.Value != null && !string.IsNullOrEmpty(row.Value.SpeciesId) && !tags.Contains(row.Value.SpeciesId))
                    tags.Add(row.Value.SpeciesId);
            return tags;
        }

        public static List<Entry> Build(WorldState world, List<string> species)
        {
            var cache = new List<Entry>();
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
                cache.Add(new Entry(pile, cx, cy, hasSite));
            }
            return cache;
        }
    }
}

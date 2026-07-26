using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// W32-03 §6: shared validated lookups for the EAT advancers. Reach constant and site-centre
// truth are IMPORTED from NeedConsumptionSystem so behaviour shifts only by phase timing,
// never by formula drift.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Shared world lookups for the EAT phase machine.</summary>
    internal static class FoodOperations
    {
        public static StockpileComponent FindPile(WorldState world, ulong siteId)
        {
            var piles = world.Stockpiles;
            if (piles == null) return null;
            for (var i = 0; i < piles.Count; i++)
                if (piles[i] != null && piles[i].SiteId.Value == siteId)
                    return piles[i];
            return null;
        }

        /// <summary>Chebyshev to the site centre &lt;= EatReachCells; siteless piles stay permissive
        /// (bare test worlds), same as the retired WithinReach.</summary>
        public static bool WithinEatReach(WorldState world, ActorRecord actor, ulong siteId)
        {
            if (world.Sites?.Records == null) return true;
            if (!NeedConsumptionSystem.TryGetSiteCentre(world, new SiteId(siteId), out var centre))
                return true;
            return actor.Position.ChebyshevDistanceTo(centre) <= NeedConsumptionSystem.EatReachCells;
        }

        /// <summary>The reserved seat is a pure function of (site centre, actor id): a deterministic
        /// commitment with nothing extra to save. Siteless piles ring the default (0,0) centre.</summary>
        public static GridPosition SeatFor(WorldState world, ulong siteId, ActorRecord actor)
        {
            NeedConsumptionSystem.TryGetSiteCentre(world, new SiteId(siteId), out var centre);
            return CommunalSeat.For(centre, (int)(actor.Id.Value % (ulong)CommunalSeat.SeatCount));
        }
    }
}

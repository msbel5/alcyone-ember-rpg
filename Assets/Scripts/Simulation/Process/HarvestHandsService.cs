using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Process
{
    /// <summary>
    /// M6 ("ekinler birden yok oluyor, kimse gelip toplamiyor"): a ripe plot is harvested by
    /// HANDS, not by fiat. The nearest living civilian within reach picks it; with nobody
    /// near, the plot WAITS ripe until someone passes - schedules route villagers past their
    /// fields daily (planting jobs, homes by the belt), so the economy never starves.
    /// </summary>
    public static class HarvestHandsService
    {
        public const int ReachCells = 2;

        /// <summary>Nearest living non-enemy actor within reach of the plot; null = no hands.</summary>
        public static ActorRecord FindHarvester(WorldState world, PlantComponent plant)
        {
            if (world?.Actors == null || plant == null) return null;
            ActorRecord best = null;
            int bestDist = int.MaxValue;
            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive || actor.Role == ActorRole.Enemy) continue;
                int dx = System.Math.Abs(actor.Position.X - plant.Position.X);
                int dy = System.Math.Abs(actor.Position.Y - plant.Position.Y);
                int dist = System.Math.Max(dx, dy);
                if (dist <= ReachCells && dist < bestDist)
                {
                    best = actor;
                    bestDist = dist;
                }
            }
            return best;
        }
    }
}

using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;

// CAN SUYU H1+H3: the first EVENT CASCADE. ShortageDetector existed but nothing consumed its
// output — a shortage was a log line, not a cause. Now: daily sweep runs the detector over
// every stockpile's food tags; a ShortageDetected transition POSTS a planting job to the job
// board (if none pending for that site), which the existing JobAssignmentSystem hands to an
// idle, fed, willing farmer. Scarcity → work → replanting → harvest: the loop answers itself.
namespace EmberCrpg.Simulation.World
{
    /// <summary>Daily: detect food shortages and answer them with planting jobs.</summary>
    public sealed class ShortageResponseSystem
    {
        public const int ShortageThreshold = 4;
        private const ulong RestockJobIdBase = 7_700_000UL;

        /// <summary>Returns jobs posted this sweep (proof metric). STATELESS by design: the step
        /// instance is shared across worlds in the registry — any instance state leaks between
        /// same-seed runs and breaks determinism (the golden caught exactly that). Everything is
        /// derived from the world: dedup via the pending-job guard, JobId from (site, day).</summary>
        public int Tick(WorldState world) => Tick(world, world?.Time ?? default);

        // Catchup contract: day index and event stamps come from the boundary stamp.
        public int Tick(WorldState world, GameTime stamp)
        {
            if (world?.Stockpiles == null || world.Events == null || world.Jobs == null) return 0;

            var tags = FoodTags(world);
            ulong dayIndex = (ulong)(stamp.TotalMinutes / 1440L);
            int posted = 0;
            foreach (var pile in world.Stockpiles)
            {
                if (pile == null) continue;
                foreach (var tag in tags)
                {
                    if (pile.Get(tag) >= ShortageThreshold) continue;
                    if (HasPendingPlanting(world, pile.SiteId)) continue;

                    world.Events.Append(new WorldEvent(
                        stamp, WorldEventKind.ShortageDetected, default, pile.SiteId,
                        $"shortage item:{tag} stock:{pile.Get(tag)} threshold:{ShortageThreshold}"));

                    var jobId = new JobId(RestockJobIdBase + pile.SiteId.Value * 512UL + (dayIndex % 512UL));
                    if (world.Jobs.Contains(jobId)) continue; // same-day repost guard
                    var requester = FirstCivilianId(world);
                    if (requester.IsEmpty) continue; // nobody left to want food — the colony is gone
                    var field = FieldPositionFor(world, pile.SiteId);
                    var job = FarmingJobRequestFactory.CreatePlantingJob(
                        jobId, pile.SiteId, field, requester, JobPriority.Active(1), quantity: 1);
                    world.Jobs.Add(job);
                    posted++;
                    world.Events.Append(new WorldEvent(
                        stamp, WorldEventKind.JobAssigned, default, pile.SiteId,
                        "restock_job_posted reason:shortage"));
                }
            }
            return posted;
        }

        private static EmberCrpg.Domain.Core.ActorId FirstCivilianId(WorldState world)
        {
            foreach (var actor in world.Actors.Records)
                if (actor != null && actor.IsAlive
                    && actor.Role != EmberCrpg.Domain.Actors.ActorRole.Player
                    && actor.Role != EmberCrpg.Domain.Actors.ActorRole.Enemy)
                    return actor.Id;
            return default;
        }

        private static bool HasPendingPlanting(WorldState world, SiteId siteId)
        {
            foreach (var job in world.Jobs.Requests)
                if (job != null && job.SiteId.Equals(siteId)
                    && job.RecipeId.Equals(FarmingJobRequestFactory.PlantCropRecipeId))
                    return true;
            return false;
        }

        private static EmberCrpg.Domain.Actors.GridPosition FieldPositionFor(WorldState world, SiteId siteId)
        {
            // Plant a new plot beside an existing plant of the site (or the site itself is fine —
            // the field position is advisory for the walk target).
            foreach (var row in world.Plants.Rows)
                if (row.Value != null && row.Value.SiteId.Equals(siteId))
                    return row.Value.Position;
            return default;
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

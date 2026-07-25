using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// W34 WORK slice (docs/ruh/w34/02 §7): FoodOperations/FarmOperations' sibling — the shared
// validation ladder for the WORK phase machine (claim, worksite, bench IO) plus the ONE home
// of the JobCompleted completion grammar. CONSTRAINT (verbatim grammar): CompleteJob is
// PlantSeedAdvancer.CompleteJob MOVED here, not rewritten — chronicle/proof/quest consumers
// read the JobCompleted reason trace unchanged. CONSTRAINT: no reservation rows anywhere in
// the WORK chain — the JobBoard claim IS the lock (§3), so these lookups never touch the ledger.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Shared world lookups + job-completion grammar for the WORK phase machine.</summary>
    internal static class WorkOperations
    {
        /// <summary>Chebyshev reach for working a bench — the bench cell is occupied furniture,
        /// so arrival is ADJACENCY, not the cell itself (PlantSeed's ≤1 work gate precedent).</summary>
        public const int WorkReachCells = 1;

        /// <summary>The actor's live claim: job exists AND is claimed by THIS actor. False on
        /// every sweep/cancel/steal race — the caller fails the chain as JobLost.</summary>
        public static bool TryGetClaim(WorldState world, ActorRecord actor, out JobRequest request)
        {
            request = null;
            var jobId = actor.ScheduleState.CurrentJobId;
            if (jobId.IsEmpty || world.Jobs == null) return false;
            if (!world.Jobs.TryGet(jobId, out request)) return false;
            return world.Jobs.GetClaimedBy(jobId) == actor.Id;
        }

        /// <summary>The job's bench: registered, active, and of the requested kind. False =
        /// the forge went cold — the caller fails the chain as WorksiteGone.</summary>
        public static bool TryGetWorksite(WorldState world, JobRequest request, out WorksiteRecord worksite)
        {
            worksite = null;
            if (world.Worksites == null) return false;
            if (!world.Worksites.TryGet(request.SiteId, request.WorksitePosition, out worksite)) return false;
            return worksite.IsActive && worksite.Kind == request.WorksiteKind;
        }

        /// <summary>The site pile as recipe IO (find-or-create, FarmOperations.FindOrCreatePile
        /// seam) — the W33 B06 bridge: production eats from and fills the SITE's real container.
        /// Null only for an empty SiteId (bare test worlds; jobs always carry a real site).</summary>
        public static IRecipeInventory SiteIo(WorldState world, SiteId siteId)
        {
            var pile = FarmOperations.FindOrCreatePile(world, siteId);
            return pile == null ? null : new StockpileRecipeInventory(pile);
        }

        /// <summary>"İş ancak eylem zinciri biterse biter" — JobCompleted grammar VERBATIM from
        /// the retired remote strip (JobAssignmentSystem.Tick.cs) so chronicle/proof consumers
        /// keep reading unchanged. Moved here from PlantSeedAdvancer (W34 §7); PlantSeed delegates.</summary>
        public static void CompleteJob(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var jobId = actor.ScheduleState.CurrentJobId;
            if (jobId.IsEmpty || world.Jobs == null || !world.Jobs.TryGet(jobId, out var request))
                return;
            world.Jobs.Complete(jobId);
            world.Events?.Append(new WorldEvent(
                stamp,
                WorldEventKind.JobCompleted,
                actor.Id,
                request.SiteId,
                $"job_completed:{request.Id.Value}",
                new ReasonTrace(new[]
                {
                    $"job:{request.Id.Value}",
                    $"recipe:{request.RecipeId.Value}",
                    $"quantity:{request.Quantity}",
                    $"worksite:{request.WorksiteKind}",
                })));
            actor.ApplyScheduleState(ActorScheduleState.Idle);
        }
    }
}

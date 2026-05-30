using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;

// Design note:
// FarmingJobRequestFactory is the narrow Phase 5 bridge from farming components
// into the existing JobBoard / JobAssignmentSystem lane. It does not run
// planting or harvest logic; it creates typed jobs that point actors at field
// worksite cells so later atoms can execute the claimed work.
namespace EmberCrpg.Simulation.Process
{
    /// <summary>Creates deterministic planting and harvest job requests for field worksites.</summary>
    public static class FarmingJobRequestFactory
    {
        public static readonly RecipeId PlantCropRecipeId = new RecipeId(5101UL);
        public static readonly RecipeId HarvestCropRecipeId = new RecipeId(5102UL);

        public static JobRequest CreatePlantingJob(
            JobId jobId,
            SiteId siteId,
            GridPosition fieldPosition,
            ActorId requesterId,
            JobPriority priority,
            int quantity = 1)
        {
            return Create(jobId, PlantCropRecipeId, siteId, fieldPosition, requesterId, priority, quantity);
        }

        public static JobRequest CreateHarvestJob(
            JobId jobId,
            SiteId siteId,
            GridPosition fieldPosition,
            ActorId requesterId,
            JobPriority priority,
            int quantity = 1)
        {
            return Create(jobId, HarvestCropRecipeId, siteId, fieldPosition, requesterId, priority, quantity);
        }

        private static JobRequest Create(
            JobId jobId,
            RecipeId recipeId,
            SiteId siteId,
            GridPosition fieldPosition,
            ActorId requesterId,
            JobPriority priority,
            int quantity)
        {
            if (priority.Equals(default(JobPriority)))
                throw new ArgumentException("Farming job priority must be active.", nameof(priority));

            return new JobRequest(
                jobId,
                recipeId,
                siteId,
                fieldPosition,
                WorksiteKind.Field,
                JobKind.Farmer,
                priority,
                quantity,
                requesterId);
        }
    }
}

using System;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Simulation.World;

namespace EmberCrpg.Simulation.Process
{
    /// <summary>
    /// Codex audit (seventh pass B-P1 #9, #10): the Data.SliceJson asmdef used
    /// to reference EmberCrpg.Simulation so WorldSaveMapper could construct
    /// a <see cref="RecipeWorkOrder"/> and a seed <see cref="WorldState"/>
    /// via <see cref="WorldFactory"/>. That bled Simulation types into
    /// the Data layer, violating the layering bible. This helper moves those
    /// Simulation-coupled rehydration steps over to the Simulation layer
    /// where they belong. Data-side mappers now hold only pure DTO &lt;-&gt;
    /// Domain conversions; callers (JsonSliceSaveService in Presentation)
    /// compose both halves.
    /// </summary>
    public static class WorldSaveRehydration
    {
        /// <summary>
        /// Project a Simulation-side RecipeWorkOrder into its pure-Data DTO.
        /// Moved out of WorldSaveMapper so the Data asmdef no longer leaks
        /// Simulation namespaces.
        /// </summary>
        public static RecipeWorkOrderSaveData ToRecipeWorkOrderData(RecipeWorkOrder order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            return new RecipeWorkOrderSaveData
            {
                recipeId = (long)order.Recipe.Id.Value,
                siteId = (long)order.SiteId.Value,
                positionX = order.Position.X,
                positionY = order.Position.Y,
                actorId = (long)order.ActorId.Value,
                progressTicks = order.ProgressTicks,
            };
        }

        /// <summary>
        /// Rehydrate a deterministic recipe work order from its save DTO.
        /// Throws when the recipe id is unknown to the supplied resolver,
        /// mirroring the previous WorldSaveMapper behaviour.
        /// </summary>
        public static RecipeWorkOrder ToRecipeWorkOrder(
            RecipeWorkOrderSaveData data,
            Func<RecipeId, RecipeDef> resolveRecipe)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (resolveRecipe == null)
                throw new ArgumentNullException(nameof(resolveRecipe));

            var recipeId = new RecipeId((ulong)data.recipeId);
            var recipe = resolveRecipe(recipeId);
            if (recipe == null)
                throw new InvalidOperationException(
                    $"RecipeWorkOrder save data references unknown recipe {recipeId}.");

            return RecipeWorkOrder.Resume(
                recipe,
                new SiteId((ulong)data.siteId),
                new EmberCrpg.Domain.Actors.GridPosition(data.positionX, data.positionY),
                new ActorId((ulong)data.actorId),
                data.progressTicks);
        }

        /// <summary>
        /// Build the seed <see cref="EmberCrpg.Domain.World.WorldState"/>
        /// used by JsonSliceSaveService.LoadFromJson before pure-mapping
        /// WorldSaveData onto it. Centralising here means the Simulation-side
        /// factory choice (WorldFactory) stays out of the Data asmdef.
        /// </summary>
        public static EmberCrpg.Domain.World.WorldState CreateSeedWorld(int roomSeed)
        {
            return new WorldFactory().Create(roomSeed);
        }
    }
}

using System.Collections.Generic;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class CraftingRecipeRow
    {
        public CraftingRecipeRow(
            string recipeId,
            string name,
            string station,
            string ingredientSummary,
            string outputSummary,
            string availabilityLabel,
            bool canCraft)
        {
            RecipeId = recipeId ?? string.Empty;
            Name = name ?? string.Empty;
            Station = station ?? string.Empty;
            IngredientSummary = ingredientSummary ?? string.Empty;
            OutputSummary = outputSummary ?? string.Empty;
            AvailabilityLabel = availabilityLabel ?? string.Empty;
            CanCraft = canCraft;
        }

        public string RecipeId { get; }
        public string Name { get; }
        public string Station { get; }
        public string IngredientSummary { get; }
        public string OutputSummary { get; }
        public string AvailabilityLabel { get; }
        public bool CanCraft { get; }
    }

    public sealed class CraftingLedgerState
    {
        public CraftingLedgerState(string stationName, IReadOnlyList<CraftingRecipeRow> recipes)
        {
            StationName = stationName ?? string.Empty;
            Recipes = recipes ?? System.Array.Empty<CraftingRecipeRow>();
        }

        public string StationName { get; }
        public IReadOnlyList<CraftingRecipeRow> Recipes { get; }
    }

    public sealed class CraftingActionResult
    {
        public CraftingActionResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public string Message { get; }
    }

    public interface ICraftingSource
    {
        CraftingLedgerState ReadCraftingState();
    }

    public interface ICraftingCommandSink
    {
        CraftingActionResult ExecuteCraft(string recipeId);
    }
}

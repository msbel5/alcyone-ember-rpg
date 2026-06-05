namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class CraftingRecipeData
    {
        public CraftingRecipeData(
            string recipeId,
            string name,
            string station,
            string ingredients,
            string outputs,
            string availability,
            bool canCraft)
        {
            RecipeId = recipeId ?? string.Empty;
            Name = name ?? string.Empty;
            Station = station ?? string.Empty;
            Ingredients = ingredients ?? string.Empty;
            Outputs = outputs ?? string.Empty;
            Availability = availability ?? string.Empty;
            CanCraft = canCraft;
        }

        public string RecipeId { get; }
        public string Name { get; }
        public string Station { get; }
        public string Ingredients { get; }
        public string Outputs { get; }
        public string Availability { get; }
        public bool CanCraft { get; }
    }

    public sealed class CraftingScreenData
    {
        public CraftingScreenData(string stationName, string statusLine, CraftingRecipeData[] recipes)
        {
            StationName = stationName ?? string.Empty;
            StatusLine = statusLine ?? string.Empty;
            Recipes = recipes ?? System.Array.Empty<CraftingRecipeData>();
        }

        public string StationName { get; }
        public string StatusLine { get; }
        public CraftingRecipeData[] Recipes { get; }
    }

    public static class IgCraftingData
    {
        public static readonly CraftingScreenData Default = new CraftingScreenData(
            "No Workstation",
            "No recipes are available here yet.",
            System.Array.Empty<CraftingRecipeData>());

        public static CraftingScreenData Current = Default;
    }
}

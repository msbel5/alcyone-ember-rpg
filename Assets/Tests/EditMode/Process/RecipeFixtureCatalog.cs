using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;

// Design note:
// RecipeFixtureCatalog is the Phase 3 competition-proof fixture rail. It keeps
// the canonical PROCESS/MATTER recipe shapes in one place so job-assignment
// tests can compare smelting and baking without copying recipe rows.
// Atom-map ref: docs/sprint-phase-3-atom-map.md competition-proof bundle.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Canonical recipe fixtures consumed by Phase 3 job assignment tests.</summary>
    public static class RecipeFixtureCatalog
    {
        /// <summary>Phase 2/Phase 3 furnace recipe: 2 iron ore + 1 fuel -> 1 iron ingot.</summary>
        public static RecipeDef SmeltIronIngot(RecipeId recipeId)
        {
            return new RecipeDef(
                recipeId,
                "furnace",
                "smithing",
                durationTicks: 2,
                new[] { new RecipeIngredient("iron_ore", 2), new RecipeIngredient("fuel", 1) },
                new[] { new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1) });
        }

        /// <summary>Phase 3 second recipe fixture: 2 grain + 1 fuel -> 1 bread.</summary>
        public static RecipeDef BakeBread(RecipeId recipeId)
        {
            return new RecipeDef(
                recipeId,
                "bakery",
                "baking",
                durationTicks: 3,
                new[] { new RecipeIngredient("grain", 2), new RecipeIngredient("fuel", 1) },
                new[] { new RecipeOutput("bread", ItemMaterial.Food, ItemQuality.Common, 1) });
        }
    }
}

using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;

namespace EmberCrpg.Data.Recipes
{
    /// <summary>
    /// Production recipe data rows. The two seed recipes (smelt iron ingot,
    /// bake bread) live here as concrete rows so the game can compete jobs at
    /// runtime without depending on test fixtures.
    /// Closes CO-07 in docs/sprint-phase-4-atom-map.md Debt ledger.
    /// </summary>
    public static class ProductionRecipeRegistry
    {
        public static readonly RecipeId SmeltIronIngotId = new RecipeId(1001UL);
        public static readonly RecipeId BakeBreadId = new RecipeId(1002UL);

        public static RecipeDef SmeltIronIngot()
        {
            return new RecipeDef(
                SmeltIronIngotId,
                "furnace",
                "smithing",
                durationTicks: 2,
                new[] { new RecipeIngredient("iron_ore", 2), new RecipeIngredient("fuel", 1) },
                new[] { new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1) });
        }

        public static RecipeDef BakeBread()
        {
            return new RecipeDef(
                BakeBreadId,
                "bakery",
                "baking",
                durationTicks: 3,
                new[] { new RecipeIngredient("grain", 2), new RecipeIngredient("fuel", 1) },
                new[] { new RecipeOutput("bread", ItemMaterial.Food, ItemQuality.Common, 1) });
        }

        /// <summary>Returns every production recipe in stable id order.</summary>
        public static IReadOnlyList<RecipeDef> AllRecipes()
        {
            return new[] { SmeltIronIngot(), BakeBread() };
        }

        /// <summary>Resolves a production recipe by id. Throws when unknown.</summary>
        public static RecipeDef Resolve(RecipeId id)
        {
            if (id.Equals(SmeltIronIngotId)) return SmeltIronIngot();
            if (id.Equals(BakeBreadId)) return BakeBread();
            throw new KeyNotFoundException($"ProductionRecipeRegistry has no recipe for {id}.");
        }
    }
}

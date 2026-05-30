using System;
using EmberCrpg.Domain.Inventory;

// Design note:
// RecipeOutput is the MATTER/PROCESS counterpart to RecipeIngredient. It is a
// pure recipe-definition row: produced item/material tag, material, quality,
// and strictly positive quantity. It does not mutate ItemStore, allocate item
// ids, advance work, or emit events; RecipeDef and RecipeSystem compose it in
// later Phase 2 atoms.
// Atom-map ref: docs/sprint-phase-2-atom-map.md Recipe definitions sub-area.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Pure output row describing an item/material result produced by a recipe.
    /// </summary>
    public sealed class RecipeOutput
    {
        public RecipeOutput(string itemTag, ItemMaterial material, ItemQuality quality, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Recipe output item tag is required.", nameof(itemTag));
            if (material == ItemMaterial.None)
                throw new ArgumentException("Recipe output material cannot be None.", nameof(material));
            if (quality == ItemQuality.None)
                throw new ArgumentException("Recipe output quality cannot be None.", nameof(quality));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Recipe output quantity must be positive.");

            ItemTag = itemTag.Trim();
            Material = material;
            Quality = quality;
            Quantity = quantity;
        }

        /// <summary>
        /// Deterministic item/material tag produced by the recipe, for example "iron_ingot".
        /// </summary>
        public string ItemTag { get; }

        /// <summary>
        /// Material assigned to produced item records when RecipeSystem instantiates outputs.
        /// </summary>
        public ItemMaterial Material { get; }

        /// <summary>
        /// Craft quality assigned to produced item records when RecipeSystem instantiates outputs.
        /// </summary>
        public ItemQuality Quality { get; }

        /// <summary>
        /// Positive number of matching items produced by the recipe.
        /// </summary>
        public int Quantity { get; }
    }
}

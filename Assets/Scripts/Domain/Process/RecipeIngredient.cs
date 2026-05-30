using System;

// Design note:
// RecipeIngredient is the first pure MATTER/PROCESS row for Phase 2 recipes. It
// describes a required stockpile input by deterministic item/material tag and
// quantity, without consuming inventory, reading stores, or ticking time. Later
// RecipeDef and RecipeSystem atoms compose these rows into smelting behaviour.
// Atom-map ref: docs/sprint-phase-2-atom-map.md Recipe definitions sub-area.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Pure input row describing an item/material tag and required quantity.
    /// </summary>
    public sealed class RecipeIngredient
    {
        public RecipeIngredient(string itemTag, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new ArgumentException("Recipe ingredient item tag is required.", nameof(itemTag));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Recipe ingredient quantity must be positive.");

            ItemTag = itemTag.Trim();
            Quantity = quantity;
        }

        /// <summary>
        /// Deterministic item/material tag required by the recipe, for example "iron_ore" or "fuel".
        /// </summary>
        public string ItemTag { get; }

        /// <summary>
        /// Positive number of matching stockpile items required by the recipe.
        /// </summary>
        public int Quantity { get; }
    }
}

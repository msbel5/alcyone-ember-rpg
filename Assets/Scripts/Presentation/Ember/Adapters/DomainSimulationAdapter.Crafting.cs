using System;
using System.Collections.Generic;
using EmberCrpg.Data.Content;
using EmberCrpg.Data.Recipes;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.Inventory;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter : ICraftingSource, ICraftingCommandSink
    {
        private readonly SettlementCraftingService _crafting = new SettlementCraftingService();

        public CraftingLedgerState ReadCraftingState()
        {
            var rows = new List<CraftingRecipeRow>();
            var recipes = ProductionRecipeRegistry.AllRecipes();
            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                rows.Add(new CraftingRecipeRow(
                    recipe.Id.Value.ToString(),
                    ResolveRecipeName(recipe),
                    recipe.WorksiteKind,
                    BuildIngredientSummary(recipe),
                    BuildOutputSummary(recipe),
                    _crafting.CanCraft(recipe, _world.PlayerInventory) ? "Ready" : BuildAvailabilityLabel(recipe),
                    _crafting.CanCraft(recipe, _world.PlayerInventory)));
            }

            return new CraftingLedgerState(ResolveStartingSettlementName() ?? "Frontier Workshop", rows);
        }

        public CraftingActionResult ExecuteCraft(string recipeId)
        {
            if (!TryResolveRecipe(recipeId, out var recipe))
                return new CraftingActionResult(false, "Unknown recipe.");

            var player = _world.Actors?.FirstByRole(ActorRole.Player);
            var crafterId = player != null ? player.Id : new ActorId(1UL);
            return _crafting.TryCraft(_world, recipe, crafterId, ResolveItemName, out var message)
                ? new CraftingActionResult(true, message)
                : new CraftingActionResult(false, message);
        }

        private static bool TryResolveRecipe(string recipeId, out RecipeDef recipe)
        {
            recipe = null;
            if (!ulong.TryParse(recipeId, out var id))
                return false;

            try
            {
                recipe = ProductionRecipeRegistry.Resolve(new RecipeId(id));
                return true;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        private string ResolveRecipeName(RecipeDef recipe)
        {
            if (recipe.Id.Equals(ProductionRecipeRegistry.SmeltIronIngotId))
                return "Smelt Iron Ingot";
            if (recipe.Id.Equals(ProductionRecipeRegistry.BakeBreadId))
                return "Bake Bread";
            return Humanize(recipe.WorksiteKind);
        }

        private string BuildIngredientSummary(RecipeDef recipe)
        {
            var parts = new List<string>(recipe.Inputs.Count);
            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                var input = recipe.Inputs[i];
                parts.Add(ResolveItemName(input.ItemTag) + " x" + input.Quantity);
            }

            return string.Join(" · ", parts);
        }

        private string BuildOutputSummary(RecipeDef recipe)
        {
            var parts = new List<string>(recipe.Outputs.Count);
            for (int i = 0; i < recipe.Outputs.Count; i++)
            {
                var output = recipe.Outputs[i];
                parts.Add(ResolveItemName(output.ItemTag) + " x" + output.Quantity);
            }

            return string.Join(" · ", parts);
        }

        private string BuildAvailabilityLabel(RecipeDef recipe)
        {
            if (_world.PlayerInventory == null)
                return "No inventory";

            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                var input = recipe.Inputs[i];
                int owned = CountQuantity(_world.PlayerInventory, input.ItemTag);
                if (owned < input.Quantity)
                    return "Need " + ResolveItemName(input.ItemTag) + " x" + (input.Quantity - owned);
            }

            return "Missing workstation";
        }

        private string ResolveItemName(string templateId)
        {
            var content = ContentDatabaseProvider.Current;
            if (content.Items.TryGetValue(templateId, out var item) && !string.IsNullOrWhiteSpace(item.name))
                return item.name;
            return Humanize(templateId);
        }

        private static int CountQuantity(InventoryState inventory, string templateId)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(templateId))
                return 0;

            int total = 0;
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                var item = inventory.Items[i];
                if (!item.IsEquipment && string.Equals(item.TemplateId, templateId, StringComparison.Ordinal))
                    total += item.Quantity;
            }

            return total;
        }

        private static string Humanize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var parts = value.Trim().Replace("-", "_").Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                parts[i] = part.Length == 1
                    ? part.ToUpperInvariant()
                    : char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant();
            }

            return string.Join(" ", parts);
        }
    }
}

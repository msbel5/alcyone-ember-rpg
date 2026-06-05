using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;

namespace EmberCrpg.Simulation.Inventory
{
    public sealed class SettlementCraftingService
    {
        private readonly RecipeSystem _recipeSystem = new RecipeSystem();

        public bool CanCraft(RecipeDef recipe, InventoryState inventory)
        {
            if (recipe == null || inventory == null)
                return false;

            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                var input = recipe.Inputs[i];
                if (CountQuantity(inventory, input.ItemTag) < input.Quantity)
                    return false;
            }

            return true;
        }

        public bool TryCraft(
            WorldState world,
            RecipeDef recipe,
            ActorId actorId,
            Func<string, string> displayNameResolver,
            out string message)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (displayNameResolver == null) throw new ArgumentNullException(nameof(displayNameResolver));

            world.EnsureInvariants();
            if (world.PlayerInventory == null)
            {
                message = "No player inventory is available.";
                return false;
            }

            if (!TryFindWorksite(world.Worksites, recipe.WorksiteKind, out var worksite))
            {
                message = "No matching workstation is available.";
                return false;
            }

            if (!_recipeSystem.TryStart(recipe, world.Worksites, worksite.SiteId, worksite.Position, world.PlayerInventory, actorId, out var order))
            {
                message = BuildMissingInputs(recipe, world.PlayerInventory, displayNameResolver);
                return false;
            }

            ulong nextItemId = NextInventoryItemId(world.PlayerInventory);
            world.Events ??= new WorldEventLog();
            while (!order.IsComplete)
            {
                _recipeSystem.Tick(
                    order,
                    world.PlayerInventory,
                    world.Events,
                    output => new InventoryItem(
                        new ItemId(nextItemId++),
                        output.ItemTag,
                        displayNameResolver(output.ItemTag),
                        1));
            }

            message = "Crafted " + BuildOutputSummary(recipe, displayNameResolver) + ".";
            return true;
        }

        private static string BuildMissingInputs(RecipeDef recipe, InventoryState inventory, Func<string, string> displayNameResolver)
        {
            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                var input = recipe.Inputs[i];
                int owned = CountQuantity(inventory, input.ItemTag);
                if (owned < input.Quantity)
                    return "Missing " + displayNameResolver(input.ItemTag) + " x" + (input.Quantity - owned) + ".";
            }

            return "The recipe cannot start.";
        }

        private static string BuildOutputSummary(RecipeDef recipe, Func<string, string> displayNameResolver)
        {
            if (recipe.Outputs.Count == 0)
                return "output";

            var output = recipe.Outputs[0];
            var label = displayNameResolver(output.ItemTag);
            return output.Quantity > 1 ? label + " x" + output.Quantity : label;
        }

        private static bool TryFindWorksite(WorksiteStore worksites, string worksiteKind, out WorksiteRecord record)
        {
            record = null;
            if (worksites == null || string.IsNullOrWhiteSpace(worksiteKind))
                return false;

            foreach (var row in worksites.Records)
            {
                if (!row.IsActive)
                    continue;
                if (!string.Equals(row.Kind.ToString(), worksiteKind, StringComparison.OrdinalIgnoreCase))
                    continue;
                record = row;
                return true;
            }

            return false;
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

        private static ulong NextInventoryItemId(InventoryState inventory)
        {
            ulong max = 1UL;
            if (inventory == null)
                return max;

            for (int i = 0; i < inventory.Items.Count; i++)
            {
                var value = inventory.Items[i].Id.Value;
                if (value >= max)
                    max = value + 1UL;
            }

            return max;
        }
    }
}

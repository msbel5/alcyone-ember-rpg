using System.Collections.Generic;
using EmberCrpg.Data.Recipes;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Data
{
    /// <summary>
    /// Pins production recipe data rows shipped from non-test code.
    /// Closes CO-07 row in docs/sprint-phase-4-atom-map.md Debt ledger.
    /// </summary>
    public sealed class ProductionRecipeRegistryTests
    {
        [Test]
        public void SmeltIronIngot_HasStableId_AndExpectedIngredients()
        {
            var recipe = ProductionRecipeRegistry.SmeltIronIngot();

            Assert.That(recipe.Id, Is.EqualTo(ProductionRecipeRegistry.SmeltIronIngotId));
            Assert.That(recipe.WorksiteKind, Is.EqualTo("furnace"));
            Assert.That(recipe.DurationTicks, Is.EqualTo(2));
            Assert.That(recipe.Inputs.Count, Is.EqualTo(2));
            Assert.That(recipe.Outputs.Count, Is.EqualTo(1));
            Assert.That(recipe.Outputs[0].ItemTag, Is.EqualTo("iron_ingot"));
        }

        [Test]
        public void BakeBread_HasStableId_AndExpectedIngredients()
        {
            var recipe = ProductionRecipeRegistry.BakeBread();

            Assert.That(recipe.Id, Is.EqualTo(ProductionRecipeRegistry.BakeBreadId));
            Assert.That(recipe.WorksiteKind, Is.EqualTo("bakery"));
            Assert.That(recipe.DurationTicks, Is.EqualTo(3));
            Assert.That(recipe.Inputs.Count, Is.EqualTo(2));
            Assert.That(recipe.Outputs.Count, Is.EqualTo(1));
            Assert.That(recipe.Outputs[0].ItemTag, Is.EqualTo("bread"));
        }

        [Test]
        public void AllRecipes_ReturnsBothInStableOrder()
        {
            var all = ProductionRecipeRegistry.AllRecipes();

            Assert.That(all.Count, Is.EqualTo(2));
            Assert.That(all[0].Id, Is.EqualTo(ProductionRecipeRegistry.SmeltIronIngotId));
            Assert.That(all[1].Id, Is.EqualTo(ProductionRecipeRegistry.BakeBreadId));
        }

        [Test]
        public void Resolve_KnownId_ReturnsRow()
        {
            Assert.That(ProductionRecipeRegistry.Resolve(ProductionRecipeRegistry.BakeBreadId).WorksiteKind, Is.EqualTo("bakery"));
            Assert.That(ProductionRecipeRegistry.Resolve(ProductionRecipeRegistry.SmeltIronIngotId).WorksiteKind, Is.EqualTo("furnace"));
        }

        [Test]
        public void Resolve_UnknownId_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() =>
                ProductionRecipeRegistry.Resolve(new RecipeId(99999UL)));
        }
    }
}

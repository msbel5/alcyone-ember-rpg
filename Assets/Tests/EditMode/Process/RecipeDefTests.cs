using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin RecipeDef as the last pure recipe-definition atom before the
// runtime RecipeSystem slice. They avoid inventory mutation and EventLog writes;
// the next visible PROCESS increment owns those behaviours.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>
    /// Verifies the pure-Domain invariants required by RecipeDef.
    /// </summary>
    public sealed class RecipeDefTests
    {
        /// <summary>
        /// Constructor stores the recipe identity, worksite, skill, duration, inputs, and outputs.
        /// </summary>
        [Test]
        public void Constructor_StoresRecipeShape()
        {
            var input = new RecipeIngredient("iron_ore", 2);
            var output = new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1);

            var recipe = new RecipeDef(new RecipeId(7UL), "furnace", "smelting", 40, new[] { input }, new[] { output });

            Assert.That(recipe.Id, Is.EqualTo(new RecipeId(7UL)));
            Assert.That(recipe.WorksiteKind, Is.EqualTo("furnace"));
            Assert.That(recipe.SkillTag, Is.EqualTo("smelting"));
            Assert.That(recipe.DurationTicks, Is.EqualTo(40));
            Assert.That(recipe.Inputs, Is.EqualTo(new[] { input }));
            Assert.That(recipe.Outputs, Is.EqualTo(new[] { output }));
        }

        /// <summary>
        /// Worksite and skill tags are normalized at the edge so matching is not whitespace-sensitive.
        /// </summary>
        [Test]
        public void Constructor_TrimsWorksiteAndSkillTags()
        {
            var recipe = CreateRecipe(worksiteKind: "  furnace  ", skillTag: "  smelting  ");

            Assert.That(recipe.WorksiteKind, Is.EqualTo("furnace"));
            Assert.That(recipe.SkillTag, Is.EqualTo("smelting"));
        }

        /// <summary>
        /// Empty recipe ids are rejected because registry/save references need stable handles.
        /// </summary>
        [Test]
        public void Constructor_RejectsEmptyRecipeId()
        {
            Assert.Throws<ArgumentException>(() => CreateRecipe(id: default(RecipeId)));
        }

        /// <summary>
        /// Blank worksite and skill tags are rejected because RecipeSystem needs deterministic keys.
        /// </summary>
        [Test]
        public void Constructor_RejectsBlankWorksiteOrSkillTags()
        {
            Assert.Throws<ArgumentException>(() => CreateRecipe(worksiteKind: null));
            Assert.Throws<ArgumentException>(() => CreateRecipe(worksiteKind: ""));
            Assert.Throws<ArgumentException>(() => CreateRecipe(worksiteKind: "   "));
            Assert.Throws<ArgumentException>(() => CreateRecipe(skillTag: null));
            Assert.Throws<ArgumentException>(() => CreateRecipe(skillTag: ""));
            Assert.Throws<ArgumentException>(() => CreateRecipe(skillTag: "   "));
        }

        /// <summary>
        /// Duration must be strictly positive and diagnostics preserve the invalid value.
        /// </summary>
        [Test]
        public void Constructor_RejectsNonPositiveDuration()
        {
            var zero = Assert.Throws<ArgumentOutOfRangeException>(() => CreateRecipe(durationTicks: 0));
            var negative = Assert.Throws<ArgumentOutOfRangeException>(() => CreateRecipe(durationTicks: -1));

            Assert.That(zero.ActualValue, Is.EqualTo(0));
            Assert.That(negative.ActualValue, Is.EqualTo(-1));
        }

        /// <summary>
        /// Missing row collections are rejected before RecipeSystem tries to execute an impossible recipe.
        /// </summary>
        [Test]
        public void Constructor_RejectsNullRowCollections()
        {
            Assert.Throws<ArgumentNullException>(() => new RecipeDef(
                new RecipeId(7UL),
                "furnace",
                "smelting",
                40,
                null,
                new[] { new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1) }));
            Assert.Throws<ArgumentNullException>(() => new RecipeDef(
                new RecipeId(7UL),
                "furnace",
                "smelting",
                40,
                new[] { new RecipeIngredient("iron_ore", 2) },
                null));
        }

        /// <summary>
        /// Empty input or output collections are rejected because every recipe transforms something into something.
        /// </summary>
        [Test]
        public void Constructor_RejectsEmptyRowCollections()
        {
            Assert.Throws<ArgumentException>(() => CreateRecipe(inputs: new RecipeIngredient[0]));
            Assert.Throws<ArgumentException>(() => CreateRecipe(outputs: new RecipeOutput[0]));
        }

        /// <summary>
        /// Null input or output rows are rejected so later runtime code can iterate safely.
        /// </summary>
        [Test]
        public void Constructor_RejectsNullRows()
        {
            Assert.Throws<ArgumentException>(() => CreateRecipe(inputs: new RecipeIngredient[] { null }));
            Assert.Throws<ArgumentException>(() => CreateRecipe(outputs: new RecipeOutput[] { null }));
        }

        /// <summary>
        /// Constructor copies row lists so caller-side mutation cannot change a recipe definition.
        /// </summary>
        [Test]
        public void Constructor_DefensivelyCopiesRows()
        {
            var inputs = new List<RecipeIngredient> { new RecipeIngredient("iron_ore", 2) };
            var outputs = new List<RecipeOutput> { new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1) };

            var recipe = CreateRecipe(inputs: inputs, outputs: outputs);
            inputs.Add(new RecipeIngredient("fuel", 1));
            outputs.Add(new RecipeOutput("slag", ItemMaterial.Iron, ItemQuality.Common, 1));

            Assert.That(recipe.Inputs, Has.Count.EqualTo(1));
            Assert.That(recipe.Outputs, Has.Count.EqualTo(1));
            Assert.That(recipe.Inputs[0].ItemTag, Is.EqualTo("iron_ore"));
            Assert.That(recipe.Outputs[0].ItemTag, Is.EqualTo("iron_ingot"));
        }

        /// <summary>
        /// Exposed row collections are read-only projections over the recipe definition.
        /// </summary>
        [Test]
        public void RowCollections_AreReadOnly()
        {
            var recipe = CreateRecipe();

            Assert.That(recipe.Inputs, Is.InstanceOf<IReadOnlyList<RecipeIngredient>>());
            Assert.That(recipe.Outputs, Is.InstanceOf<IReadOnlyList<RecipeOutput>>());
            Assert.Throws<NotSupportedException>(() => ((IList<RecipeIngredient>)recipe.Inputs).Add(new RecipeIngredient("fuel", 1)));
            Assert.Throws<NotSupportedException>(() => ((IList<RecipeOutput>)recipe.Outputs).Add(new RecipeOutput("slag", ItemMaterial.Iron, ItemQuality.Common, 1)));
        }

        /// <summary>
        /// The first canonical Phase 2 recipe shape is pinned before RecipeSystem consumes it.
        /// </summary>
        [Test]
        public void SmeltIronIngotShape_IsPinned()
        {
            var recipe = new RecipeDef(
                new RecipeId(1001UL),
                "furnace",
                "smelting",
                40,
                new[]
                {
                    new RecipeIngredient("iron_ore", 2),
                    new RecipeIngredient("fuel", 1),
                },
                new[]
                {
                    new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1),
                });

            Assert.That(recipe.Id, Is.EqualTo(new RecipeId(1001UL)));
            Assert.That(recipe.WorksiteKind, Is.EqualTo("furnace"));
            Assert.That(recipe.SkillTag, Is.EqualTo("smelting"));
            Assert.That(recipe.DurationTicks, Is.EqualTo(40));
            Assert.That(recipe.Inputs, Has.Count.EqualTo(2));
            Assert.That(recipe.Inputs[0].ItemTag, Is.EqualTo("iron_ore"));
            Assert.That(recipe.Inputs[0].Quantity, Is.EqualTo(2));
            Assert.That(recipe.Inputs[1].ItemTag, Is.EqualTo("fuel"));
            Assert.That(recipe.Inputs[1].Quantity, Is.EqualTo(1));
            Assert.That(recipe.Outputs, Has.Count.EqualTo(1));
            Assert.That(recipe.Outputs[0].ItemTag, Is.EqualTo("iron_ingot"));
            Assert.That(recipe.Outputs[0].Material, Is.EqualTo(ItemMaterial.Iron));
            Assert.That(recipe.Outputs[0].Quality, Is.EqualTo(ItemQuality.Common));
            Assert.That(recipe.Outputs[0].Quantity, Is.EqualTo(1));
        }

        private static RecipeDef CreateRecipe(
            RecipeId? id = null,
            string worksiteKind = "furnace",
            string skillTag = "smelting",
            int durationTicks = 40,
            IEnumerable<RecipeIngredient> inputs = null,
            IEnumerable<RecipeOutput> outputs = null)
        {
            return new RecipeDef(
                id ?? new RecipeId(7UL),
                worksiteKind,
                skillTag,
                durationTicks,
                inputs ?? new[] { new RecipeIngredient("iron_ore", 2) },
                outputs ?? new[] { new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1) });
        }
    }
}

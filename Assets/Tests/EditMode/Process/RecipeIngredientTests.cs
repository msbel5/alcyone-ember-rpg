using System;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin the pure recipe-input row before RecipeDef and RecipeSystem
// consume it. They intentionally avoid inventory mutation or EventLog assertions;
// those belong to the later PROCESS/MATTER execution atom.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>
    /// Verifies the pure-Domain invariants required by RecipeIngredient.
    /// </summary>
    public sealed class RecipeIngredientTests
    {
        /// <summary>
        /// Constructor stores the deterministic input tag and required quantity.
        /// </summary>
        [Test]
        public void Constructor_StoresTagAndQuantity()
        {
            var ingredient = new RecipeIngredient("iron_ore", 2);

            Assert.That(ingredient.ItemTag, Is.EqualTo("iron_ore"));
            Assert.That(ingredient.Quantity, Is.EqualTo(2));
        }

        /// <summary>
        /// Tags are normalized at the edge so recipe matching is not whitespace-sensitive.
        /// </summary>
        [Test]
        public void Constructor_TrimsItemTag()
        {
            var ingredient = new RecipeIngredient("  fuel  ", 1);

            Assert.That(ingredient.ItemTag, Is.EqualTo("fuel"));
        }

        /// <summary>
        /// Blank item/material tags are rejected because RecipeSystem needs deterministic keys.
        /// </summary>
        [Test]
        public void Constructor_RejectsBlankItemTag()
        {
            Assert.Throws<ArgumentException>(() => new RecipeIngredient(null, 1));
            Assert.Throws<ArgumentException>(() => new RecipeIngredient("", 1));
            Assert.Throws<ArgumentException>(() => new RecipeIngredient("   ", 1));
        }

        /// <summary>
        /// Quantity must be strictly positive.
        /// </summary>
        [Test]
        public void Constructor_RejectsNonPositiveQuantity()
        {
            var zero = Assert.Throws<ArgumentOutOfRangeException>(() => new RecipeIngredient("iron_ore", 0));
            var negative = Assert.Throws<ArgumentOutOfRangeException>(() => new RecipeIngredient("iron_ore", -1));

            Assert.That(zero.ActualValue, Is.EqualTo(0));
            Assert.That(negative.ActualValue, Is.EqualTo(-1));
        }
    }
}

using System;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin the pure recipe-output row before RecipeDef and RecipeSystem
// consume it. They intentionally avoid ItemStore mutation or EventLog output;
// those belong to later PROCESS/MATTER execution atoms.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>
    /// Verifies the pure-Domain invariants required by RecipeOutput.
    /// </summary>
    public sealed class RecipeOutputTests
    {
        /// <summary>
        /// Constructor stores the deterministic output tag, material, quality, and quantity.
        /// </summary>
        [Test]
        public void Constructor_StoresOutputShape()
        {
            var output = new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1);

            Assert.That(output.ItemTag, Is.EqualTo("iron_ingot"));
            Assert.That(output.Material, Is.EqualTo(ItemMaterial.Iron));
            Assert.That(output.Quality, Is.EqualTo(ItemQuality.Common));
            Assert.That(output.Quantity, Is.EqualTo(1));
        }

        /// <summary>
        /// Tags are normalized at the edge so recipe output matching is not whitespace-sensitive.
        /// </summary>
        [Test]
        public void Constructor_TrimsItemTag()
        {
            var output = new RecipeOutput("  iron_ingot  ", ItemMaterial.Iron, ItemQuality.Common, 1);

            Assert.That(output.ItemTag, Is.EqualTo("iron_ingot"));
        }

        /// <summary>
        /// Blank item/material tags are rejected because RecipeSystem needs deterministic keys.
        /// </summary>
        [Test]
        public void Constructor_RejectsBlankItemTag()
        {
            Assert.Throws<ArgumentException>(() => new RecipeOutput(null, ItemMaterial.Iron, ItemQuality.Common, 1));
            Assert.Throws<ArgumentException>(() => new RecipeOutput("", ItemMaterial.Iron, ItemQuality.Common, 1));
            Assert.Throws<ArgumentException>(() => new RecipeOutput("   ", ItemMaterial.Iron, ItemQuality.Common, 1));
        }

        /// <summary>
        /// Material and quality sentinels are rejected so produced items can become ItemRecord rows later.
        /// </summary>
        [Test]
        public void Constructor_RejectsMaterialOrQualitySentinel()
        {
            Assert.Throws<ArgumentException>(() => new RecipeOutput("iron_ingot", ItemMaterial.None, ItemQuality.Common, 1));
            Assert.Throws<ArgumentException>(() => new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.None, 1));
        }

        /// <summary>
        /// Quantity must be strictly positive and the invalid value is preserved for diagnostics.
        /// </summary>
        [Test]
        public void Constructor_RejectsNonPositiveQuantity()
        {
            var zero = Assert.Throws<ArgumentOutOfRangeException>(() => new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 0));
            var negative = Assert.Throws<ArgumentOutOfRangeException>(() => new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, -1));

            Assert.That(zero.ActualValue, Is.EqualTo(0));
            Assert.That(negative.ActualValue, Is.EqualTo(-1));
        }
    }
}

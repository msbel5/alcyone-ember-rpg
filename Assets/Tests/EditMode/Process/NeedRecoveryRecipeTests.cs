using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin the pure need-recovery recipe shape before the runtime
// eat/sleep system consumes it.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies pure need-recovery recipe invariants.</summary>
    public sealed class NeedRecoveryRecipeTests
    {
        [Test]
        public void Constructor_StoresNormalizedRecoveryShape()
        {
            var recipe = new NeedRecoveryRecipe("  meal-basic  ", "  eat_meal  ", NeedKind.Hunger, 50, "  simple_meal  ");

            Assert.That(recipe.Id, Is.EqualTo("meal-basic"));
            Assert.That(recipe.ActionKind, Is.EqualTo("eat_meal"));
            Assert.That(recipe.NeedKind, Is.EqualTo(NeedKind.Hunger));
            Assert.That(recipe.RecoveryAmount, Is.EqualTo(50));
            Assert.That(recipe.ConsumedItemTemplateId, Is.EqualTo("simple_meal"));
            Assert.That(recipe.RequiresInventoryItem, Is.True);
        }

        [Test]
        public void Constructor_AllowsInventoryFreeRecovery()
        {
            var recipe = new NeedRecoveryRecipe("sleep-basic", "sleep", NeedKind.Fatigue, 40);

            Assert.That(recipe.ConsumedItemTemplateId, Is.Null);
            Assert.That(recipe.RequiresInventoryItem, Is.False);
        }

        [Test]
        public void Constructor_RejectsEmptyIdsAndMissingActionKind()
        {
            Assert.Throws<ArgumentException>(() => new NeedRecoveryRecipe(null, "eat_meal", NeedKind.Hunger, 50, "simple_meal"));
            Assert.Throws<ArgumentException>(() => new NeedRecoveryRecipe("", "eat_meal", NeedKind.Hunger, 50, "simple_meal"));
            Assert.Throws<ArgumentException>(() => new NeedRecoveryRecipe("meal-basic", null, NeedKind.Hunger, 50, "simple_meal"));
            Assert.Throws<ArgumentException>(() => new NeedRecoveryRecipe("meal-basic", "   ", NeedKind.Hunger, 50, "simple_meal"));
        }

        [Test]
        public void Constructor_RejectsNonRecoveryDeltasAndMissingNeed()
        {
            Assert.Throws<ArgumentException>(() => new NeedRecoveryRecipe("bad", "eat_meal", NeedKind.None, 50, "simple_meal"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NeedRecoveryRecipe("bad", "eat_meal", NeedKind.Hunger, 0, "simple_meal"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NeedRecoveryRecipe("bad", "eat_meal", NeedKind.Hunger, -1, "simple_meal"));
        }

        [Test]
        public void Constructor_RejectsBlankConsumedItemTemplate()
        {
            Assert.Throws<ArgumentException>(() => new NeedRecoveryRecipe("meal-basic", "eat_meal", NeedKind.Hunger, 50, ""));
            Assert.Throws<ArgumentException>(() => new NeedRecoveryRecipe("meal-basic", "eat_meal", NeedKind.Hunger, 50, "   "));
        }
    }
}

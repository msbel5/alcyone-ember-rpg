using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin the recipe-handle contract before RecipeDef / RecipeSystem
// consumers exist. They cover value semantics only; lookup, save/load, ticking,
// and EventLog output belong to later Faz 2 atoms.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>
    /// Verifies the stable value semantics required by Ember recipe handles.
    /// </summary>
    public sealed class RecipeIdTests
    {
        /// <summary>
        /// A constructed recipe handle exposes the raw identifier supplied by the caller.
        /// </summary>
        [Test]
        public void Constructor_StoresValue()
        {
            var id = new RecipeId(42UL);

            Assert.That(id.Value, Is.EqualTo(42UL));
        }

        /// <summary>
        /// Two recipe handles with the same raw identifier compare equal.
        /// </summary>
        [Test]
        public void SameValue_IsEqual()
        {
            var left = new RecipeId(42UL);
            var right = new RecipeId(42UL);

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
        }

        /// <summary>
        /// Two recipe handles with different raw identifiers compare unequal.
        /// </summary>
        [Test]
        public void DifferentValue_IsNotEqual()
        {
            var left = new RecipeId(42UL);
            var right = new RecipeId(43UL);

            Assert.That(left, Is.Not.EqualTo(right));
            Assert.That(left != right, Is.True);
        }

        /// <summary>
        /// The default recipe handle is the empty no-recipe sentinel.
        /// </summary>
        [Test]
        public void Default_IsEmpty()
        {
            Assert.That(default(RecipeId).IsEmpty, Is.True);
        }

        /// <summary>
        /// Equal recipe handles produce stable matching hash codes.
        /// </summary>
        [Test]
        public void SameValue_HasSameHashCode()
        {
            var left = new RecipeId(42UL);
            var right = new RecipeId(42UL);

            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        /// <summary>
        /// The empty recipe handle has a distinct debug label.
        /// </summary>
        [Test]
        public void Empty_ToString_ReturnsEmptyLabel()
        {
            Assert.That(default(RecipeId).ToString(), Is.EqualTo("RecipeId.Empty"));
        }

        /// <summary>
        /// A non-empty recipe handle includes its raw identifier in the debug string.
        /// </summary>
        [Test]
        public void NonEmpty_ToString_ContainsRawValue()
        {
            Assert.That(new RecipeId(42UL).ToString(), Does.Contain("42"));
        }
    }
}

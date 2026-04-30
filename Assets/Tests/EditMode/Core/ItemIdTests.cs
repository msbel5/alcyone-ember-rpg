using EmberCrpg.Domain.Core;
using NUnit.Framework;

// Design note:
// These tests pin the item-handle contract before the production type exists.
// They cover value semantics only; allocation, lookup, save/load, and logging belong elsewhere.
namespace EmberCrpg.Tests.EditMode.Core
{
    /// <summary>
    /// Verifies the stable value semantics required by Ember item handles.
    /// </summary>
    public sealed class ItemIdTests
    {
        /// <summary>
        /// A constructed item handle exposes the raw identifier supplied by the caller.
        /// </summary>
        [Test]
        public void Constructor_StoresValue()
        {
            var id = new ItemId(42UL);

            Assert.That(id.Value, Is.EqualTo(42UL));
        }

        /// <summary>
        /// Two item handles with the same raw identifier compare equal.
        /// </summary>
        [Test]
        public void SameValue_IsEqual()
        {
            var left = new ItemId(42UL);
            var right = new ItemId(42UL);

            Assert.That(left, Is.EqualTo(right));
        }

        /// <summary>
        /// Two item handles with different raw identifiers compare unequal.
        /// </summary>
        [Test]
        public void DifferentValue_IsNotEqual()
        {
            var left = new ItemId(42UL);
            var right = new ItemId(43UL);

            Assert.That(left, Is.Not.EqualTo(right));
        }

        /// <summary>
        /// The default item handle is the empty no-item sentinel.
        /// </summary>
        [Test]
        public void Default_IsEmpty()
        {
            Assert.That(default(ItemId).IsEmpty, Is.True);
        }

        /// <summary>
        /// Equal item handles produce stable matching hash codes.
        /// </summary>
        [Test]
        public void SameValue_HasSameHashCode()
        {
            var left = new ItemId(42UL);
            var right = new ItemId(42UL);

            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        /// <summary>
        /// The empty item handle has a distinct debug label.
        /// </summary>
        [Test]
        public void Empty_ToString_ReturnsEmptyLabel()
        {
            Assert.That(default(ItemId).ToString(), Is.EqualTo("ItemId.Empty"));
        }

        /// <summary>
        /// A non-empty item handle includes its raw identifier in the debug string.
        /// </summary>
        [Test]
        public void NonEmpty_ToString_ContainsRawValue()
        {
            Assert.That(new ItemId(42UL).ToString(), Does.Contain("42"));
        }
    }
}

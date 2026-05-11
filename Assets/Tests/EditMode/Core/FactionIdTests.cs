using EmberCrpg.Domain.Core;
using NUnit.Framework;

// Design note:
// These tests pin the faction-handle contract before downstream FactionRecord / FactionStore consumers exist.
// They cover value semantics only; allocation, lookup, save/load, and logging belong elsewhere.
namespace EmberCrpg.Tests.EditMode.Core
{
    /// <summary>
    /// Verifies the stable value semantics required by Ember faction handles.
    /// </summary>
    public sealed class FactionIdTests
    {
        /// <summary>
        /// A constructed faction handle exposes the raw identifier supplied by the caller.
        /// </summary>
        [Test]
        public void Constructor_StoresValue()
        {
            var id = new FactionId(42UL);

            Assert.That(id.Value, Is.EqualTo(42UL));
        }

        /// <summary>
        /// Two faction handles with the same raw identifier compare equal.
        /// </summary>
        [Test]
        public void SameValue_IsEqual()
        {
            var left = new FactionId(42UL);
            var right = new FactionId(42UL);

            Assert.That(left, Is.EqualTo(right));
        }

        /// <summary>
        /// Two faction handles with different raw identifiers compare unequal.
        /// </summary>
        [Test]
        public void DifferentValue_IsNotEqual()
        {
            var left = new FactionId(42UL);
            var right = new FactionId(43UL);

            Assert.That(left, Is.Not.EqualTo(right));
        }

        /// <summary>
        /// The default faction handle is the empty no-faction sentinel.
        /// </summary>
        [Test]
        public void Default_IsEmpty()
        {
            Assert.That(default(FactionId).IsEmpty, Is.True);
        }

        /// <summary>
        /// Equal faction handles produce stable matching hash codes.
        /// </summary>
        [Test]
        public void SameValue_HasSameHashCode()
        {
            var left = new FactionId(42UL);
            var right = new FactionId(42UL);

            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        /// <summary>
        /// The empty faction handle has a distinct debug label.
        /// </summary>
        [Test]
        public void Empty_ToString_ReturnsEmptyLabel()
        {
            Assert.That(default(FactionId).ToString(), Is.EqualTo("FactionId.Empty"));
        }

        /// <summary>
        /// A non-empty faction handle includes its raw identifier in the debug string.
        /// </summary>
        [Test]
        public void NonEmpty_ToString_ContainsRawValue()
        {
            Assert.That(new FactionId(42UL).ToString(), Does.Contain("42"));
        }
    }
}

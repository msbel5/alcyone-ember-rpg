using EmberCrpg.Domain.Core;
using NUnit.Framework;

// Design note:
// These tests pin the site-handle contract before downstream SiteRecord / SiteStore consumers exist.
// They cover value semantics only; allocation, lookup, save/load, and logging belong elsewhere.
namespace EmberCrpg.Tests.EditMode.Core
{
    /// <summary>
    /// Verifies the stable value semantics required by Ember site handles.
    /// </summary>
    public sealed class SiteIdTests
    {
        /// <summary>
        /// A constructed site handle exposes the raw identifier supplied by the caller.
        /// </summary>
        [Test]
        public void Constructor_StoresValue()
        {
            var id = new SiteId(42UL);

            Assert.That(id.Value, Is.EqualTo(42UL));
        }

        /// <summary>
        /// Two site handles with the same raw identifier compare equal.
        /// </summary>
        [Test]
        public void SameValue_IsEqual()
        {
            var left = new SiteId(42UL);
            var right = new SiteId(42UL);

            Assert.That(left, Is.EqualTo(right));
        }

        /// <summary>
        /// Two site handles with different raw identifiers compare unequal.
        /// </summary>
        [Test]
        public void DifferentValue_IsNotEqual()
        {
            var left = new SiteId(42UL);
            var right = new SiteId(43UL);

            Assert.That(left, Is.Not.EqualTo(right));
        }

        /// <summary>
        /// The default site handle is the empty no-site sentinel.
        /// </summary>
        [Test]
        public void Default_IsEmpty()
        {
            Assert.That(default(SiteId).IsEmpty, Is.True);
        }

        /// <summary>
        /// Equal site handles produce stable matching hash codes.
        /// </summary>
        [Test]
        public void SameValue_HasSameHashCode()
        {
            var left = new SiteId(42UL);
            var right = new SiteId(42UL);

            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        /// <summary>
        /// The empty site handle has a distinct debug label.
        /// </summary>
        [Test]
        public void Empty_ToString_ReturnsEmptyLabel()
        {
            Assert.That(default(SiteId).ToString(), Is.EqualTo("SiteId.Empty"));
        }

        /// <summary>
        /// A non-empty site handle includes its raw identifier in the debug string.
        /// </summary>
        [Test]
        public void NonEmpty_ToString_ContainsRawValue()
        {
            Assert.That(new SiteId(42UL).ToString(), Does.Contain("42"));
        }
    }
}

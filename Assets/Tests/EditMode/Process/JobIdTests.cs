using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin Phase 3's stable job-handle contract before JobRequest or
// JobBoard consumers exist. They cover value semantics only; allocation, lookup,
// save/load, assignment, and EventLog output belong to later atoms.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>
    /// Verifies the stable value semantics required by Ember job handles.
    /// </summary>
    public sealed class JobIdTests
    {
        /// <summary>
        /// A constructed job handle exposes the raw identifier supplied by the caller.
        /// </summary>
        [Test]
        public void Constructor_StoresValue()
        {
            var id = new JobId(42UL);

            Assert.That(id.Value, Is.EqualTo(42UL));
        }

        /// <summary>
        /// Two job handles with the same raw identifier compare equal.
        /// </summary>
        [Test]
        public void SameValue_IsEqual()
        {
            var left = new JobId(42UL);
            var right = new JobId(42UL);

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
        }

        /// <summary>
        /// Two job handles with different raw identifiers compare unequal.
        /// </summary>
        [Test]
        public void DifferentValue_IsNotEqual()
        {
            var left = new JobId(42UL);
            var right = new JobId(43UL);

            Assert.That(left, Is.Not.EqualTo(right));
            Assert.That(left != right, Is.True);
        }

        /// <summary>
        /// The default job handle is the empty no-job sentinel.
        /// </summary>
        [Test]
        public void Default_IsEmpty()
        {
            Assert.That(default(JobId).IsEmpty, Is.True);
        }

        /// <summary>
        /// Equal job handles produce stable matching hash codes.
        /// </summary>
        [Test]
        public void SameValue_HasSameHashCode()
        {
            var left = new JobId(42UL);
            var right = new JobId(42UL);

            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        /// <summary>
        /// The empty job handle has a distinct debug label.
        /// </summary>
        [Test]
        public void Empty_ToString_ReturnsEmptyLabel()
        {
            Assert.That(default(JobId).ToString(), Is.EqualTo("JobId.Empty"));
        }

        /// <summary>
        /// A non-empty job handle includes its raw identifier in the debug string.
        /// </summary>
        [Test]
        public void NonEmpty_ToString_ContainsRawValue()
        {
            Assert.That(new JobId(42UL).ToString(), Does.Contain("42"));
        }
    }
}

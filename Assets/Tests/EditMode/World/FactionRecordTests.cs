using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using NUnit.Framework;

// Design note:
// These tests pin the FactionRecord constructor contract and tag-bag behavior
// before FactionStore consumers exist. Coverage stays scoped to the pure record;
// allocation, lookup, save/load, and logging belong elsewhere.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies the pure-Domain invariants required of FactionRecord.</summary>
    public sealed class FactionRecordTests
    {
        private static FactionRecord MakeRecord()
        {
            return new FactionRecord(
                new FactionId(5UL),
                "Ember Wardens",
                new[] { "guard", "lawful" });
        }

        /// <summary>Constructor stores every field exactly as supplied.</summary>
        [Test]
        public void Constructor_StoresFields()
        {
            var record = MakeRecord();

            Assert.That(record.Id, Is.EqualTo(new FactionId(5UL)));
            Assert.That(record.Name, Is.EqualTo("Ember Wardens"));
            Assert.That(record.Tags, Is.EqualTo(new[] { "guard", "lawful" }));
        }

        /// <summary>The empty FactionId sentinel cannot back a record.</summary>
        [Test]
        public void Constructor_RejectsEmptyId()
        {
            Assert.Throws<ArgumentException>(() => new FactionRecord(
                default,
                "Outsiders",
                new[] { "neutral" }));
        }

        /// <summary>A blank or whitespace name is rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsBlankName()
        {
            Assert.Throws<ArgumentException>(() => new FactionRecord(
                new FactionId(1UL),
                "   ",
                new[] { "neutral" }));
        }

        /// <summary>A null tag enumerable is rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsNullTags()
        {
            Assert.Throws<ArgumentNullException>(() => new FactionRecord(
                new FactionId(1UL),
                "Outsiders",
                null));
        }

        /// <summary>Blank or whitespace tag entries are rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsBlankTag()
        {
            Assert.Throws<ArgumentException>(() => new FactionRecord(
                new FactionId(1UL),
                "Outsiders",
                new[] { "lawful", "   " }));
        }

        /// <summary>An empty tag bag is accepted; the seed Faz-6 faction may carry none.</summary>
        [Test]
        public void Constructor_AcceptsEmptyTags()
        {
            var record = new FactionRecord(
                new FactionId(2UL),
                "Wanderers",
                Array.Empty<string>());

            Assert.That(record.Tags, Is.Empty);
            Assert.That(record.HasTag("lawful"), Is.False);
        }

        /// <summary>Tags are returned in insertion order with no deduplication or reordering.</summary>
        [Test]
        public void Tags_PreserveInsertionOrder()
        {
            var record = new FactionRecord(
                new FactionId(9UL),
                "Mixed",
                new[] { "neutral", "guard", "neutral" });

            Assert.That(record.Tags, Is.EqualTo(new[] { "neutral", "guard", "neutral" }));
        }

        /// <summary>Mutating the source array after construction does not leak into the record.</summary>
        [Test]
        public void Tags_DefensiveCopyAtConstruction()
        {
            var source = new[] { "guard", "lawful" };
            var record = new FactionRecord(new FactionId(3UL), "Ember Wardens", source);

            source[0] = "outlaw";

            Assert.That(record.Tags, Is.EqualTo(new[] { "guard", "lawful" }));
        }

        /// <summary>Tags projection is not a string[] under the hood, so callers cannot
        /// cast back and mutate the internal tag bag.</summary>
        [Test]
        public void Tags_ProjectionIsNotBackingArray()
        {
            var record = MakeRecord();

            Assert.That(record.Tags, Is.Not.InstanceOf<string[]>());
        }

        /// <summary>Even when the caller tries to mutate via a downcast, the projection
        /// stays immutable and the original tag bag survives unchanged.</summary>
        [Test]
        public void Tags_ProjectionCannotBeMutatedViaDowncast()
        {
            var record = MakeRecord();
            var projection = record.Tags;

            var mutableArray = projection as string[];
            Assert.That(mutableArray, Is.Null,
                "Tags must not expose the backing string[] to callers.");

            Assert.That(record.Tags, Is.EqualTo(new[] { "guard", "lawful" }));
        }

        /// <summary>HasTag returns true only for an exact, case-sensitive match supplied at construction.</summary>
        [Test]
        public void HasTag_KnownTag_IsTrue()
        {
            var record = MakeRecord();

            Assert.That(record.HasTag("guard"), Is.True);
            Assert.That(record.HasTag("lawful"), Is.True);
        }

        /// <summary>HasTag rejects unknown, case-mismatched, blank, and null inputs.</summary>
        [Test]
        public void HasTag_UnknownOrInvalid_IsFalse()
        {
            var record = MakeRecord();

            Assert.That(record.HasTag("outlaw"), Is.False);
            Assert.That(record.HasTag("Guard"), Is.False);
            Assert.That(record.HasTag("   "), Is.False);
            Assert.That(record.HasTag(null), Is.False);
        }
    }
}

using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using NUnit.Framework;

// Design note:
// These tests pin the SiteRecord constructor contract and bounds-containment
// before SiteStore consumers exist. Coverage stays scoped to the pure record;
// allocation, lookup, save/load, and logging belong elsewhere.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies the pure-Domain invariants required of SiteRecord.</summary>
    public sealed class SiteRecordTests
    {
        private static SiteRecord MakeRecord()
        {
            return new SiteRecord(
                new SiteId(7UL),
                SiteKind.Settlement,
                "Hollow Run",
                new GridPosition(0, 0),
                new GridPosition(4, 3));
        }

        /// <summary>Constructor stores every field exactly as supplied.</summary>
        [Test]
        public void Constructor_StoresFields()
        {
            var record = MakeRecord();

            Assert.That(record.Id, Is.EqualTo(new SiteId(7UL)));
            Assert.That(record.Kind, Is.EqualTo(SiteKind.Settlement));
            Assert.That(record.Name, Is.EqualTo("Hollow Run"));
            Assert.That(record.MinBound, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(record.MaxBound, Is.EqualTo(new GridPosition(4, 3)));
        }

        /// <summary>The empty SiteId sentinel cannot back a record.</summary>
        [Test]
        public void Constructor_RejectsEmptyId()
        {
            Assert.Throws<ArgumentException>(() => new SiteRecord(
                default,
                SiteKind.Region,
                "Anywhere",
                new GridPosition(0, 0),
                new GridPosition(1, 1)));
        }

        /// <summary>The None sentinel kind is rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsNoneKind()
        {
            Assert.Throws<ArgumentException>(() => new SiteRecord(
                new SiteId(1UL),
                SiteKind.None,
                "Anywhere",
                new GridPosition(0, 0),
                new GridPosition(1, 1)));
        }

        /// <summary>A blank or whitespace name is rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsBlankName()
        {
            Assert.Throws<ArgumentException>(() => new SiteRecord(
                new SiteId(1UL),
                SiteKind.Region,
                "   ",
                new GridPosition(0, 0),
                new GridPosition(1, 1)));
        }

        /// <summary>maxBound must be component-wise greater-or-equal to minBound.</summary>
        [Test]
        public void Constructor_RejectsInvertedBounds()
        {
            Assert.Throws<ArgumentException>(() => new SiteRecord(
                new SiteId(1UL),
                SiteKind.Dungeon,
                "Sunken Vault",
                new GridPosition(2, 2),
                new GridPosition(1, 5)));
        }

        /// <summary>Grid coordinates strictly inside the bounds are contained.</summary>
        [Test]
        public void Contains_InsidePoint_IsTrue()
        {
            var record = MakeRecord();

            Assert.That(record.Contains(new GridPosition(2, 1)), Is.True);
        }

        /// <summary>Coordinates exactly on the bounds remain contained (inclusive).</summary>
        [Test]
        public void Contains_BoundaryPoint_IsTrue()
        {
            var record = MakeRecord();

            Assert.That(record.Contains(record.MinBound), Is.True);
            Assert.That(record.Contains(record.MaxBound), Is.True);
        }

        /// <summary>Coordinates outside the bounds are rejected.</summary>
        [Test]
        public void Contains_OutsidePoint_IsFalse()
        {
            var record = MakeRecord();

            Assert.That(record.Contains(new GridPosition(5, 1)), Is.False);
            Assert.That(record.Contains(new GridPosition(0, -1)), Is.False);
        }
    }
}

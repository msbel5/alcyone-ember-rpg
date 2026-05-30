using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin the first pure Worksite state atoms before WorksiteStore or
// RecipeSystem exist. Coverage stays scoped to constructor invariants and the
// immutable active/inactive toggle; lookup, ticking, and EventLog writes belong
// to later Phase 2 atoms.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies the pure-Domain invariants required by WorksiteRecord.</summary>
    public sealed class WorksiteRecordTests
    {
        private static WorksiteRecord MakeRecord(bool isActive = true)
        {
            return new WorksiteRecord(
                new SiteId(11UL),
                new GridPosition(3, 4),
                WorksiteKind.Furnace,
                isActive);
        }

        /// <summary>Constructor stores every field exactly as supplied.</summary>
        [Test]
        public void Constructor_StoresFields()
        {
            var record = MakeRecord();

            Assert.That(record.SiteId, Is.EqualTo(new SiteId(11UL)));
            Assert.That(record.Position, Is.EqualTo(new GridPosition(3, 4)));
            Assert.That(record.Kind, Is.EqualTo(WorksiteKind.Furnace));
            Assert.That(record.IsActive, Is.True);
        }

        /// <summary>The empty SiteId sentinel cannot back a worksite component.</summary>
        [Test]
        public void Constructor_RejectsEmptySiteId()
        {
            Assert.Throws<ArgumentException>(() => new WorksiteRecord(
                default,
                new GridPosition(0, 0),
                WorksiteKind.Furnace,
                true));
        }

        /// <summary>The None sentinel kind is rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsNoneKind()
        {
            Assert.Throws<ArgumentException>(() => new WorksiteRecord(
                new SiteId(11UL),
                new GridPosition(0, 0),
                WorksiteKind.None,
                true));
        }

        /// <summary>Inactive worksites are valid state and remain pinned on the record.</summary>
        [Test]
        public void Constructor_AllowsInactiveState()
        {
            var record = MakeRecord(isActive: false);

            Assert.That(record.IsActive, Is.False);
        }

        /// <summary>Active-state changes return a new record without mutating identity or location.</summary>
        [Test]
        public void WithActive_ReturnsCopyWithRequestedState()
        {
            var active = MakeRecord();

            var inactive = active.WithActive(false);
            var activeAgain = inactive.WithActive(true);

            Assert.That(inactive, Is.Not.SameAs(active));
            Assert.That(inactive.SiteId, Is.EqualTo(active.SiteId));
            Assert.That(inactive.Position, Is.EqualTo(active.Position));
            Assert.That(inactive.Kind, Is.EqualTo(active.Kind));
            Assert.That(inactive.IsActive, Is.False);
            Assert.That(activeAgain.IsActive, Is.True);
        }
    }
}

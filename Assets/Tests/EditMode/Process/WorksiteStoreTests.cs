using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin the narrow WorksiteStore contract before RecipeSystem exists:
// deterministic site-cell lookup, duplicate/default rejection, and insertion-order
// enumeration. Runtime ticking, inventory consumption, and EventLog writes remain
// in later Faz 2 atoms.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies the pure-Domain registry contract for WorksiteStore.</summary>
    public sealed class WorksiteStoreTests
    {
        private static readonly SiteId FirstSite = new SiteId(11UL);
        private static readonly SiteId SecondSite = new SiteId(22UL);

        private static WorksiteRecord MakeRecord(SiteId siteId, int x, int y, bool isActive = true)
        {
            return new WorksiteRecord(
                siteId,
                new GridPosition(x, y),
                WorksiteKind.Furnace,
                isActive);
        }

        /// <summary>Add stores a worksite and makes it available by site id plus grid position.</summary>
        [Test]
        public void Add_StoresRecordBySiteAndPosition()
        {
            var store = new WorksiteStore();
            var record = MakeRecord(FirstSite, 3, 4);

            store.Add(record);

            Assert.That(store.Count, Is.EqualTo(1));
            Assert.That(store.Contains(FirstSite, new GridPosition(3, 4)), Is.True);
            Assert.That(store.Get(FirstSite, new GridPosition(3, 4)), Is.SameAs(record));
        }

        /// <summary>Null records are rejected before any key extraction.</summary>
        [Test]
        public void Add_RejectsNullRecord()
        {
            var store = new WorksiteStore();

            Assert.Throws<ArgumentNullException>(() => store.Add(null));
        }

        /// <summary>Only one worksite can occupy a site cell.</summary>
        [Test]
        public void Add_RejectsDuplicateSiteCell()
        {
            var store = new WorksiteStore();
            store.Add(MakeRecord(FirstSite, 3, 4));

            Assert.Throws<InvalidOperationException>(() => store.Add(MakeRecord(FirstSite, 3, 4, isActive: false)));
        }

        /// <summary>The same grid position in a different site is a distinct worksite key.</summary>
        [Test]
        public void Add_AllowsSamePositionInDifferentSites()
        {
            var store = new WorksiteStore();
            var first = MakeRecord(FirstSite, 3, 4);
            var second = MakeRecord(SecondSite, 3, 4);

            store.Add(first);
            store.Add(second);

            Assert.That(store.Count, Is.EqualTo(2));
            Assert.That(store.Get(FirstSite, new GridPosition(3, 4)), Is.SameAs(first));
            Assert.That(store.Get(SecondSite, new GridPosition(3, 4)), Is.SameAs(second));
        }

        /// <summary>Strict Get rejects the empty site sentinel instead of silently missing.</summary>
        [Test]
        public void Get_RejectsEmptySiteId()
        {
            var store = new WorksiteStore();

            Assert.Throws<ArgumentException>(() => store.Get(default, new GridPosition(0, 0)));
        }

        /// <summary>Strict Get throws for a non-empty site cell that has no worksite.</summary>
        [Test]
        public void Get_MissingSiteCell_ThrowsKeyNotFound()
        {
            var store = new WorksiteStore();

            Assert.Throws<KeyNotFoundException>(() => store.Get(FirstSite, new GridPosition(9, 9)));
        }

        /// <summary>TryGet returns true and exposes the stored record for a registered worksite.</summary>
        [Test]
        public void TryGet_KnownSiteCell_ReturnsStoredRecord()
        {
            var store = new WorksiteStore();
            var record = MakeRecord(FirstSite, 3, 4);
            store.Add(record);

            var found = store.TryGet(FirstSite, new GridPosition(3, 4), out var actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(record));
        }

        /// <summary>TryGet returns false for empty or missing keys and clears the out value.</summary>
        [Test]
        public void TryGet_ReturnsFalseForEmptyOrMissingKey()
        {
            var store = new WorksiteStore();

            Assert.That(store.TryGet(default, new GridPosition(0, 0), out var emptyRecord), Is.False);
            Assert.That(emptyRecord, Is.Null);
            Assert.That(store.TryGet(FirstSite, new GridPosition(9, 9), out var missingRecord), Is.False);
            Assert.That(missingRecord, Is.Null);
        }

        /// <summary>Contains returns false for empty or unregistered site cells.</summary>
        [Test]
        public void Contains_ReturnsFalseForEmptyOrMissingKey()
        {
            var store = new WorksiteStore();
            store.Add(MakeRecord(FirstSite, 3, 4));

            Assert.That(store.Contains(default, new GridPosition(3, 4)), Is.False);
            Assert.That(store.Contains(FirstSite, new GridPosition(9, 9)), Is.False);
        }

        /// <summary>Remove returns false for empty or unregistered site cells without mutating state.</summary>
        [Test]
        public void Remove_ReturnsFalseForEmptyOrMissingKey()
        {
            var store = new WorksiteStore();
            var record = MakeRecord(FirstSite, 3, 4);
            store.Add(record);

            Assert.That(store.Remove(default, new GridPosition(3, 4)), Is.False);
            Assert.That(store.Remove(FirstSite, new GridPosition(9, 9)), Is.False);
            Assert.That(store.Count, Is.EqualTo(1));
            Assert.That(store.Get(FirstSite, new GridPosition(3, 4)), Is.SameAs(record));
        }

        /// <summary>Remove updates both lookup and deterministic enumeration order.</summary>
        [Test]
        public void Remove_DropsRecordAndOrderEntry()
        {
            var store = new WorksiteStore();
            var first = MakeRecord(FirstSite, 1, 1);
            var second = MakeRecord(FirstSite, 2, 2);
            store.Add(first);
            store.Add(second);

            var removed = store.Remove(FirstSite, new GridPosition(1, 1));

            Assert.That(removed, Is.True);
            Assert.That(store.Contains(FirstSite, new GridPosition(1, 1)), Is.False);
            Assert.That(store.Records.ToArray(), Is.EqualTo(new[] { second }));
        }

        /// <summary>Records enumerate in stable insertion order.</summary>
        [Test]
        public void Records_EnumerateInInsertionOrder()
        {
            var store = new WorksiteStore();
            var records = new List<WorksiteRecord>
            {
                MakeRecord(FirstSite, 1, 1),
                MakeRecord(FirstSite, 2, 2),
                MakeRecord(SecondSite, 1, 1)
            };

            foreach (var record in records)
                store.Add(record);

            Assert.That(store.Records.ToArray(), Is.EqualTo(records.ToArray()));
        }

        /// <summary>Clear empties lookup and order together.</summary>
        [Test]
        public void Clear_DropsAllRecords()
        {
            var store = new WorksiteStore();
            store.Add(MakeRecord(FirstSite, 1, 1));
            store.Add(MakeRecord(SecondSite, 2, 2));

            store.Clear();

            Assert.That(store.Count, Is.EqualTo(0));
            Assert.That(store.Contains(FirstSite, new GridPosition(1, 1)), Is.False);
            Assert.That(store.Records.ToArray(), Is.Empty);
        }
    }
}

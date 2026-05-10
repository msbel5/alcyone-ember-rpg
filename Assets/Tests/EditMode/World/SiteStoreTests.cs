using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Actors;
using NUnit.Framework;

// Design note:
// These tests pin Faz 1's WORLD-box Core Store contract: SiteStore. They
// cover Add/Get/TryGet/Remove/Contains/Count/Clear, deterministic insertion
// -order enumeration, and rejection of the empty SiteId sentinel. Pure
// Domain — no Unity references. Mirrors ActorStoreTests so the four Faz 1
// stores share a single regression shape.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Pins Faz 1 SiteStore Add/Get/TryGet/Remove/Contains/Count/Clear/Records.</summary>
    public sealed class SiteStoreTests
    {
        [Test]
        public void Add_ThenGet_ReturnsSameRecord()
        {
            var store = new SiteStore();
            var record = MakeRecord(1, SiteKind.Settlement, "Hollow Run");

            store.Add(record);

            Assert.That(store.Get(record.Id), Is.SameAs(record));
            Assert.That(store.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_NullRecord_Throws()
        {
            var store = new SiteStore();
            Assert.Throws<ArgumentNullException>(() => store.Add(null));
        }

        [Test]
        public void Add_DuplicateId_Throws()
        {
            var store = new SiteStore();
            store.Add(MakeRecord(7, SiteKind.Region, "First"));
            Assert.Throws<InvalidOperationException>(
                () => store.Add(MakeRecord(7, SiteKind.Dungeon, "Second")));
        }

        [Test]
        public void Get_MissingId_ThrowsKeyNotFound()
        {
            var store = new SiteStore();
            Assert.Throws<KeyNotFoundException>(() => store.Get(new SiteId(42UL)));
        }

        [Test]
        public void Get_EmptyId_Throws()
        {
            var store = new SiteStore();
            Assert.Throws<ArgumentException>(() => store.Get(default));
        }

        [Test]
        public void TryGet_MissingId_ReturnsFalseAndNull()
        {
            var store = new SiteStore();

            var found = store.TryGet(new SiteId(99UL), out var record);

            Assert.That(found, Is.False);
            Assert.That(record, Is.Null);
        }

        [Test]
        public void TryGet_EmptyId_ReturnsFalseAndNull()
        {
            var store = new SiteStore();
            store.Add(MakeRecord(1, SiteKind.Region, "Anywhere"));

            var found = store.TryGet(default, out var record);

            Assert.That(found, Is.False);
            Assert.That(record, Is.Null);
        }

        [Test]
        public void TryGet_KnownId_ReturnsRecord()
        {
            var store = new SiteStore();
            var record = MakeRecord(3, SiteKind.Dungeon, "Sunken Vault");
            store.Add(record);

            var found = store.TryGet(record.Id, out var actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(record));
        }

        [Test]
        public void Contains_RespectsRegistrationAndEmptyId()
        {
            var store = new SiteStore();
            var record = MakeRecord(5, SiteKind.Settlement, "Trading Post");
            store.Add(record);

            Assert.That(store.Contains(record.Id), Is.True);
            Assert.That(store.Contains(new SiteId(99UL)), Is.False);
            Assert.That(store.Contains(default), Is.False);
        }

        [Test]
        public void Remove_KnownId_DropsRecordAndDecrementsCount()
        {
            var store = new SiteStore();
            var record = MakeRecord(11, SiteKind.Region, "Borderlands");
            store.Add(record);

            var removed = store.Remove(record.Id);

            Assert.That(removed, Is.True);
            Assert.That(store.Count, Is.EqualTo(0));
            Assert.That(store.Contains(record.Id), Is.False);
        }

        [Test]
        public void Remove_MissingOrEmptyId_ReturnsFalse()
        {
            var store = new SiteStore();
            Assert.That(store.Remove(new SiteId(11UL)), Is.False);
            Assert.That(store.Remove(default), Is.False);
        }

        [Test]
        public void Clear_DropsEveryRecord()
        {
            var store = new SiteStore();
            store.Add(MakeRecord(1, SiteKind.Region, "Borderlands"));
            store.Add(MakeRecord(2, SiteKind.Settlement, "Hollow Run"));

            store.Clear();

            Assert.That(store.Count, Is.EqualTo(0));
            Assert.That(store.Records.ToList(), Is.Empty);
        }

        [Test]
        public void Records_EnumeratesInInsertionOrder()
        {
            var store = new SiteStore();
            var first = MakeRecord(2, SiteKind.Region, "Region");
            var second = MakeRecord(7, SiteKind.Settlement, "Town");
            var third = MakeRecord(3, SiteKind.Dungeon, "Crypt");

            store.Add(first);
            store.Add(second);
            store.Add(third);

            Assert.That(store.Records.ToList(), Is.EqualTo(new[] { first, second, third }));
        }

        [Test]
        public void Records_AfterRemove_PreservesRemainingOrder()
        {
            var store = new SiteStore();
            var first = MakeRecord(1, SiteKind.Region, "Region");
            var second = MakeRecord(2, SiteKind.Settlement, "Town");
            var third = MakeRecord(3, SiteKind.Dungeon, "Crypt");

            store.Add(first);
            store.Add(second);
            store.Add(third);

            store.Remove(second.Id);

            Assert.That(store.Records.ToList(), Is.EqualTo(new[] { first, third }));
            Assert.That(store.Count, Is.EqualTo(2));
        }

        private static SiteRecord MakeRecord(ulong id, SiteKind kind, string name)
        {
            return new SiteRecord(
                new SiteId(id),
                kind,
                name,
                new GridPosition(0, 0),
                new GridPosition(4, 3));
        }
    }
}

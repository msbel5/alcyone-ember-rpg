using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using NUnit.Framework;

// Design note:
// These tests pin Faz 1's SOCIETY-seed Core Store contract: FactionStore.
// They cover Add/Get/TryGet/Remove/Contains/Count/Clear, deterministic
// insertion-order enumeration, and rejection of the empty FactionId sentinel.
// Pure Domain — no Unity references. Mirrors SiteStoreTests / ActorStoreTests
// so the four Faz 1 stores share a single regression shape.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Pins Faz 1 FactionStore Add/Get/TryGet/Remove/Contains/Count/Clear/Records.</summary>
    public sealed class FactionStoreTests
    {
        [Test]
        public void Add_ThenGet_ReturnsSameRecord()
        {
            var store = new FactionStore();
            var record = MakeRecord(1, "Hollow Brotherhood");

            store.Add(record);

            Assert.That(store.Get(record.Id), Is.SameAs(record));
            Assert.That(store.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_NullRecord_Throws()
        {
            var store = new FactionStore();
            Assert.Throws<ArgumentNullException>(() => store.Add(null));
        }

        [Test]
        public void Add_DuplicateId_Throws()
        {
            var store = new FactionStore();
            store.Add(MakeRecord(7, "First"));
            Assert.Throws<InvalidOperationException>(
                () => store.Add(MakeRecord(7, "Second")));
        }

        [Test]
        public void Get_MissingId_ThrowsKeyNotFound()
        {
            var store = new FactionStore();
            Assert.Throws<KeyNotFoundException>(() => store.Get(new FactionId(42UL)));
        }

        [Test]
        public void Get_EmptyId_Throws()
        {
            var store = new FactionStore();
            Assert.Throws<ArgumentException>(() => store.Get(default));
        }

        [Test]
        public void TryGet_MissingId_ReturnsFalseAndNull()
        {
            var store = new FactionStore();

            var found = store.TryGet(new FactionId(99UL), out var record);

            Assert.That(found, Is.False);
            Assert.That(record, Is.Null);
        }

        [Test]
        public void TryGet_EmptyId_ReturnsFalseAndNull()
        {
            var store = new FactionStore();
            store.Add(MakeRecord(1, "Anywhere"));

            var found = store.TryGet(default, out var record);

            Assert.That(found, Is.False);
            Assert.That(record, Is.Null);
        }

        [Test]
        public void TryGet_KnownId_ReturnsRecord()
        {
            var store = new FactionStore();
            var record = MakeRecord(3, "Sunken Court");
            store.Add(record);

            var found = store.TryGet(record.Id, out var actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(record));
        }

        [Test]
        public void Contains_RespectsRegistrationAndEmptyId()
        {
            var store = new FactionStore();
            var record = MakeRecord(5, "Trading Guild");
            store.Add(record);

            Assert.That(store.Contains(record.Id), Is.True);
            Assert.That(store.Contains(new FactionId(99UL)), Is.False);
            Assert.That(store.Contains(default), Is.False);
        }

        [Test]
        public void Remove_KnownId_DropsRecordAndDecrementsCount()
        {
            var store = new FactionStore();
            var record = MakeRecord(11, "Borderlands Watch");
            store.Add(record);

            var removed = store.Remove(record.Id);

            Assert.That(removed, Is.True);
            Assert.That(store.Count, Is.EqualTo(0));
            Assert.That(store.Contains(record.Id), Is.False);
        }

        [Test]
        public void Remove_MissingOrEmptyId_ReturnsFalse()
        {
            var store = new FactionStore();
            Assert.That(store.Remove(new FactionId(11UL)), Is.False);
            Assert.That(store.Remove(default), Is.False);
        }

        [Test]
        public void Clear_DropsEveryRecord()
        {
            var store = new FactionStore();
            store.Add(MakeRecord(1, "Borderlands Watch"));
            store.Add(MakeRecord(2, "Hollow Brotherhood"));

            store.Clear();

            Assert.That(store.Count, Is.EqualTo(0));
            Assert.That(store.Records.ToList(), Is.Empty);
        }

        [Test]
        public void Records_EnumeratesInInsertionOrder()
        {
            var store = new FactionStore();
            var first = MakeRecord(2, "Watch");
            var second = MakeRecord(7, "Guild");
            var third = MakeRecord(3, "Court");

            store.Add(first);
            store.Add(second);
            store.Add(third);

            Assert.That(store.Records.ToList(), Is.EqualTo(new[] { first, second, third }));
        }

        [Test]
        public void Records_AfterRemove_PreservesRemainingOrder()
        {
            var store = new FactionStore();
            var first = MakeRecord(1, "Watch");
            var second = MakeRecord(2, "Guild");
            var third = MakeRecord(3, "Court");

            store.Add(first);
            store.Add(second);
            store.Add(third);

            store.Remove(second.Id);

            Assert.That(store.Records.ToList(), Is.EqualTo(new[] { first, third }));
            Assert.That(store.Count, Is.EqualTo(2));
        }

        private static FactionRecord MakeRecord(ulong id, string name)
        {
            return new FactionRecord(new FactionId(id), name, Array.Empty<string>());
        }
    }
}

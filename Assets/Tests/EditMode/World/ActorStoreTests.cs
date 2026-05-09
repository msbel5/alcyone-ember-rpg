using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using NUnit.Framework;

// Design note:
// These tests pin Faz 1's first Core Store contract: ActorStore. They cover
// Add/Get/TryGet/Remove/Contains/Count/Clear, deterministic insertion-order
// enumeration, and rejection of the empty ActorId sentinel. Pure Domain —
// no Unity references.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Pins Faz 1 ActorStore Add/Get/TryGet/Remove/Contains/Count/Clear/Records.</summary>
    public sealed class ActorStoreTests
    {
        [Test]
        public void Add_ThenGet_ReturnsSameRecord()
        {
            var store = new ActorStore();
            var record = MakeRecord(1, "Player", ActorRole.Player);

            store.Add(record);

            Assert.That(store.Get(record.Id), Is.SameAs(record));
            Assert.That(store.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_NullRecord_Throws()
        {
            var store = new ActorStore();
            Assert.Throws<ArgumentNullException>(() => store.Add(null));
        }

        [Test]
        public void Add_EmptyId_Throws()
        {
            var store = new ActorStore();
            var record = MakeRecord(0, "Empty", ActorRole.Player);
            Assert.Throws<ArgumentException>(() => store.Add(record));
        }

        [Test]
        public void Add_DuplicateId_Throws()
        {
            var store = new ActorStore();
            store.Add(MakeRecord(7, "First", ActorRole.Player));
            Assert.Throws<InvalidOperationException>(
                () => store.Add(MakeRecord(7, "Second", ActorRole.Talker)));
        }

        [Test]
        public void Get_MissingId_ThrowsKeyNotFound()
        {
            var store = new ActorStore();
            Assert.Throws<KeyNotFoundException>(() => store.Get(new ActorId(42)));
        }

        [Test]
        public void Get_EmptyId_Throws()
        {
            var store = new ActorStore();
            Assert.Throws<ArgumentException>(() => store.Get(default));
        }

        [Test]
        public void TryGet_MissingId_ReturnsFalseAndNull()
        {
            var store = new ActorStore();

            var found = store.TryGet(new ActorId(99), out var record);

            Assert.That(found, Is.False);
            Assert.That(record, Is.Null);
        }

        [Test]
        public void TryGet_EmptyId_ReturnsFalseAndNull()
        {
            var store = new ActorStore();
            store.Add(MakeRecord(1, "Player", ActorRole.Player));

            var found = store.TryGet(default, out var record);

            Assert.That(found, Is.False);
            Assert.That(record, Is.Null);
        }

        [Test]
        public void TryGet_KnownId_ReturnsRecord()
        {
            var store = new ActorStore();
            var record = MakeRecord(3, "Guard", ActorRole.Guard);
            store.Add(record);

            var found = store.TryGet(record.Id, out var actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(record));
        }

        [Test]
        public void Contains_RespectsRegistrationAndEmptyId()
        {
            var store = new ActorStore();
            var record = MakeRecord(5, "Merchant", ActorRole.Merchant);
            store.Add(record);

            Assert.That(store.Contains(record.Id), Is.True);
            Assert.That(store.Contains(new ActorId(99)), Is.False);
            Assert.That(store.Contains(default), Is.False);
        }

        [Test]
        public void Remove_KnownId_DropsRecordAndDecrementsCount()
        {
            var store = new ActorStore();
            var record = MakeRecord(11, "Enemy", ActorRole.Enemy);
            store.Add(record);

            var removed = store.Remove(record.Id);

            Assert.That(removed, Is.True);
            Assert.That(store.Count, Is.EqualTo(0));
            Assert.That(store.Contains(record.Id), Is.False);
        }

        [Test]
        public void Remove_MissingOrEmptyId_ReturnsFalse()
        {
            var store = new ActorStore();
            Assert.That(store.Remove(new ActorId(11)), Is.False);
            Assert.That(store.Remove(default), Is.False);
        }

        [Test]
        public void Clear_DropsEveryRecord()
        {
            var store = new ActorStore();
            store.Add(MakeRecord(1, "Player", ActorRole.Player));
            store.Add(MakeRecord(2, "Talker", ActorRole.Talker));

            store.Clear();

            Assert.That(store.Count, Is.EqualTo(0));
            Assert.That(store.Records.ToList(), Is.Empty);
        }

        [Test]
        public void Records_EnumeratesInInsertionOrder()
        {
            var store = new ActorStore();
            var first = MakeRecord(2, "Talker", ActorRole.Talker);
            var second = MakeRecord(7, "Guard", ActorRole.Guard);
            var third = MakeRecord(3, "Merchant", ActorRole.Merchant);

            store.Add(first);
            store.Add(second);
            store.Add(third);

            Assert.That(store.Records.ToList(), Is.EqualTo(new[] { first, second, third }));
        }

        [Test]
        public void Records_AfterRemove_PreservesRemainingOrder()
        {
            var store = new ActorStore();
            var first = MakeRecord(1, "Player", ActorRole.Player);
            var second = MakeRecord(2, "Talker", ActorRole.Talker);
            var third = MakeRecord(3, "Guard", ActorRole.Guard);

            store.Add(first);
            store.Add(second);
            store.Add(third);

            store.Remove(second.Id);

            Assert.That(store.Records.ToList(), Is.EqualTo(new[] { first, third }));
            Assert.That(store.Count, Is.EqualTo(2));
        }

        // Faz 1 role-view shims: lay the rail for migrating SliceWorldState's
        // named slice fields (Player/Talker/Merchant/Guard/Enemy) onto
        // ActorStore lookups by ActorRole. The concrete consumer is the next
        // Faz 1 PR (SliceWorldState reads these shims and marks its named
        // fields [Obsolete]).

        [Test]
        public void RecordsByRole_OnlyMatchingRoleInInsertionOrder()
        {
            var store = new ActorStore();
            var player = MakeRecord(1, "Player", ActorRole.Player);
            var firstGuard = MakeRecord(2, "GuardA", ActorRole.Guard);
            var talker = MakeRecord(3, "Talker", ActorRole.Talker);
            var secondGuard = MakeRecord(4, "GuardB", ActorRole.Guard);
            store.Add(player);
            store.Add(firstGuard);
            store.Add(talker);
            store.Add(secondGuard);

            Assert.That(
                store.RecordsByRole(ActorRole.Guard).ToList(),
                Is.EqualTo(new[] { firstGuard, secondGuard }));
        }

        [Test]
        public void RecordsByRole_NoMatch_ReturnsEmpty()
        {
            var store = new ActorStore();
            store.Add(MakeRecord(1, "Player", ActorRole.Player));

            Assert.That(store.RecordsByRole(ActorRole.Enemy).ToList(), Is.Empty);
        }

        [Test]
        public void RecordsByRole_EmptyStore_ReturnsEmpty()
        {
            var store = new ActorStore();
            Assert.That(store.RecordsByRole(ActorRole.Player).ToList(), Is.Empty);
        }

        [Test]
        public void FirstByRole_ReturnsFirstInInsertionOrder()
        {
            var store = new ActorStore();
            var firstMerchant = MakeRecord(1, "MerchantA", ActorRole.Merchant);
            var secondMerchant = MakeRecord(2, "MerchantB", ActorRole.Merchant);
            store.Add(firstMerchant);
            store.Add(secondMerchant);

            Assert.That(store.FirstByRole(ActorRole.Merchant), Is.SameAs(firstMerchant));
        }

        [Test]
        public void FirstByRole_NoMatch_Throws()
        {
            var store = new ActorStore();
            store.Add(MakeRecord(1, "Player", ActorRole.Player));

            Assert.Throws<InvalidOperationException>(() => store.FirstByRole(ActorRole.Enemy));
        }

        [Test]
        public void TryFirstByRole_KnownRole_ReturnsFirstAndTrue()
        {
            var store = new ActorStore();
            var player = MakeRecord(1, "Player", ActorRole.Player);
            var firstTalker = MakeRecord(2, "TalkerA", ActorRole.Talker);
            var secondTalker = MakeRecord(3, "TalkerB", ActorRole.Talker);
            store.Add(player);
            store.Add(firstTalker);
            store.Add(secondTalker);

            var found = store.TryFirstByRole(ActorRole.Talker, out var record);

            Assert.That(found, Is.True);
            Assert.That(record, Is.SameAs(firstTalker));
        }

        [Test]
        public void TryFirstByRole_NoMatch_ReturnsFalseAndNull()
        {
            var store = new ActorStore();
            store.Add(MakeRecord(1, "Player", ActorRole.Player));

            var found = store.TryFirstByRole(ActorRole.Enemy, out var record);

            Assert.That(found, Is.False);
            Assert.That(record, Is.Null);
        }

        [Test]
        public void TryFirstByRole_EmptyStore_ReturnsFalseAndNull()
        {
            var store = new ActorStore();

            var found = store.TryFirstByRole(ActorRole.Player, out var record);

            Assert.That(found, Is.False);
            Assert.That(record, Is.Null);
        }

        private static ActorRecord MakeRecord(ulong id, string name, ActorRole role)
        {
            var stats = new EmberStatBlock(10, 10, 10, 10, 10, 10);
            var vitals = new ActorVitals(
                new VitalStat(10, 10),
                new VitalStat(10, 10),
                new VitalStat(10, 10));
            var position = new GridPosition(0, 0);
            return new ActorRecord(
                new ActorId(id),
                name,
                role,
                stats,
                vitals,
                position,
                accuracy: 50,
                dodge: 10,
                armor: 0,
                baseDamage: 1);
        }
    }
}

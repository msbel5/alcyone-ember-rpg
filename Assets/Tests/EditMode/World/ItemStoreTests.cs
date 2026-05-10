using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.World;
using NUnit.Framework;

// Design note:
// These tests pin Faz 1's MATTER-box Core Store contract: ItemStore. They
// cover Add/Get/TryGet/Remove/Contains/Count/Clear, deterministic
// insertion-order enumeration, and rejection of the empty ItemId sentinel.
// Pure Domain — no Unity references. Mirrors ActorStoreTests / SiteStoreTests
// so the four Faz 1 stores share a single regression shape.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Pins Faz 1 ItemStore Add/Get/TryGet/Remove/Contains/Count/Clear/Records.</summary>
    public sealed class ItemStoreTests
    {
        [Test]
        public void Add_ThenGet_ReturnsSameRecord()
        {
            var store = new ItemStore();
            var record = MakeRecord(1, ItemMaterial.Iron, ItemQuality.Fine, EquipmentSlot.Weapon);

            store.Add(record);

            Assert.That(store.Get(record.Id), Is.SameAs(record));
            Assert.That(store.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_NullRecord_Throws()
        {
            var store = new ItemStore();
            Assert.Throws<ArgumentNullException>(() => store.Add(null));
        }

        [Test]
        public void Add_DuplicateId_Throws()
        {
            var store = new ItemStore();
            store.Add(MakeRecord(7, ItemMaterial.Wood, ItemQuality.Common, EquipmentSlot.None));
            Assert.Throws<InvalidOperationException>(
                () => store.Add(MakeRecord(7, ItemMaterial.Iron, ItemQuality.Masterwork, EquipmentSlot.Weapon)));
        }

        [Test]
        public void Get_MissingId_ThrowsKeyNotFound()
        {
            var store = new ItemStore();
            Assert.Throws<KeyNotFoundException>(() => store.Get(new ItemId(42UL)));
        }

        [Test]
        public void Get_EmptyId_Throws()
        {
            var store = new ItemStore();
            Assert.Throws<ArgumentException>(() => store.Get(default));
        }

        [Test]
        public void TryGet_MissingId_ReturnsFalseAndNull()
        {
            var store = new ItemStore();

            var found = store.TryGet(new ItemId(99UL), out var record);

            Assert.That(found, Is.False);
            Assert.That(record, Is.Null);
        }

        [Test]
        public void TryGet_EmptyId_ReturnsFalseAndNull()
        {
            var store = new ItemStore();
            store.Add(MakeRecord(1, ItemMaterial.Cloth, ItemQuality.Common, EquipmentSlot.None));

            var found = store.TryGet(default, out var record);

            Assert.That(found, Is.False);
            Assert.That(record, Is.Null);
        }

        [Test]
        public void TryGet_KnownId_ReturnsRecord()
        {
            var store = new ItemStore();
            var record = MakeRecord(3, ItemMaterial.Iron, ItemQuality.Masterwork, EquipmentSlot.Weapon);
            store.Add(record);

            var found = store.TryGet(record.Id, out var actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(record));
        }

        [Test]
        public void Contains_RespectsRegistrationAndEmptyId()
        {
            var store = new ItemStore();
            var record = MakeRecord(5, ItemMaterial.Wood, ItemQuality.Fine, EquipmentSlot.None);
            store.Add(record);

            Assert.That(store.Contains(record.Id), Is.True);
            Assert.That(store.Contains(new ItemId(99UL)), Is.False);
            Assert.That(store.Contains(default), Is.False);
        }

        [Test]
        public void Remove_KnownId_DropsRecordAndDecrementsCount()
        {
            var store = new ItemStore();
            var record = MakeRecord(11, ItemMaterial.Cloth, ItemQuality.Common, EquipmentSlot.None);
            store.Add(record);

            var removed = store.Remove(record.Id);

            Assert.That(removed, Is.True);
            Assert.That(store.Count, Is.EqualTo(0));
            Assert.That(store.Contains(record.Id), Is.False);
        }

        [Test]
        public void Remove_MissingOrEmptyId_ReturnsFalse()
        {
            var store = new ItemStore();
            Assert.That(store.Remove(new ItemId(11UL)), Is.False);
            Assert.That(store.Remove(default), Is.False);
        }

        [Test]
        public void Clear_DropsEveryRecord()
        {
            var store = new ItemStore();
            store.Add(MakeRecord(1, ItemMaterial.Wood, ItemQuality.Common, EquipmentSlot.None));
            store.Add(MakeRecord(2, ItemMaterial.Iron, ItemQuality.Fine, EquipmentSlot.Weapon));

            store.Clear();

            Assert.That(store.Count, Is.EqualTo(0));
            Assert.That(store.Records.ToList(), Is.Empty);
        }

        [Test]
        public void Records_EnumeratesInInsertionOrder()
        {
            var store = new ItemStore();
            var first = MakeRecord(2, ItemMaterial.Wood, ItemQuality.Common, EquipmentSlot.None);
            var second = MakeRecord(7, ItemMaterial.Iron, ItemQuality.Fine, EquipmentSlot.Weapon);
            var third = MakeRecord(3, ItemMaterial.Cloth, ItemQuality.Masterwork, EquipmentSlot.None);

            store.Add(first);
            store.Add(second);
            store.Add(third);

            Assert.That(store.Records.ToList(), Is.EqualTo(new[] { first, second, third }));
        }

        [Test]
        public void Records_AfterRemove_PreservesRemainingOrder()
        {
            var store = new ItemStore();
            var first = MakeRecord(1, ItemMaterial.Wood, ItemQuality.Common, EquipmentSlot.None);
            var second = MakeRecord(2, ItemMaterial.Iron, ItemQuality.Fine, EquipmentSlot.Weapon);
            var third = MakeRecord(3, ItemMaterial.Cloth, ItemQuality.Masterwork, EquipmentSlot.None);

            store.Add(first);
            store.Add(second);
            store.Add(third);

            store.Remove(second.Id);

            Assert.That(store.Records.ToList(), Is.EqualTo(new[] { first, third }));
            Assert.That(store.Count, Is.EqualTo(2));
        }

        private static ItemRecord MakeRecord(ulong id, ItemMaterial material, ItemQuality quality, EquipmentSlot slot)
        {
            return new ItemRecord(new ItemId(id), material, quality, slot);
        }
    }
}

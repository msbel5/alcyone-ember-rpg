using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using NUnit.Framework;

// Design note:
// These tests pin the ItemRecord constructor contract before ItemStore consumers
// exist. Coverage stays scoped to the pure record; allocation, lookup, save/load,
// pricing, and bonuses belong elsewhere. Mirrors SiteRecordTests so the four
// Phase 1 stores converge on one regression shape.
namespace EmberCrpg.Tests.EditMode.Inventory
{
    /// <summary>Verifies the pure-Domain invariants required of ItemRecord.</summary>
    public sealed class ItemRecordTests
    {
        private static ItemRecord MakeRecord()
        {
            return new ItemRecord(
                new ItemId(11UL),
                ItemMaterial.Iron,
                ItemQuality.Fine,
                EquipmentSlot.Weapon);
        }

        /// <summary>Constructor stores every field exactly as supplied.</summary>
        [Test]
        public void Constructor_StoresFields()
        {
            var record = MakeRecord();

            Assert.That(record.Id, Is.EqualTo(new ItemId(11UL)));
            Assert.That(record.Material, Is.EqualTo(ItemMaterial.Iron));
            Assert.That(record.Quality, Is.EqualTo(ItemQuality.Fine));
            Assert.That(record.Slot, Is.EqualTo(EquipmentSlot.Weapon));
        }

        /// <summary>Equipment slot drives the IsEquipment view shim.</summary>
        [Test]
        public void IsEquipment_TrueForSlottedRecord()
        {
            var record = MakeRecord();

            Assert.That(record.IsEquipment, Is.True);
        }

        /// <summary>Non-equipment records (Slot=None) are legal and report IsEquipment=false.</summary>
        [Test]
        public void Constructor_AllowsNoneSlot_ForNonEquipmentRecord()
        {
            var record = new ItemRecord(
                new ItemId(3UL),
                ItemMaterial.Cloth,
                ItemQuality.Common,
                EquipmentSlot.None);

            Assert.That(record.Slot, Is.EqualTo(EquipmentSlot.None));
            Assert.That(record.IsEquipment, Is.False);
        }

        /// <summary>The empty ItemId sentinel cannot back a record.</summary>
        [Test]
        public void Constructor_RejectsEmptyId()
        {
            Assert.Throws<ArgumentException>(() => new ItemRecord(
                default,
                ItemMaterial.Iron,
                ItemQuality.Common,
                EquipmentSlot.None));
        }

        /// <summary>The None sentinel material is rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsNoneMaterial()
        {
            Assert.Throws<ArgumentException>(() => new ItemRecord(
                new ItemId(1UL),
                ItemMaterial.None,
                ItemQuality.Common,
                EquipmentSlot.None));
        }

        /// <summary>The None sentinel quality is rejected at construction.</summary>
        [Test]
        public void Constructor_RejectsNoneQuality()
        {
            Assert.Throws<ArgumentException>(() => new ItemRecord(
                new ItemId(1UL),
                ItemMaterial.Iron,
                ItemQuality.None,
                EquipmentSlot.None));
        }
    }
}

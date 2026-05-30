using System;
using System.IO;
using EmberCrpg.Data.Save;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>EMB-011: durable file-based save slots + corrupt-save quarantine.</summary>
    public sealed class FileSaveRepositoryTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "ember-saverepo-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test]
        public void Save_Then_TryLoad_Roundtrips()
        {
            var repo = new FileSaveRepository(_root);
            repo.Save(0, "{\"hello\":1}");
            Assert.That(repo.SlotExists(0), Is.True);
            Assert.That(repo.TryLoad(0, _ => true, out var json), Is.True);
            Assert.That(json, Is.EqualTo("{\"hello\":1}"));
        }

        [Test]
        public void TryLoad_MissingSlot_ReturnsFalse()
        {
            var repo = new FileSaveRepository(_root);
            Assert.That(repo.TryLoad(3, _ => true, out var json), Is.False);
            Assert.That(json, Is.Null);
        }

        [Test]
        public void TryLoad_CorruptContent_QuarantinesAndReturnsFalse()
        {
            var repo = new FileSaveRepository(_root);
            repo.Save(1, "not-json-garbage");
            // isValid rejects it -> quarantined
            Assert.That(repo.TryLoad(1, raw => raw.StartsWith("{"), out var json), Is.False);
            Assert.That(json, Is.Null);
            Assert.That(repo.SlotExists(1), Is.False, "corrupt slot should be freed");
            Assert.That(File.Exists(repo.SlotPath(1) + ".corrupt"), Is.True, "bad save quarantined");
        }

        [Test]
        public void ListSlots_ReportsOccupied()
        {
            var repo = new FileSaveRepository(_root);
            repo.Save(0, "{}");
            repo.Save(2, "{}");
            var slots = repo.ListSlots();
            Assert.That(slots, Is.EquivalentTo(new[] { 0, 2 }));
        }

        [Test]
        public void Save_Overwrite_DoesNotLeaveTmp()
        {
            var repo = new FileSaveRepository(_root);
            repo.Save(0, "{\"v\":1}");
            repo.Save(0, "{\"v\":2}");
            Assert.That(repo.TryLoad(0, _ => true, out var json), Is.True);
            Assert.That(json, Is.EqualTo("{\"v\":2}"));
            Assert.That(File.Exists(repo.SlotPath(0) + ".tmp"), Is.False);
        }
    }
}

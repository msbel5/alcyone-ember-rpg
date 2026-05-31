using System;
using System.IO;
using System.Linq;
using EmberCrpg.Data.Save;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    public sealed class SaveSlotRepositoryTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "ember-slotrepo-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void SaveTyped_RoundtripsPayloadAndMetadata()
        {
            var repo = new FileSaveRepository(_root);
            var slot = SaveSlotId.Manual(2);
            var payload = PayloadJson("Scene_A", 7, 128);
            var meta = new SaveSlotMetadata
            {
                metadataVersion = 1,
                envelopeVersion = 3,
                schemaVersion = 7,
                slotKind = "Manual",
                slotIndex = 2,
                label = "Before Boss",
                sceneName = "Scene_A",
                playtimeMinutes = 128,
                savedAtUtcIso = "2026-05-31T20:38:00Z",
                thumbnailPath = string.Empty
            };

            repo.Save(slot, payload, meta);

            Assert.That(repo.SlotExists(slot), Is.True);
            Assert.That(repo.TryLoadPayload(slot, _ => true, out var loadedPayload), Is.True);
            Assert.That(loadedPayload, Is.EqualTo(payload));
            Assert.That(repo.TryLoadMetadata(slot, out var loadedMeta), Is.True);
            Assert.That(loadedMeta.slotKind, Is.EqualTo("Manual"));
            Assert.That(loadedMeta.slotIndex, Is.EqualTo(2));
            Assert.That(loadedMeta.label, Is.EqualTo("Before Boss"));
            Assert.That(loadedMeta.sceneName, Is.EqualTo("Scene_A"));
            Assert.That(loadedMeta.playtimeMinutes, Is.EqualTo(128));
            Assert.That(loadedMeta.schemaVersion, Is.EqualTo(7));
            Assert.That(loadedMeta.envelopeVersion, Is.EqualTo(3));
        }

        [Test]
        public void ListAll_DeterministicOrder_QuickAutoManualAscending()
        {
            var repo = new FileSaveRepository(_root);
            repo.Save(SaveSlotId.Manual(2), PayloadJson("M2", 1, 20), Meta("M2", "Manual 3"));
            repo.Save(SaveSlotId.Quick, PayloadJson("Q", 1, 10), Meta("Q", "Quick"));
            repo.Save(SaveSlotId.Auto, PayloadJson("A", 1, 11), Meta("A", "Auto"));
            repo.Save(SaveSlotId.Manual(0), PayloadJson("M0", 1, 12), Meta("M0", "Manual 1"));

            var all = repo.ListAll(4);

            Assert.That(all.Count, Is.EqualTo(4));
            Assert.That(all.Select(x => x.slotKind).ToArray(), Is.EqualTo(new[] { "Quick", "Auto", "Manual", "Manual" }));
            Assert.That(all.Select(x => x.slotIndex).ToArray(), Is.EqualTo(new[] { 0, 0, 0, 2 }));
            Assert.That(all.Select(x => x.sceneName).ToArray(), Is.EqualTo(new[] { "Q", "A", "M0", "M2" }));
        }

        [Test]
        public void Delete_RemovesPayloadSidecarAndCorruptSiblings()
        {
            var repo = new FileSaveRepository(_root);
            var slot = SaveSlotId.Manual(1);
            repo.Save(slot, PayloadJson("DeleteMe", 4, 99), Meta("DeleteMe", "DeleteMe"));

            File.WriteAllText(repo.SlotPath(slot) + ".corrupt", "x");
            File.WriteAllText(repo.SlotPath(slot) + ".corrupt.2", "y");

            Assert.That(repo.Delete(slot), Is.True);
            Assert.That(repo.SlotExists(slot), Is.False);
            Assert.That(File.Exists(repo.MetadataPath(slot)), Is.False);
            Assert.That(File.Exists(repo.SlotPath(slot) + ".corrupt"), Is.False);
            Assert.That(File.Exists(repo.SlotPath(slot) + ".corrupt.2"), Is.False);
            Assert.That(repo.Delete(slot), Is.False);
        }

        [Test]
        public void ListAll_CorruptSidecar_ReconstructsFromPayload()
        {
            var repo = new FileSaveRepository(_root);
            var slot = SaveSlotId.Manual(0);
            repo.Save(slot, PayloadJson("Forge", 5, 3456), Meta("WrongScene", "Old Label"));

            File.WriteAllText(repo.MetadataPath(slot), "{");
            Assert.That(repo.TryLoadMetadata(slot, out _), Is.False, "sidecar should be rejected as corrupt");

            var all = repo.ListAll(1);
            Assert.That(all.Count, Is.EqualTo(1));
            Assert.That(all[0].slotKind, Is.EqualTo("Manual"));
            Assert.That(all[0].slotIndex, Is.EqualTo(0));
            Assert.That(all[0].sceneName, Is.EqualTo("Forge"));
            Assert.That(all[0].playtimeMinutes, Is.EqualTo(3456));
            Assert.That(all[0].schemaVersion, Is.EqualTo(5));
            Assert.That(all[0].savedAtUtcIso, Is.EqualTo(string.Empty));
        }

        private static SaveSlotMetadata Meta(string sceneName, string label)
        {
            return new SaveSlotMetadata
            {
                metadataVersion = 1,
                envelopeVersion = 1,
                schemaVersion = 1,
                slotKind = "Manual",
                slotIndex = 0,
                label = label,
                sceneName = sceneName,
                playtimeMinutes = 1,
                savedAtUtcIso = "2026-05-31T20:38:00Z",
                thumbnailPath = string.Empty
            };
        }

        private static string PayloadJson(string sceneName, int schemaVersion, long totalMinutes)
        {
            var domain = "{\"schemaVersion\":" + schemaVersion + ",\"totalMinutes\":" + totalMinutes + "}";
            var escapedDomain = domain.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return "{\"sceneName\":\"" + sceneName + "\",\"domainStateJson\":\"" + escapedDomain + "\"}";
        }
    }
}

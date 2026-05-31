#if UNITY_EDITOR
using System;
using System.IO;
using EmberCrpg.Data.Save;
using EmberCrpg.Presentation.Ember;
using EmberCrpg.Presentation.Ember.Save;
using NUnit.Framework;
using UnityEngine;

namespace EmberCrpg.Tests.EditMode.Save
{
    public sealed class EmberSaveServiceResolutionTests
    {
        [SetUp]
        public void SetUp()
        {
            ClearPrefs();
        }

        [TearDown]
        public void TearDown()
        {
            ClearPrefs();
        }

        [Test]
        public void AuditIsLoadableSaveJson_RejectsEmptyMalformedAndMissingScene()
        {
            Assert.That(EmberSaveService.AuditIsLoadableSaveJson(null), Is.False);
            Assert.That(EmberSaveService.AuditIsLoadableSaveJson(string.Empty), Is.False);
            Assert.That(EmberSaveService.AuditIsLoadableSaveJson("{"), Is.False);
            Assert.That(EmberSaveService.AuditIsLoadableSaveJson(Json(string.Empty)), Is.False);
        }

        [Test]
        public void AuditResolveLatestSaveJson_PrefersLastFileSlotOverLegacyPlayerPrefs()
        {
            var root = NewTempRoot();
            try
            {
                var repo = new FileSaveRepository(root);
                var fileJson = Json(EmberScenes.SmithingOverworld);
                var legacyJson = Json(EmberScenes.TavernDialog);
                repo.Save(EmberSaveService.AuditDefaultSlot, fileJson);
                PlayerPrefs.SetInt(EmberSaveService.AuditLastSlotKey, EmberSaveService.AuditDefaultSlot);
                PlayerPrefs.SetString(EmberSaveService.AuditSaveKey, legacyJson);

                Assert.That(EmberSaveService.AuditResolveLatestSaveJson(repo), Is.EqualTo(fileJson));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void AuditResolveLatestSaveJson_PrefersNamedQuickSlotOverLegacyIntegerSlot()
        {
            var root = NewTempRoot();
            try
            {
                var repo = new FileSaveRepository(root);
                var legacyJson = Json(EmberScenes.SeasonFarm);
                var quickJson = Json(EmberScenes.SmithingOverworld);
                repo.Save(EmberSaveService.AuditDefaultSlot, legacyJson);
                repo.Save(SaveSlotId.Quick, quickJson, new SaveSlotMetadata
                {
                    label = "Quicksave",
                    sceneName = EmberScenes.SmithingOverworld,
                });
                PlayerPrefs.SetInt(EmberSaveService.AuditLastSlotKey, EmberSaveService.AuditDefaultSlot);

                Assert.That(EmberSaveService.AuditResolveLatestSaveJson(repo), Is.EqualTo(quickJson));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void AuditResolveLatestSaveJson_CorruptQuickSlotFallsBackToLegacyIntegerSlot()
        {
            var root = NewTempRoot();
            try
            {
                var repo = new FileSaveRepository(root);
                var legacyJson = Json(EmberScenes.SeasonFarm);
                repo.Save(EmberSaveService.AuditDefaultSlot, legacyJson);
                Directory.CreateDirectory(Path.GetDirectoryName(repo.SlotPath(SaveSlotId.Quick)));
                File.WriteAllText(repo.SlotPath(SaveSlotId.Quick), "{");
                PlayerPrefs.SetInt(EmberSaveService.AuditLastSlotKey, EmberSaveService.AuditDefaultSlot);

                Assert.That(EmberSaveService.AuditResolveLatestSaveJson(repo), Is.EqualTo(legacyJson));
                Assert.That(File.Exists(repo.SlotPath(SaveSlotId.Quick) + ".corrupt"), Is.True);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void AuditResolveLatestSaveJson_QuarantinesCorruptSlotThenFallsBackToLegacy()
        {
            var root = NewTempRoot();
            try
            {
                var repo = new FileSaveRepository(root);
                Directory.CreateDirectory(Path.GetDirectoryName(repo.SlotPath(EmberSaveService.AuditDefaultSlot)));
                File.WriteAllText(repo.SlotPath(EmberSaveService.AuditDefaultSlot), "{");
                var legacyJson = Json(EmberScenes.SmithingOverworld);
                PlayerPrefs.SetInt(EmberSaveService.AuditLastSlotKey, EmberSaveService.AuditDefaultSlot);
                PlayerPrefs.SetString(EmberSaveService.AuditSaveKey, legacyJson);

                Assert.That(EmberSaveService.AuditResolveLatestSaveJson(repo), Is.EqualTo(legacyJson));
                Assert.That(File.Exists(repo.SlotPath(EmberSaveService.AuditDefaultSlot) + ".corrupt"), Is.True);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void TryResolveLatestSave_UsesPlayerPrefsFallbackWhenSelectedSlotMissing()
        {
            PlayerPrefs.SetInt(EmberSaveService.AuditLastSlotKey, 999);
            PlayerPrefs.SetString(EmberSaveService.AuditSaveKey, Json(EmberScenes.SmithingOverworld));

            Assert.That(EmberSaveService.TryResolveLatestSave(out var data), Is.True);
            Assert.That(data.sceneName, Is.EqualTo(EmberScenes.SmithingOverworld));
        }

        [Test]
        public void TryResolveLatestSave_RejectsMalformedPlayerPrefsFallback()
        {
            PlayerPrefs.SetInt(EmberSaveService.AuditLastSlotKey, 999);
            PlayerPrefs.SetString(EmberSaveService.AuditSaveKey, "{");

            Assert.That(EmberSaveService.TryResolveLatestSave(out _), Is.False);
        }

        [Test]
        public void AuditIsKnownBuildScene_RejectsInvalidSceneNames()
        {
            Assert.That(EmberSaveService.AuditIsKnownBuildScene(EmberScenes.SmithingOverworld), Is.True);
            Assert.That(EmberSaveService.AuditIsKnownBuildScene("__missing_scene__"), Is.False);
        }

        private static string Json(string sceneName)
        {
            return JsonUtility.ToJson(new SaveData { sceneName = sceneName });
        }

        private static string NewTempRoot()
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-save-resolution-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        private static void ClearPrefs()
        {
            PlayerPrefs.DeleteKey(EmberSaveService.AuditSaveKey);
            PlayerPrefs.DeleteKey(EmberSaveService.AuditLastSlotKey);
            PlayerPrefs.Save();
        }
    }
}
#endif

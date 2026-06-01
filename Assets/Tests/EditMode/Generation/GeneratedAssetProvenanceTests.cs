using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Generation
{
    public sealed class GeneratedAssetProvenanceTests
    {
        [Test]
        public void GeneratedEntry_WithNoStamp_IsRequiresGeneration()
        {
            var root = CreateTempRoot();
            try
            {
                var entry = NewGeneratedEntry("dice", "dice");
                WriteAsset(root, entry, new byte[] { 1, 2, 3 });

                var report = AssetManifestScanner
                    .ScanAsync(new[] { entry }, root, CancellationToken.None, StaticPromptCatalog.CreateDefault())
                    .Result;

                Assert.That(report.Entries[0].State, Is.EqualTo(EntryState.RequiresGeneration));
                Assert.That(report.Entries[0].Reason, Is.EqualTo("stale_missing_provenance"));
            }
            finally { Delete(root); }
        }

        [Test]
        public void GeneratedEntry_WithMatchingStamp_IsCached()
        {
            var root = CreateTempRoot();
            try
            {
                var catalog = StaticPromptCatalog.CreateDefault();
                var entry = NewGeneratedEntry("dice", "dice");
                var assetPath = WriteAsset(root, entry, new byte[] { 1, 2, 3 });
                GeneratedAssetProvenance.Write(assetPath, entry, catalog);

                var report = AssetManifestScanner
                    .ScanAsync(new[] { entry }, root, CancellationToken.None, catalog)
                    .Result;

                Assert.That(report.Entries[0].State, Is.EqualTo(EntryState.Cached));
            }
            finally { Delete(root); }
        }

        [Test]
        public void GeneratedEntry_WithChangedPrompt_IsRequiresGeneration()
        {
            var root = CreateTempRoot();
            try
            {
                var entry = NewGeneratedEntry("dice", "dice");
                var assetPath = WriteAsset(root, entry, new byte[] { 1, 2, 3 });
                GeneratedAssetProvenance.Write(assetPath, entry, StaticPromptCatalog.CreateDefault());

                var changed = new StaticPromptCatalog(new Dictionary<string, string>
                {
                    ["dice"] = StaticPromptCatalog.EmberStyleHeader
                        + ", one centered carved iron die, "
                        + StaticPromptCatalog.EmberNegativeFooter
                });

                var report = AssetManifestScanner
                    .ScanAsync(new[] { entry }, root, CancellationToken.None, changed)
                    .Result;

                Assert.That(report.Entries[0].State, Is.EqualTo(EntryState.RequiresGeneration));
                Assert.That(report.Entries[0].Reason, Is.EqualTo("stale_prompt_version"));
            }
            finally { Delete(root); }
        }

        [Test]
        public void DefaultStaticIconPrompts_RequestSingleCenteredSubjects()
        {
            var catalog = StaticPromptCatalog.CreateDefault();
            Assert.That(StaticPromptCatalog.EmberGenerationNegative, Does.Contain("no multiple objects"));
            Assert.That(StaticPromptCatalog.EmberGenerationNegative, Does.Contain("no scattered objects"));
            foreach (var entry in CoreAssetManifest.CreateDefault().Entries.Where(e => e.RequiresGeneration))
            {
                if (entry.Category == "environment" || entry.Category == "splash") continue;
                Assert.That(catalog.TryGetPrompt(entry.StaticPromptKey, out var prompt), Is.True, entry.Id);
                Assert.That(prompt, Does.Contain("single subject centered"), entry.Id);
                Assert.That(prompt.ToLowerInvariant(), Does.Not.Contain("four "), entry.Id);
                Assert.That(prompt.ToLowerInvariant(), Does.Not.Contain("scattered"), entry.Id);
            }
        }

        private static ManifestEntry NewGeneratedEntry(string id, string promptKey)
        {
            return new ManifestEntry(id, "ui", "Assets/Generated/Core/" + id + ".png", promptKey, 64, 64, true, 300, "sd15-lcm");
        }

        private static string WriteAsset(string root, ManifestEntry entry, byte[] bytes)
        {
            var path = AssetManifestScanner.Resolve(root, entry.ExpectedPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
            return path;
        }

        private static string CreateTempRoot()
        {
            return Path.Combine(Path.GetTempPath(), "ember-provenance-" + System.Guid.NewGuid().ToString("N"));
        }

        private static void Delete(string root)
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}

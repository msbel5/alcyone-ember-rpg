using System;
using System.IO;
using System.Text;
using EmberCrpg.Simulation.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class ModelManifestTests
    {
        [Test]
        public void LoadFromJson_ParsesArrayEntries()
        {
            var json = @"[
                { ""id"": ""a"", ""path"": ""sub/a.gguf"", ""size"": 12345, ""sha256"": ""abc"", ""url"": ""https://x/y.gguf"" },
                { ""id"": ""b"", ""path"": ""b.onnx"", ""size"": 0, ""sha256"": ""TBD"", ""url"": """" }
            ]";

            var entries = ModelManifest.LoadFromJson(json);
            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].Id, Is.EqualTo("a"));
            Assert.That(entries[0].Path, Is.EqualTo("sub/a.gguf"));
            Assert.That(entries[0].Size, Is.EqualTo(12345));
            Assert.That(entries[0].Sha256, Is.EqualTo("abc"));
            Assert.That(entries[0].Url, Is.EqualTo("https://x/y.gguf"));
            Assert.That(entries[1].Sha256, Is.EqualTo("TBD"));
        }

        [Test]
        public void LoadFromJson_EmptyOrNull_ReturnsEmpty()
        {
            Assert.That(ModelManifest.LoadFromJson(null).Count, Is.EqualTo(0));
            Assert.That(ModelManifest.LoadFromJson("").Count, Is.EqualTo(0));
            Assert.That(ModelManifest.LoadFromJson("[]").Count, Is.EqualTo(0));
        }

        [Test]
        public void VerifyAllPresent_ReportsMissingFiles()
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-manifest-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var existing = Path.Combine(root, "present.bin");
                File.WriteAllBytes(existing, new byte[] { 1, 2, 3, 4, 5 });

                var entries = new[]
                {
                    new ModelEntry("present", "present.bin", 5, "TBD", string.Empty),
                    new ModelEntry("missing", "missing.bin", 5, "TBD", string.Empty),
                };

                var missing = ModelManifest.VerifyAllPresent(entries, root);
                Assert.That(missing.Count, Is.EqualTo(1));
                Assert.That(missing[0].Id, Is.EqualTo("missing"));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void VerifyAllPresent_DetectsSha256Mismatch()
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-manifest-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var filePath = Path.Combine(root, "data.bin");
                File.WriteAllBytes(filePath, Encoding.ASCII.GetBytes("hello"));
                var actual = ModelManifest.ComputeSha256(filePath);
                Assert.That(actual.Length, Is.EqualTo(64));

                var entries = new[]
                {
                    new ModelEntry("good", "data.bin", 5, actual, string.Empty),
                    new ModelEntry("bad", "data.bin", 5, new string('0', 64), string.Empty),
                };
                var missing = ModelManifest.VerifyAllPresent(entries, root);
                Assert.That(missing.Count, Is.EqualTo(1));
                Assert.That(missing[0].Id, Is.EqualTo("bad"));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void VerifyAllPresent_HashPlaceholder_SkipsCheck()
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-manifest-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var filePath = Path.Combine(root, "data.bin");
                File.WriteAllBytes(filePath, new byte[] { 1, 2, 3 });

                // Placeholder hashes should be accepted regardless of file contents.
                foreach (var placeholder in new[] { "", "TBD", "PENDING", "placeholder-future-hash" })
                {
                    var entries = new[] { new ModelEntry("e", "data.bin", 3, placeholder, string.Empty) };
                    var missing = ModelManifest.VerifyAllPresent(entries, root);
                    Assert.That(missing.Count, Is.EqualTo(0), "placeholder '" + placeholder + "' should be skipped");
                }
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void ResolvePath_HandlesUnixAndWindowsSeparators()
        {
            var root = Path.Combine("base", "root");
            var resolved = ModelManifest.ResolvePath(root, "sub/a/b.txt");
            Assert.That(resolved, Does.EndWith("b.txt"));
            // OS-specific separator normalisation
            Assert.That(resolved, Does.Contain("sub" + Path.DirectorySeparatorChar + "a" + Path.DirectorySeparatorChar + "b.txt"));
        }

        // EMB-005 regression guard: the SHIPPED StreamingAssets manifest must declare paths that
        // match the real nested on-disk model layout the runtime (ForgeBootstrap) loads:
        //   sdxl-turbo/<component>/model.onnx, sd-1.5/<component>/model.onnx, all-minilm-l6-v2/...
        // The old manifest had flattened paths (sdxl-turbo/text_encoder.onnx) and wrong dir names
        // (minilm-l6-v2, sd15-lcm) so VerifyAllPresent reported everything missing and the
        // downloader would have fetched the wrong layout. This test fails if anyone reverts that.
        [Test]
        public void ShippedManifest_PathsMatchNestedRuntimeLayout()
        {
            var manifestPath = FindShippedManifest();
            if (manifestPath == null)
            {
                Assert.Ignore("StreamingAssets/Models/manifest.json not reachable from test host; skipping shipped-manifest check.");
                return;
            }

            var entries = ModelManifest.LoadFromJson(File.ReadAllText(manifestPath));
            Assert.That(entries.Count, Is.GreaterThan(0), "shipped manifest parsed to zero entries");

            var modelsRoot = Path.GetDirectoryName(manifestPath);
            foreach (var e in entries)
            {
                // No flattened diffusion component paths — every onnx component lives in its own dir.
                Assert.That(e.Path, Does.Not.Match(@"^(sdxl-turbo|sd-1\.5)/[^/]+\.onnx$"),
                    "flattened onnx path (should be <dir>/<component>/model.onnx): " + e.Path);
                // Correct directory names.
                Assert.That(e.Path, Does.Not.StartWith("minilm-l6-v2/"),
                    "wrong dir name (should be all-minilm-l6-v2/): " + e.Path);
                Assert.That(e.Path, Does.Not.StartWith("sd15-lcm/"),
                    "wrong dir name (should be sd-1.5/): " + e.Path);

                // Every declared path must resolve to a real file in the bundle.
                var full = ModelManifest.ResolvePath(modelsRoot, e.Path);
                Assert.That(File.Exists(full), Is.True, "manifest entry '" + e.Id + "' path missing on disk: " + e.Path);

                // The deprecated larger-tier 3B entry must not be in the shipped verify manifest
                // (not bundled; larger tiers are a future opt-in download, documented separately).
                Assert.That(e.Id, Does.Not.Contain("3b"), "shipped manifest must not list the un-bundled qwen 3B tier");
            }
        }

        // Walk up from the test host's base directory looking for the project's StreamingAssets
        // manifest. Works under both Unity EditMode (base dir inside the project) and the pure-C#
        // fallback harness (base dir inside the repo). Returns null if not found.
        private static string FindShippedManifest()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int hops = 0; dir != null && hops < 12; hops++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Models", "manifest.json");
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }
    }
}

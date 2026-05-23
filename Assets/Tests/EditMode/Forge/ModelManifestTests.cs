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
    }
}

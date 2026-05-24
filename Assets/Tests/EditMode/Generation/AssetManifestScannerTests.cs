using System.IO;
using System.Linq;
using System.Threading;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Generation
{
    public sealed class AssetManifestScannerTests
    {
        [Test]
        public void EmptyCacheMarksGeneratedEntriesMissing()
        {
            var root = CreateTempRoot();
            try
            {
                var report = AssetManifestScanner.ScanAsync(CoreAssetManifest.CreateDefault().Entries, root, CancellationToken.None).Result;
                Assert.That(report.Total, Is.EqualTo(CoreAssetManifest.CreateDefault().Entries.Count));
                Assert.That(report.Missing, Is.GreaterThan(0));
                Assert.That(report.Entries.Any(e => e.State == EntryState.Missing), Is.True);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test]
        public void FullCacheMarksEntriesCachedAndIsIdempotent()
        {
            var root = CreateTempRoot();
            try
            {
                foreach (var entry in CoreAssetManifest.CreateDefault().Entries)
                {
                    var path = Path.Combine(root, entry.ExpectedPath.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
                }

                var first = AssetManifestScanner.ScanAsync(CoreAssetManifest.CreateDefault().Entries, root, CancellationToken.None).Result;
                var second = AssetManifestScanner.ScanAsync(CoreAssetManifest.CreateDefault().Entries, root, CancellationToken.None).Result;
                Assert.That(first.Cached, Is.EqualTo(first.Total));
                Assert.That(second.Cached, Is.EqualTo(first.Cached));
                Assert.That(second.Missing, Is.EqualTo(first.Missing));
            }
            finally { Directory.Delete(root, true); }
        }

        private static string CreateTempRoot()
        {
            return Path.Combine(Path.GetTempPath(), "ember-manifest-scan-" + System.Guid.NewGuid().ToString("N"));
        }
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Generation
{
    public sealed class VisibleGenerationFlowTests
    {
        [Test]
        public void ScanRowsAndCachedThumbnail_AreExposedBeforeTopUp()
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-visible-flow-" + Guid.NewGuid().ToString("N"));
            var cachedPath = "Assets/Generated/Core/item_cached.png";
            var generatedPath = "Assets/Generated/Core/item_missing.png";

            try
            {
                var cachedAbsolute = AssetManifestScanner.Resolve(root, cachedPath);
                Directory.CreateDirectory(Path.GetDirectoryName(cachedAbsolute));
                File.WriteAllBytes(cachedAbsolute, new byte[] { 137, 80, 78, 71 });

                var entries = new[]
                {
                    new ManifestEntry("item_cached", "item", cachedPath, "item_sword", 16, 16, true, 5, "sd15-lcm"),
                    new ManifestEntry("item_missing", "item", generatedPath, "item_bow", 16, 16, true, 5, "sd15-lcm"),
                    new ManifestEntry("font_missing", "font", "Assets/TextMesh Pro/Resources/Fonts & Materials/never.asset", "", 1, 1, false, 5, ""),
                };

                var scanStates = new List<EntryState>();
                var scanThumbCount = 0;
                var flow = new VisibleGenerationFlow(root, new FakeForge(""), StaticPromptCatalog.CreateDefault(), new GenerationFailureLog(Path.Combine(root, "Logs", "generation-failures.json")));
                flow.ScanRow += (row, entry) => scanStates.Add(row.State);
                flow.ScanThumbnail += (row, entry, bytes) => scanThumbCount++;

                var result = flow.RunCoreAssetTopUpAsync(entries, CancellationToken.None).Result;

                Assert.That(result.ScanReport.Total, Is.EqualTo(3));
                Assert.That(scanStates, Does.Contain(EntryState.Cached));
                Assert.That(scanStates, Does.Contain(EntryState.RequiresGeneration));
                Assert.That(result.RequestedGeneration, Is.EqualTo(1));
                Assert.That(result.SucceededGeneration, Is.EqualTo(1));
                Assert.That(result.FailedGeneration, Is.EqualTo(0));
                Assert.That(scanThumbCount, Is.GreaterThanOrEqualTo(1));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void TopUp_ContinuesAfterFailure_AndLogsFailureLine()
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-visible-flow-" + Guid.NewGuid().ToString("N"));
            var logPath = Path.Combine(root, "Logs", "generation-failures.json");

            try
            {
                var entries = new[]
                {
                    new ManifestEntry("item_a", "item", "Assets/Generated/Core/item_a.png", "item_sword", 16, 16, true, 5, "sd15-lcm"),
                    new ManifestEntry("item_b", "item", "Assets/Generated/Core/item_b.png", "item_bow", 16, 16, true, 5, "sd15-lcm"),
                };
                var flow = new VisibleGenerationFlow(root, new FakeForge("item_b"), StaticPromptCatalog.CreateDefault(), new GenerationFailureLog(logPath));
                var result = flow.RunCoreAssetTopUpAsync(entries, CancellationToken.None).Result;

                Assert.That(result.RequestedGeneration, Is.EqualTo(2));
                Assert.That(result.StartedGeneration, Is.EqualTo(2));
                Assert.That(result.SucceededGeneration, Is.EqualTo(1));
                Assert.That(result.FailedGeneration, Is.EqualTo(1));
                Assert.That(File.ReadAllLines(logPath).Length, Is.EqualTo(1));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private sealed class FakeForge : IAssetForge
        {
            private readonly string _failId;

            public FakeForge(string failId)
            {
                _failId = failId;
            }

            public bool IsAvailable() => true;

            public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                if (request.RequestId == _failId)
                    return Task.FromResult(AssetGenerationResult.Failed(request.RequestId, "fake_failure"));

                return Task.FromResult(new AssetGenerationResult(request.RequestId, new byte[] { 137, 80, 78, 71 }, "image/png", 1, true, ""));
            }
        }
    }
}

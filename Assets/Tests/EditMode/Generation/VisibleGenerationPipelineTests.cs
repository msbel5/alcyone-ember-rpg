using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Generation
{
    public sealed class VisibleGenerationPipelineTests
    {
        [Test]
        public void RunContinuesAfterFailureAndAppendsOneJsonLine()
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-visible-pipeline-" + Guid.NewGuid().ToString("N"));
            var logPath = Path.Combine(root, "Logs", "generation-failures.json");
            try
            {
                var entries = new[]
                {
                    new ManifestEntry("a", "item", "Assets/Generated/a.png", "item_sword", 16, 16, true, 5, ""),
                    new ManifestEntry("b", "item", "Assets/Generated/b.png", "item_bow", 16, 16, true, 5, ""),
                    new ManifestEntry("c", "item", "Assets/Generated/c.png", "item_staff", 16, 16, true, 5, ""),
                };
                var events = new List<string>();
                var pipeline = new VisibleGenerationPipeline(root, new FakeForge("b"), StaticPromptCatalog.CreateDefault(), new GenerationFailureLog(logPath));
                pipeline.EntryStarted += e => events.Add("start:" + e.Id);
                pipeline.EntrySucceeded += (e, bytes, ms) => events.Add("ok:" + e.Id);
                pipeline.EntryFailed += (e, reason, ex) => events.Add("fail:" + e.Id + ":" + reason);
                pipeline.Completed += r => events.Add("done:" + r.Succeeded + ":" + r.Failed);

                var result = pipeline.RunAsync(entries, CancellationToken.None).Result;

                Assert.That(result.Succeeded, Is.EqualTo(2));
                Assert.That(result.Failed, Is.EqualTo(1));
                Assert.That(events, Is.EqualTo(new[] { "start:a", "ok:a", "start:b", "fail:b:fake_failure", "start:c", "ok:c", "done:2:1" }));
                Assert.That(File.ReadAllLines(logPath).Length, Is.EqualTo(1));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test]
        public void CancellationStopsCleanly()
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-visible-pipeline-" + Guid.NewGuid().ToString("N"));
            try
            {
                var entries = new[] { new ManifestEntry("a", "item", "Assets/Generated/a.png", "item_sword", 16, 16, true, 5, "") };
                using (var cts = new CancellationTokenSource())
                {
                    cts.Cancel();
                    var pipeline = new VisibleGenerationPipeline(root, new FakeForge(""), StaticPromptCatalog.CreateDefault(), new GenerationFailureLog(Path.Combine(root, "Logs", "generation-failures.json")));
                    Assert.That(
                        async () => await pipeline.RunAsync(entries, cts.Token),
                        Throws.InstanceOf<OperationCanceledException>());
                }
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        // B17 (W33-05 fix 2): a placeholder "success" writes the PNG (the loading run still
        // shows SOMETHING) but must NEVER receive a .promptmeta stamp — a stamped placeholder
        // reads "fresh" forever and the 8x8 grey freezes as canonical even after a real
        // model is installed. No stamp => IsFresh says stale_missing_provenance => retried.
        [Test]
        public void PlaceholderSuccess_IsNeverStamped_SoTheScannerRetriesIt()
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-visible-pipeline-" + Guid.NewGuid().ToString("N"));
            try
            {
                var entries = new[] { new ManifestEntry("a", "item", "Assets/Generated/a.png", "item_sword", 16, 16, true, 5, "") };
                var catalog = StaticPromptCatalog.CreateDefault();
                var pipeline = new VisibleGenerationPipeline(root, new PlaceholderForge(), catalog,
                    new GenerationFailureLog(Path.Combine(root, "Logs", "generation-failures.json")));

                var result = pipeline.RunAsync(entries, CancellationToken.None).Result;

                Assert.That(result.Succeeded, Is.EqualTo(1), "a placeholder still counts as a (fallback) success");
                Assert.That(result.Placeholders, Is.EqualTo(1), "EMB-042 provenance count is preserved");
                var fullPath = AssetManifestScanner.Resolve(root, "Assets/Generated/a.png");
                Assert.That(File.Exists(fullPath), Is.True, "the placeholder PNG is still written");
                Assert.That(File.Exists(fullPath + ".promptmeta"), Is.False,
                    "B17: a placeholder is a visible stand-in, never provenance — no freshness stamp");

                var rescan = AssetManifestScanner.ScanAsync(entries, root, CancellationToken.None, catalog).Result;
                var row = rescan.Entries.Single();
                Assert.That(row.State, Is.EqualTo(EntryState.RequiresGeneration),
                    "a stampless placeholder must be retried the moment a real model exists");
                Assert.That(row.Reason, Is.EqualTo("stale_missing_provenance"));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        // Control pin: the new isPlaceholder flag must not over-suppress — a REAL generation
        // still stamps and rescans as Cached (the happy path the B17 change may not break).
        [Test]
        public void RealSuccess_StillStamps_AndRescansAsCached()
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-visible-pipeline-" + Guid.NewGuid().ToString("N"));
            try
            {
                var entries = new[] { new ManifestEntry("a", "item", "Assets/Generated/a.png", "item_sword", 16, 16, true, 5, "") };
                var catalog = StaticPromptCatalog.CreateDefault();
                var pipeline = new VisibleGenerationPipeline(root, new FakeForge(""), catalog,
                    new GenerationFailureLog(Path.Combine(root, "Logs", "generation-failures.json")));

                var result = pipeline.RunAsync(entries, CancellationToken.None).Result;

                Assert.That(result.Succeeded, Is.EqualTo(1));
                Assert.That(result.Placeholders, Is.Zero);
                var fullPath = AssetManifestScanner.Resolve(root, "Assets/Generated/a.png");
                Assert.That(File.Exists(fullPath + ".promptmeta"), Is.True, "a real generation is stamped fresh");

                var rescan = AssetManifestScanner.ScanAsync(entries, root, CancellationToken.None, catalog).Result;
                Assert.That(rescan.Entries.Single().State, Is.EqualTo(EntryState.Cached));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        private sealed class PlaceholderForge : IAssetForge
        {
            public bool IsAvailable() => false; // placeholder mode: no working model
            public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new AssetGenerationResult(
                    request.RequestId, new byte[] { 137, 80, 78, 71 }, "image/png", 1, true,
                    "placeholder", isPlaceholder: true));
            }
        }

        private sealed class FakeForge : IAssetForge
        {
            private readonly string _failId;
            public FakeForge(string failId) => _failId = failId;
            public bool IsAvailable() => true;
            public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                if (request.RequestId == _failId) return Task.FromResult(AssetGenerationResult.Failed(request.RequestId, "fake_failure"));
                return Task.FromResult(new AssetGenerationResult(request.RequestId, new byte[] { 137, 80, 78, 71 }, "image/png", 1, true, ""));
            }
        }
    }
}

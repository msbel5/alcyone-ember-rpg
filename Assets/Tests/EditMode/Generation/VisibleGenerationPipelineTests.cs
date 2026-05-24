using System;
using System.Collections.Generic;
using System.IO;
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
                    Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.RunAsync(entries, cts.Token));
                }
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
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

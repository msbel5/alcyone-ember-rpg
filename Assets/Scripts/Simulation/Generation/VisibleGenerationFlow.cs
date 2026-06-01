using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Generation;

namespace EmberCrpg.Simulation.Generation
{
    public sealed class VisibleGenerationFlowResult
    {
        public VisibleGenerationFlowResult(ManifestScanReport scanReport, int requested, int started, int succeeded, int failed)
        {
            ScanReport = scanReport ?? throw new ArgumentNullException(nameof(scanReport));
            RequestedGeneration = requested;
            StartedGeneration = started;
            SucceededGeneration = succeeded;
            FailedGeneration = failed;
        }

        public ManifestScanReport ScanReport { get; }
        public int RequestedGeneration { get; }
        public int StartedGeneration { get; }
        public int SucceededGeneration { get; }
        public int FailedGeneration { get; }
    }

    public sealed class VisibleGenerationFlow
    {
        private readonly string _projectRoot;
        private readonly IAssetForge _forge;
        private readonly StaticPromptCatalog _catalog;
        private readonly GenerationFailureLog _failureLog;

        public VisibleGenerationFlow(string projectRoot, IAssetForge forge, StaticPromptCatalog catalog, GenerationFailureLog failureLog)
        {
            _projectRoot = string.IsNullOrWhiteSpace(projectRoot) ? throw new ArgumentException("Project root is required.", nameof(projectRoot)) : projectRoot;
            _forge = forge ?? throw new ArgumentNullException(nameof(forge));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _failureLog = failureLog ?? throw new ArgumentNullException(nameof(failureLog));
        }

        public event Action<EntryRow, ManifestEntry> ScanRow;
        public event Action<EntryRow, ManifestEntry, byte[]> ScanThumbnail;
        public event Action<ManifestEntry> EntryStarted;
        public event Action<ManifestEntry, byte[], long> EntrySucceeded;
        public event Action<ManifestEntry, string, string> EntryFailed;
        public event Action<VisibleGenerationFlowResult> Completed;

        public async Task<VisibleGenerationFlowResult> RunCoreAssetTopUpAsync(IReadOnlyList<ManifestEntry> manifestEntries, CancellationToken cancellationToken, int maxGenerationEntries = int.MaxValue)
        {
            if (manifestEntries == null) throw new ArgumentNullException(nameof(manifestEntries));

            var lookup = new Dictionary<string, ManifestEntry>(StringComparer.Ordinal);
            for (int i = 0; i < manifestEntries.Count; i++)
                if (!lookup.ContainsKey(manifestEntries[i].Id))
                    lookup[manifestEntries[i].Id] = manifestEntries[i];

            var scan = await AssetManifestScanner.ScanAsync(manifestEntries, _projectRoot, cancellationToken, _catalog);
            var toGenerate = new List<ManifestEntry>();
            for (int i = 0; i < scan.Entries.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = scan.Entries[i];
                if (!lookup.TryGetValue(row.EntryId, out var entry)) continue;
                ScanRow?.Invoke(row, entry);
                if (row.State == EntryState.Cached)
                {
                    var bytes = TryReadBytes(entry.ExpectedPath);
                    if (bytes != null) ScanThumbnail?.Invoke(row, entry, bytes);
                    continue;
                }

                if (entry.RequiresGeneration && row.State == EntryState.RequiresGeneration && toGenerate.Count < maxGenerationEntries)
                    toGenerate.Add(entry);
            }

            int started = 0;
            int succeeded = 0;
            int failed = 0;
            var pipeline = new VisibleGenerationPipeline(_projectRoot, _forge, _catalog, _failureLog);
            pipeline.EntryStarted += entry =>
            {
                started++;
                EntryStarted?.Invoke(entry);
            };
            pipeline.EntrySucceeded += (entry, bytes, elapsedMs) =>
            {
                succeeded++;
                EntrySucceeded?.Invoke(entry, bytes, elapsedMs);
            };
            pipeline.EntryFailed += (entry, reason, exceptionType) =>
            {
                failed++;
                EntryFailed?.Invoke(entry, reason, exceptionType);
            };

            await pipeline.RunAsync(toGenerate, cancellationToken);
            var result = new VisibleGenerationFlowResult(scan, toGenerate.Count, started, succeeded, failed);
            Completed?.Invoke(result);
            return result;
        }

        private byte[] TryReadBytes(string expectedPath)
        {
            try
            {
                var fullPath = AssetManifestScanner.Resolve(_projectRoot, expectedPath);
                if (!File.Exists(fullPath)) return null;
                var ext = Path.GetExtension(fullPath);
                if (!string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase) && !string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase) && !string.Equals(ext, ".jpeg", StringComparison.OrdinalIgnoreCase))
                    return null;
                return File.ReadAllBytes(fullPath);
            }
            catch
            {
                return null;
            }
        }
    }
}

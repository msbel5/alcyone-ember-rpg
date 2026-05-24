using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Generation;

namespace EmberCrpg.Simulation.Generation
{
    public static class AssetManifestScanner
    {
        public static Task<ManifestScanReport> ScanAsync(IReadOnlyList<ManifestEntry> entries, string projectRoot, CancellationToken cancellationToken)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (string.IsNullOrWhiteSpace(projectRoot)) throw new ArgumentException("Project root is required.", nameof(projectRoot));
            var rows = new List<EntryRow>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = entries[i];
                var fullPath = Resolve(projectRoot, entry.ExpectedPath);
                if (File.Exists(fullPath)) rows.Add(new EntryRow(entry.Id, entry.Category, entry.ExpectedPath, EntryState.Cached, "cached"));
                else rows.Add(new EntryRow(entry.Id, entry.Category, entry.ExpectedPath, entry.RequiresGeneration ? EntryState.Missing : EntryState.Missing, entry.RequiresGeneration ? "requires_generation" : "missing_non_generated_asset"));
            }
            return Task.FromResult(new ManifestScanReport(rows));
        }

        public static string Resolve(string projectRoot, string assetsRelativePath)
        {
            return Path.Combine(projectRoot, assetsRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}

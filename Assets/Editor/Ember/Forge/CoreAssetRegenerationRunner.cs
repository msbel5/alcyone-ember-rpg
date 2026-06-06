using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Editor.Ember.GeneratedAssets;
using EmberCrpg.Infrastructure.AiDm;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.Runtime;
using EmberCrpg.Simulation.Generation;
using UnityEditor;
using UnityEngine;

// Why this file is intentionally long: it owns the full editor-only clear/backup/regenerate/sync flow for core assets, including batchmode-safe and interactive entry paths.
namespace EmberCrpg.Editor.Ember.Forge
{
    public static class CoreAssetRegenerationRunner
    {
        private const string SettingsAssetPath = "Assets/Settings/GeneratedAssetPipelineSettings.asset";
        private static CancellationTokenSource _activeCancellation;
        private static Task<CoreAssetRegenerationSummary> _activeRun;

        public static void Start(CoreAssetRegenerationScope scope)
        {
            if (_activeRun != null && !_activeRun.IsCompleted)
            {
                Debug.LogWarning("[CoreAssetRegen] A regeneration run is already active.");
                return;
            }

            _activeCancellation = new CancellationTokenSource();
            _activeRun = RunAsync(scope, _activeCancellation.Token);
            Observe(scope, _activeRun);
        }

        public static void Cancel()
        {
            if (_activeCancellation == null) return;
            Debug.LogWarning("[CoreAssetRegen] Cancellation requested.");
            _activeCancellation.Cancel();
        }

        public static void RunBlocking(CoreAssetRegenerationScope scope)
        {
            try
            {
                var summary = RunBlockingCore(scope);
                Debug.Log("[CoreAssetRegen] Completed scope=" + scope + " backup=" + summary.BackupRoot);
            }
            finally
            {
                ClearProgressBar();
            }
        }

        private static async void Observe(CoreAssetRegenerationScope scope, Task<CoreAssetRegenerationSummary> run)
        {
            try
            {
                var summary = await run;
                Debug.Log("[CoreAssetRegen] Completed scope=" + scope + " backup=" + summary.BackupRoot);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[CoreAssetRegen] Cancelled scope=" + scope + ".");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CoreAssetRegen] Failed: " + ex);
            }
            finally
            {
                ClearProgressBar();
                _activeCancellation?.Dispose();
                _activeCancellation = null;
                _activeRun = null;
            }
        }

        private static async Task<CoreAssetRegenerationSummary> RunAsync(CoreAssetRegenerationScope scope, CancellationToken cancellationToken)
        {
            var catalog = StaticPromptCatalog.CreateDefault();
            var runtimeRoot = ForgeRuntimeHelpers.ResolveRuntimeRoot();
            var selected = CoreAssetRegenerationSelector.Select(CoreAssetManifest.CreateDefault().Entries, scope);
            if (selected.Count == 0)
            {
                Debug.LogWarning("[CoreAssetRegen] No manifest entries matched scope " + scope + ".");
                return new CoreAssetRegenerationSummary(scope.ToString(), string.Empty, 0, 0, 0, 0, 0, 0, 0);
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var backupRoot = Path.Combine(runtimeRoot, "Assets", "Generated", "_backup_" + stamp);
            var backedUp = BackupStaleEntries(selected, runtimeRoot, backupRoot, catalog);
            AssetDatabase.Refresh();

            var scan = await AssetManifestScanner.ScanAsync(selected, runtimeRoot, cancellationToken, catalog);
            var toGenerate = ResolveGenerationEntries(selected, scan);
            Debug.Log("[CoreAssetRegen] Scope=" + scope + " selected=" + selected.Count + " staleBackedUp=" + backedUp + " queued=" + toGenerate.Count + ".");

            var settings = LoadSettings();
            var database = GeneratedAssetDatabaseEditorUtility.LoadOrCreate();
            using var forge = CreateForge();
            var generation = await RunGenerationPhaseAsync(scope, runtimeRoot, catalog, toGenerate, forge, !Application.isBatchMode, cancellationToken);

            SyncFreshEntries(database, selected, runtimeRoot, catalog, settings, cancellationToken);
            database.RebuildStableIds();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CoreAssetRegen] Summary selected=" + selected.Count
                + " backed_up=" + backedUp
                + " queued=" + toGenerate.Count
                + " succeeded=" + generation.Succeeded
                + " failed=" + generation.Failed
                + " placeholders=" + generation.Placeholders
                + " backup_root=" + backupRoot);

            ClearProgressBar();
            return new CoreAssetRegenerationSummary(scope.ToString(), backupRoot, selected.Count, backedUp, toGenerate.Count, generation.Succeeded, generation.Failed, generation.Placeholders, CountCached(selected, runtimeRoot, catalog));
        }

        private static CoreAssetRegenerationSummary RunBlockingCore(CoreAssetRegenerationScope scope)
        {
            var catalog = StaticPromptCatalog.CreateDefault();
            var runtimeRoot = ForgeRuntimeHelpers.ResolveRuntimeRoot();
            var selected = CoreAssetRegenerationSelector.Select(CoreAssetManifest.CreateDefault().Entries, scope);
            if (selected.Count == 0)
            {
                Debug.LogWarning("[CoreAssetRegen] No manifest entries matched scope " + scope + ".");
                return new CoreAssetRegenerationSummary(scope.ToString(), string.Empty, 0, 0, 0, 0, 0, 0, 0);
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var backupRoot = Path.Combine(runtimeRoot, "Assets", "Generated", "_backup_" + stamp);
            var backedUp = BackupStaleEntries(selected, runtimeRoot, backupRoot, catalog);
            AssetDatabase.Refresh();

            var scan = AssetManifestScanner.ScanAsync(selected, runtimeRoot, CancellationToken.None, catalog).GetAwaiter().GetResult();
            var toGenerate = ResolveGenerationEntries(selected, scan);
            Debug.Log("[CoreAssetRegen] Scope=" + scope + " selected=" + selected.Count + " staleBackedUp=" + backedUp + " queued=" + toGenerate.Count + ".");

            // Batchmode executeMethod cannot block the editor thread on an async task that posts continuations
            // back to Unity's sync context. Run only the pure generation phase off-thread; keep AssetDatabase on
            // the main thread before/after generation.
            using var forge = CreateForge();
            var generation = SyncTaskBridge.Run(() => RunGenerationPhaseAsync(scope, runtimeRoot, catalog, toGenerate, forge, false, CancellationToken.None));

            var settings = LoadSettings();
            var database = GeneratedAssetDatabaseEditorUtility.LoadOrCreate();
            SyncFreshEntries(database, selected, runtimeRoot, catalog, settings, CancellationToken.None);
            database.RebuildStableIds();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CoreAssetRegen] Summary selected=" + selected.Count
                + " backed_up=" + backedUp
                + " queued=" + toGenerate.Count
                + " succeeded=" + generation.Succeeded
                + " failed=" + generation.Failed
                + " placeholders=" + generation.Placeholders
                + " backup_root=" + backupRoot);

            return new CoreAssetRegenerationSummary(scope.ToString(), backupRoot, selected.Count, backedUp, toGenerate.Count, generation.Succeeded, generation.Failed, generation.Placeholders, CountCached(selected, runtimeRoot, catalog));
        }

        private static async Task<CoreAssetGenerationPhaseSummary> RunGenerationPhaseAsync(
            CoreAssetRegenerationScope scope,
            string runtimeRoot,
            StaticPromptCatalog catalog,
            IReadOnlyList<ManifestEntry> toGenerate,
            SerializedAssetForge forge,
            bool allowCancelableProgress,
            CancellationToken cancellationToken)
        {
            var failureLog = new GenerationFailureLog(Path.Combine(runtimeRoot, "Logs", "generation-failures.json"));
            var pipeline = new VisibleGenerationPipeline(runtimeRoot, forge, catalog, failureLog);
            var startedAt = new Dictionary<string, DateTime>(StringComparer.Ordinal);
            var started = 0;
            var failed = 0;
            var succeeded = 0;

            pipeline.EntryStarted += entry =>
            {
                startedAt[entry.Id] = DateTime.UtcNow;
                started++;
                LogProgress(started, toGenerate.Count, entry.Id, "started", 0L);
            };
            pipeline.EntrySucceeded += (entry, _, elapsedMs) =>
            {
                succeeded++;
                LogProgress(Math.Max(started, succeeded + failed), toGenerate.Count, entry.Id, "succeeded", elapsedMs);
                ReportCancelableProgress(scope, started, toGenerate.Count, entry.Id, allowCancelableProgress);
            };
            pipeline.EntryFailed += (entry, reason, exceptionType) =>
            {
                failed++;
                var elapsedMs = startedAt.TryGetValue(entry.Id, out var began)
                    ? (long)(DateTime.UtcNow - began).TotalMilliseconds
                    : 0L;
                LogProgress(Math.Max(started, succeeded + failed), toGenerate.Count, entry.Id, "failed:" + reason + (string.IsNullOrWhiteSpace(exceptionType) ? string.Empty : "/" + exceptionType), elapsedMs);
                ReportCancelableProgress(scope, started, toGenerate.Count, entry.Id, allowCancelableProgress);
            };

            var placeholders = 0;
            if (toGenerate.Count > 0)
            {
                var result = await pipeline.RunAsync(toGenerate, cancellationToken);
                placeholders = result.Placeholders;
            }

            return new CoreAssetGenerationPhaseSummary(succeeded, failed, placeholders);
        }

        private static int BackupStaleEntries(IReadOnlyList<ManifestEntry> entries, string runtimeRoot, string backupRoot, StaticPromptCatalog catalog)
        {
            var backedUp = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || !entry.RequiresGeneration) continue;
                var absolute = AssetManifestScanner.Resolve(runtimeRoot, entry.ExpectedPath);
                if (!File.Exists(absolute)) continue;
                if (GeneratedAssetProvenance.IsFresh(absolute, entry, catalog, out _)) continue;
                MoveToBackup(absolute, runtimeRoot, backupRoot);
                MoveToBackup(absolute + ".promptmeta", runtimeRoot, backupRoot);
                backedUp++;
            }

            return backedUp;
        }

        private static void MoveToBackup(string sourcePath, string runtimeRoot, string backupRoot)
        {
            if (!File.Exists(sourcePath)) return;
            var generatedRoot = Path.Combine(runtimeRoot, "Assets", "Generated");
            var relative = sourcePath.StartsWith(generatedRoot, StringComparison.OrdinalIgnoreCase)
                ? sourcePath.Substring(generatedRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : Path.GetFileName(sourcePath);
            var destination = Path.Combine(backupRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? backupRoot);
            if (File.Exists(destination)) File.Delete(destination);
            File.Move(sourcePath, destination);
            Debug.Log("[CoreAssetRegen] backed_up " + relative);
        }

        private static List<ManifestEntry> ResolveGenerationEntries(IReadOnlyList<ManifestEntry> entries, ManifestScanReport scan)
        {
            var lookup = new Dictionary<string, ManifestEntry>(StringComparer.Ordinal);
            for (var i = 0; i < entries.Count; i++)
                if (entries[i] != null)
                    lookup[entries[i].Id] = entries[i];

            var toGenerate = new List<ManifestEntry>();
            for (var i = 0; i < scan.Entries.Count; i++)
            {
                var row = scan.Entries[i];
                if (row.State != EntryState.RequiresGeneration) continue;
                if (lookup.TryGetValue(row.EntryId, out var entry) && entry.RequiresGeneration)
                    toGenerate.Add(entry);
            }

            return toGenerate;
        }

        private static void SyncFreshEntries(GeneratedAssetDatabase database, IReadOnlyList<ManifestEntry> selected, string runtimeRoot, StaticPromptCatalog catalog, GeneratedAssetPipelineSettings settings, CancellationToken cancellationToken)
        {
            for (var i = 0; i < selected.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = selected[i];
                if (entry == null || !entry.RequiresGeneration) continue;
                var absolute = AssetManifestScanner.Resolve(runtimeRoot, entry.ExpectedPath);
                if (!File.Exists(absolute)) continue;
                if (!GeneratedAssetProvenance.IsFresh(absolute, entry, catalog, out _)) continue;

                ImportGeneratedAsset(entry, settings);
                var record = CoreAssetLibraryRecordBuilder.Build(
                    entry,
                    GeneratedAssetProvenance.ResolvePrompt(entry, catalog),
                    StaticPromptCatalog.EmberGenerationNegative,
                    GeneratedAssetProvenance.ComputePromptHash(entry, catalog),
                    DateTime.UtcNow.ToString("o"));
                GeneratedAssetDatabaseEditorUtility.UpsertByStableIdOrPath(database, record);
            }
        }

        private static void ImportGeneratedAsset(ManifestEntry entry, GeneratedAssetPipelineSettings settings)
        {
            if (IsSpriteLike(entry.Category))
            {
                GeneratedSpriteImportUtility.ApplySpriteImportSettings(entry.ExpectedPath, settings);
                return;
            }

            if (IsTextureLike(entry.Category))
            {
                GeneratedTextureImportUtility.ApplyExistingColorTextureSettings(entry.ExpectedPath, settings, repeat: true, sRgb: true);
                return;
            }

            AssetDatabase.ImportAsset(entry.ExpectedPath, ImportAssetOptions.ForceUpdate);
        }

        private static bool IsSpriteLike(string category)
        {
            return string.Equals(category, "npc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "portrait", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "item", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "ui", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "spell", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "logo", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "door", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "window", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTextureLike(string category)
        {
            return string.Equals(category, "environment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "wall", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "roof", StringComparison.OrdinalIgnoreCase);
        }

        private static GeneratedAssetPipelineSettings LoadSettings()
        {
            return AssetDatabase.LoadAssetAtPath<GeneratedAssetPipelineSettings>(SettingsAssetPath)
                ?? ScriptableObject.CreateInstance<GeneratedAssetPipelineSettings>();
        }

        private static SerializedAssetForge CreateForge()
        {
            var modelRoot = ResolveModelDirectory();
            var realForge = EmberForgeFactory.BuildForge(modelRoot, out _, out var failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
                Debug.LogWarning("[CoreAssetRegen] Forge init note: " + failureReason);
            return new SerializedAssetForge(realForge, new SnapshotResourceProbe(SystemInfo.graphicsMemorySize, SystemInfo.systemMemorySize));
        }

        private static string ResolveModelDirectory()
        {
            var persistent = Path.Combine(Application.persistentDataPath, "Models");
            return Directory.Exists(persistent)
                ? persistent
                : Path.Combine(Application.streamingAssetsPath, "Models");
        }

        private static void LogProgress(int index, int total, string entryId, string status, long elapsedMs)
        {
            Debug.Log("[CoreAssetRegen] " + index + "/" + Math.Max(total, 1) + " " + entryId + " " + status + " " + elapsedMs + "ms");
        }

        private static void ReportCancelableProgress(CoreAssetRegenerationScope scope, int completed, int total, string entryId, bool allowCancelableProgress)
        {
            if (!allowCancelableProgress || total <= 0) return;
            var cancel = EditorUtility.DisplayCancelableProgressBar(
                "Core Asset Regeneration",
                scope + " :: " + entryId,
                Mathf.Clamp01(completed / (float)total));
            if (cancel)
                Cancel();
        }

        private static void ClearProgressBar()
        {
            if (!Application.isBatchMode)
                EditorUtility.ClearProgressBar();
        }

        private static int CountCached(IReadOnlyList<ManifestEntry> entries, string runtimeRoot, StaticPromptCatalog catalog)
        {
            var cached = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || !entry.RequiresGeneration) continue;
                var absolute = AssetManifestScanner.Resolve(runtimeRoot, entry.ExpectedPath);
                if (File.Exists(absolute) && GeneratedAssetProvenance.IsFresh(absolute, entry, catalog, out _))
                    cached++;
            }

            return cached;
        }

        private sealed class SnapshotResourceProbe : IResourceProbe
        {
            private readonly long _videoMemoryMb;
            private readonly long _systemMemoryMb;

            public SnapshotResourceProbe(long videoMemoryMb, long systemMemoryMb)
            {
                _videoMemoryMb = videoMemoryMb;
                _systemMemoryMb = systemMemoryMb;
            }

            public long AvailableVideoMemoryMb() => _videoMemoryMb;
            public long AvailableSystemMemoryMb() => _systemMemoryMb;
        }
    }

    public readonly struct CoreAssetRegenerationSummary
    {
        public CoreAssetRegenerationSummary(string scopeName, string backupRoot, int selected, int backedUp, int queued, int succeeded, int failed, int placeholders, int cached)
        {
            ScopeName = scopeName ?? string.Empty;
            BackupRoot = backupRoot ?? string.Empty;
            Selected = selected;
            BackedUp = backedUp;
            Queued = queued;
            Succeeded = succeeded;
            Failed = failed;
            Placeholders = placeholders;
            Cached = cached;
        }

        public string ScopeName { get; }
        public string BackupRoot { get; }
        public int Selected { get; }
        public int BackedUp { get; }
        public int Queued { get; }
        public int Succeeded { get; }
        public int Failed { get; }
        public int Placeholders { get; }
        public int Cached { get; }
    }

    internal readonly struct CoreAssetGenerationPhaseSummary
    {
        public CoreAssetGenerationPhaseSummary(int succeeded, int failed, int placeholders)
        {
            Succeeded = succeeded;
            Failed = failed;
            Placeholders = placeholders;
        }

        public int Succeeded { get; }
        public int Failed { get; }
        public int Placeholders { get; }
    }
}

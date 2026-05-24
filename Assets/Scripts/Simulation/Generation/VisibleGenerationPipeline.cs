using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Simulation.Generation
{
    public sealed class PipelineResult
    {
        public PipelineResult(int total, int succeeded, int failed)
        {
            Total = total;
            Succeeded = succeeded;
            Failed = failed;
        }

        public int Total { get; }
        public int Succeeded { get; }
        public int Failed { get; }
    }

    public sealed class VisibleGenerationPipeline
    {
        private readonly string _projectRoot;
        private readonly IAssetForge _forge;
        private readonly StaticPromptCatalog _catalog;
        private readonly GenerationFailureLog _failureLog;

        public VisibleGenerationPipeline(string projectRoot, IAssetForge forge, StaticPromptCatalog catalog, GenerationFailureLog failureLog)
        {
            _projectRoot = string.IsNullOrWhiteSpace(projectRoot) ? throw new ArgumentException("Project root is required.", nameof(projectRoot)) : projectRoot;
            _forge = forge ?? throw new ArgumentNullException(nameof(forge));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _failureLog = failureLog ?? throw new ArgumentNullException(nameof(failureLog));
        }

        public event Action<ManifestEntry> EntryStarted;
        public event Action<ManifestEntry, float> EntryProgress;
        public event Action<ManifestEntry, byte[]> EntryThumbnail;
        public event Action<ManifestEntry, byte[], long> EntrySucceeded;
        public event Action<ManifestEntry, string, string> EntryFailed;
        public event Action<PipelineResult> Completed;

        public async Task<PipelineResult> RunAsync(System.Collections.Generic.IReadOnlyList<ManifestEntry> entries, CancellationToken cancellationToken)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            int succeeded = 0;
            int failed = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = entries[i];
                EntryStarted?.Invoke(entry);
                EntryProgress?.Invoke(entry, 0f);
                var started = DateTime.UtcNow;
                var prompt = ResolvePrompt(entry);
                var promptHash = Hash(prompt + entry.Width + "x" + entry.Height + entry.ModelHint);
                try
                {
                    using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        timeout.CancelAfter(TimeSpan.FromSeconds(entry.TimeoutSeconds));
                        var result = await _forge.GenerateAsync(ToRequest(entry, prompt, promptHash), timeout.Token);
                        var elapsed = (long)(DateTime.UtcNow - started).TotalMilliseconds;
                        if (result.Success)
                        {
                            Write(entry, result.ImageBytes);
                            succeeded++;
                            EntryThumbnail?.Invoke(entry, result.ImageBytes);
                            EntryProgress?.Invoke(entry, 1f);
                            EntrySucceeded?.Invoke(entry, result.ImageBytes, elapsed);
                        }
                        else
                        {
                            failed++;
                            Fail(entry, result.FailureReason, string.Empty, promptHash, elapsed);
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    var elapsed = (long)(DateTime.UtcNow - started).TotalMilliseconds;
                    Fail(entry, ex.Message, ex.GetType().Name, promptHash, elapsed);
                }
            }

            var completed = new PipelineResult(entries.Count, succeeded, failed);
            Completed?.Invoke(completed);
            return completed;
        }

        private void Fail(ManifestEntry entry, string reason, string exceptionType, string promptHash, long elapsed)
        {
            var finalReason = string.IsNullOrWhiteSpace(reason) ? "generation_failed" : reason;
            _failureLog.Append(entry.Id, entry.Category, finalReason, exceptionType, promptHash, elapsed);
            EntryFailed?.Invoke(entry, finalReason, exceptionType);
        }

        private string ResolvePrompt(ManifestEntry entry)
        {
            if (!entry.RequiresGeneration) return string.Empty;
            if (_catalog.TryGetPrompt(entry.StaticPromptKey, out var prompt)) return prompt;
            return StaticPromptCatalog.EmberStyleHeader + ", missing prompt key " + entry.StaticPromptKey + ", " + StaticPromptCatalog.EmberNegativeFooter;
        }

        private AssetGenerationRequest ToRequest(ManifestEntry entry, string prompt, string promptHash)
        {
            return new AssetGenerationRequest(entry.Id, ToSubject(entry.Category), WorldStyle.DarkFantasyGrim, WorldGenre.Survival, "ember", promptHash, entry.Width, entry.Height, StableSeed(entry.Id), prompt, StaticPromptCatalog.EmberNegativeFooter, entry.TimeoutSeconds, entry.ModelHint);
        }

        private static AssetSubjectKind ToSubject(string category)
        {
            if (string.Equals(category, "item", StringComparison.OrdinalIgnoreCase)) return AssetSubjectKind.Item;
            if (string.Equals(category, "splash", StringComparison.OrdinalIgnoreCase)) return AssetSubjectKind.Splash;
            return AssetSubjectKind.Npc;
        }

        private void Write(ManifestEntry entry, byte[] bytes)
        {
            var fullPath = AssetManifestScanner.Resolve(_projectRoot, entry.ExpectedPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, bytes ?? Array.Empty<byte>());
        }

        private static uint StableSeed(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < value.Length; i++) hash = (hash ^ value[i]) * 16777619u;
                return hash == 0u ? 1u : hash;
            }
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var sb = new StringBuilder("sha256:");
                for (int i = 0; i < bytes.Length; i++) sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}

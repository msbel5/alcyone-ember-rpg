using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.Generation;
using EmberCrpg.Ui.Foundation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmberCrpg.Presentation.Ember.Boot
{
    public sealed class BootBootstrap : MonoBehaviour
    {
        [SerializeField] private string _nextScene = "MainMenu";

        private async void Awake()
        {
            EnsureSurface();
            var forge = ForgeLocator.AssetForge ?? new SkipAssetForge();
            var result = await RunAsync(forge, Application.dataPath, _nextScene, CancellationToken.None);
            if (!string.IsNullOrEmpty(result.RequestedScene)) _ = SceneManager.LoadSceneAsync(result.RequestedScene);
        }

        public static Task<BootFlowResult> RunForTestsAsync(IAssetForge forge)
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-boot-test-" + Guid.NewGuid().ToString("N"));
            return RunAsync(forge, root, "MainMenu", CancellationToken.None, maxEntries: 6);
        }

        private static async Task<BootFlowResult> RunAsync(IAssetForge forge, string root, string nextScene, CancellationToken ct, int maxEntries = int.MaxValue)
        {
            var panel = UiSurfaceLocator.Current?.Mount("BootScreen");
            var manifest = CoreAssetManifest.CreateDefault();
            var entries = new System.Collections.Generic.List<ManifestEntry>();
            for (int i = 0; i < manifest.Entries.Count && entries.Count < maxEntries; i++)
                if (manifest.Entries[i].RequiresGeneration) entries.Add(manifest.Entries[i]);

            var log = new GenerationFailureLog(Path.Combine(root, "Logs", "generation-failures.json"));
            var pipeline = new VisibleGenerationPipeline(root, forge, StaticPromptCatalog.CreateDefault(), log);
            int started = 0, succeeded = 0, failed = 0;
            pipeline.EntryStarted += e => { started++; panel?.LogLine("log", UiLogSeverity.Info, "[start] " + e.Id); };
            pipeline.EntrySucceeded += (e, bytes, ms) => { succeeded++; panel?.LogLine("log", UiLogSeverity.Success, "[ok] " + e.Id); };
            pipeline.EntryFailed += (e, reason, ex) => { failed++; panel?.LogLine("log", UiLogSeverity.Error, "[error] " + e.Id + " " + reason); };
            await pipeline.RunAsync(entries, ct);
            panel?.LogLine("log", UiLogSeverity.Success, "Generation complete: " + succeeded + "/" + started + " succeeded, " + failed + " failed.");
            await Task.Delay(2500, ct);
            if (panel != null) UiSurfaceLocator.Current?.Unmount(panel);
            return new BootFlowResult(started, succeeded, failed, nextScene);
        }

        private static void EnsureSurface()
        {
            VisibleUiSurface.Ensure();
        }

        private sealed class SkipAssetForge : IAssetForge
        {
            public bool IsAvailable() => false;
            public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(AssetGenerationResult.Failed(request.RequestId, "forge_unavailable"));
            }
        }
    }

    public sealed class BootFlowResult
    {
        public BootFlowResult(int started, int succeeded, int failed, string requestedScene)
        {
            Started = started;
            Succeeded = succeeded;
            Failed = failed;
            RequestedScene = requestedScene;
        }

        public int Started { get; }
        public int Succeeded { get; }
        public int Failed { get; }
        public string RequestedScene { get; }
    }
}

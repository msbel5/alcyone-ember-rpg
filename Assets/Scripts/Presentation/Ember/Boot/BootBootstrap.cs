using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.Generation;
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
            EnsureForgeBootstrap();
            var forge = EmberCrpg.Presentation.Ember.Forge.ForgeLocator.AssetForge ?? new SkipAssetForge();
            // Limit Boot's blocking generation to the first 3 entries (splash_background, logo_full,
            // logo_compact). Remaining icons/items/spells generate visibly on-demand later so the
            // player isn't trapped on Boot waiting for ~34 SD15-LCM inferences in a row.
            var result = await RunAsync(forge, RuntimeRoot(), _nextScene, CancellationToken.None, maxEntries: 3);
            if (!string.IsNullOrEmpty(result.RequestedScene))
            {
                var op = SceneManager.LoadSceneAsync(result.RequestedScene);
                if (op != null)
                {
                    while (!op.isDone) await System.Threading.Tasks.Task.Yield();
                }
                // Boot finished + target scene loaded; release the loading overlay so the menu is visible.
                EmberCrpg.Presentation.Ember.Loading.LoadingScreen.Dismiss();
            }
        }

        public static Task<BootFlowResult> RunForTestsAsync(IAssetForge forge)
        {
            var root = Path.Combine(Path.GetTempPath(), "ember-boot-test-" + Guid.NewGuid().ToString("N"));
            return RunAsync(forge, root, "MainMenu", CancellationToken.None, maxEntries: 6);
        }

        private static async Task<BootFlowResult> RunAsync(IAssetForge forge, string root, string nextScene, CancellationToken ct, int maxEntries = int.MaxValue)
        {
            LoadingScreen.ShowForContext(new LoadingScreenContext("boot", "Ember Boot", "area_transition"));
            LoadingScreen.SetInputBlocking(true);
            LoadingScreen.SetProgress(0f, "Scanning core manifests");
            LoadingScreen.LogLine(EmberCrpg.Ui.Foundation.UiLogSeverity.Info, "[boot] visible generation starting");
            var manifest = CoreAssetManifest.CreateDefault();
            var entries = new System.Collections.Generic.List<ManifestEntry>(manifest.Entries.Count);
            for (int i = 0; i < manifest.Entries.Count && entries.Count < maxEntries; i++)
                entries.Add(manifest.Entries[i]);

            var log = new GenerationFailureLog(Path.Combine(root, "Logs", "generation-failures.json"));
            var flow = new VisibleGenerationFlow(root, forge, StaticPromptCatalog.CreateDefault(), log);

            int scanned = 0;
            int started = 0;
            int succeeded = 0;
            int failed = 0;

            flow.ScanRow += (row, entry) =>
            {
                scanned++;
                LoadingScreen.SetProgress(entries.Count == 0 ? 1f : (float)scanned / entries.Count, "Scan " + row.EntryId);
                var severity = row.State == EntryState.RequiresGeneration ? EmberCrpg.Ui.Foundation.UiLogSeverity.Warning : EmberCrpg.Ui.Foundation.UiLogSeverity.Info;
                LoadingScreen.LogLine(severity, "[scan] " + row.EntryId + " => " + row.State.ToString().ToLowerInvariant());
            };
            flow.ScanThumbnail += (row, entry, bytes) =>
            {
                LoadingScreen.ShowThumbnail(ToTexture(bytes), row.EntryId + " (cached)");
            };
            flow.EntryStarted += e =>
            {
                started++;
                LoadingScreen.SetProgress(0.5f + (entries.Count == 0 ? 0f : 0.5f * ((float)(started - 1) / entries.Count)), "Generating " + e.Id);
                LoadingScreen.LogLine(EmberCrpg.Ui.Foundation.UiLogSeverity.Info, "[start] " + e.Id + " " + e.Width + "x" + e.Height + " model=" + e.ModelHint);
            };
            flow.EntrySucceeded += (e, bytes, ms) =>
            {
                succeeded++;
                LoadingScreen.SetProgress(0.5f + (entries.Count == 0 ? 0f : 0.5f * ((float)started / entries.Count)), "Generated " + e.Id);
                LoadingScreen.ShowThumbnail(ToTexture(bytes), e.Id + " (" + ms + "ms)");
                LoadingScreen.LogLine(EmberCrpg.Ui.Foundation.UiLogSeverity.Success, "[ok] " + e.Id + " " + ms + "ms");
            };
            flow.EntryFailed += (e, reason, ex) =>
            {
                failed++;
                LoadingScreen.SetProgress(0.5f + (entries.Count == 0 ? 0f : 0.5f * ((float)started / entries.Count)), "Skipped " + e.Id);
                LoadingScreen.LogLine(EmberCrpg.Ui.Foundation.UiLogSeverity.Error, "[error] " + e.Id + " " + reason + (string.IsNullOrEmpty(ex) ? string.Empty : " (" + ex + ")"));
            };

            var result = await flow.RunCoreAssetTopUpAsync(entries, ct, maxEntries);
            succeeded = result.SucceededGeneration;
            failed = result.FailedGeneration;
            started = result.StartedGeneration;
            LoadingScreen.LogLine(EmberCrpg.Ui.Foundation.UiLogSeverity.Success, "Generation complete: " + succeeded + "/" + started + " succeeded, " + failed + " failed.");
            LoadingScreen.SetProgress(1f, "Entering main menu");
            await Task.Delay(2500, ct);
            LoadingScreen.Dismiss();
            return new BootFlowResult(started, succeeded, failed, nextScene);
        }

        private static void EnsureSurface()
        {
            VisibleUiSurface.Ensure();
        }

        private static void EnsureForgeBootstrap()
        {
            if (EmberCrpg.Presentation.Ember.Forge.ForgeLocator.AssetForge != null) return;
            var existing = FindFirstObjectByType<EmberCrpg.Presentation.Ember.Forge.ForgeBootstrap>();
            if (existing != null) return;
            var go = new GameObject("ForgeBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<EmberCrpg.Presentation.Ember.Forge.ForgeBootstrap>();
        }

        private static Texture2D ToTexture(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return texture.LoadImage(bytes) ? texture : null;
        }

        private static string RuntimeRoot()
        {
            var parent = Directory.GetParent(Application.dataPath);
            return parent != null ? parent.FullName : Application.dataPath;
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

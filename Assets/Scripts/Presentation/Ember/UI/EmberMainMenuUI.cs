using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Configuration;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Presentation.Ember.Runtime;
using EmberCrpg.Presentation.Ember.UI.Options;
using TMPro;
using EmberCrpg.Simulation.Generation;
using EmberCrpg.Ui.Foundation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed partial class EmberMainMenuUI : MonoBehaviour
    {
        [SerializeField] private string _firstSceneName = EmberScenes.CharacterCreation;

        private IUiPanel _titlePanel;
        private PauseMenu _optionsOwnerProxy;
        private OptionsScreen _optionsScreen;
        private Coroutine _optionsOwnerResetCoroutine;

        private void Awake()
        {
            EnsureEventSystemExists();
            UnlockCursor();

            var newGameButton = FindButton("New Game");
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(NewGame);
                FindButton("Continue")?.onClick.AddListener(Continue);
                FindOrCreateOptionsButton(newGameButton)?.onClick.AddListener(OpenOptions);
                FindButton("Quit")?.onClick.AddListener(Quit);
                return;
            }

            MountUiToolkitTitleMenu();
            // Fire-and-forget background generation for the remaining manifest entries
            // (Boot capped at 3 critical assets; the rest get generated while the player
            // reads the menu). Refresh decorations every couple seconds so newly produced
            // PNGs appear next to the menu without a manual reload.
            _ = RunBackgroundGenerationAsync();
            _decorationRefreshCoroutine = StartCoroutine(RefreshDecorationsLoop());
        }

        private UnityEngine.Coroutine _decorationRefreshCoroutine;

        private System.Collections.IEnumerator RefreshDecorationsLoop()
        {
            var seconds = EmberRuntimeOptionsProvider.Current.Menu.DecorationRefreshSeconds;
            // Hash-based change detection so we only rebuild the decoration strip when
            // the generated directory actually changes.
            string lastSignature = string.Empty;
            for (;;)
            {
                yield return new UnityEngine.WaitForSecondsRealtime(seconds);
                var sig = BuildGeneratedDirectorySignature();
                if (sig != lastSignature)
                {
                    lastSignature = sig;
                    PopulateDecorations();
                }
            }
        }

        private static string BuildGeneratedDirectorySignature()
        {
            var root = ForgeRuntimeHelpers.ResolveRuntimeRoot();
            var dir = System.IO.Path.Combine(root, "Assets", "Generated", "Core");
            if (!System.IO.Directory.Exists(dir)) return string.Empty;
            var files = System.IO.Directory.GetFiles(dir, "*.png");
            System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < files.Length; i++)
            {
                sb.Append(System.IO.Path.GetFileName(files[i]));
                sb.Append('=');
                sb.Append(new System.IO.FileInfo(files[i]).Length);
                sb.Append(';');
            }
            return sb.ToString();
        }

        private void PopulateDecorations()
        {
            // MainMenu shows only the splash backdrop now (icons + decoration strip removed per
            // design direction). Re-apply in case Boot regenerated splash_background after mount.
            ApplyGeneratedBackdrop();
        }

        private static async System.Threading.Tasks.Task RunBackgroundGenerationAsync()
        {
            try
            {
                var options = EmberRuntimeOptionsProvider.Current;
                ForgeRuntimeHelpers.EnsureForgeBootstrap();
                // Yield until ForgeLocator.AssetForge is populated; otherwise we'd race ahead with
                // a null forge (same trap Boot had).
                await ForgeRuntimeHelpers.WaitForForgeAsync(options.Menu.ForgeWaitFrames);
                var forge = ForgeLocator.AssetForge;
                if (forge == null) return;

                var root = ForgeRuntimeHelpers.ResolveRuntimeRoot();
                var manifest = CoreAssetManifest.CreateDefault();
                var failureLog = new GenerationFailureLog(System.IO.Path.Combine(root, "Logs", "generation-failures.json"));
                var flow = new VisibleGenerationFlow(root, forge, StaticPromptCatalog.CreateDefault(), failureLog);
                // No max cap — the manifest is finite (~34 entries) and the pipeline auto-skips
                // cached entries, so the cost is bounded by the missing icons only.
                await flow.RunCoreAssetTopUpAsync(manifest.Entries, System.Threading.CancellationToken.None);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[MainMenu] background asset top-up failed: " + ex.Message);
            }
        }

        private void OnDestroy()
        {
            if (_titlePanel == null) return;
            UiSurfaceLocator.Current?.Unmount(_titlePanel);
            _titlePanel = null;
        }

        private void MountUiToolkitTitleMenu()
        {
            VisibleUiSurface.Ensure();
            _titlePanel = UiSurfaceLocator.Current?.Mount("TitleMenu");
            _titlePanel?.SetText("version", "Alcyone Ember — early build");
            _titlePanel?.SetText("status", "Backend ready. Missing assets are generated visibly on New Game.");
            _titlePanel?.SetButtonHandler("new_game", NewGame);
            _titlePanel?.SetButtonHandler("continue", Continue);
            _titlePanel?.SetButtonHandler("load", LoadGame);
            _titlePanel?.SetButtonHandler("options", OpenOptions);
            _titlePanel?.SetButtonHandler("quit", Quit);
            // Pull whatever PNGs already exist on disk immediately so proof captures (and any
            // post-Boot return-to-menu) show decorations without waiting for the 2s refresh tick.
            PopulateDecorations();
        }

        public void OpenOptions()
        {
            var owner = EnsureOptionsOwnerProxy();
            var canvas = owner.GetComponentInParent<Canvas>();
            if (_optionsScreen == null)
            {
                _optionsScreen = Object.FindFirstObjectByType<OptionsScreen>(FindObjectsInactive.Include);
                if (_optionsScreen == null)
                {
                    var go = new GameObject("OptionsScreen", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(OptionsScreen));
                    go.transform.SetParent(canvas.transform, worldPositionStays: false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
                    _optionsScreen = go.GetComponent<OptionsScreen>();
                }

                _optionsScreen.Initialize(font: null, panelFrame: null);
            }

            if (_optionsOwnerResetCoroutine != null)
                StopCoroutine(_optionsOwnerResetCoroutine);

            HideOptionsOwnerProxy();
            _optionsScreen.Open(owner);
            _optionsOwnerResetCoroutine = StartCoroutine(ResetOptionsOwnerProxyWhenClosed());
        }

        private PauseMenu EnsureOptionsOwnerProxy()
        {
            if (_optionsOwnerProxy != null)
                return _optionsOwnerProxy;

            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                var canvasGo = new GameObject(
                    "MainMenuOptionsCanvas",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new UnityEngine.Vector2(1920f, 1080f);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 2000;
            }

            var proxy = new GameObject("MainMenuOptionsOwnerProxy", typeof(RectTransform), typeof(CanvasGroup), typeof(PauseMenu));
            proxy.transform.SetParent(canvas.transform, worldPositionStays: false);
            var rt = proxy.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            _optionsOwnerProxy = proxy.GetComponent<PauseMenu>();

            for (int i = proxy.transform.childCount - 1; i >= 0; i--)
                Destroy(proxy.transform.GetChild(i).gameObject);

            HideOptionsOwnerProxy();
            return _optionsOwnerProxy;
        }

        private void HideOptionsOwnerProxy()
        {
            if (_optionsOwnerProxy == null)
                return;

            _optionsOwnerProxy.enabled = false;
            if (_optionsOwnerProxy.TryGetComponent<CanvasGroup>(out var group))
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        private System.Collections.IEnumerator ResetOptionsOwnerProxyWhenClosed()
        {
            var screenGroup = _optionsScreen != null ? _optionsScreen.GetComponent<CanvasGroup>() : null;
            while (screenGroup != null && screenGroup.interactable)
                yield return null;
            HideOptionsOwnerProxy();
            _optionsOwnerResetCoroutine = null;
        }

        private void ApplyGeneratedBackdrop()
        {
            if (_titlePanel == null) return;
            var tex = LoadGeneratedTexture("splash_background");
            if (tex != null) _titlePanel.SetThumbnail("backdrop", tex);
        }

        private static Texture2D LoadGeneratedTexture(string entryId)
        {
            var root = ForgeRuntimeHelpers.ResolveRuntimeRoot();
            var path = Path.Combine(root, "Assets", "Generated", "Core", entryId + ".png");
            if (!File.Exists(path)) return null;
            if (!GeneratedAssetProvenance.IsFreshCoreAsset(entryId, path)) return null;
            var bytes = File.ReadAllBytes(path);
            var texture = ForgeRuntimeHelpers.TryDecodeTexture(bytes);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
            }
            return texture;
        }

        private static async Task RunScenarioAssetTopUpAsync()
        {
            var options = EmberRuntimeOptionsProvider.Current;
            ForgeRuntimeHelpers.EnsureForgeBootstrap();
            await ForgeRuntimeHelpers.WaitForForgeAsync(options.Menu.ForgeWaitFrames);

            LoadingScreen.ShowForContext(new LoadingScreenContext("character_creation", "Preparing Character Creation", "generation"));
            LoadingScreen.SetProgress(0f, "Scanning scenario assets");
            LoadingScreen.LogLine(UiLogSeverity.Info, "[new-game] scanning scenario assets");

            var forge = ForgeLocator.AssetForge;
            if (forge == null || !forge.IsAvailable())
            {
                LoadingScreen.LogLine(UiLogSeverity.Warning, "[new-game] forge unavailable; cached assets only");
                await Task.Delay(options.Menu.PreSceneDelayMs);
                return;
            }

            var selected = SelectScenarioEntries(CoreAssetManifest.CreateDefault().Entries);
            var root = ForgeRuntimeHelpers.ResolveRuntimeRoot();
            var flow = new VisibleGenerationFlow(
                root,
                forge,
                StaticPromptCatalog.CreateDefault(),
                new GenerationFailureLog(Path.Combine(root, "Logs", "generation-failures.json")));

            int scanned = 0;
            int started = 0;
            flow.ScanRow += (row, entry) =>
            {
                scanned++;
                LoadingScreen.SetProgress(selected.Count == 0 ? 1f : (float)scanned / selected.Count * 0.35f, "Scan " + row.EntryId);
                LoadingScreen.LogLine(row.State == EntryState.Cached ? UiLogSeverity.Info : UiLogSeverity.Warning, "[scan] " + row.EntryId + " => " + row.State);
            };
            flow.ScanThumbnail += (row, entry, bytes) => LoadingScreen.ShowThumbnail(ForgeRuntimeHelpers.TryDecodeTexture(bytes), row.EntryId + " cached");
            flow.EntryStarted += entry =>
            {
                started++;
                LoadingScreen.SetProgress(0.35f, "Generating " + entry.Id);
                LoadingScreen.LogLine(UiLogSeverity.Info, "[start] " + entry.Id);
            };
            flow.EntrySucceeded += (entry, bytes, elapsedMs) =>
            {
                LoadingScreen.SetProgress(selected.Count == 0 ? 1f : 0.35f + (0.65f * started / selected.Count), "Generated " + entry.Id);
                LoadingScreen.ShowThumbnail(ForgeRuntimeHelpers.TryDecodeTexture(bytes), entry.Id);
                LoadingScreen.LogLine(UiLogSeverity.Success, "[ok] " + entry.Id + " " + elapsedMs + "ms");
            };
            flow.EntryFailed += (entry, reason, exceptionType) =>
                LoadingScreen.LogLine(UiLogSeverity.Error, "[error] " + entry.Id + " " + reason);

            var result = await flow.RunCoreAssetTopUpAsync(selected, CancellationToken.None);
            LoadingScreen.SetProgress(1f, "Scenario assets ready");
            LoadingScreen.LogLine(UiLogSeverity.Success, "[new-game] scenario top-up complete: " + result.SucceededGeneration + "/" + result.StartedGeneration + " generated");
            await Task.Delay(options.Menu.ScenarioReadyDelayMs);
        }

        private static List<ManifestEntry> SelectScenarioEntries(IReadOnlyList<ManifestEntry> entries)
        {
            var tokens = EmberRuntimeOptionsProvider.Current.Menu.ScenarioManifestIds;
            var selected = new List<ManifestEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.RequiresGeneration) continue;
                if (MatchesScenarioId(entry.Id, tokens))
                    selected.Add(entry);
            }
            return selected;
        }

        private static bool MatchesScenarioId(string entryId, IReadOnlyList<string> tokens)
        {
            if (string.IsNullOrWhiteSpace(entryId) || tokens == null || tokens.Count == 0) return false;
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (string.IsNullOrWhiteSpace(token)) continue;
                if (token.EndsWith("_", System.StringComparison.Ordinal))
                {
                    if (entryId.StartsWith(token, System.StringComparison.Ordinal)) return true;
                }
                else if (string.Equals(entryId, token, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static Button FindButton(string name)
        {
            var roots = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < roots.Length; i++)
                if (roots[i].name == name && roots[i].TryGetComponent<Button>(out var button))
                    return button;
            return null;
        }

        private static Button FindOrCreateOptionsButton(Button referenceButton)
        {
            var existing = FindButton("Options") ?? FindButton("OPTIONS");
            if (existing != null || referenceButton == null)
                return existing;

            var clone = Object.Instantiate(referenceButton.gameObject, referenceButton.transform.parent);
            clone.name = "Options";
            if (clone.TryGetComponent<Button>(out var button))
                button.onClick = new Button.ButtonClickedEvent();

            var quit = FindButton("Quit");
            clone.transform.SetSiblingIndex(quit != null
                ? quit.transform.GetSiblingIndex()
                : referenceButton.transform.GetSiblingIndex() + 1);
            ApplyButtonLabel(clone.transform, "OPTIONS");
            return clone.GetComponent<Button>();
        }

        private static void ApplyButtonLabel(Transform root, string label)
        {
            var tmp = root.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (tmp != null)
                tmp.text = label;

            var legacy = root.GetComponentInChildren<Text>(includeInactive: true);
            if (legacy != null)
                legacy.text = label;
        }
    }
}

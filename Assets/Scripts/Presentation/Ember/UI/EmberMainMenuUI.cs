using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Simulation.Generation;
using EmberCrpg.Ui.Foundation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class EmberMainMenuUI : MonoBehaviour
    {
        [SerializeField] private string _firstSceneName = "CharacterCreation";

        private IUiPanel _titlePanel;

        private void Awake()
        {
            EnsureEventSystemExists();
            UnlockCursor();

            var newGameButton = FindButton("New Game");
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(NewGame);
                FindButton("Continue")?.onClick.AddListener(Continue);
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
            // Hash-based change detection so we only rebuild the decoration strip when
            // the generated directory actually changes.
            string lastSignature = string.Empty;
            for (;;)
            {
                yield return new UnityEngine.WaitForSecondsRealtime(2f);
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
            var parent = System.IO.Directory.GetParent(Application.dataPath);
            var root = parent != null ? parent.FullName : Application.dataPath;
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
            // Re-apply the splash backdrop (in case Boot regenerated it after MainMenu mounted)
            // and refresh any per-button icons so the menu auto-wires every PNG that arrives.
            ApplyGeneratedBackdrop();
            ApplyButtonIcons();
            ApplyAutoDecorationStrip();
        }

        // Entry ids that are already wired to a named icon slot above each menu button.
        // Anything else in the generated directory gets dropped into decoration_strip.
        private static readonly System.Collections.Generic.HashSet<string> PinnedIconEntryIds =
            new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "splash_background", "logo_full", "logo_compact",
                "new_game", "continue", "journal", "settings", "error",
            };

        private void ApplyAutoDecorationStrip()
        {
            if (_titlePanel == null) return;
            var parent = System.IO.Directory.GetParent(Application.dataPath);
            var root = parent != null ? parent.FullName : Application.dataPath;
            var dir = System.IO.Path.Combine(root, "Assets", "Generated", "Core");
            if (!System.IO.Directory.Exists(dir))
            {
                _titlePanel.SetThumbnailGrid("decoration_strip", System.Array.Empty<Texture2D>());
                return;
            }
            var textures = new System.Collections.Generic.List<Texture2D>();
            foreach (var path in System.IO.Directory.GetFiles(dir, "*.png"))
            {
                var entryId = System.IO.Path.GetFileNameWithoutExtension(path);
                if (PinnedIconEntryIds.Contains(entryId)) continue;
                var tex = LoadGeneratedTexture(entryId);
                if (tex != null) textures.Add(tex);
            }
            _titlePanel.SetThumbnailGrid("decoration_strip", textures);
        }

        private void ApplyButtonIcons()
        {
            if (_titlePanel == null) return;
            // Map button slot -> manifest entry id so each newly-generated icon snaps to its slot.
            TryApplyIcon("icon_new_game", "new_game");
            TryApplyIcon("icon_continue", "continue");
            TryApplyIcon("icon_load", "journal");
            TryApplyIcon("icon_options", "settings");
            TryApplyIcon("icon_quit", "error");
        }

        private void TryApplyIcon(string slot, string entryId)
        {
            var tex = LoadGeneratedTexture(entryId);
            if (tex != null) _titlePanel.SetThumbnail(slot, tex);
        }

        private static async System.Threading.Tasks.Task RunBackgroundGenerationAsync()
        {
            try
            {
                EnsureForgeBootstrap();
                // Yield until ForgeLocator.AssetForge is populated; otherwise we'd race ahead with
                // a null forge (same trap Boot had).
                for (int waited = 0; waited < 180 && ForgeLocator.AssetForge == null; waited++)
                    await System.Threading.Tasks.Task.Yield();
                var forge = ForgeLocator.AssetForge;
                if (forge == null) return;

                var parent = System.IO.Directory.GetParent(Application.dataPath);
                var root = parent != null ? parent.FullName : Application.dataPath;
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

        public void NewGame()
        {
            _ = NewGameAsync();
        }

        private async Task NewGameAsync()
        {
            await RunScenarioAssetTopUpAsync();
            LoadingScreen.Dismiss();
            await Task.Delay(350);
            SceneManager.LoadScene(string.IsNullOrWhiteSpace(_firstSceneName) ? "CharacterCreation" : _firstSceneName);
        }

        private void MountUiToolkitTitleMenu()
        {
            VisibleUiSurface.Ensure();
            _titlePanel = UiSurfaceLocator.Current?.Mount("TitleMenu");
            _titlePanel?.SetText("version", "PR #214 visible generation cutover");
            _titlePanel?.SetText("status", "Backend ready. Missing assets are generated visibly on New Game.");
            _titlePanel?.SetButtonHandler("new_game", NewGame);
            _titlePanel?.SetButtonHandler("continue", Continue);
            _titlePanel?.SetButtonHandler("load", LoadGame);
            _titlePanel?.SetButtonHandler("options", OpenOptions);
            _titlePanel?.SetButtonHandler("quit", Quit);
            ApplyGeneratedBackdrop();
        }

        public void LoadGame()
        {
            // Wire to PlayerPrefs check; if no save exists, fall back to Continue's no-save path.
            string json = PlayerPrefs.GetString("ember.save.v1");
            if (string.IsNullOrEmpty(json))
            {
                _titlePanel?.SetText("status", "No saves yet — starting a new game.");
                NewGame();
                return;
            }
            Continue();
        }

        public void OpenOptions()
        {
            // Placeholder until the Options panel ships; status row gives visible feedback.
            _titlePanel?.SetText("status", "Options panel coming soon. Audio/Graphics/Controls planned for the next sprint.");
        }

        private void ApplyGeneratedBackdrop()
        {
            if (_titlePanel == null) return;
            var tex = LoadGeneratedTexture("splash_background");
            if (tex != null) _titlePanel.SetThumbnail("backdrop", tex);
        }

        private static Texture2D LoadGeneratedTexture(string entryId)
        {
            var parent = Directory.GetParent(Application.dataPath);
            var root = parent != null ? parent.FullName : Application.dataPath;
            var path = Path.Combine(root, "Assets", "Generated", "Core", entryId + ".png");
            if (!File.Exists(path)) return null;
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            return texture.LoadImage(bytes) ? texture : null;
        }

        private static async Task RunScenarioAssetTopUpAsync()
        {
            EnsureForgeBootstrap();
            await Task.Yield();

            LoadingScreen.ShowForContext(new LoadingScreenContext("character_creation", "Preparing Character Creation", "generation"));
            LoadingScreen.SetProgress(0f, "Scanning scenario assets");
            LoadingScreen.LogLine(UiLogSeverity.Info, "[new-game] scanning scenario assets");

            var forge = ForgeLocator.AssetForge;
            if (forge == null || !forge.IsAvailable())
            {
                LoadingScreen.LogLine(UiLogSeverity.Warning, "[new-game] forge unavailable; cached assets only");
                await Task.Delay(350);
                return;
            }

            var selected = SelectScenarioEntries(CoreAssetManifest.CreateDefault().Entries);
            var root = RuntimeRoot();
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
            flow.ScanThumbnail += (row, entry, bytes) => LoadingScreen.ShowThumbnail(ToTexture(bytes), row.EntryId + " cached");
            flow.EntryStarted += entry =>
            {
                started++;
                LoadingScreen.SetProgress(0.35f, "Generating " + entry.Id);
                LoadingScreen.LogLine(UiLogSeverity.Info, "[start] " + entry.Id);
            };
            flow.EntrySucceeded += (entry, bytes, elapsedMs) =>
            {
                LoadingScreen.SetProgress(selected.Count == 0 ? 1f : 0.35f + (0.65f * started / selected.Count), "Generated " + entry.Id);
                LoadingScreen.ShowThumbnail(ToTexture(bytes), entry.Id);
                LoadingScreen.LogLine(UiLogSeverity.Success, "[ok] " + entry.Id + " " + elapsedMs + "ms");
            };
            flow.EntryFailed += (entry, reason, exceptionType) =>
                LoadingScreen.LogLine(UiLogSeverity.Error, "[error] " + entry.Id + " " + reason);

            var result = await flow.RunCoreAssetTopUpAsync(selected, CancellationToken.None);
            LoadingScreen.SetProgress(1f, "Scenario assets ready");
            LoadingScreen.LogLine(UiLogSeverity.Success, "[new-game] scenario top-up complete: " + result.SucceededGeneration + "/" + result.StartedGeneration + " generated");
            await Task.Delay(300);
        }

        private static List<ManifestEntry> SelectScenarioEntries(IReadOnlyList<ManifestEntry> entries)
        {
            var selected = new List<ManifestEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.RequiresGeneration) continue;
                if (entry.Id == "dice" || entry.Id == "skill" || entry.Id == "new_game" || entry.Id.StartsWith("logo_"))
                    selected.Add(entry);
            }
            return selected;
        }

        public void Continue()
        {
            string json = PlayerPrefs.GetString("ember.save.v1");
            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<EmberCrpg.Presentation.Ember.Save.SaveData>(json);
                if (data != null && !string.IsNullOrEmpty(data.sceneName) && IsKnownBuildScene(data.sceneName))
                {
                    EmberCrpg.Presentation.Ember.Save.EmberSaveService.PreparePendingLoad(data);
                    LoadingScreen.ShowForContext(new LoadingScreenContext(data.sceneName, data.sceneName, "area_transition"));
                    SceneManager.LoadScene(data.sceneName);
                    return;
                }
            }
            NewGame();
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void EnsureForgeBootstrap()
        {
            if (ForgeLocator.AssetForge != null) return;
            if (FindFirstObjectByType<ForgeBootstrap>() != null) return;
            var go = new GameObject("ForgeBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<ForgeBootstrap>();
        }

        private static string RuntimeRoot()
        {
            var parent = Directory.GetParent(Application.dataPath);
            return parent != null ? parent.FullName : Application.dataPath;
        }

        private static Texture2D ToTexture(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return texture.LoadImage(bytes) ? texture : null;
        }

        private static Button FindButton(string name)
        {
            var roots = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < roots.Length; i++)
                if (roots[i].name == name && roots[i].TryGetComponent<Button>(out var button))
                    return button;
            return null;
        }

        private static void EnsureEventSystemExists()
        {
            if (EventSystem.current != null) return;
            if (FindFirstObjectByType<EventSystem>() != null) return;
            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static bool IsKnownBuildScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return false;
#if UNITY_EDITOR
            foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
            {
                if (scene == null || string.IsNullOrEmpty(scene.path)) continue;
                var stem = Path.GetFileNameWithoutExtension(scene.path);
                if (string.Equals(stem, sceneName, System.StringComparison.Ordinal)) return true;
            }
            return false;
#else
            return true;
#endif
        }
    }
}

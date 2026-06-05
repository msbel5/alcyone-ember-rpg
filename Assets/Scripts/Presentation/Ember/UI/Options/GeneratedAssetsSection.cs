using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Simulation.Generation;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    /// <summary>Pattern: IOptionsSection content builder. Why: generated-art controls slot into the shell without host edits.</summary>
    [Preserve]
    public sealed partial class GeneratedAssetsSection : IOptionsSection
    {
        private readonly StaticPromptCatalog _catalog = StaticPromptCatalog.CreateDefault();
        private readonly List<TileView> _tiles = new List<TileView>();
        private TMP_Text _note;
        private Button _regenerateAllButton;
        private bool _busy;

        public string Title => "Generated Assets";
        public int Order => 20;

        public void Build(Transform contentMount)
        {
            _tiles.Clear();
            var root = NewRoot(contentMount);
            _note = MakeLabel(root, "Browse cached AI art. Live regenerate uses the serialized forge when available; otherwise it regenerates on next load.", 15f, ParchmentDim, TextAlignmentOptions.Left);
            Place(_note.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -52f), new Vector2(-16f, -14f));
            BuildToolbar(root);
            var grid = BuildGrid(root);
            var entries = CoreAssetManifest.CreateDefault().Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.RequiresGeneration) continue;
                var tile = CreateTile(grid, entry);
                _tiles.Add(tile);
                RefreshTile(tile);
            }
        }

        // Why: bulk regenerate should respect the same one-at-a-time forge path and keep the UI honest.
        private async void RegenerateAll()
        {
            if (_busy) return;
            SetBusy(true);
            try
            {
                SetNote("Regenerating all generated assets...");
                for (int i = 0; i < _tiles.Count; i++) await RegenerateTileAsync(_tiles[i], true);
                SetNote("Generated asset sweep complete.");
            }
            finally
            {
                SetBusy(false);
            }
        }

        // Why: each tile needs the same invalidate-first flow whether it was clicked alone or in a batch.
        private async void RegenerateOne(TileView tile)
        {
            if (_busy || tile == null) return;
            SetBusy(true);
            try { await RegenerateTileAsync(tile, false); }
            finally { SetBusy(false); }
        }

        // Why: invalidate first so failure still leaves the next generation pass with a truthful cache state.
        private async Task RegenerateTileAsync(TileView tile, bool bulk)
        {
            if (tile == null) return;
            Invalidate(tile.Entry);
            RefreshTile(tile);
            var forge = ForgeLocator.AssetForge;
            if (forge == null || !forge.IsAvailable())
            {
                SetNote($"Invalidated {tile.Entry.Id}; regenerates on next load.");
                return;
            }

            SetBadge(tile, bulk ? "queued" : "regenerating", Gold);
            try
            {
                var result = await forge.GenerateAsync(BuildRequest(tile.Entry), CancellationToken.None);
                if (result.Success)
                {
                    WriteGeneratedAsset(tile.Entry, result.ImageBytes);
                    SetNote(result.IsPlaceholder
                        ? $"Regenerated {tile.Entry.Id} with placeholder output."
                        : $"Regenerated {tile.Entry.Id}.");
                }
                else
                {
                    SetNote($"Live regenerate failed for {tile.Entry.Id}; next load will retry ({result.FailureReason}).");
                }
            }
            catch (Exception ex)
            {
                SetNote($"Live regenerate failed for {tile.Entry.Id}; next load will retry ({ex.Message}).");
            }

            RefreshTile(tile);
        }

        // Why: live requests must match the production pipeline's prompt, seed, and subject mapping.
        private AssetGenerationRequest BuildRequest(ManifestEntry entry)
        {
            var promptHash = GeneratedAssetProvenance.ComputePromptHash(entry, _catalog);
            return new AssetGenerationRequest(
                entry.Id,
                ToSubject(entry.Category),
                WorldStyle.DarkFantasyGrim,
                WorldGenre.Survival,
                "ember",
                promptHash,
                entry.Width,
                entry.Height,
                StableSeed(entry.Id),
                GeneratedAssetProvenance.ResolvePrompt(entry, _catalog),
                StaticPromptCatalog.EmberGenerationNegative,
                entry.TimeoutSeconds,
                entry.ModelHint);
        }

        // Why: writing the refreshed PNG and stamp keeps later scans and loaders in sync.
        private void WriteGeneratedAsset(ManifestEntry entry, byte[] bytes)
        {
            var path = PreferredAssetPath(entry);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
            File.WriteAllBytes(path, bytes ?? Array.Empty<byte>());
            GeneratedAssetProvenance.Write(path, entry, _catalog);
        }

        private void BuildToolbar(RectTransform parent)
        {
            var row = NewRect("Toolbar", parent);
            Place(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -96f), new Vector2(-16f, -58f));
            _regenerateAllButton = MakeButton(row, "Regenerate All", RegenerateAll);
            Place((RectTransform)_regenerateAllButton.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(168f, 0f));
            var hint = MakeLabel(row, "(falls back to next load when the live forge is unavailable)", 13f, ParchmentDim, TextAlignmentOptions.Left);
            Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(180f, 0f), new Vector2(0f, 0f));
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            if (_regenerateAllButton != null) _regenerateAllButton.interactable = !busy;
            for (int i = 0; i < _tiles.Count; i++)
                if (_tiles[i].Button != null)
                    _tiles[i].Button.interactable = !busy;
        }

        private static AssetSubjectKind ToSubject(string category)
        {
            if (string.Equals(category, "item", StringComparison.OrdinalIgnoreCase)) return AssetSubjectKind.Item;
            if (string.Equals(category, "ui", StringComparison.OrdinalIgnoreCase)) return AssetSubjectKind.Item;
            if (string.Equals(category, "spell", StringComparison.OrdinalIgnoreCase)) return AssetSubjectKind.Item;
            if (string.Equals(category, "logo", StringComparison.OrdinalIgnoreCase)) return AssetSubjectKind.Item;
            if (string.Equals(category, "environment", StringComparison.OrdinalIgnoreCase)) return AssetSubjectKind.Region;
            if (string.Equals(category, "splash", StringComparison.OrdinalIgnoreCase)) return AssetSubjectKind.Splash;
            return AssetSubjectKind.Npc;
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
    }
}

using System;
using System.IO;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Simulation.Generation; // GeneratedAssetProvenance lives here (same as the sibling partial)
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    public sealed partial class GeneratedAssetsSection
    {
        private static readonly Color Parchment = new Color(0.949f, 0.859f, 0.620f, 1f);
        private static readonly Color ParchmentDim = new Color(0.902f, 0.851f, 0.702f, 1f);
        private static readonly Color Gold = new Color(1f, 0.851f, 0.298f, 1f);
        private static readonly Color PanelBrown = new Color(0.18f, 0.14f, 0.09f, 0.92f);
        private static readonly Color PanelBrownHi = new Color(0.227f, 0.18f, 0.114f, 1f);
        private static readonly Color Success = new Color(0.46f, 0.83f, 0.55f, 1f);
        private static readonly Color Warning = new Color(0.851f, 0.702f, 0.102f, 1f);

        private sealed class TileView
        {
            public ManifestEntry Entry;
            public RectTransform Root;
            public RawImage Thumbnail;
            public TMP_Text Badge;
            public Button Button;
            public Texture2D Texture;
        }

        // Why: the section owns one full-frame content surface under the host's shared box chrome.
        private RectTransform NewRoot(Transform parent)
        {
            var root = NewRect("GeneratedAssets", parent);
            Stretch(root);
            return root;
        }

        // Why: the grid needs masking and its own scroll viewport because the host only gives a blank mount.
        private RectTransform BuildGrid(RectTransform parent)
        {
            var scrollRoot = NewRect("ScrollRoot", parent, typeof(Image), typeof(ScrollRect));
            Place(scrollRoot, Vector2.zero, new Vector2(1f, 1f), new Vector2(16f, 16f), new Vector2(-16f, -108f));
            scrollRoot.GetComponent<Image>().color = PanelBrown;
            var viewport = NewRect("Viewport", scrollRoot, typeof(Image), typeof(RectMask2D));
            Stretch(viewport);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            var content = NewRect("Content", viewport, typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(166f, 220f);
            grid.spacing = new Vector2(12f, 12f);
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        // Why: each asset tile keeps its own thumbnail, badge, and regenerate action in one reusable card.
        private TileView CreateTile(Transform parent, ManifestEntry entry)
        {
            var root = NewRect(entry.Id, parent, typeof(Image), typeof(LayoutElement));
            root.GetComponent<Image>().color = PanelBrownHi;
            root.GetComponent<LayoutElement>().preferredWidth = 166f;
            root.GetComponent<LayoutElement>().preferredHeight = 220f;
            var thumbFrame = NewRect("ThumbFrame", root, typeof(Image));
            Place(thumbFrame, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -122f), new Vector2(-10f, -10f));
            thumbFrame.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            var thumb = NewRect("Thumb", thumbFrame, typeof(RawImage)).GetComponent<RawImage>();
            Stretch((RectTransform)thumb.transform);
            var id = MakeLabel(root, entry.Id, 15f, Parchment, TextAlignmentOptions.Center);
            Place(id.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -154f), new Vector2(-8f, -126f));
            var badge = MakeLabel(root, string.Empty, 13f, Warning, TextAlignmentOptions.Center);
            Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -178f), new Vector2(-8f, -154f));
            var button = MakeButton(root, "Regenerate", null);
            Place((RectTransform)button.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 10f), new Vector2(-10f, 42f));
            var tile = new TileView { Entry = entry, Root = root, Thumbnail = thumb, Badge = badge, Button = button };
            button.onClick.AddListener(() => RegenerateOne(tile));
            return tile;
        }

        // Why: the user needs current cache state, not just a button that hopes the file exists.
        private void RefreshTile(TileView tile)
        {
            if (tile == null || tile.Root == null) return;
            if (tile.Texture != null) UnityEngine.Object.Destroy(tile.Texture);
            tile.Texture = null;
            var path = ResolveExistingPath(tile.Entry);
            if (string.IsNullOrEmpty(path))
            {
                tile.Thumbnail.texture = Texture2D.whiteTexture;
                tile.Thumbnail.color = PanelBrown;
                SetBadge(tile, "(not generated)", Warning);
                return;
            }

            var fresh = GeneratedAssetProvenance.IsFresh(path, tile.Entry, _catalog, out _);
            tile.Texture = TryLoad(path);
            tile.Thumbnail.texture = tile.Texture != null ? tile.Texture : Texture2D.whiteTexture;
            tile.Thumbnail.color = tile.Texture != null ? Color.white : PanelBrown;
            SetBadge(tile, fresh ? "fresh" : "stale", fresh ? Success : Warning);
        }

        // Why: invalidation must remove only writable cache candidates so packaged content is never touched.
        private void Invalidate(ManifestEntry entry)
        {
            var paths = WritablePaths(entry);
            for (int i = 0; i < paths.Length; i++)
            {
                TryDelete(paths[i]);
                TryDelete(paths[i] + ".promptmeta");
            }
            Debug.Log("[Options] invalidated " + entry.Id + " for regeneration");
        }

        private string ResolveExistingPath(ManifestEntry entry)
        {
            var paths = LookupPaths(entry);
            for (int i = 0; i < paths.Length; i++) if (File.Exists(paths[i])) return paths[i];
            return string.Empty;
        }

        private string PreferredAssetPath(ManifestEntry entry) => Application.isEditor ? WritablePaths(entry)[0] : WritablePaths(entry)[1];
        private string[] LookupPaths(ManifestEntry entry) => new[] { EditorAssetPath(entry), PersistentAssetPath(entry), StreamingAssetPath(entry) };
        private string[] WritablePaths(ManifestEntry entry) => new[] { EditorAssetPath(entry), PersistentAssetPath(entry) };
        private static string EditorAssetPath(ManifestEntry entry) => Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath, "Assets", "Generated", "Core", entry.Id + ".png");
        private static string PersistentAssetPath(ManifestEntry entry) => Path.Combine(Application.persistentDataPath, "Generated", "Core", entry.Id + ".png");
        private static string StreamingAssetPath(ManifestEntry entry) => Path.Combine(Application.streamingAssetsPath, "Generated", "Core", entry.Id + ".png");
    }
}
